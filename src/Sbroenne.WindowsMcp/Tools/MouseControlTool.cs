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
    /// Control mouse input on Windows. Supports move, click, double_click, right_click, middle_click, drag, scroll, and get_position actions.
    /// </summary>
    /// <remarks>
    /// <para><strong>BREAKING CHANGE:</strong> monitorIndex parameter is now REQUIRED when x/y coordinates are provided.</para>
    /// <para><strong>COORDINATES:</strong> All x/y coordinates are relative to the specified monitor.</para>
    /// <para>Example: monitorIndex=0, x=100, y=50 clicks 100px from left, 50px from top of primary monitor.</para>
    /// <para>For secondary monitor: monitorIndex=1, x=100, y=50 clicks 100px from left, 50px from top of secondary monitor.</para>
    /// <para><strong>MONITOR CONTEXT:</strong> Successful operations with explicit coordinates return monitor_index, monitor_width, and monitor_height in the response.</para>
    /// <para><strong>QUERY POSITION:</strong> Use action='get_position' to query current cursor position with monitor context.</para>
    /// <para><strong>ERROR CASES:</strong></para>
    /// <list type="bullet">
    /// <item>missing_required_parameter: monitorIndex not provided when coordinates are specified</item>
    /// <item>invalid_coordinates: monitorIndex out of range (must be 0 to MonitorCount-1)</item>
    /// <item>coordinates_out_of_bounds: coordinates outside monitor dimensions</item>
    /// </list>
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The mouse action to perform: move, click, double_click, right_click, middle_click, drag, scroll, or get_position.</param>
    /// <param name="x">X-coordinate relative to the monitor's left edge (required for move with monitorIndex, optional for clicks with monitorIndex).</param>
    /// <param name="y">Y-coordinate relative to the monitor's top edge (required for move with monitorIndex, optional for clicks with monitorIndex).</param>
    /// <param name="endX">End x-coordinate relative to the monitor (required for drag action with monitorIndex).</param>
    /// <param name="endY">End y-coordinate relative to the monitor (required for drag action with monitorIndex).</param>
    /// <param name="direction">Scroll direction: up, down, left, or right (required for scroll action).</param>
    /// <param name="amount">Number of scroll clicks (default: 1).</param>
    /// <param name="modifiers">Modifier keys to hold during action: ctrl, shift, alt (comma-separated).</param>
    /// <param name="button">Mouse button for drag: left, right, or middle (default: left).</param>
    /// <param name="monitorIndex">Monitor index (0-based). REQUIRED when x/y/endX/endY coordinates are provided. Specifies which monitor coordinates are relative to. Not required for coordinate-less actions (click at current position) or get_position.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the mouse operation including success status, monitor-relative cursor position, monitor context (index, width, height), window title at cursor, and error details if failed.</returns>
    [McpServerTool(Name = "mouse_control", Title = "Mouse Control", Destructive = true, UseStructuredContent = true)]
    [Description("Control mouse input on Windows. Supports move, click, double_click, right_click, middle_click, drag, scroll, and get_position actions. BREAKING CHANGE: monitorIndex is now REQUIRED when coordinates (x/y/endX/endY) are provided. All coordinates are monitor-relative. Example: monitorIndex=0, x=100, y=50 clicks 100px from left, 50px from top of primary monitor. Successful operations return monitor context (monitor_index, monitor_width, monitor_height). Use get_position to query current cursor position with monitor info.")]
    [return: Description("The result of the mouse operation including success status, final cursor position (monitor-relative), monitor context (monitor_index, monitor_width, monitor_height for operations with explicit coordinates), window title at cursor, and error details if failed.")]
    public async Task<MouseControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The mouse action to perform: move, click, double_click, right_click, middle_click, drag, scroll, or get_position (query current cursor position with monitor context)")] string action,
        [Description("X-coordinate relative to the monitor's left edge. REQUIRES monitorIndex parameter. Required for move, optional for clicks. Omit for coordinate-less click at current position.")] int? x = null,
        [Description("Y-coordinate relative to the monitor's top edge. REQUIRES monitorIndex parameter. Required for move, optional for clicks. Omit for coordinate-less click at current position.")] int? y = null,
        [Description("End x-coordinate relative to the monitor. REQUIRES monitorIndex parameter. Required for drag action.")] int? endX = null,
        [Description("End y-coordinate relative to the monitor. REQUIRES monitorIndex parameter. Required for drag action.")] int? endY = null,
        [Description("Scroll direction: up, down, left, or right (required for scroll action)")] string? direction = null,
        [Description("Number of scroll clicks (default: 1)")] int amount = 1,
        [Description("Modifier keys to hold during action: ctrl, shift, alt (comma-separated)")] string? modifiers = null,
        [Description("Mouse button for drag: left, right, or middle (default: left)")] string? button = null,
        [Description("Monitor index (0-based, 0=primary). REQUIRED when x/y/endX/endY coordinates are provided. Coordinates are interpreted relative to this monitor's top-left corner. Returns error 'missing_required_parameter' if omitted when coordinates are specified. Not required for coordinate-less actions or get_position.")] int? monitorIndex = null,
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

            // NEW VALIDATION: Check if coordinates are provided
            var hasCoordinates = (x.HasValue && y.HasValue) || (endX.HasValue && endY.HasValue);

            // NEW VALIDATION: Require monitorIndex when coordinates are provided
            if (hasCoordinates && !monitorIndex.HasValue)
            {
                var availableIndices = Enumerable.Range(0, _monitorService.MonitorCount).ToList();
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.MissingRequiredParameter,
                    "monitorIndex is required when using x/y coordinates",
                    errorDetails: new Dictionary<string, object>
                    {
                        { "valid_indices", availableIndices }
                    });
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            // Use monitorIndex if provided, otherwise default to 0 for coordinate-less actions
            var targetMonitorIndex = monitorIndex ?? 0;

            // NEW VALIDATION: Validate monitorIndex is in valid range
            if (monitorIndex.HasValue && (targetMonitorIndex < 0 || targetMonitorIndex >= _monitorService.MonitorCount))
            {
                var availableIndices = Enumerable.Range(0, _monitorService.MonitorCount).ToList();
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.InvalidCoordinates,
                    $"Invalid monitorIndex: {targetMonitorIndex}",
                    errorDetails: new Dictionary<string, object>
                    {
                        { "valid_indices", availableIndices },
                        { "provided_index", targetMonitorIndex }
                    });
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            // Translate monitor-relative coordinates to absolute screen coordinates
            int? absoluteX = x, absoluteY = y, absoluteEndX = endX, absoluteEndY = endY;
            var monitor = _monitorService.GetMonitor(targetMonitorIndex);
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

            // NEW VALIDATION: Check if coordinates are within monitor bounds
            if (hasCoordinates && monitorIndex.HasValue)
            {
                // Validate start coordinates (x, y) if provided
                if (x.HasValue && y.HasValue)
                {
                    if (x.Value < 0 || x.Value >= monitor.Width || y.Value < 0 || y.Value >= monitor.Height)
                    {
                        var result = MouseControlResult.CreateFailure(
                            MouseControlErrorCode.CoordinatesOutOfBounds,
                            $"Coordinates ({x.Value}, {y.Value}) out of bounds for monitor {targetMonitorIndex}",
                            errorDetails: new Dictionary<string, object>
                            {
                                { "valid_bounds", new
                                    {
                                        left = monitor.X,
                                        top = monitor.Y,
                                        right = monitor.X + monitor.Width,
                                        bottom = monitor.Y + monitor.Height
                                    }
                                },
                                { "provided_coordinates", new { x = x.Value, y = y.Value } }
                            });
                        _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                        return result;
                    }
                }

                // Validate end coordinates (endX, endY) for drag if provided
                if (endX.HasValue && endY.HasValue)
                {
                    if (endX.Value < 0 || endX.Value >= monitor.Width || endY.Value < 0 || endY.Value >= monitor.Height)
                    {
                        var result = MouseControlResult.CreateFailure(
                            MouseControlErrorCode.CoordinatesOutOfBounds,
                            $"End coordinates ({endX.Value}, {endY.Value}) out of bounds for monitor {targetMonitorIndex}",
                            errorDetails: new Dictionary<string, object>
                            {
                                { "valid_bounds", new
                                    {
                                        left = monitor.X,
                                        top = monitor.Y,
                                        right = monitor.X + monitor.Width,
                                        bottom = monitor.Y + monitor.Height
                                    }
                                },
                                { "provided_coordinates", new { x = endX.Value, y = endY.Value } }
                            });
                        _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                        return result;
                    }
                }
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

                case MouseAction.GetPosition:
                    operationResult = await GetCurrentPositionAsync(linkedToken);
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
                // Add monitor context to successful operations that used explicit coordinates
                if (monitorIndex.HasValue)
                {
                    var monitorInfo = _monitorService.GetMonitor(targetMonitorIndex);
                    if (monitorInfo != null)
                    {
                        operationResult = operationResult with
                        {
                            MonitorIndex = targetMonitorIndex,
                            MonitorWidth = monitorInfo.Width,
                            MonitorHeight = monitorInfo.Height
                        };
                    }
                }

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
            "get_position" => MouseAction.GetPosition,
            _ => null,
        };
    }

    private async Task<MouseControlResult> GetCurrentPositionAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Suppress async warning (no actual async operation needed)

        // Get current cursor position (absolute screen coordinates)
        Native.NativeMethods.GetCursorPos(out var cursorPos);
        int absoluteX = cursorPos.X;
        int absoluteY = cursorPos.Y;

        // Determine which monitor contains this position
        var monitors = _monitorService.GetMonitors();
        MonitorInfo? targetMonitor = null;
        int? monitorIndex = null;

        for (int i = 0; i < monitors.Count; i++)
        {
            var mon = monitors[i];
            if (absoluteX >= mon.X && absoluteX < mon.X + mon.Width &&
                absoluteY >= mon.Y && absoluteY < mon.Y + mon.Height)
            {
                targetMonitor = mon;
                monitorIndex = i;
                break;
            }
        }

        // If no monitor found (shouldn't happen), use primary monitor
        if (targetMonitor == null || !monitorIndex.HasValue)
        {
            targetMonitor = monitors[0];
            monitorIndex = 0;
        }

        // Calculate monitor-relative coordinates
        int relativeX = absoluteX - targetMonitor.X;
        int relativeY = absoluteY - targetMonitor.Y;

        // Create result with monitor context
        var result = MouseControlResult.CreateSuccess(
            new Coordinates(relativeX, relativeY));

        // Add monitor context
        return result with
        {
            MonitorIndex = monitorIndex.Value,
            MonitorWidth = targetMonitor.Width,
            MonitorHeight = targetMonitor.Height
        };
    }

}
