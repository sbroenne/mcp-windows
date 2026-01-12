using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for managing windows on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public static partial class WindowManagementTool
{
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
    /// <param name="action">The window action to perform: list, find, activate, get_foreground, get_state, wait_for_state, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, move_to_monitor, move_and_activate, or ensure_visible.</param>
    /// <param name="handle">Window handle for actions that target a specific window. Get the handle from the list or find action.</param>
    /// <param name="title">Window title to search for (for find and wait_for). Uses substring match unless regex=true.</param>
    /// <param name="processName">Process name to search for (for find action). More reliable than title - matches 'Notepad', 'chrome', etc. Case-insensitive.</param>
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
    [McpServerTool(Name = "window_management", Title = "Window Management", Destructive = true, OpenWorld = false)]
    public static async partial Task<string> ExecuteAsync(
        WindowAction action,
        [DefaultValue(null)] string? handle,
        [DefaultValue(null)] string? title,
        [DefaultValue(null)] string? processName,
        [DefaultValue(null)] string? filter,
        [DefaultValue(false)] bool regex,
        [DefaultValue(false)] bool includeAllDesktops,
        [DefaultValue(null)] int? x,
        [DefaultValue(null)] int? y,
        [DefaultValue(null)] int? width,
        [DefaultValue(null)] int? height,
        [DefaultValue(null)] int? timeoutMs,
        [DefaultValue(null)] string? target,
        [DefaultValue(null)] int? monitorIndex,
        [DefaultValue(null)] string? state,
        [DefaultValue(null)] string? excludeTitle,
        [DefaultValue(false)] bool discardChanges,
        CancellationToken cancellationToken)
    {
        try
        {
            WindowManagementResult operationResult;

            switch (action)
            {
                case WindowAction.List:
                    operationResult = await HandleListAsync(filter, regex, includeAllDesktops, excludeTitle, cancellationToken);
                    break;

                case WindowAction.Find:
                    operationResult = await HandleFindAsync(title, processName, regex, cancellationToken);
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
                    operationResult = await HandleCloseAsync(handle, discardChanges, cancellationToken);
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

                case WindowAction.MoveAndActivate:
                    operationResult = await HandleMoveAndActivateAsync(handle, x, y, cancellationToken);
                    break;

                case WindowAction.EnsureVisible:
                    operationResult = await HandleEnsureVisibleAsync(handle, cancellationToken);
                    break;

                default:
                    operationResult = WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            return JsonSerializer.Serialize(operationResult, WindowsToolsBase.JsonOptions);
        }
        catch (OperationCanceledException)
        {
            return JsonSerializer.Serialize(
                WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.Timeout,
                    "Operation was cancelled"),
                WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.SerializeToolError("window_management", ex);
        }
    }

    private static async Task<WindowManagementResult> HandleListAsync(
        string? filter,
        bool useRegex,
        bool includeAllDesktops,
        string? excludeTitle,
        CancellationToken cancellationToken)
    {
        var result = await WindowsToolsBase.WindowService.ListWindowsAsync(filter, useRegex, includeAllDesktops, cancellationToken);

        if (result.Success && !string.IsNullOrEmpty(excludeTitle) && result.Windows is not null)
        {
            var filteredWindows = result.Windows
                .Where(w => !w.Title.Contains(excludeTitle, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return WindowManagementResult.CreateListSuccess(filteredWindows);
        }

        return result;
    }

    private static async Task<WindowManagementResult> HandleFindAsync(
        string? title,
        string? processName,
        bool useRegex,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(processName))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "'title' or 'processName' is required for find action. Use processName for exact app matching (e.g., processName='Notepad').");
        }

        return await WindowsToolsBase.WindowService.FindWindowAsync(title, processName, useRegex, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleActivateAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for activate action");
        }

        return await WindowsToolsBase.WindowService.ActivateWindowAsync(handle, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleGetForegroundAsync(CancellationToken cancellationToken)
    {
        return await WindowsToolsBase.WindowService.GetForegroundWindowAsync(cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleMinimizeAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for minimize action");
        }

        return await WindowsToolsBase.WindowService.MinimizeWindowAsync(handle, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleMaximizeAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for maximize action");
        }

        return await WindowsToolsBase.WindowService.MaximizeWindowAsync(handle, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleRestoreAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for restore action");
        }

        return await WindowsToolsBase.WindowService.RestoreWindowAsync(handle, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleCloseAsync(
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

        return await WindowsToolsBase.WindowService.CloseWindowAsync(handle, discardChanges, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleMoveAsync(
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

        return await WindowsToolsBase.WindowService.MoveWindowAsync(handle, x.Value, y.Value, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleResizeAsync(
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

        return await WindowsToolsBase.WindowService.ResizeWindowAsync(handle, width.Value, height.Value, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleSetBoundsAsync(
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

        return await WindowsToolsBase.WindowService.SetBoundsAsync(handle, bounds, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleWaitForAsync(
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

        return await WindowsToolsBase.WindowService.WaitForWindowAsync(title, useRegex, timeoutMs, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleMoveToMonitorAsync(
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

            var targetMonitor = parsedTarget.Value switch
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
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidCoordinates,
                    errorMessage);
            }

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

        if (!resolvedMonitorIndex.HasValue)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Either 'target' or 'monitorIndex' is required for move_to_monitor action. Use target='primary_screen' or target='secondary_screen'.");
        }

        return await WindowsToolsBase.WindowService.MoveToMonitorAsync(handle, resolvedMonitorIndex.Value, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleGetStateAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for get_state action");
        }

        return await WindowsToolsBase.WindowService.GetWindowStateAsync(handle, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleWaitForStateAsync(
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

        return await WindowsToolsBase.WindowService.WaitForStateAsync(handle, targetState.Value, timeoutMs, cancellationToken);
    }

    private static async Task<WindowManagementResult> HandleMoveAndActivateAsync(
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

        var moveResult = await WindowsToolsBase.WindowService.MoveWindowAsync(handle, x.Value, y.Value, cancellationToken);
        if (!moveResult.Success)
        {
            return moveResult;
        }

        var activateResult = await WindowsToolsBase.WindowService.ActivateWindowAsync(handle, cancellationToken);
        if (!activateResult.Success)
        {
            return activateResult;
        }

        return WindowManagementResult.CreateWindowSuccess(
            activateResult.Window!,
            "Window moved and activated successfully");
    }

    private static async Task<WindowManagementResult> HandleEnsureVisibleAsync(
        string? handleString,
        CancellationToken cancellationToken)
    {
        if (!WindowHandleParser.TryParse(handleString, out nint handle))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Valid handle is required for ensure_visible action");
        }

        if (NativeMethods.IsIconic(handle))
        {
            var restoreResult = await WindowsToolsBase.WindowService.RestoreWindowAsync(handle, cancellationToken);
            if (!restoreResult.Success)
            {
                return restoreResult;
            }
        }

        return await WindowsToolsBase.WindowService.ActivateWindowAsync(handle, cancellationToken);
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

    private enum MonitorTarget
    {
        PrimaryScreen,
        SecondaryScreen
    }
}