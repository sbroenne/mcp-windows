using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for managing windows on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed partial class WindowManagementTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IWindowService _windowService;
    private readonly IMonitorService _monitorService;
    private readonly WindowOperationLogger? _logger;
    private readonly WindowConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManagementTool"/> class.
    /// </summary>
    /// <param name="windowService">The window service.</param>
    /// <param name="monitorService">The monitor service.</param>
    /// <param name="configuration">The window configuration.</param>
    /// <param name="logger">Optional operation logger.</param>
    public WindowManagementTool(
        IWindowService windowService,
        IMonitorService monitorService,
        WindowConfiguration configuration,
        WindowOperationLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(monitorService);
        ArgumentNullException.ThrowIfNull(configuration);

        _windowService = windowService;
        _monitorService = monitorService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Manage windows on Windows. Supports list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, and move_to_monitor actions.
    /// </summary>
    /// <remarks>
    /// Use move_to_monitor to move a window to a specific monitor:
    /// - Use target='primary_screen' or target='secondary_screen' for easy targeting
    /// - Use monitorIndex for 3+ monitor setups (use screenshot_control action='list_monitors' to find indices)
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The window action to perform: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, or move_to_monitor.</param>
    /// <param name="handle">Window handle (required for activate, minimize, maximize, restore, close, move, resize, set_bounds, move_to_monitor, get_state, wait_for_state).</param>
    /// <param name="title">Window title to search for (required for find and wait_for).</param>
    /// <param name="filter">Filter windows by title or process name (for list action).</param>
    /// <param name="regex">Use regex matching for title/filter (default: false).</param>
    /// <param name="includeAllDesktops">Include windows on other virtual desktops (default: false).</param>
    /// <param name="x">X-coordinate for move or set_bounds action.</param>
    /// <param name="y">Y-coordinate for move or set_bounds action.</param>
    /// <param name="width">Width for resize or set_bounds action.</param>
    /// <param name="height">Height for resize or set_bounds action.</param>
    /// <param name="timeoutMs">Timeout in milliseconds for wait_for and wait_for_state actions (default: 5000).</param>
    /// <param name="target">Monitor target for move_to_monitor action: 'primary_screen' (main display), 'secondary_screen' (other monitor in 2-monitor setups).</param>
    /// <param name="monitorIndex">Target monitor index for move_to_monitor action (0-based). Alternative to target for 3+ monitor setups.</param>
    /// <param name="state">Target window state for wait_for_state action: 'normal', 'minimized', 'maximized', or 'hidden'.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the window operation including success status and window information.</returns>
    [McpServerTool(Name = "window_management", Title = "Window Management", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Manage windows on Windows. This is usually the workflow start. Common flow: (1) window_management(action='find' or 'list') to get a window handle, (2) window_management(action='activate') to focus it, (3) pass the returned handle verbatim as ui_automation.windowHandle or screenshot_control.windowHandle. Handle format: decimal string (digits only) from window_management output. Supports actions: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, move_to_monitor. Troubleshooting: if find returns no results, use list (optionally regex=true). If activate fails, try restore first.")]
    [return: Description("The result includes success status, window list or single window info (handle, title, process_name, state, is_foreground), and error details if failed. Save the 'handle' value to use with activate, close, or other window operations.")]
    public async Task<WindowManagementResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The window action to perform: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, or move_to_monitor")] string action,
        [Description("Window handle (required for activate, minimize, maximize, restore, close, move, resize, set_bounds, move_to_monitor, get_state, wait_for_state)")] string? handle = null,
        [Description("Window title to search for (required for find and wait_for)")] string? title = null,
        [Description("Filter windows by title or process name (for list action)")] string? filter = null,
        [Description("Use regex matching for title/filter (default: false)")] bool regex = false,
        [Description("Include windows on other virtual desktops (default: false)")] bool includeAllDesktops = false,
        [Description("X-coordinate for move or set_bounds action")] int? x = null,
        [Description("Y-coordinate for move or set_bounds action")] int? y = null,
        [Description("Width for resize or set_bounds action")] int? width = null,
        [Description("Height for resize or set_bounds action")] int? height = null,
        [Description("Timeout in milliseconds for wait_for and wait_for_state actions (default: 5000)")] int? timeoutMs = null,
        [Description("Monitor target for move_to_monitor action: 'primary_screen' (main display), 'secondary_screen' (other monitor in 2-monitor setups). For 3+ monitors, use monitorIndex.")] string? target = null,
        [Description("Target monitor index for move_to_monitor action (0-based). Alternative to 'target' for 3+ monitor setups.")] int? monitorIndex = null,
        [Description("Target window state for wait_for_state action: 'normal', 'minimized', 'maximized', or 'hidden'")] string? state = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("WindowManagement");
        clientLogger?.LogWindowOperationStarted(action ?? "null");

        try
        {
            // Validate and parse the action
            if (string.IsNullOrWhiteSpace(action))
            {
                var result = WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidAction,
                    "Action parameter is required");
                _logger?.LogWindowOperation("null", success: false, errorMessage: result.Error);
                return result;
            }

            var windowAction = ParseAction(action);
            if (windowAction is null)
            {
                var result = WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidAction,
                    $"Unknown action: '{action}'. Valid actions are: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, move_to_monitor");
                _logger?.LogWindowOperation(action, success: false, errorMessage: result.Error);
                return result;
            }

            WindowManagementResult operationResult;

            switch (windowAction.Value)
            {
                case WindowAction.List:
                    operationResult = await HandleListAsync(filter, regex, includeAllDesktops, cancellationToken);
                    break;

                case WindowAction.Find:
                    operationResult = await HandleFindAsync(title, regex, cancellationToken);
                    break;

                case WindowAction.Activate:
                    operationResult = await HandleActivateAsync(handle, cancellationToken);
                    break;

                case WindowAction.GetForeground:
                    operationResult = await HandleGetForegroundAsync(cancellationToken);
                    break;

                case WindowAction.Minimize:
                    operationResult = await HandleMinimizeAsync(handle, cancellationToken);
                    break;

                case WindowAction.Maximize:
                    operationResult = await HandleMaximizeAsync(handle, cancellationToken);
                    break;

                case WindowAction.Restore:
                    operationResult = await HandleRestoreAsync(handle, cancellationToken);
                    break;

                case WindowAction.Close:
                    operationResult = await HandleCloseAsync(handle, cancellationToken);
                    break;

                case WindowAction.Move:
                    operationResult = await HandleMoveAsync(handle, x, y, cancellationToken);
                    break;

                case WindowAction.Resize:
                    operationResult = await HandleResizeAsync(handle, width, height, cancellationToken);
                    break;

                case WindowAction.SetBounds:
                    operationResult = await HandleSetBoundsAsync(handle, x, y, width, height, cancellationToken);
                    break;

                case WindowAction.WaitFor:
                    operationResult = await HandleWaitForAsync(title, regex, timeoutMs, cancellationToken);
                    break;

                case WindowAction.MoveToMonitor:
                    operationResult = await HandleMoveToMonitorAsync(handle, target, monitorIndex, cancellationToken);
                    break;

                case WindowAction.GetState:
                    operationResult = await HandleGetStateAsync(handle, cancellationToken);
                    break;

                case WindowAction.WaitForState:
                    operationResult = await HandleWaitForStateAsync(handle, state, timeoutMs, cancellationToken);
                    break;

                default:
                    operationResult = WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            stopwatch.Stop();

            _logger?.LogWindowOperation(
                action,
                success: operationResult.Success,
                windowCount: operationResult.Windows?.Count,
                windowTitle: operationResult.Window?.Title,
                errorMessage: operationResult.Error);

            return operationResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var errorResult = WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.Timeout,
                "Operation was cancelled");
            _logger?.LogWindowOperation(action ?? "null", success: false, errorMessage: errorResult.Error);
            return errorResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(action ?? "null", ex);
            var errorResult = WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SystemError,
                $"An unexpected error occurred: {ex.Message}");
            return errorResult;
        }
    }

    private async Task<WindowManagementResult> HandleListAsync(
        string? filter,
        bool useRegex,
        bool includeAllDesktops,
        CancellationToken cancellationToken)
    {
        return await _windowService.ListWindowsAsync(filter, useRegex, includeAllDesktops, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleFindAsync(
        string? title,
        bool useRegex,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Title is required for find action");
        }

        return await _windowService.FindWindowAsync(title, useRegex, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleActivateAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for activate action");
        }

        return await _windowService.ActivateWindowAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleGetForegroundAsync(CancellationToken cancellationToken)
    {
        return await _windowService.GetForegroundWindowAsync(cancellationToken);
    }

    private async Task<WindowManagementResult> HandleMinimizeAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for minimize action");
        }

        return await _windowService.MinimizeWindowAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleMaximizeAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for maximize action");
        }

        return await _windowService.MaximizeWindowAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleRestoreAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for restore action");
        }

        return await _windowService.RestoreWindowAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleCloseAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for close action");
        }

        return await _windowService.CloseWindowAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleMoveAsync(
        string? handleString,
        int? x,
        int? y,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for move action");
        }

        if (!x.HasValue || !y.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Both x and y coordinates are required for move action");
        }

        return await _windowService.MoveWindowAsync(handle, x.Value, y.Value, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleResizeAsync(
        string? handleString,
        int? width,
        int? height,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for resize action");
        }

        if (!width.HasValue || !height.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Both width and height are required for resize action");
        }

        return await _windowService.ResizeWindowAsync(handle, width.Value, height.Value, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleSetBoundsAsync(
        string? handleString,
        int? x,
        int? y,
        int? width,
        int? height,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for set_bounds action");
        }

        if (!x.HasValue || !y.HasValue || !width.HasValue || !height.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "All bounds parameters (x, y, width, height) are required for set_bounds action");
        }

        var bounds = new WindowBounds
        {
            X = x.Value,
            Y = y.Value,
            Width = width.Value,
            Height = height.Value
        };

        return await _windowService.SetBoundsAsync(handle, bounds, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleWaitForAsync(
        string? title,
        bool useRegex,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Title is required for wait_for action");
        }

        return await _windowService.WaitForWindowAsync(title, useRegex, timeoutMs, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleMoveToMonitorAsync(
        string? handleString,
        string? target,
        int? monitorIndex,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for move_to_monitor action");
        }

        // Resolve target to monitorIndex if provided
        int? resolvedMonitorIndex = monitorIndex;
        if (!string.IsNullOrWhiteSpace(target))
        {
            var parsedTarget = ParseMonitorTarget(target);
            if (parsedTarget == null)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidCoordinates,
                    $"Invalid target: '{target}'. Valid values are: 'primary_screen', 'secondary_screen'");
            }

            // Resolve target to monitor
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
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidCoordinates,
                    errorMessage);
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

        if (!resolvedMonitorIndex.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Either 'target' or 'monitorIndex' is required for move_to_monitor action. Use target='primary_screen' or target='secondary_screen'.");
        }

        return await _windowService.MoveToMonitorAsync(handle, resolvedMonitorIndex.Value, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleGetStateAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for get_state action");
        }

        return await _windowService.GetWindowStateAsync(handle, cancellationToken);
    }

    private async Task<WindowManagementResult> HandleWaitForStateAsync(
        string? handleString,
        string? stateString,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for wait_for_state action");
        }

        var targetState = ParseWindowState(stateString);
        if (targetState is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                $"Invalid state: '{stateString}'. Valid states are: normal, minimized, maximized, hidden");
        }

        return await _windowService.WaitForStateAsync(handle, targetState.Value, timeoutMs, cancellationToken);
    }

    private static WindowState? ParseWindowState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        return state.ToLowerInvariant() switch
        {
            "normal" or "restored" => WindowState.Normal,
            "minimized" or "min" => WindowState.Minimized,
            "maximized" or "max" => WindowState.Maximized,
            "hidden" => WindowState.Hidden,
            _ => null
        };
    }

    private static MonitorTarget? ParseMonitorTarget(string? target)
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
    /// Monitor target for window operations.
    /// </summary>
    private enum MonitorTarget
    {
        /// <summary>Primary screen (main display with taskbar).</summary>
        PrimaryScreen,
        /// <summary>Secondary screen (other monitor in 2-monitor setups).</summary>
        SecondaryScreen
    }

    private static WindowAction? ParseAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "list" => WindowAction.List,
            "find" => WindowAction.Find,
            "activate" => WindowAction.Activate,
            "get_foreground" => WindowAction.GetForeground,
            "minimize" => WindowAction.Minimize,
            "maximize" => WindowAction.Maximize,
            "restore" => WindowAction.Restore,
            "close" => WindowAction.Close,
            "move" => WindowAction.Move,
            "resize" => WindowAction.Resize,
            "set_bounds" => WindowAction.SetBounds,
            "wait_for" => WindowAction.WaitFor,
            "move_to_monitor" => WindowAction.MoveToMonitor,
            "get_state" => WindowAction.GetState,
            "wait_for_state" => WindowAction.WaitForState,
            _ => null,
        };
    }

}
