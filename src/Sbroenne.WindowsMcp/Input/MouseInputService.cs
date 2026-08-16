using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Implementation of mouse input operations using Windows SendInput API.
/// </summary>
public sealed class MouseInputService
{
    private readonly ModifierKeyManager _modifierKeyManager = new();
    /// <inheritdoc />
    public Task<MouseControlResult> MoveAsync(int x, int y, CancellationToken cancellationToken = default) =>
        MoveCoreAsync(x, y, extraFlags: 0, cancellationToken);

    /// <summary>
    /// Moves the cursor as part of a held-button stroke. Sets <c>MOUSEEVENTF_MOVE_NOCOALESCE</c> so Windows
    /// delivers this vertex to the target even when the target thread is busy; a plain move may be coalesced
    /// with the next one, which would cut across the corner between them.
    /// </summary>
    private Task<MouseControlResult> MoveForStrokeAsync(int x, int y, CancellationToken cancellationToken) =>
        MoveCoreAsync(x, y, NativeConstants.MOUSEEVENTF_MOVE_NOCOALESCE, cancellationToken);

    private static Task<MouseControlResult> MoveCoreAsync(int x, int y, uint extraFlags, CancellationToken cancellationToken)
    {
        // Validate coordinates against virtual screen bounds
        var (isValid, screenBounds) = CoordinateNormalizer.ValidateCoordinates(x, y);

        if (!isValid)
        {
            return Task.FromResult(MouseControlResult.CreateFailure(
                MouseControlErrorCode.CoordinatesOutOfBounds,
                $"Coordinates ({x}, {y}) are out of bounds. Valid bounds: Left={screenBounds.Left}, Top={screenBounds.Top}, Right={screenBounds.Right}, Bottom={screenBounds.Bottom}",
                screenBounds));
        }

        // Normalize coordinates for SendInput
        var (normalizedX, normalizedY) = CoordinateNormalizer.Normalize(x, y, screenBounds);

        // Build the INPUT structure for mouse movement
        var input = new INPUT
        {
            Type = NativeConstants.INPUT_MOUSE,
            Data = new INPUTUNION
            {
                Mouse = new MOUSEINPUT
                {
                    Dx = normalizedX,
                    Dy = normalizedY,
                    MouseData = 0,
                    DwFlags = NativeConstants.MOUSEEVENTF_MOVE | NativeConstants.MOUSEEVENTF_ABSOLUTE | NativeConstants.MOUSEEVENTF_VIRTUALDESK | extraFlags,
                    Time = 0,
                    DwExtraInfo = 0,
                },
            },
        };

        // Send the input
        var inputSpan = new INPUT[] { input };
        var result = NativeMethods.SendInput(1, inputSpan, INPUT.Size);

        if (result != 1)
        {
            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(MouseControlResult.CreateFailure(
                errorCode,
                errorMessage,
                screenBounds));
        }

        // Get the final cursor position to confirm movement
        NativeMethods.GetCursorPos(out var finalPos);
        var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

        return Task.FromResult(MouseControlResult.CreateSuccess(finalPosition, screenBounds));
    }

