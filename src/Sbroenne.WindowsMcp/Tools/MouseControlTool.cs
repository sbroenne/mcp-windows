using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling the mouse cursor on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public static partial class MouseControlTool
{
    private static readonly string[] ValidTargets = ["primary_screen", "secondary_screen"];

    /// <summary>
    /// Low-level mouse input for canvas/drawing. AVOID for buttons/controls - use ui_automation(click) instead.
    /// BEFORE USING: Get coordinates from ui_automation(find) bounding rects OR screenshot_control(annotate=true). Never guess positions.
    /// USE FOR: drag operations, canvas drawing, custom controls without UIA.
    /// DRAG: Use x,y for START and endX,endY for END position (NOT startX/startY).
    /// Actions: move, click, double_click, right_click, middle_click, drag, scroll, get_position.
    /// </summary>
    /// <remarks>
    /// <para><strong>MONITOR TARGETING:</strong></para>
    /// <list type="bullet">
    /// <item>'primary_screen': Target the main display (with taskbar). Most common choice.</item>
    /// <item>'secondary_screen': Target the other monitor. Only works with exactly 2 monitors.</item>
    /// <item>monitorIndex: For 3+ monitors, use screenshot_control action='list_monitors' first to find the index.</item>
    /// </list>
    /// <para><strong>COORDINATES:</strong> All x/y coordinates are relative to the specified monitor.</para>
    /// <para><strong>MONITOR CONTEXT:</strong> Successful operations with explicit coordinates return monitor_index, monitor_width, and monitor_height in the response.</para>
    /// <para><strong>QUERY POSITION:</strong> Use action='get_position' to query current cursor position with monitor context.</para>
    /// </remarks>
    /// <param name="action">The mouse action to perform: move, click, double_click, right_click, middle_click, drag, scroll, or get_position.</param>
    /// <param name="target">Monitor target: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor in 2-monitor setups). For 3+ monitors, use monitorIndex instead.</param>
    /// <param name="x">X-coordinate relative to the monitor's left edge (required for move, optional for clicks).</param>
    /// <param name="y">Y-coordinate relative to the monitor's top edge (required for move, optional for clicks).</param>
    /// <param name="endX">End x-coordinate relative to the monitor (required for drag action).</param>
    /// <param name="endY">End y-coordinate relative to the monitor (required for drag action).</param>
    /// <param name="direction">Scroll direction: up, down, left, or right (required for scroll action).</param>
    /// <param name="amount">Number of scroll clicks (default: 1).</param>
    /// <param name="modifiers">Modifier keys to hold during action: ctrl, shift, alt (comma-separated).</param>
    /// <param name="button">Mouse button for drag: left, right, or middle (default: left).</param>
    /// <param name="monitorIndex">Monitor index (0-based). Alternative to target for 3+ monitor setups. Use screenshot_control action='list_monitors' to find indices.</param>
    /// <param name="expectedWindowTitle">Expected window title (partial match). If specified, operation fails if foreground window title doesn't match.</param>
    /// <param name="expectedProcessName">Expected process name. If specified, operation fails if foreground window's process doesn't match.</param>
    /// <param name="windowHandle">Window handle for window-relative coordinates. When provided, x/y are relative to the window's top-left corner.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result includes success status, cursor position, monitor context, and 'target_window' for click actions.</returns>
    [McpServerTool(Name = "mouse_control", Title = "Mouse Control", Destructive = true, OpenWorld = false)]
    public static async Task<string> ExecuteAsync(
        MouseAction action,
        [DefaultValue(null)] string? target = null,
        [DefaultValue(null)] int? x = null,
        [DefaultValue(null)] int? y = null,
        [DefaultValue(null)] int? endX = null,
        [DefaultValue(null)] int? endY = null,
        [DefaultValue(null)] string? direction = null,
        [DefaultValue(1)] int amount = 1,
        [DefaultValue(null)] string? modifiers = null,
        [DefaultValue(null)] string? button = null,
        [DefaultValue(null)] int? monitorIndex = null,
        [DefaultValue(null)] string? expectedWindowTitle = null,
        [DefaultValue(null)] string? expectedProcessName = null,
        [DefaultValue(null)] string? windowHandle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a linked token source with the configured timeout
            using var timeoutCts = new CancellationTokenSource(WindowsToolsBase.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var linkedToken = linkedCts.Token;

            // Pre-flight check: verify target window if expected values are specified
            if (!string.IsNullOrEmpty(expectedWindowTitle) || !string.IsNullOrEmpty(expectedProcessName))
            {
                var targetCheckResult = await VerifyTargetWindowAsync(expectedWindowTitle, expectedProcessName, linkedToken);
                if (!targetCheckResult.Success)
                {
                    return JsonSerializer.Serialize(targetCheckResult, WindowsToolsBase.JsonOptions);
                }
            }

            // Check if coordinates are provided
            var hasCoordinates = (x.HasValue && y.HasValue) || (endX.HasValue && endY.HasValue);

            // Window-relative coordinate mode: if windowHandle is provided, coordinates are relative to window
            bool isWindowRelativeMode = !string.IsNullOrEmpty(windowHandle) && hasCoordinates;
            int? windowBasedMonitorIndex = null;
            int windowLeft = 0, windowTop = 0;

            if (isWindowRelativeMode)
            {
                if (!WindowHandleParser.TryParse(windowHandle, out nint parsedWindowHandle) || parsedWindowHandle == nint.Zero)
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidCoordinates,
                        $"Invalid windowHandle: '{windowHandle}'. Expected decimal string from window_management.");
                    return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                }

                // Get window rect
                if (!NativeMethods.GetWindowRect(parsedWindowHandle, out var windowRect))
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidCoordinates,
                        $"Could not get window position for handle {windowHandle}. The window may no longer exist.");
                    return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                }

                windowLeft = windowRect.Left;
                windowTop = windowRect.Top;

                // Determine which monitor contains this window's center
                var windowCenterX = windowRect.Left + (windowRect.Right - windowRect.Left) / 2;
                var windowCenterY = windowRect.Top + (windowRect.Bottom - windowRect.Top) / 2;
                var monitors = WindowsToolsBase.MonitorService.GetMonitors();

                for (int i = 0; i < monitors.Count; i++)
                {
                    var mon = monitors[i];
                    if (windowCenterX >= mon.X && windowCenterX < mon.X + mon.Width &&
                        windowCenterY >= mon.Y && windowCenterY < mon.Y + mon.Height)
                    {
                        windowBasedMonitorIndex = i;
                        break;
                    }
                }

                // Default to primary monitor if not found
                windowBasedMonitorIndex ??= 0;
            }

            // Resolve target to monitorIndex if provided
            int? resolvedMonitorIndex = monitorIndex;
            if (!string.IsNullOrWhiteSpace(target))
            {
                var parsedTarget = ParseTarget(target);
                if (parsedTarget == null)
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidCoordinates,
                        $"Invalid target: '{target}'. Valid values are: 'primary_screen', 'secondary_screen'");
                    return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                }

                // Resolve target to monitor index
                MonitorInfo? targetMonitor = parsedTarget.Value switch
                {
                    MonitorTarget.PrimaryScreen => WindowsToolsBase.MonitorService.GetPrimaryMonitor(),
                    MonitorTarget.SecondaryScreen => WindowsToolsBase.MonitorService.GetSecondaryMonitor(),
                    _ => null
                };

                if (targetMonitor == null)
                {
                    var errorMessage = parsedTarget.Value == MonitorTarget.SecondaryScreen
                        ? "Cannot use 'secondary_screen' target: requires exactly 2 monitors. Use 'monitorIndex' for 3+ monitor setups."
                        : $"Could not resolve target '{target}' to a monitor";
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidCoordinates,
                        errorMessage);
                    return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                }

                // Find the index of this monitor
                var monitors = WindowsToolsBase.MonitorService.GetMonitors();
                for (int i = 0; i < monitors.Count; i++)
                {
                    if (monitors[i].X == targetMonitor.X && monitors[i].Y == targetMonitor.Y)
                    {
                        resolvedMonitorIndex = i;
                        break;
                    }
                }
            }

            // Require target, monitorIndex, or windowHandle when coordinates are provided
            if (hasCoordinates && !resolvedMonitorIndex.HasValue && !isWindowRelativeMode)
            {
                var availableIndices = Enumerable.Range(0, WindowsToolsBase.MonitorService.MonitorCount).ToList();
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.MissingRequiredParameter,
                    "Either 'target', 'monitorIndex', or 'windowHandle' is required when using x/y coordinates. Use target='primary_screen' or target='secondary_screen' for easy targeting, or windowHandle for window-relative coordinates.",
                    errorDetails: new Dictionary<string, object>
                    {
                        { "valid_targets", ValidTargets },
                        { "valid_indices", availableIndices }
                    });
                return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
            }

            // Use resolved monitorIndex, windowBasedMonitorIndex, or default to 0 for coordinate-less actions
            var targetMonitorIndex = resolvedMonitorIndex ?? windowBasedMonitorIndex ?? 0;

            // Validate monitorIndex is in valid range
            if (resolvedMonitorIndex.HasValue && (targetMonitorIndex < 0 || targetMonitorIndex >= WindowsToolsBase.MonitorService.MonitorCount))
            {
                var availableIndices = Enumerable.Range(0, WindowsToolsBase.MonitorService.MonitorCount).ToList();
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidCoordinates,
                    $"Invalid monitorIndex: {targetMonitorIndex}",
                    errorDetails: new Dictionary<string, object>
                    {
                        { "valid_indices", availableIndices },
                        { "provided_index", targetMonitorIndex }
                    });
                return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
            }

            // Translate coordinates to absolute screen coordinates
            int? absoluteX = x, absoluteY = y, absoluteEndX = endX, absoluteEndY = endY;
            var monitor = WindowsToolsBase.MonitorService.GetMonitor(targetMonitorIndex);
            if (monitor == null)
            {
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidCoordinates,
                    $"Invalid monitor index: {monitorIndex}. Available monitors: 0-{WindowsToolsBase.MonitorService.MonitorCount - 1}");
                return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
            }

            if (isWindowRelativeMode)
            {
                // Translate coordinates relative to window origin
                if (x.HasValue)
                {
                    absoluteX = windowLeft + x.Value;
                }

                if (y.HasValue)
                {
                    absoluteY = windowTop + y.Value;
                }

                if (endX.HasValue)
                {
                    absoluteEndX = windowLeft + endX.Value;
                }

                if (endY.HasValue)
                {
                    absoluteEndY = windowTop + endY.Value;
                }
            }
            else
            {
                // Translate coordinates relative to monitor origin
                if (x.HasValue)
                {
                    absoluteX = monitor.X + x.Value;
                }

                if (y.HasValue)
                {
                    absoluteY = monitor.Y + y.Value;
                }

                if (endX.HasValue)
                {
                    absoluteEndX = monitor.X + endX.Value;
                }

                if (endY.HasValue)
                {
                    absoluteEndY = monitor.Y + endY.Value;
                }
            }

            // Validate coordinates are within monitor bounds
            if (hasCoordinates && resolvedMonitorIndex.HasValue)
            {
                if (x.HasValue && y.HasValue)
                {
                    if (x.Value < 0 || x.Value >= monitor.Width || y.Value < 0 || y.Value >= monitor.Height)
                    {
                        var result = MouseControlResult.CreateFailure(
                            MouseControlErrorCode.CoordinatesOutOfBounds,
                            $"Coordinates ({x.Value}, {y.Value}) out of bounds for monitor {targetMonitorIndex}",
                            errorDetails: new Dictionary<string, object>
                            {
                                { "valid_bounds", new { left = monitor.X, top = monitor.Y, right = monitor.X + monitor.Width, bottom = monitor.Y + monitor.Height } },
                                { "provided_coordinates", new { x = x.Value, y = y.Value } }
                            });
                        return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                    }
                }

                if (endX.HasValue && endY.HasValue)
                {
                    if (endX.Value < 0 || endX.Value >= monitor.Width || endY.Value < 0 || endY.Value >= monitor.Height)
                    {
                        var result = MouseControlResult.CreateFailure(
                            MouseControlErrorCode.CoordinatesOutOfBounds,
                            $"End coordinates ({endX.Value}, {endY.Value}) out of bounds for monitor {targetMonitorIndex}",
                            errorDetails: new Dictionary<string, object>
                            {
                                { "valid_bounds", new { left = monitor.X, top = monitor.Y, right = monitor.X + monitor.Width, bottom = monitor.Y + monitor.Height } },
                                { "provided_coordinates", new { x = endX.Value, y = endY.Value } }
                            });
                        return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
                    }
                }
            }

            MouseControlResult operationResult;

            switch (action)
            {
                case MouseAction.Move:
                    operationResult = await HandleMoveAsync(absoluteX, absoluteY, linkedToken);
                    break;

                case MouseAction.Click:
                    operationResult = await HandleClickAsync(absoluteX, absoluteY, modifiers, linkedToken);
                    break;

                case MouseAction.DoubleClick:
                    operationResult = await HandleDoubleClickAsync(absoluteX, absoluteY, modifiers, linkedToken);
                    break;

                case MouseAction.RightClick:
                    operationResult = await HandleRightClickAsync(absoluteX, absoluteY, modifiers, linkedToken);
                    break;

                case MouseAction.MiddleClick:
                    operationResult = await HandleMiddleClickAsync(absoluteX, absoluteY, linkedToken);
                    break;

                case MouseAction.Drag:
                    operationResult = await HandleDragAsync(absoluteX, absoluteY, absoluteEndX, absoluteEndY, button, linkedToken);
                    break;

                case MouseAction.Scroll:
                    operationResult = await HandleScrollAsync(absoluteX, absoluteY, direction, amount, linkedToken);
                    break;

                case MouseAction.GetPosition:
                    operationResult = GetCurrentPosition();
                    break;

                default:
                    operationResult = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            if (operationResult.Success)
            {
                // Add monitor context and convert coordinates to monitor-relative for operations with explicit coordinates
                if (resolvedMonitorIndex.HasValue || isWindowRelativeMode)
                {
                    var monitorInfo = WindowsToolsBase.MonitorService.GetMonitor(targetMonitorIndex);
                    if (monitorInfo != null)
                    {
                        // Convert absolute cursor position to monitor-relative coordinates
                        int relativeX = operationResult.FinalPosition.X - monitorInfo.X;
                        int relativeY = operationResult.FinalPosition.Y - monitorInfo.Y;

                        operationResult = operationResult with
                        {
                            FinalPosition = new FinalPosition(relativeX, relativeY),
                            MonitorIndex = targetMonitorIndex,
                            MonitorWidth = monitorInfo.Width,
                            MonitorHeight = monitorInfo.Height
                        };
                    }
                }

                // Attach target window info for input operations (not get_position)
                if (action != MouseAction.GetPosition)
                {
                    operationResult = await AttachTargetWindowInfoAsync(operationResult, linkedToken);
                }
            }

            return JsonSerializer.Serialize(operationResult, WindowsToolsBase.JsonOptions);
        }
        catch (OperationCanceledException)
        {
            var errorResult = MouseControlResult.CreateFailure(
                MouseControlErrorCode.OperationTimeout,
                $"Operation timed out after {WindowsToolsBase.TimeoutMs}ms");
            return JsonSerializer.Serialize(errorResult, WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.SerializeToolError("mouse_control", ex);
        }
    }

    private static async Task<MouseControlResult> HandleMoveAsync(int? x, int? y, CancellationToken cancellationToken)
    {
        if (!x.HasValue || !y.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Move action requires both x and y coordinates");
        }

        return await WindowsToolsBase.MouseInputService.MoveAsync(x.Value, y.Value, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform click operation: secure desktop (UAC, lock screen) is active");
        }

        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        var modifierKeys = ParseModifiers(modifiersString);
        return await WindowsToolsBase.MouseInputService.ClickAsync(x, y, modifierKeys, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleDoubleClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform double-click operation: secure desktop (UAC, lock screen) is active");
        }

        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot double-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        var modifierKeys = ParseModifiers(modifiersString);
        return await WindowsToolsBase.MouseInputService.DoubleClickAsync(x, y, modifierKeys, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleRightClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform right-click operation: secure desktop (UAC, lock screen) is active");
        }

        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot right-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        var modifierKeys = ParseModifiers(modifiersString);
        return await WindowsToolsBase.MouseInputService.RightClickAsync(x, y, modifierKeys, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleMiddleClickAsync(int? x, int? y, CancellationToken cancellationToken)
    {
        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform middle-click operation: secure desktop (UAC, lock screen) is active");
        }

        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot middle-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        return await WindowsToolsBase.MouseInputService.MiddleClickAsync(x, y, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleDragAsync(int? startX, int? startY, int? endX, int? endY, string? buttonString, CancellationToken cancellationToken)
    {
        if (!startX.HasValue || !startY.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Drag requires x and y for START position (not startX/startY)");
        }

        if (!endX.HasValue || !endY.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Drag requires endX and endY for END position");
        }

        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform drag operation: secure desktop (UAC, lock screen) is active");
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(startX.Value, startY.Value))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot drag from elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        var mouseButton = ParseMouseButton(buttonString);
        return await WindowsToolsBase.MouseInputService.DragAsync(startX.Value, startY.Value, endX.Value, endY.Value, mouseButton, cancellationToken);
    }

    private static async Task<MouseControlResult> HandleScrollAsync(int? x, int? y, string? directionString, int amount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(directionString))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Scroll action requires a direction parameter (up, down, left, or right)");
        }

        var scrollDirection = ParseScrollDirection(directionString);
        if (!scrollDirection.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.InvalidScrollDirection,
                $"Invalid scroll direction: '{directionString}'. Valid directions are: up, down, left, right");
        }

        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform scroll operation: secure desktop (UAC, lock screen) is active");
        }

        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        if (WindowsToolsBase.ElevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot scroll in elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        return await WindowsToolsBase.MouseInputService.ScrollAsync(scrollDirection.Value, amount, x, y, cancellationToken);
    }

    private static MouseControlResult GetCurrentPosition()
    {
        NativeMethods.GetCursorPos(out var cursorPos);
        int absoluteX = cursorPos.X;
        int absoluteY = cursorPos.Y;

        var monitors = WindowsToolsBase.MonitorService.GetMonitors();
        MonitorInfo? targetMonitor = null;
        int? foundMonitorIndex = null;

        for (int i = 0; i < monitors.Count; i++)
        {
            var mon = monitors[i];
            if (absoluteX >= mon.X && absoluteX < mon.X + mon.Width &&
                absoluteY >= mon.Y && absoluteY < mon.Y + mon.Height)
            {
                targetMonitor = mon;
                foundMonitorIndex = i;
                break;
            }
        }

        if (targetMonitor == null || !foundMonitorIndex.HasValue)
        {
            targetMonitor = monitors[0];
            foundMonitorIndex = 0;
        }

        int relativeX = absoluteX - targetMonitor.X;
        int relativeY = absoluteY - targetMonitor.Y;

        var result = MouseControlResult.CreateSuccess(new Coordinates(relativeX, relativeY));
        return result with
        {
            MonitorIndex = foundMonitorIndex.Value,
            MonitorWidth = targetMonitor.Width,
            MonitorHeight = targetMonitor.Height
        };
    }

    private static async Task<MouseControlResult> AttachTargetWindowInfoAsync(MouseControlResult result, CancellationToken cancellationToken)
    {
        try
        {
            var foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return result;
            }

            var windowInfo = await WindowsToolsBase.WindowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return result;
            }

            return result with
            {
                TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo)
            };
        }
        catch
        {
            return result;
        }
    }

    private static async Task<MouseControlResult> VerifyTargetWindowAsync(string? expectedTitle, string? expectedProcessName, CancellationToken cancellationToken)
    {
        try
        {
            var foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return MouseControlResult.CreateFailure(
                    MouseControlErrorCode.WrongTargetWindow,
                    "No foreground window found. Cannot verify target window.");
            }

            var windowInfo = await WindowsToolsBase.WindowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return MouseControlResult.CreateFailure(
                    MouseControlErrorCode.WrongTargetWindow,
                    "Could not retrieve foreground window information.");
            }

            if (!string.IsNullOrEmpty(expectedTitle))
            {
                if (string.IsNullOrEmpty(windowInfo.Title) ||
                    !windowInfo.Title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    var checkResult = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.WrongTargetWindow,
                        $"Foreground window title '{windowInfo.Title}' does not contain expected text '{expectedTitle}'. Aborting to prevent click in wrong window.");
                    return checkResult with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            if (!string.IsNullOrEmpty(expectedProcessName))
            {
                if (string.IsNullOrEmpty(windowInfo.ProcessName) ||
                    !windowInfo.ProcessName.Equals(expectedProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    var checkResult = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.WrongTargetWindow,
                        $"Foreground window process '{windowInfo.ProcessName}' does not match expected process '{expectedProcessName}'. Aborting to prevent click in wrong window.");
                    return checkResult with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            return MouseControlResult.CreateSuccess(new Coordinates(0, 0));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.WrongTargetWindow,
                $"Failed to verify target window: {ex.Message}");
        }
    }

    private static MouseButton ParseMouseButton(string? buttonString)
    {
        if (string.IsNullOrWhiteSpace(buttonString))
        {
            return MouseButton.Left;
        }

        return buttonString.ToLowerInvariant() switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left,
        };
    }

    private static ScrollDirection? ParseScrollDirection(string? directionString)
    {
        if (string.IsNullOrWhiteSpace(directionString))
        {
            return null;
        }

        return directionString.ToLowerInvariant() switch
        {
            "up" => ScrollDirection.Up,
            "down" => ScrollDirection.Down,
            "left" => ScrollDirection.Left,
            "right" => ScrollDirection.Right,
            _ => null,
        };
    }

    private static ModifierKey ParseModifiers(string? modifiersString)
    {
        if (string.IsNullOrWhiteSpace(modifiersString))
        {
            return ModifierKey.None;
        }

        var modifierKeys = ModifierKey.None;
        var parts = modifiersString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            modifierKeys |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                "alt" => ModifierKey.Alt,
                _ => ModifierKey.None,
            };
        }

        return modifierKeys;
    }

    private static MonitorTarget? ParseTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        return target.ToLowerInvariant() switch
        {
            "primary_screen" or "primaryscreen" or "primary" => MonitorTarget.PrimaryScreen,
            "secondary_screen" or "secondaryscreen" or "secondary" => MonitorTarget.SecondaryScreen,
            _ => null
        };
    }

    private enum MonitorTarget
    {
        PrimaryScreen,
        SecondaryScreen
    }
}
