using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Window;

/// <summary>
/// Main window management service that orchestrates window operations.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowService
{
    private readonly WindowEnumerator _enumerator;
    private readonly WindowActivator _activator;
    private readonly MonitorService _monitorService;
    private readonly SecureDesktopDetector _secureDesktopDetector;
    private readonly WindowConfiguration _configuration;
    private readonly WindowOperationLogger? _logger;
    private readonly UIAutomationService? _automationService;

    /// <summary>
    /// Timeout for waiting for save dialogs to appear after WM_CLOSE.
    /// </summary>
    private static readonly TimeSpan SaveDialogTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Polling interval for dialog detection retry loop.
    /// </summary>
    private static readonly TimeSpan SaveDialogPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowService"/> class.
    /// </summary>
    /// <param name="enumerator">Window enumerator service.</param>
    /// <param name="activator">Window activator service.</param>
    /// <param name="monitorService">Monitor service for multi-monitor support.</param>
    /// <param name="secureDesktopDetector">Secure desktop detector.</param>
    /// <param name="configuration">Window configuration.</param>
    /// <param name="automationService">UI automation service for dialog dismissal.</param>
    /// <param name="logger">Optional operation logger.</param>
    public WindowService(
        WindowEnumerator enumerator,
        WindowActivator activator,
        MonitorService monitorService,
        SecureDesktopDetector secureDesktopDetector,
        WindowConfiguration configuration,
        UIAutomationService? automationService = null,
        WindowOperationLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(enumerator);
        ArgumentNullException.ThrowIfNull(activator);
        ArgumentNullException.ThrowIfNull(monitorService);
        ArgumentNullException.ThrowIfNull(secureDesktopDetector);
        ArgumentNullException.ThrowIfNull(configuration);

        _enumerator = enumerator;
        _activator = activator;
        _monitorService = monitorService;
        _secureDesktopDetector = secureDesktopDetector;
        _configuration = configuration;
        _automationService = automationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> ListWindowsAsync(
        string? filter = null,
        bool useRegex = false,
        bool includeAllDesktops = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var windows = await _enumerator.EnumerateWindowsAsync(
                filter, useRegex, includeAllDesktops, cancellationToken);

            _logger?.LogWindowOperation("list", success: true, windowCount: windows.Count);

            return WindowManagementResult.CreateListSuccess(windows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWindowOperation("list", success: false, errorMessage: ex.Message);
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.EnumerationFailed,
                $"Failed to enumerate windows: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> FindWindowAsync(
        string title,
        bool useRegex = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var windows = await _enumerator.FindWindowsAsync(title, useRegex, cancellationToken);

            _logger?.LogWindowOperation("find", success: true, windowCount: windows.Count, filter: title);

            return WindowManagementResult.CreateListSuccess(windows);
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            _logger?.LogWindowOperation("find", success: false, errorMessage: ex.Message);
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidRegexPattern,
                $"Invalid regex pattern: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWindowOperation("find", success: false, errorMessage: ex.Message);
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.EnumerationFailed,
                $"Failed to find windows: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> ActivateWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            _logger?.LogWindowOperation("activate", success: false, errorMessage: "Secure desktop active");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SecureDesktopActive,
                "Cannot activate window while secure desktop (UAC prompt or lock screen) is active");
        }

        // Get current window info to check if elevated
        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        if (windowInfo.IsElevated)
        {
            _logger?.LogWindowOperation("activate", success: false, errorMessage: "Elevated window");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.ElevatedWindowActive,
                "Cannot activate elevated (admin) window from non-elevated process");
        }

        // Attempt activation
        bool success = await _activator.ActivateWindowAsync(handle, useFallbackStrategies: true, cancellationToken);

        if (!success)
        {
            _logger?.LogWindowOperation("activate", success: false, handle: handle);
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.ActivationFailed,
                "Failed to activate window after trying all strategies");
        }

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        _logger?.LogWindowOperation("activate", success: true, handle: handle, windowTitle: updatedInfo?.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> GetForegroundWindowAsync(
        CancellationToken cancellationToken = default)
    {
        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            _logger?.LogWindowOperation("get_foreground", success: false, errorMessage: "Secure desktop active");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SecureDesktopActive,
                "Secure desktop (UAC prompt or lock screen) is active");
        }

        var foregroundHandle = _activator.GetForegroundWindow();
        if (foregroundHandle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                "No foreground window found");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                "Foreground window info not available");
        }

        _logger?.LogWindowOperation("get_foreground", success: true, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> MinimizeWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        return await ChangeWindowStateAsync(handle, WindowAction.Minimize, NativeConstants.SW_MINIMIZE, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> MaximizeWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        return await ChangeWindowStateAsync(handle, WindowAction.Maximize, NativeConstants.SW_MAXIMIZE, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> RestoreWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        return await ChangeWindowStateAsync(handle, WindowAction.Restore, NativeConstants.SW_RESTORE, cancellationToken);
    }

    /// <summary>
    /// Closes a window by sending WM_CLOSE message.
    /// </summary>
    /// <param name="handle">The window handle.</param>
    /// <param name="discardChanges">If true, automatically dismisses save confirmation dialogs by clicking 'Don't Save'.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the close operation.</returns>
    public async Task<WindowManagementResult> CloseWindowAsync(
        nint handle,
        bool discardChanges = false,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        // Verify window exists
        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        // Send WM_CLOSE message
        bool posted = NativeMethods.PostMessage(handle, NativeConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        if (!posted)
        {
            _logger?.LogWindowOperation("close", success: false, handle: handle, errorMessage: "PostMessage failed");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.CloseFailed,
                "Failed to send close message to window");
        }

        // If discardChanges is true, wait for and dismiss any save confirmation dialogs
        if (discardChanges && _automationService != null)
        {
            await DismissSaveDialogAsync(handle, cancellationToken);
        }

        _logger?.LogWindowOperation("close", success: true, handle: handle, windowTitle: windowInfo.Title);

        // Note: We return the window info before close. The window may prompt for save, etc.
        return WindowManagementResult.CreateWindowSuccess(windowInfo);
    }

    /// <summary>
    /// Attempts to dismiss a save confirmation dialog by clicking "Don't Save" button.
    /// </summary>
    private async Task DismissSaveDialogAsync(nint parentHandle, CancellationToken cancellationToken)
    {
        // If automation service is not available, we can't dismiss dialogs
        if (_automationService == null)
        {
            _logger?.LogWindowOperation("close_dialog_dismiss", success: false, handle: parentHandle,
                errorMessage: "UIAutomationService not available for dialog dismissal");
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Give the dialog time to appear
        await Task.Delay(SaveDialogPollInterval, cancellationToken);

        while (stopwatch.Elapsed < SaveDialogTimeout && !cancellationToken.IsCancellationRequested)
        {
            // Check if the parent window is still valid (it may have closed without a dialog)
            if (!NativeMethods.IsWindow(parentHandle))
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Window closed without save dialog");
                return;
            }

            // Try to find and click "Don't Save" button using UI Automation
            // We search for common "Don't Save" button patterns:
            // - Windows 11: Button with AutomationId "SecondaryButton"
            // - Windows 10: Button with AutomationId "CommandButton_7"
            // - Generic: Button with name containing "Don't Save" or "No"
            //
            // Note: We do NOT pass WindowHandle because the save dialog is a separate
            // modal window (not a child of the parent in UI Automation terms).
            // By omitting WindowHandle, the search uses the foreground window which
            // will be the dialog when it appears (FlaUI/pywinauto pattern).

            // Try Windows 11 pattern first (SecondaryButton)
            var result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                AutomationId = "SecondaryButton",
                ControlType = "Button",
                TimeoutMs = 0
            }, cancellationToken);

            if (result.Success)
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Dismissed dialog using SecondaryButton");
                return;
            }

            // Try Windows 10 pattern (CommandButton_7)
            result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                AutomationId = "CommandButton_7",
                ControlType = "Button",
                TimeoutMs = 0
            }, cancellationToken);

            if (result.Success)
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Dismissed dialog using CommandButton_7");
                return;
            }

            // Try generic pattern: button containing "Don't" (handles "Don't Save", "Don't save", etc.)
            // Note: We use "t save" to avoid apostrophe encoding issues (Unicode ' vs ASCII ')
            result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                NameContains = "t save",
                ControlType = "Button",
                TimeoutMs = 0
            }, cancellationToken);

            if (result.Success)
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Dismissed dialog using Don't Save button");
                return;
            }

            // Also try "No" button (common in some dialogs)
            // Note: MessageBox buttons use "&No" with ampersand accelerator key
            result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                Name = "&No",
                ControlType = "Button",
                TimeoutMs = 0
            }, cancellationToken);

            if (result.Success)
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Dismissed dialog using &No button");
                return;
            }

            // Also try without ampersand for non-MessageBox dialogs
            result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                Name = "No",
                ControlType = "Button",
                TimeoutMs = 0
            }, cancellationToken);

            if (result.Success)
            {
                _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
                    errorMessage: "Dismissed dialog using No button");
                return;
            }

            await Task.Delay(SaveDialogPollInterval, cancellationToken);
        }

        // If we reach here, no dialog was found or dismissed - that's okay, the window may have closed normally
        _logger?.LogWindowOperation("close_dialog_dismiss", success: true, handle: parentHandle,
            errorMessage: "No save dialog found within timeout");
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> MoveWindowAsync(
        nint handle,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        // Use SetWindowPos with SWP_NOSIZE to move without resizing
        bool success = NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            x, y,
            0, 0,  // Ignored with SWP_NOSIZE
            NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

        if (!success)
        {
            _logger?.LogWindowOperation("move", success: false, handle: handle, errorMessage: "SetWindowPos failed");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MoveFailed,
                "Failed to move window");
        }

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        _logger?.LogWindowOperation("move", success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> ResizeWindowAsync(
        nint handle,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        if (width <= 0 || height <= 0)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidCoordinates,
                $"Invalid dimensions: width={width}, height={height}. Both must be positive.");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        // Use SetWindowPos with SWP_NOMOVE to resize without moving
        bool success = NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            0, 0,  // Ignored with SWP_NOMOVE
            width, height,
            NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

        if (!success)
        {
            _logger?.LogWindowOperation("resize", success: false, handle: handle, errorMessage: "SetWindowPos failed");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.ResizeFailed,
                "Failed to resize window");
        }

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        _logger?.LogWindowOperation("resize", success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> SetBoundsAsync(
        nint handle,
        WindowBounds bounds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidCoordinates,
                $"Invalid dimensions: width={bounds.Width}, height={bounds.Height}. Both must be positive.");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        // Use SetWindowPos for atomic move+resize
        bool success = NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            bounds.X, bounds.Y,
            bounds.Width, bounds.Height,
            NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

        if (!success)
        {
            _logger?.LogWindowOperation("set_bounds", success: false, handle: handle, errorMessage: "SetWindowPos failed");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MoveFailed,
                "Failed to set window bounds");
        }

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        _logger?.LogWindowOperation("set_bounds", success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> WaitForWindowAsync(
        string title,
        bool useRegex = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(title))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "Title is required for wait_for operation");
        }

        int timeout = timeoutMs ?? _configuration.WaitForTimeoutMs;
        int pollInterval = 250; // Poll every 250ms
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var windows = await _enumerator.FindWindowsAsync(title, useRegex, cancellationToken);
                if (windows.Count > 0)
                {
                    _logger?.LogWindowOperation("wait_for", success: true, windowTitle: windows[0].Title, filter: title);
                    return WindowManagementResult.CreateWindowSuccess(windows[0]);
                }
            }
            catch (System.Text.RegularExpressions.RegexParseException ex)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.InvalidRegexPattern,
                    $"Invalid regex pattern: {ex.Message}");
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        _logger?.LogWindowOperation("wait_for", success: false, filter: title, errorMessage: "Timeout");

        return WindowManagementResult.CreateFailure(
            WindowManagementErrorCode.Timeout,
            $"Timeout waiting for window matching '{title}' after {timeout}ms");
    }

    private async Task<WindowManagementResult> ChangeWindowStateAsync(
        nint handle,
        WindowAction action,
        int showCommand,
        CancellationToken cancellationToken)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        NativeMethods.ShowWindow(handle, showCommand);

        // Brief delay to allow window to settle
        await Task.Delay(50, cancellationToken);

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        string actionName = action.ToString().ToLowerInvariant();
        _logger?.LogWindowOperation(actionName, success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> MoveToMonitorAsync(
        nint handle,
        int monitorIndex,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        // Get target monitor info
        var targetMonitor = _monitorService.GetMonitor(monitorIndex);
        if (targetMonitor == null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidCoordinates,
                $"Invalid monitor index: {monitorIndex}. Available monitors: 0-{_monitorService.MonitorCount - 1}");
        }

        // Get current window info
        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        // Calculate new position: center the window on the target monitor
        // while preserving the window size
        // Note: Width/Height are the logical dimensions that match the coordinate system
        int newX = targetMonitor.X + (targetMonitor.Width - windowInfo.Bounds.Width) / 2;
        int newY = targetMonitor.Y + (targetMonitor.Height - windowInfo.Bounds.Height) / 2;

        // Ensure the window fits within the monitor bounds
        if (newX < targetMonitor.X)
        {
            newX = targetMonitor.X;
        }

        if (newY < targetMonitor.Y)
        {
            newY = targetMonitor.Y;
        }

        // If window is larger than monitor, position at monitor origin
        if (windowInfo.Bounds.Width > targetMonitor.Width)
        {
            newX = targetMonitor.X;
        }

        if (windowInfo.Bounds.Height > targetMonitor.Height)
        {
            newY = targetMonitor.Y;
        }

        // Move the window
        bool success = NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            newX, newY,
            0, 0,  // Don't change size
            NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);

        if (!success)
        {
            _logger?.LogWindowOperation("move_to_monitor", success: false, handle: handle, errorMessage: "SetWindowPos failed");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MoveFailed,
                "Failed to move window to target monitor");
        }

        // Get updated window info
        var updatedInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);

        _logger?.LogWindowOperation("move_to_monitor", success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(updatedInfo ?? windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> GetWindowStateAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            _logger?.LogWindowOperation("get_state", success: false, errorMessage: "Secure desktop active");
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SecureDesktopActive,
                "Secure desktop (UAC prompt or lock screen) is active");
        }

        var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        if (windowInfo is null)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Window with handle {handle} not found");
        }

        _logger?.LogWindowOperation("get_state", success: true, handle: handle, windowTitle: windowInfo.Title);

        return WindowManagementResult.CreateWindowSuccess(windowInfo);
    }

    /// <inheritdoc/>
    public async Task<WindowManagementResult> WaitForStateAsync(
        nint handle,
        WindowState targetState,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.InvalidHandle,
                "Invalid window handle (zero)");
        }

        var timeout = timeoutMs ?? 5000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int pollIntervalMs = 100;

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var windowInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
            if (windowInfo is null)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.WindowNotFound,
                    $"Window with handle {handle} not found while waiting for state");
            }

            if (windowInfo.State == targetState)
            {
                _logger?.LogWindowOperation("wait_for_state", success: true, handle: handle, windowTitle: windowInfo.Title);
                return WindowManagementResult.CreateWindowSuccess(windowInfo);
            }

            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        // Timeout - get final state for error message
        var finalInfo = await _enumerator.GetWindowInfoAsync(handle, cancellationToken);
        var currentState = finalInfo?.State.ToString() ?? "unknown";

        _logger?.LogWindowOperation("wait_for_state", success: false, handle: handle, errorMessage: $"Timeout waiting for state {targetState}");

        return WindowManagementResult.CreateFailure(
            WindowManagementErrorCode.Timeout,
            $"Timeout after {timeout}ms waiting for window to reach state '{targetState}'. Current state: '{currentState}'");
    }

    /// <inheritdoc/>
    public Task<Models.WindowInfo?> GetWindowInfoAsync(
        nint handle,
        CancellationToken cancellationToken = default)
    {
        return _enumerator.GetWindowInfoAsync(handle, cancellationToken);
    }
}
