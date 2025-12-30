using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling the mouse cursor on Windows.
/// </summary>
[McpServerToolType]
public sealed partial class MouseControlTool
{
    private static readonly string[] ValidTargets = ["primary_screen", "secondary_screen"];

    private readonly IMouseInputService _mouseInputService;
    private readonly IMonitorService _monitorService;
    private readonly IWindowEnumerator _windowEnumerator;
    private readonly IWindowService _windowService;
    private readonly IElevationDetector _elevationDetector;
    private readonly ISecureDesktopDetector _secureDesktopDetector;
    private readonly MouseOperationLogger _logger;
    private readonly MouseConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseControlTool"/> class.
    /// </summary>
    /// <param name="mouseInputService">The mouse input service.</param>
    /// <param name="monitorService">The monitor service.</param>
    /// <param name="windowEnumerator">The window enumerator for getting target window info.</param>
    /// <param name="windowService">The window service for finding and activating windows.</param>
    /// <param name="elevationDetector">The elevation detector.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="logger">The operation logger.</param>
    /// <param name="configuration">The mouse configuration.</param>
    public MouseControlTool(
        IMouseInputService mouseInputService,
        IMonitorService monitorService,
        IWindowEnumerator windowEnumerator,
        IWindowService windowService,
        IElevationDetector elevationDetector,
        ISecureDesktopDetector secureDesktopDetector,
        MouseOperationLogger logger,
        MouseConfiguration configuration)
    {
        _mouseInputService = mouseInputService ?? throw new ArgumentNullException(nameof(mouseInputService));
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _windowEnumerator = windowEnumerator ?? throw new ArgumentNullException(nameof(windowEnumerator));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _elevationDetector = elevationDetector ?? throw new ArgumentNullException(nameof(elevationDetector));
        _secureDesktopDetector = secureDesktopDetector ?? throw new ArgumentNullException(nameof(secureDesktopDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Control mouse input on Windows. Supports move, click, double_click, right_click, middle_click, drag, scroll, and get_position actions.
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
    /// <para><strong>ERROR CASES:</strong></para>
    /// <list type="bullet">
    /// <item>missing_required_parameter: Neither target nor monitorIndex provided when coordinates are specified</item>
    /// <item>invalid_target: Invalid target value (use 'primary_screen' or 'secondary_screen')</item>
    /// <item>invalid_coordinates: monitorIndex out of range (must be 0 to MonitorCount-1)</item>
    /// <item>coordinates_out_of_bounds: coordinates outside monitor dimensions</item>
    /// </list>
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The mouse action to perform: move, click, double_click, right_click, middle_click, drag, scroll, or get_position.</param>
    /// <param name="app">Application window to target by title (partial match). The server automatically finds and activates the window.</param>
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
    /// <returns>The result of the mouse operation including success status, monitor-relative cursor position, monitor context (index, width, height), window title at cursor, and error details if failed.</returns>
    [McpServerTool(Name = "mouse_control", Title = "Mouse Control", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Low-level mouse input for raw coordinate clicks. DO NOT use for clicking buttons - use ui_automation(action='click') instead. mouse_control is ONLY for: 1) raw coordinate clicks when you have exact x,y, 2) custom-drawn controls without UIA support, 3) games. Actions: move, click, double_click, right_click, middle_click, drag, scroll, get_position.")]
    [return: Description("The result includes success status, cursor position, monitor context, and 'target_window' (handle, title, process_name) for click actions. If expectedWindowTitle/expectedProcessName was specified but didn't match, success=false with error_code='wrong_target_window'.")]
    public async Task<MouseControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The mouse action: move, click, double_click, right_click, middle_click, drag, scroll, get_position")] string action,
        [Description("Application window to target by title (partial match, case-insensitive). Example: app='Visual Studio Code' or app='Notepad'. The server automatically finds and activates the window before the mouse action.")] string? app = null,
        [Description("Monitor target: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor in 2-monitor setups). For 3+ monitors, use monitorIndex instead.")] string? target = null,
        [Description("X-coordinate relative to the monitor's left edge. Required for move, optional for clicks. Omit for coordinate-less click at current position.")] int? x = null,
        [Description("Y-coordinate relative to the monitor's top edge. Required for move, optional for clicks. Omit for coordinate-less click at current position.")] int? y = null,
        [Description("End x-coordinate relative to the monitor. Required for drag action.")] int? endX = null,
        [Description("End y-coordinate relative to the monitor. Required for drag action.")] int? endY = null,
        [Description("Scroll direction: up, down, left, or right (required for scroll action)")] string? direction = null,
        [Description("Number of scroll clicks (default: 1)")] int amount = 1,
        [Description("Modifier keys to hold during action: ctrl, shift, alt (comma-separated)")] string? modifiers = null,
        [Description("Mouse button for drag: left, right, or middle (default: left)")] string? button = null,
        [Description("Monitor index (0-based). Alternative to 'target' for 3+ monitor setups. Use screenshot_control action='list_monitors' to find indices. Not required for coordinate-less actions or get_position.")] int? monitorIndex = null,
        [Description("Expected window title (partial match). If specified, operation fails with 'wrong_target_window' if the foreground window title doesn't contain this text. Use this to prevent clicking in the wrong application.")] string? expectedWindowTitle = null,
        [Description("Expected process name (e.g., 'Code', 'chrome', 'notepad'). If specified, operation fails with 'wrong_target_window' if the foreground window's process doesn't match. Use this to prevent clicking in the wrong application.")] string? expectedProcessName = null,

        [Description("Window handle (decimal string from window_management). When provided with x/y, coordinates are relative to the window's top-left corner instead of the monitor. Useful for clicking fixed positions within a specific window.")] string? windowHandle = null,
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
            // Resolve 'app' parameter to windowHandle if specified
            Models.WindowInfoCompact? resolvedWindow = null;
            if (!string.IsNullOrWhiteSpace(app) && string.IsNullOrWhiteSpace(windowHandle))
            {
                var findResult = await _windowService.FindWindowAsync(app, useRegex: false, linkedToken);
                if (!findResult.Success || (findResult.Windows?.Count ?? 0) == 0)
                {
                    // Try listing all windows to provide helpful suggestions
                    var listResult = await _windowService.ListWindowsAsync(cancellationToken: linkedToken);
                    var availableWindows = listResult.Windows?.Take(10).Select(w => $"'{w.Title}'").ToArray() ?? [];
                    var suggestion = availableWindows.Length > 0
                        ? $"Available windows: {string.Join(", ", availableWindows)}"
                        : "No windows found. Ensure the application is running.";

                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.WrongTargetWindow,
                        $"No window found matching app='{app}'. {suggestion}");
                    _logger.LogOperationFailure(correlationId, action ?? "null", result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                // If multiple windows match, use the first one (most recently active)
                resolvedWindow = findResult.Windows![0];
                windowHandle = resolvedWindow.Handle;

                // Activate the window before performing mouse action
                await _windowService.ActivateWindowAsync(nint.Parse(windowHandle), linkedToken);
            }

            // Pre-flight check: verify target window if expected values are specified
            if (!string.IsNullOrEmpty(expectedWindowTitle) || !string.IsNullOrEmpty(expectedProcessName))
            {
                var targetCheckResult = await VerifyTargetWindowAsync(expectedWindowTitle, expectedProcessName, linkedToken);
                if (!targetCheckResult.Success)
                {
                    _logger.LogOperationFailure(correlationId, action ?? "null", targetCheckResult.ErrorCode.ToString(), targetCheckResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return targetCheckResult;
                }
            }

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
                    $"Unknown action: '{action}'. Valid actions are: move, click, double_click, right_click, middle_click, drag, scroll, get_position");
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            // NEW VALIDATION: Check if coordinates are provided
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
                    _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                // Get window rect
                if (!NativeMethods.GetWindowRect(parsedWindowHandle, out var windowRect))
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.InvalidCoordinates,
                        $"Could not get window position for handle {windowHandle}. The window may no longer exist.");
                    _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                windowLeft = windowRect.Left;
                windowTop = windowRect.Top;

                // Determine which monitor contains this window's center
                var windowCenterX = windowRect.Left + (windowRect.Right - windowRect.Left) / 2;
                var windowCenterY = windowRect.Top + (windowRect.Bottom - windowRect.Top) / 2;
                var monitors = _monitorService.GetMonitors();

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
                    _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                // Resolve target to monitor index
                MonitorInfo? targetMonitor = parsedTarget.Value switch
                {
                    MonitorTarget.PrimaryScreen => _monitorService.GetPrimaryMonitor(),
                    MonitorTarget.SecondaryScreen => _monitorService.GetSecondaryMonitor(),
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
                    _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                // Find the index of this monitor
                var monitors = _monitorService.GetMonitors();
                for (int i = 0; i < monitors.Count; i++)
                {
                    if (monitors[i].X == targetMonitor.X && monitors[i].Y == targetMonitor.Y)
                    {
                        resolvedMonitorIndex = i;
                        break;
                    }
                }
            }

            // NEW VALIDATION: Require target, monitorIndex, or windowHandle when coordinates are provided
            if (hasCoordinates && !resolvedMonitorIndex.HasValue && !isWindowRelativeMode)
            {
                var availableIndices = Enumerable.Range(0, _monitorService.MonitorCount).ToList();
                var result = MouseControlResult.CreateFailure(
                    MouseControlErrorCode.MissingRequiredParameter,
                    "Either 'target', 'monitorIndex', or 'windowHandle' is required when using x/y coordinates. Use target='primary_screen' or target='secondary_screen' for easy targeting, or windowHandle for window-relative coordinates.",
                    errorDetails: new Dictionary<string, object>
                    {
                        { "valid_targets", ValidTargets },
                        { "valid_indices", availableIndices }
                    });
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            // Use resolved monitorIndex, windowBasedMonitorIndex, or default to 0 for coordinate-less actions
            var targetMonitorIndex = resolvedMonitorIndex ?? windowBasedMonitorIndex ?? 0;

            // NEW VALIDATION: Validate monitorIndex is in valid range
            if (resolvedMonitorIndex.HasValue && (targetMonitorIndex < 0 || targetMonitorIndex >= _monitorService.MonitorCount))
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

            // Translate coordinates to absolute screen coordinates
            // - If windowHandle provided: coordinates are relative to window's top-left corner
            // - If target/monitorIndex provided: coordinates are relative to monitor's top-left corner
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

            // NEW VALIDATION: Check if coordinates are within monitor bounds (using logical dimensions)
            if (hasCoordinates && resolvedMonitorIndex.HasValue)
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
                // Add monitor context and convert coordinates to monitor-relative for operations with explicit coordinates
                if (resolvedMonitorIndex.HasValue || isWindowRelativeMode)
                {
                    var monitorInfo = _monitorService.GetMonitor(targetMonitorIndex);
                    if (monitorInfo != null)
                    {
                        // Convert absolute cursor position to monitor-relative coordinates
                        // FinalPosition from MouseInputService is in absolute screen coordinates
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
                if (mouseAction.Value != MouseAction.GetPosition)
                {
                    operationResult = await AttachTargetWindowInfoAsync(operationResult, linkedToken);
                }

                _logger.LogOperationSuccess(correlationId, action, operationResult.FinalPosition.X, operationResult.FinalPosition.Y, operationResult.TargetWindow?.Title, stopwatch.ElapsedMilliseconds);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

    /// <summary>
    /// Monitor target for mouse operations.
    /// </summary>
    private enum MonitorTarget
    {
        /// <summary>Primary screen (main display with taskbar).</summary>
        PrimaryScreen,
        /// <summary>Secondary screen (other monitor in 2-monitor setups).</summary>
        SecondaryScreen
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

    /// <summary>
    /// Attaches information about the current foreground window to the result.
    /// This helps LLM agents verify that mouse operations targeted the correct window.
    /// </summary>
    private async Task<MouseControlResult> AttachTargetWindowInfoAsync(MouseControlResult result, CancellationToken cancellationToken)
    {
        try
        {
            var foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return result; // No foreground window, return original result
            }

            var windowInfo = await _windowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return result; // Couldn't get window info, return original result
            }

            return result with
            {
                TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo)
            };
        }
        catch
        {
            // Best effort - if we can't get the window info, just return the original result
            return result;
        }
    }

    /// <summary>
    /// Verifies that the foreground window matches the expected target before performing mouse actions.
    /// This prevents clicks from being sent to the wrong application.
    /// </summary>
    /// <param name="expectedTitle">Expected window title (partial match, case-insensitive).</param>
    /// <param name="expectedProcessName">Expected process name (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result if window matches, failure result with WrongTargetWindow error if not.</returns>
    private async Task<MouseControlResult> VerifyTargetWindowAsync(string? expectedTitle, string? expectedProcessName, CancellationToken cancellationToken)
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

            var windowInfo = await _windowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return MouseControlResult.CreateFailure(
                    MouseControlErrorCode.WrongTargetWindow,
                    "Could not retrieve foreground window information.");
            }

            // Check expected title (partial, case-insensitive match)
            if (!string.IsNullOrEmpty(expectedTitle))
            {
                if (string.IsNullOrEmpty(windowInfo.Title) ||
                    !windowInfo.Title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.WrongTargetWindow,
                        $"Foreground window title '{windowInfo.Title}' does not contain expected text '{expectedTitle}'. Aborting to prevent click in wrong window.");
                    return result with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            // Check expected process name (case-insensitive match)
            if (!string.IsNullOrEmpty(expectedProcessName))
            {
                if (string.IsNullOrEmpty(windowInfo.ProcessName) ||
                    !windowInfo.ProcessName.Equals(expectedProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    var result = MouseControlResult.CreateFailure(
                        MouseControlErrorCode.WrongTargetWindow,
                        $"Foreground window process '{windowInfo.ProcessName}' does not match expected process '{expectedProcessName}'. Aborting to prevent click in wrong window.");
                    return result with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            // Window matches expectations
            return MouseControlResult.CreateSuccess(new Coordinates(0, 0));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return MouseControlResult.CreateFailure(
                MouseControlErrorCode.WrongTargetWindow,
                $"Failed to verify target window: {ex.Message}");
        }
    }
}