    /// <inheritdoc />
    public Task<MouseControlResult> ClickAsync(int? x, int? y, ModifierKey modifiers = ModifierKey.None, CancellationToken cancellationToken = default)
    {
        ScreenBounds? screenBounds = null;

        // If coordinates are provided, validate and move to them first
        if (x.HasValue && y.HasValue)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(x.Value, y.Value);
            screenBounds = bounds;

            if (!isValid)
            {
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Coordinates ({x.Value}, {y.Value}) are out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds));
            }

            // Move to the coordinates first
            var moveResult = MoveAsync(x.Value, y.Value, cancellationToken).GetAwaiter().GetResult();
            if (!moveResult.Success)
            {
                return Task.FromResult(moveResult);
            }
        }

        // Get the current screen bounds if not already retrieved
        screenBounds ??= CoordinateNormalizer.GetVirtualScreenBounds();

        // Get the target window info under the cursor before clicking
        NativeMethods.GetCursorPos(out var currentPos);
        var targetWindowInfo = GetTargetWindowInfoAtPoint(currentPos.X, currentPos.Y);

        // Press modifier keys if specified (only those not already pressed by user)
        IReadOnlyList<int> pressedModifiers = [];
        try
        {
            pressedModifiers = _modifierKeyManager.PressModifiers(modifiers);

            // Build the INPUT structures for mouse down and mouse up
            var inputs = new INPUT[]
            {
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTDOWN,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTUP,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
            };

            // Send the input
            var result = NativeMethods.SendInput(2, inputs, INPUT.Size);

            if (result != 2)
            {
                var error = Marshal.GetLastWin32Error();
                var (errorCode, errorMessage) = MapSendInputError(error);
                return Task.FromResult(MouseControlResult.CreateFailure(
                    errorCode,
                    errorMessage,
                    screenBounds));
            }

            // Get the final cursor position
            NativeMethods.GetCursorPos(out var finalPos);
            var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

            var successResult = MouseControlResult.CreateSuccess(finalPosition, screenBounds);
            return Task.FromResult(successResult with { TargetWindow = targetWindowInfo });
        }
        finally
        {
            // Always release modifiers that we pressed, even on failure
            _modifierKeyManager.ReleaseModifiers(pressedModifiers);
        }
    }

    /// <summary>
    /// Gets the title of the window at the specified screen coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The window title, or null if no window or no title.</returns>
    /// <remarks>
    /// This method uses GetAncestor with GA_ROOT to retrieve the top-level window title.
    /// Without this, WindowFromPoint may return child windows like "Chrome Legacy Window"
    /// for Electron-based apps (VS Code, Slack, etc.) instead of the actual application window.
    /// </remarks>
    internal static string? GetWindowTitleAtPoint(int x, int y)
    {
        var point = new POINT(x, y);
        var hwnd = NativeMethods.WindowFromPoint(point);

        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        // Get the top-level window (root ancestor) to avoid returning child window names
        // like "Chrome Legacy Window" for Electron apps (VS Code, Teams, Slack, etc.)
        var rootHwnd = NativeMethods.GetAncestor(hwnd, NativeConstants.GA_ROOT);
        if (rootHwnd != IntPtr.Zero)
        {
            hwnd = rootHwnd;
        }

        const int maxTitleLength = 256;
        var titleBuffer = new char[maxTitleLength];
        var length = NativeMethods.GetWindowText(hwnd, titleBuffer, maxTitleLength);

        if (length <= 0)
        {
            return null;
        }

        return new string(titleBuffer, 0, length);
    }

    /// <summary>
    /// Gets target window information at the specified screen coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The target window info, or null if no window found.</returns>
    internal static TargetWindowInfo? GetTargetWindowInfoAtPoint(int x, int y)
    {
        var point = new POINT(x, y);
        var hwnd = NativeMethods.WindowFromPoint(point);

        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        // Get the top-level window (root ancestor) to avoid returning child window info
        // like "Chrome Legacy Window" for Electron apps (VS Code, Teams, Slack, etc.)
        var rootHwnd = NativeMethods.GetAncestor(hwnd, NativeConstants.GA_ROOT);
        if (rootHwnd != IntPtr.Zero)
        {
            hwnd = rootHwnd;
        }

        // Get window title
        const int maxTitleLength = 256;
        var titleBuffer = new char[maxTitleLength];
        var length = NativeMethods.GetWindowText(hwnd, titleBuffer, maxTitleLength);
        var title = length > 0 ? new string(titleBuffer, 0, length) : string.Empty;

        // Get process info
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        var processName = string.Empty;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
        }
        catch
        {
            // Process may have exited or we don't have access
        }

        return TargetWindowInfo.Create(hwnd, title, processName, (int)processId);
    }

    /// <inheritdoc />
    public Task<MouseControlResult> DoubleClickAsync(int? x, int? y, ModifierKey modifiers = ModifierKey.None, CancellationToken cancellationToken = default)
    {
        ScreenBounds? screenBounds = null;

        // If coordinates are provided, validate and move to them first
        if (x.HasValue && y.HasValue)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(x.Value, y.Value);
            screenBounds = bounds;

            if (!isValid)
            {
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Coordinates ({x.Value}, {y.Value}) are out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds));
            }

            // Move to the coordinates first
            var moveResult = MoveAsync(x.Value, y.Value, cancellationToken).GetAwaiter().GetResult();
            if (!moveResult.Success)
            {
                return Task.FromResult(moveResult);
            }
        }

        // Get the current screen bounds if not already retrieved
        screenBounds ??= CoordinateNormalizer.GetVirtualScreenBounds();

        // Get the target window info under the cursor before clicking
        NativeMethods.GetCursorPos(out var currentPos);
        var targetWindowInfo = GetTargetWindowInfoAtPoint(currentPos.X, currentPos.Y);

        // Press modifier keys before the double-click
        var pressedModifiers = _modifierKeyManager.PressModifiers(modifiers);

        try
        {
            // Build the INPUT structures for double-click (4 events: down, up, down, up)
            // Windows recognizes a double-click when two clicks occur within GetDoubleClickTime() milliseconds
            // at the same location. Since we're sending all inputs at once via SendInput, they will be
            // processed immediately in sequence, which is well within the double-click time window.
            var inputs = new INPUT[]
            {
                // First click down
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTDOWN,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
                // First click up
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTUP,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
                // Second click down
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTDOWN,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
                // Second click up
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_LEFTUP,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
            };

            // Send the input
            var result = NativeMethods.SendInput(4, inputs, INPUT.Size);

            if (result != 4)
            {
                var error = Marshal.GetLastWin32Error();
                var (errorCode, errorMessage) = MapSendInputError(error);
                return Task.FromResult(MouseControlResult.CreateFailure(
                    errorCode,
                    errorMessage,
                    screenBounds));
            }

            // Get the final cursor position
            NativeMethods.GetCursorPos(out var finalPos);
            var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

            var successResult = MouseControlResult.CreateSuccess(finalPosition, screenBounds);
            return Task.FromResult(successResult with { TargetWindow = targetWindowInfo });
        }
        finally
        {
            // Always release modifiers that we pressed, even on failure
            _modifierKeyManager.ReleaseModifiers(pressedModifiers);
        }
    }

    /// <inheritdoc />
    public Task<MouseControlResult> RightClickAsync(int? x, int? y, ModifierKey modifiers = ModifierKey.None, CancellationToken cancellationToken = default)
    {
        ScreenBounds? screenBounds = null;

        // If coordinates are provided, validate and move to them first
        if (x.HasValue && y.HasValue)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(x.Value, y.Value);
            screenBounds = bounds;

            if (!isValid)
            {
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Coordinates ({x.Value}, {y.Value}) are out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds));
            }

            // Move to the coordinates first
            var moveResult = MoveAsync(x.Value, y.Value, cancellationToken).GetAwaiter().GetResult();
            if (!moveResult.Success)
            {
                return Task.FromResult(moveResult);
            }
        }

        // Get the current screen bounds if not already retrieved
        screenBounds ??= CoordinateNormalizer.GetVirtualScreenBounds();

        // Get the target window info under the cursor before clicking
        NativeMethods.GetCursorPos(out var currentPos);
        var targetWindowInfo = GetTargetWindowInfoAtPoint(currentPos.X, currentPos.Y);

        // Press modifier keys before the click
        var pressedModifiers = _modifierKeyManager.PressModifiers(modifiers);

        try
        {
            // Build the INPUT structures for right mouse down and right mouse up
            var inputs = new INPUT[]
            {
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_RIGHTDOWN,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = NativeConstants.MOUSEEVENTF_RIGHTUP,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
            };

            // Send the input
            var result = NativeMethods.SendInput(2, inputs, INPUT.Size);

            if (result != 2)
            {
                var error = Marshal.GetLastWin32Error();
                var (errorCode, errorMessage) = MapSendInputError(error);
                return Task.FromResult(MouseControlResult.CreateFailure(
                    errorCode,
                    errorMessage,
                    screenBounds));
            }

            // Get the final cursor position
            NativeMethods.GetCursorPos(out var finalPos);
            var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

            var successResult = MouseControlResult.CreateSuccess(finalPosition, screenBounds);
            return Task.FromResult(successResult with { TargetWindow = targetWindowInfo });
        }
        finally
        {
            // Always release modifiers that we pressed, even on failure
            _modifierKeyManager.ReleaseModifiers(pressedModifiers);
        }
    }

    /// <inheritdoc />
    public Task<MouseControlResult> MiddleClickAsync(int? x, int? y, CancellationToken cancellationToken = default)
    {
        ScreenBounds? screenBounds = null;

        // If coordinates are provided, validate and move to them first
        if (x.HasValue && y.HasValue)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(x.Value, y.Value);
            screenBounds = bounds;

            if (!isValid)
            {
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Coordinates ({x.Value}, {y.Value}) are out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds));
            }

            // Move to the coordinates first
            var moveResult = MoveAsync(x.Value, y.Value, cancellationToken).GetAwaiter().GetResult();
            if (!moveResult.Success)
            {
                return Task.FromResult(moveResult);
            }
        }

        // Get the current screen bounds if not already retrieved
        screenBounds ??= CoordinateNormalizer.GetVirtualScreenBounds();

        // Get the target window info under the cursor before clicking
        NativeMethods.GetCursorPos(out var currentPos);
        var targetWindowInfo = GetTargetWindowInfoAtPoint(currentPos.X, currentPos.Y);

        // Build the INPUT structures for middle mouse down and middle mouse up
        var inputs = new INPUT[]
        {
            new INPUT
            {
                Type = NativeConstants.INPUT_MOUSE,
                Data = new INPUTUNION
                {
                    Mouse = new MOUSEINPUT
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        DwFlags = NativeConstants.MOUSEEVENTF_MIDDLEDOWN,
                        Time = 0,
                        DwExtraInfo = 0,
                    },
                },
            },
            new INPUT
            {
                Type = NativeConstants.INPUT_MOUSE,
                Data = new INPUTUNION
                {
                    Mouse = new MOUSEINPUT
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        DwFlags = NativeConstants.MOUSEEVENTF_MIDDLEUP,
                        Time = 0,
                        DwExtraInfo = 0,
                    },
                },
            },
        };

        // Send the input
        var result = NativeMethods.SendInput(2, inputs, INPUT.Size);

        if (result != 2)
        {
            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(MouseControlResult.CreateFailure(
                errorCode,
                errorMessage,
                screenBounds));
        }

        // Get the final cursor position
        NativeMethods.GetCursorPos(out var finalPos);
        var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

        var successResult = MouseControlResult.CreateSuccess(finalPosition, screenBounds);
        return Task.FromResult(successResult with { TargetWindow = targetWindowInfo });
    }

    /// <inheritdoc />
    public Task<MouseControlResult> DragAsync(int startX, int startY, int endX, int endY, MouseButton button = MouseButton.Left, CancellationToken cancellationToken = default) =>
        StrokeAsync([new Coordinates(startX, startY), new Coordinates(endX, endY)], button, ModifierKey.None, cancellationToken);

    /// <summary>
    /// Presses a mouse button at the first point, moves through every remaining point, and releases at the last -
    /// one continuous stroke with no intermediate pen lifts.
    /// </summary>
    /// <param name="points">Absolute screen coordinates to trace, in order. At least two are required.</param>
    /// <param name="button">The mouse button to hold down for the duration of the stroke.</param>
    /// <param name="modifiers">Modifier keys to hold while stroking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result whose final position is the last point, with target window info sampled at the first point.</returns>
    /// <remarks>
    /// <see cref="DragAsync"/> is the two-point case of this method, so drag and stroke can never diverge.
    /// Every move after the press is sent with <c>MOUSEEVENTF_MOVE_NOCOALESCE</c> so no vertex is dropped when the
    /// target thread is busy. The button is always released in a <c>finally</c> block, even when a move fails partway through.
    /// </remarks>
    public async Task<MouseControlResult> StrokeAsync(
        IReadOnlyList<Coordinates> points,
        MouseButton button = MouseButton.Left,
        ModifierKey modifiers = ModifierKey.None,
        CancellationToken cancellationToken = default)
    {
        if (points is null || points.Count < 2)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                $"A stroke requires at least 2 points; got {points?.Count ?? 0}.");
        }

        // Validate every point up front so a bad vertex fails before any button is pressed
        ScreenBounds screenBounds = default;
        for (var i = 0; i < points.Count; i++)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(points[i].X, points[i].Y);
            if (!isValid)
            {
                return MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Point {i} ({points[i].X}, {points[i].Y}) is out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds);
            }

            screenBounds = bounds;
        }

        // Step 1: Move to the first point
        var moveToStartResult = await MoveAsync(points[0].X, points[0].Y, cancellationToken);
        if (!moveToStartResult.Success)
        {
            return moveToStartResult;
        }

        // Get the target window info at the start position
        var targetWindowInfo = GetTargetWindowInfoAtPoint(points[0].X, points[0].Y);

        // Determine which button down/up flags to use
        uint buttonDownFlag;
        uint buttonUpFlag;
        switch (button)
        {
            case MouseButton.Right:
                buttonDownFlag = NativeConstants.MOUSEEVENTF_RIGHTDOWN;
                buttonUpFlag = NativeConstants.MOUSEEVENTF_RIGHTUP;
                break;
            case MouseButton.Middle:
                buttonDownFlag = NativeConstants.MOUSEEVENTF_MIDDLEDOWN;
                buttonUpFlag = NativeConstants.MOUSEEVENTF_MIDDLEUP;
                break;
            default: // MouseButton.Left
                buttonDownFlag = NativeConstants.MOUSEEVENTF_LEFTDOWN;
                buttonUpFlag = NativeConstants.MOUSEEVENTF_LEFTUP;
                break;
        }

        var pressedModifiers = _modifierKeyManager.PressModifiers(modifiers);

        try
        {
            // Step 2: Press the mouse button down
            var buttonDownInput = new INPUT[]
            {
                new INPUT
                {
                    Type = NativeConstants.INPUT_MOUSE,
                    Data = new INPUTUNION
                    {
                        Mouse = new MOUSEINPUT
                        {
                            Dx = 0,
                            Dy = 0,
                            MouseData = 0,
                            DwFlags = buttonDownFlag,
                            Time = 0,
                            DwExtraInfo = 0,
                        },
                    },
                },
            };

            var buttonDownResult = NativeMethods.SendInput(1, buttonDownInput, INPUT.Size);
            if (buttonDownResult != 1)
            {
                var error = Marshal.GetLastWin32Error();
                var (errorCode, errorMessage) = MapSendInputError(error);
                return MouseControlResult.CreateFailure(
                    errorCode,
                    $"SendInput failed for button down: {errorMessage}",
                    screenBounds);
            }

            try
            {
                // Step 3: Trace through the remaining points with the button held
                for (var i = 1; i < points.Count; i++)
                {
                    var moveResult = await MoveForStrokeAsync(points[i].X, points[i].Y, cancellationToken);
                    if (!moveResult.Success)
                    {
                        // Even if a move fails, the button is released in finally
                        return moveResult;
                    }
                }

                // Get the final cursor position
                NativeMethods.GetCursorPos(out var finalPos);
                var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

                var successResult = MouseControlResult.CreateSuccess(finalPosition, screenBounds);
                return successResult with { TargetWindow = targetWindowInfo };
            }
            finally
            {
                // Step 4: Always release the mouse button, even on failure
                var buttonUpInput = new INPUT[]
                {
                    new INPUT
                    {
                        Type = NativeConstants.INPUT_MOUSE,
                        Data = new INPUTUNION
                        {
                            Mouse = new MOUSEINPUT
                            {
                                Dx = 0,
                                Dy = 0,
                                MouseData = 0,
                                DwFlags = buttonUpFlag,
                                Time = 0,
                                DwExtraInfo = 0,
                            },
                        },
                    },
                };

                // Send button up - ignore result since we're in finally block
                _ = NativeMethods.SendInput(1, buttonUpInput, INPUT.Size);
            }
        }
        finally
        {
            // Always release modifiers that we pressed, even on failure
            _modifierKeyManager.ReleaseModifiers(pressedModifiers);
        }
    }

    /// <inheritdoc />
    public Task<MouseControlResult> ScrollAsync(ScrollDirection direction, int amount, int? x, int? y, CancellationToken cancellationToken = default)
    {
        ScreenBounds? screenBounds = null;

        // If coordinates are provided, validate and move to them first
        if (x.HasValue && y.HasValue)
        {
            var (isValid, bounds) = CoordinateNormalizer.ValidateCoordinates(x.Value, y.Value);
            screenBounds = bounds;

            if (!isValid)
            {
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.CoordinatesOutOfBounds,
                    $"Coordinates ({x.Value}, {y.Value}) are out of bounds. Valid bounds: Left={bounds.Left}, Top={bounds.Top}, Right={bounds.Right}, Bottom={bounds.Bottom}",
                    bounds));
            }

            // Move to the coordinates first
            var moveResult = MoveAsync(x.Value, y.Value, cancellationToken).GetAwaiter().GetResult();
            if (!moveResult.Success)
            {
                return Task.FromResult(moveResult);
            }
        }

        // Get the current screen bounds if not already retrieved
        screenBounds ??= CoordinateNormalizer.GetVirtualScreenBounds();

        // Get the window title under the cursor before scrolling
        NativeMethods.GetCursorPos(out var currentPos);

        // If amount is 0, we can just return success without sending any input
        if (amount == 0)
        {
            var zeroPosition = new Coordinates(currentPos.X, currentPos.Y);
            return Task.FromResult(MouseControlResult.CreateSuccess(zeroPosition, screenBounds));
        }

        // Determine the event flag and wheel delta based on direction
        // WHEEL_DELTA is 120. Positive values scroll up/right, negative scroll down/left.
        uint eventFlag;
        int wheelDelta;

        switch (direction)
        {
            case ScrollDirection.Up:
                eventFlag = NativeConstants.MOUSEEVENTF_WHEEL;
                wheelDelta = NativeConstants.WHEEL_DELTA * amount;
                break;
            case ScrollDirection.Down:
                eventFlag = NativeConstants.MOUSEEVENTF_WHEEL;
                wheelDelta = -NativeConstants.WHEEL_DELTA * amount;
                break;
            case ScrollDirection.Left:
                eventFlag = NativeConstants.MOUSEEVENTF_HWHEEL;
                wheelDelta = -NativeConstants.WHEEL_DELTA * amount;
                break;
            case ScrollDirection.Right:
                eventFlag = NativeConstants.MOUSEEVENTF_HWHEEL;
                wheelDelta = NativeConstants.WHEEL_DELTA * amount;
                break;
            default:
                return Task.FromResult(MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidScrollDirection,
                    $"Unknown scroll direction: {direction}",
                    screenBounds));
        }

        // Build the INPUT structure for the scroll
        var inputs = new INPUT[]
        {
            new INPUT
            {
                Type = NativeConstants.INPUT_MOUSE,
                Data = new INPUTUNION
                {
                    Mouse = new MOUSEINPUT
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = wheelDelta,
                        DwFlags = eventFlag,
                        Time = 0,
                        DwExtraInfo = 0,
                    },
                },
            },
        };

        // Send the input
        var result = NativeMethods.SendInput(1, inputs, INPUT.Size);

        if (result != 1)
        {
            var error = Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(MouseControlResult.CreateFailure(
                errorCode,
                errorMessage,
                screenBounds));
        }

        // Get the final cursor position
        NativeMethods.GetCursorPos(out var finalPos);
        var finalPosition = new Coordinates(finalPos.X, finalPos.Y);

        return Task.FromResult(MouseControlResult.CreateSuccess(finalPosition, screenBounds));
    }

    /// <summary>
    /// Maps a Win32 error code from SendInput to an appropriate MouseControlErrorCode.
    /// </summary>
    /// <param name="win32ErrorCode">The Win32 error code from GetLastError.</param>
    /// <returns>A tuple of the appropriate error code and error message.</returns>
    private static (MouseControlErrorCode ErrorCode, string Message) MapSendInputError(int win32ErrorCode)
    {
        return win32ErrorCode switch
        {
            NativeConstants.ERROR_ACCESS_DENIED => (
                MouseControlErrorCode.ElevatedProcessTarget,
                "SendInput was blocked. The target window may belong to an elevated (admin) process or input is blocked by system state."),
            _ => (
                MouseControlErrorCode.SendInputFailed,
                $"SendInput failed with error code {win32ErrorCode}")
        };
    }
}