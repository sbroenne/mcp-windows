using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling the mouse cursor on Windows.
/// </summary>
[McpServerToolType]
public sealed partial class MouseControlTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IMouseInputService _mouseInputService;
    private readonly IMonitorService _monitorService;
    private readonly IElevationDetector _elevationDetector;
    private readonly ISecureDesktopDetector _secureDesktopDetector;
    private readonly MouseOperationLogger _logger;
    private readonly MouseConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseControlTool"/> class.
    /// </summary>
    /// <param name="mouseInputService">The mouse input service.</param>
    /// <param name="monitorService">The monitor service.</param>
    /// <param name="elevationDetector">The elevation detector.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="logger">The operation logger.</param>
    /// <param name="configuration">The mouse configuration.</param>
    public MouseControlTool(
        IMouseInputService mouseInputService,
        IMonitorService monitorService,
        IElevationDetector elevationDetector,
        ISecureDesktopDetector secureDesktopDetector,
        MouseOperationLogger logger,
        MouseConfiguration configuration)
    {
        _mouseInputService = mouseInputService ?? throw new ArgumentNullException(nameof(mouseInputService));
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _elevationDetector = elevationDetector ?? throw new ArgumentNullException(nameof(elevationDetector));
        _secureDesktopDetector = secureDesktopDetector ?? throw new ArgumentNullException(nameof(secureDesktopDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Control mouse input on Windows. Supports move, click, double_click, right_click, middle_click, drag, and scroll actions.
    /// </summary>
    /// <remarks>
    /// COORDINATES: All x/y coordinates are relative to the specified monitor (default: monitor 0 = primary).
    /// Example: x=100, y=50 clicks 100px from left, 50px from top of the monitor.
    /// For secondary monitor, use monitorIndex=1.
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The mouse action to perform: move, click, double_click, right_click, middle_click, drag, or scroll.</param>
    /// <param name="x">X-coordinate relative to the monitor's left edge (required for move, optional for clicks).</param>
    /// <param name="y">Y-coordinate relative to the monitor's top edge (required for move, optional for clicks).</param>
    /// <param name="endX">End x-coordinate relative to the monitor (required for drag action).</param>
    /// <param name="endY">End y-coordinate relative to the monitor (required for drag action).</param>
    /// <param name="direction">Scroll direction: up, down, left, or right (required for scroll action).</param>
    /// <param name="amount">Number of scroll clicks (default: 1).</param>
    /// <param name="modifiers">Modifier keys to hold during action: ctrl, shift, alt (comma-separated).</param>
    /// <param name="button">Mouse button for drag: left, right, or middle (default: left).</param>
    /// <param name="monitorIndex">Monitor index (0-based, default: 0 = primary monitor). All coordinates are relative to this monitor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the mouse operation including success status, cursor position, and any errors.</returns>
    [McpServerTool(Name = "mouse_control", Title = "Mouse Control", Destructive = true, UseStructuredContent = true)]
    [Description("Control mouse input on Windows. Supports move, click, double_click, right_click, middle_click, drag, and scroll actions. COORDINATES: All x/y coordinates are relative to the specified monitor (default: monitor 0 = primary). Example: x=100, y=50 clicks 100px from left, 50px from top of the monitor. For secondary monitor, use monitorIndex=1.")]
    [return: Description("The result of the mouse operation including success status, final cursor position, window title at cursor, and error details if failed.")]
    public async Task<MouseControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The mouse action to perform: move, click, double_click, right_click, middle_click, drag, or scroll")] string action,
        [Description("X-coordinate relative to the monitor's left edge (required for move, optional for clicks).")] int? x = null,
        [Description("Y-coordinate relative to the monitor's top edge (required for move, optional for clicks).")] int? y = null,
        [Description("End x-coordinate relative to the monitor (required for drag action).")] int? endX = null,
        [Description("End y-coordinate relative to the monitor (required for drag action).")] int? endY = null,
        [Description("Scroll direction: up, down, left, or right (required for scroll action)")] string? direction = null,
        [Description("Number of scroll clicks (default: 1)")] int amount = 1,
        [Description("Modifier keys to hold during action: ctrl, shift, alt (comma-separated)")] string? modifiers = null,
        [Description("Mouse button for drag: left, right, or middle (default: left)")] string? button = null,
        [Description("Monitor index (0-based, default: 0 = primary monitor). All coordinates are relative to this monitor.")] int monitorIndex = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = MouseOperationLogger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("MouseControl");
        clientLogger?.LogMouseOperationStarted(action ?? "null");

        _logger.LogOperationStart(correlationId, action ?? "null");

        // Create a linked token source with the configured timeout
        using var timeoutCts = new CancellationTokenSource(_configuration.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        try
        {
            // Validate and parse the action
            if (string.IsNullOrWhiteSpace(action))
            {
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidAction,
                    "Action parameter is required");
                _logger.LogOperationFailure(correlationId, "null", result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            var mouseAction = ParseAction(action);
            if (mouseAction == null)
            {
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidAction,
                    $"Unknown action: '{action}'. Valid actions are: move, click, double_click, right_click, middle_click, drag, scroll");
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            // Translate monitor-relative coordinates to absolute screen coordinates
            int? absoluteX = x, absoluteY = y, absoluteEndX = endX, absoluteEndY = endY;
            var monitor = _monitorService.GetMonitor(monitorIndex);
            if (monitor == null)
            {
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidCoordinates,
                    $"Invalid monitor index: {monitorIndex}. Available monitors: 0-{_monitorService.MonitorCount - 1}");
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

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

            MouseControlResult operationResult;

            switch (mouseAction.Value)
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

                default:
                    operationResult = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            stopwatch.Stop();

            if (operationResult.Success)
            {
                _logger.LogOperationSuccess(correlationId, action, operationResult.FinalPosition.X, operationResult.FinalPosition.Y, operationResult.WindowTitle, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogOperationFailure(correlationId, action, operationResult.ErrorCode.ToString(), operationResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            }

            return operationResult;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            var errorResult = MouseControlResult.CreateFailure(
                MouseControlErrorCode.OperationTimeout,
                $"Operation timed out after {_configuration.TimeoutMs}ms");
            _logger.LogOperationFailure(correlationId, action ?? "null", errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested by caller, not timeout
            stopwatch.Stop();
            var errorResult = MouseControlResult.CreateFailure(
                MouseControlErrorCode.UnexpectedError,
                "Operation was cancelled");
            _logger.LogOperationFailure(correlationId, action ?? "null", errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogOperationException(correlationId, action ?? "null", ex);
            var errorResult = MouseControlResult.CreateFailure(
                MouseControlErrorCode.UnexpectedError,
                $"An unexpected error occurred: {ex.Message}");
            return errorResult;
        }
    }

    private async Task<MouseControlResult> HandleMoveAsync(int? x, int? y, CancellationToken cancellationToken)
    {
        // Validate required parameters for move
        if (!x.HasValue || !y.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Move action requires both x and y coordinates");
        }

        return await _mouseInputService.MoveAsync(x.Value, y.Value, cancellationToken);
    }

    private async Task<MouseControlResult> HandleClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform click operation: secure desktop (UAC, lock screen) is active");
        }

        // Determine the target coordinates for elevation check
        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            // Use current cursor position
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        // Check if the target window is elevated
        if (_elevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        // Parse modifier keys (for future use - currently not implemented)
        var modifiers = ParseModifiers(modifiersString);

        return await _mouseInputService.ClickAsync(x, y, modifiers, cancellationToken);
    }

    private async Task<MouseControlResult> HandleDoubleClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform double-click operation: secure desktop (UAC, lock screen) is active");
        }

        // Determine the target coordinates for elevation check
        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            // Use current cursor position
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        // Check if the target window is elevated
        if (_elevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot double-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        // Parse modifier keys (for future use - currently not implemented)
        var modifiers = ParseModifiers(modifiersString);

        return await _mouseInputService.DoubleClickAsync(x, y, modifiers, cancellationToken);
    }

    private async Task<MouseControlResult> HandleRightClickAsync(int? x, int? y, string? modifiersString, CancellationToken cancellationToken)
    {
        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform right-click operation: secure desktop (UAC, lock screen) is active");
        }

        // Determine the target coordinates for elevation check
        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            // Use current cursor position
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        // Check if the target window is elevated
        if (_elevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot right-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        // Parse modifier keys (for future use - currently not implemented)
        var modifiers = ParseModifiers(modifiersString);

        return await _mouseInputService.RightClickAsync(x, y, modifiers, cancellationToken);
    }

    private async Task<MouseControlResult> HandleMiddleClickAsync(int? x, int? y, CancellationToken cancellationToken)
    {
        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform middle-click operation: secure desktop (UAC, lock screen) is active");
        }

        // Determine the target coordinates for elevation check
        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            // Use current cursor position
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        // Check if the target window is elevated
        if (_elevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot middle-click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        return await _mouseInputService.MiddleClickAsync(x, y, cancellationToken);
    }

    private async Task<MouseControlResult> HandleDragAsync(int? startX, int? startY, int? endX, int? endY, string? buttonString, CancellationToken cancellationToken)
    {
        // Validate required parameters for drag
        if (!startX.HasValue || !startY.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Drag action requires x and y coordinates for start position");
        }

        if (!endX.HasValue || !endY.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Drag action requires end_x and end_y coordinates for end position");
        }

        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform drag operation: secure desktop (UAC, lock screen) is active");
        }

        // Check if the target window (start position) is elevated
        if (_elevationDetector.IsTargetElevated(startX.Value, startY.Value))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot drag from elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        // Parse the button parameter
        var mouseButton = ParseMouseButton(buttonString);

        return await _mouseInputService.DragAsync(startX.Value, startY.Value, endX.Value, endY.Value, mouseButton, cancellationToken);
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
            _ => MouseButton.Left, // Default to left button
        };
    }

    private async Task<MouseControlResult> HandleScrollAsync(int? x, int? y, string? directionString, int amount, CancellationToken cancellationToken)
    {
        // Validate required direction parameter
        if (string.IsNullOrWhiteSpace(directionString))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.MissingRequiredParameter,
                "Scroll action requires a direction parameter (up, down, left, or right)");
        }

        // Parse the direction
        var direction = ParseScrollDirection(directionString);
        if (!direction.HasValue)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.InvalidScrollDirection,
                $"Invalid scroll direction: '{directionString}'. Valid directions are: up, down, left, right");
        }

        // Check if secure desktop is active before any operation
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.SecureDesktopActive,
                "Cannot perform scroll operation: secure desktop (UAC, lock screen) is active");
        }

        // Determine the target coordinates for elevation check
        int targetX, targetY;
        if (x.HasValue && y.HasValue)
        {
            targetX = x.Value;
            targetY = y.Value;
        }
        else
        {
            // Use current cursor position
            var currentPos = Coordinates.FromCurrent();
            targetX = currentPos.X;
            targetY = currentPos.Y;
        }

        // Check if the target window is elevated
        if (_elevationDetector.IsTargetElevated(targetX, targetY))
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.ElevatedProcessTarget,
                "Cannot scroll in elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");
        }

        return await _mouseInputService.ScrollAsync(direction.Value, amount, x, y, cancellationToken);
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

        var modifiers = ModifierKey.None;
        var parts = modifiersString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                "alt" => ModifierKey.Alt,
                _ => ModifierKey.None,
            };
        }

        return modifiers;
    }

    private static MouseAction? ParseAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "move" => MouseAction.Move,
            "click" => MouseAction.Click,
            "double_click" => MouseAction.DoubleClick,
            "right_click" => MouseAction.RightClick,
            "middle_click" => MouseAction.MiddleClick,
            "drag" => MouseAction.Drag,
            "scroll" => MouseAction.Scroll,
            _ => null,
        };
    }

}
