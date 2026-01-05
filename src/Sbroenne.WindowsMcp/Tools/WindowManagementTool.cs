using System.Diagnostics;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
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
    private readonly WindowService _windowService;
    private readonly MonitorService _monitorService;
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
        WindowService windowService,
        MonitorService monitorService,
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
    /// Manage existing windows. Use app tool to launch applications first, then use this tool to manage the windows.
    /// Supports: list, find, activate, minimize, maximize, restore, close, move, resize, and more.
    /// </summary>
    /// <remarks>
    /// To launch apps, use the app tool. This tool manages windows AFTER they exist.
    ///
    /// CLOSE WITHOUT SAVING: Use close action with discardChanges=true to automatically dismiss 'Save?' dialogs.
    /// NOTE: discardChanges only works on English Windows (looks for 'Don't Save' button text).
    /// Example: window_management(action='close', handle='123', discardChanges=true)
    ///
    /// Use move_to_monitor to move a window to a specific monitor:
    /// - Use target='primary_screen' or target='secondary_screen' for easy targeting
    /// - Use monitorIndex for 3+ monitor setups (use screenshot_control action='list_monitors' to find indices)
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The window action to perform: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, move_to_monitor, move_and_activate, or ensure_visible.</param>
    /// <param name="handle">Window handle for actions that target a specific window. Get the handle from the list or find action.</param>
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
    /// <param name="excludeTitle">Window title to exclude from list results (for list action).</param>
    /// <param name="discardChanges">For close action: if true, automatically dismisses 'Save?' dialogs by clicking 'Don't Save'. Only works on English Windows. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the window operation including success status and window information.</returns>
    [McpServerTool(Name = "window_management", Title = "Window Management", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<WindowManagementResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        WindowAction action,
        string? handle = null,
        string? title = null,
        string? filter = null,
        bool regex = false,
        bool includeAllDesktops = false,
        int? x = null,
        int? y = null,
        int? width = null,
        int? height = null,
        int? timeoutMs = null,
        string? target = null,
        int? monitorIndex = null,
        string? state = null,
        string? excludeTitle = null,
        bool discardChanges = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("WindowManagement");
        clientLogger?.LogWindowOperationStarted(action.ToString());

        try
        {
            // Use handle directly - no app resolution (LLMs should call list/find first)
            string? resolvedHandle = handle;

            WindowManagementResult operationResult;

            switch (action)
            {
                case WindowAction.List:
                    operationResult = await HandleListAsync(filter, regex, includeAllDesktops, excludeTitle, cancellationToken);
                    break;

                case WindowAction.Find:
                    operationResult = await HandleFindAsync(title, regex, cancellationToken);
                    break;

                case WindowAction.Activate:
                    operationResult = await HandleActivateAsync(resolvedHandle, cancellationToken);
                    break;

                case WindowAction.GetForeground:
                    operationResult = await HandleGetForegroundAsync(cancellationToken);
                    break;

                case WindowAction.Minimize:
                    operationResult = await HandleMinimizeAsync(resolvedHandle, cancellationToken);
                    break;

                case WindowAction.Maximize:
                    operationResult = await HandleMaximizeAsync(resolvedHandle, cancellationToken);
                    break;

                case WindowAction.Restore:
                    operationResult = await HandleRestoreAsync(resolvedHandle, cancellationToken);
                    break;

                case WindowAction.Close:
                    operationResult = await HandleCloseAsync(resolvedHandle, discardChanges, cancellationToken);
                    break;

                case WindowAction.Move:
                    operationResult = await HandleMoveAsync(resolvedHandle, x, y, cancellationToken);
                    break;

                case WindowAction.Resize:
                    operationResult = await HandleResizeAsync(resolvedHandle, width, height, cancellationToken);
                    break;

                case WindowAction.SetBounds:
                    operationResult = await HandleSetBoundsAsync(resolvedHandle, x, y, width, height, cancellationToken);
                    break;

                case WindowAction.WaitFor:
                    operationResult = await HandleWaitForAsync(title, regex, timeoutMs, cancellationToken);
                    break;

                case WindowAction.MoveToMonitor:
                    operationResult = await HandleMoveToMonitorAsync(resolvedHandle, target, monitorIndex, cancellationToken);
                    break;

                case WindowAction.GetState:
                    operationResult = await HandleGetStateAsync(resolvedHandle, cancellationToken);
                    break;

                case WindowAction.WaitForState:
                    operationResult = await HandleWaitForStateAsync(resolvedHandle, state, timeoutMs, cancellationToken);
                    break;

                case WindowAction.MoveAndActivate:
                    operationResult = await HandleMoveAndActivateAsync(resolvedHandle, x, y, cancellationToken);
                    break;

                case WindowAction.EnsureVisible:
                    operationResult = await HandleEnsureVisibleAsync(resolvedHandle, cancellationToken);
                    break;

                default:
                    operationResult = WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            stopwatch.Stop();

            _logger?.LogWindowOperation(
                action.ToString(),
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
            _logger?.LogWindowOperation(action.ToString(), success: false, errorMessage: errorResult.Error);
            return errorResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger?.LogError(action.ToString(), ex);
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
        string? excludeTitle,
        CancellationToken cancellationToken)
    {
        var result = await _windowService.ListWindowsAsync(filter, useRegex, includeAllDesktops, cancellationToken);

        // Apply excludeTitle filter if specified
        if (result.Success && !string.IsNullOrEmpty(excludeTitle) && result.Windows is not null)
        {
            var filteredWindows = result.Windows
                .Where(w => !w.Title.Contains(excludeTitle, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return WindowManagementResult.CreateListSuccess(filteredWindows);
        }

        return result;
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
                "'title' is required for find action. Example: find(title='Notepad')");
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
        bool discardChanges,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for close action");
        }

        return await _windowService.CloseWindowAsync(handle, discardChanges, cancellationToken);
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
                "'title' is required for wait_for action. Example: wait_for(title='Notepad')");
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

    private async Task<WindowManagementResult> HandleMoveAndActivateAsync(
        string? handleString,
        int? x,
        int? y,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for move_and_activate action");
        }

        if (!x.HasValue || !y.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Both x and y coordinates are required for move_and_activate action");
        }

        // Move the window first
        var moveResult = await _windowService.MoveWindowAsync(handle, x.Value, y.Value, cancellationToken);
        if (!moveResult.Success)
        {
            return moveResult;
        }

        // Then activate it
        var activateResult = await _windowService.ActivateWindowAsync(handle, cancellationToken);
        if (!activateResult.Success)
        {
            return activateResult;
        }

        // Return combined success result with window info from the activate result
        return WindowManagementResult.CreateWindowSuccess(
            activateResult.Window!,
            "Window moved and activated successfully");
    }

    private async Task<WindowManagementResult> HandleEnsureVisibleAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for ensure_visible action");
        }

        // Check if window is minimized and restore it if needed
        if (NativeMethods.IsIconic(handle))
        {
            var restoreResult = await _windowService.RestoreWindowAsync(handle, cancellationToken);
            if (!restoreResult.Success)
            {
                return restoreResult;
            }
        }

        // Then activate it to bring to foreground
        return await _windowService.ActivateWindowAsync(handle, cancellationToken);
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
}
