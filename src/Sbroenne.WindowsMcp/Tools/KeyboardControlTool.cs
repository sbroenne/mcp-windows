using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Serialization;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling keyboard input on Windows.
/// </summary>
[McpServerToolType]
public sealed partial class KeyboardControlTool : IDisposable
{
    private readonly IKeyboardInputService _keyboardInputService;
    private readonly IWindowEnumerator _windowEnumerator;
    private readonly IWindowService _windowService;
    private readonly IElevationDetector _elevationDetector;
    private readonly ISecureDesktopDetector _secureDesktopDetector;
    private readonly KeyboardOperationLogger _logger;
    private readonly KeyboardConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardControlTool"/> class.
    /// </summary>
    /// <param name="keyboardInputService">The keyboard input service.</param>
    /// <param name="windowEnumerator">The window enumerator for getting target window info.</param>
    /// <param name="windowService">The window service for finding and activating windows.</param>
    /// <param name="elevationDetector">The elevation detector.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="logger">The operation logger.</param>
    /// <param name="configuration">The keyboard configuration.</param>
    public KeyboardControlTool(
        IKeyboardInputService keyboardInputService,
        IWindowEnumerator windowEnumerator,
        IWindowService windowService,
        IElevationDetector elevationDetector,
        ISecureDesktopDetector secureDesktopDetector,
        KeyboardOperationLogger logger,
        KeyboardConfiguration configuration)
    {
        _keyboardInputService = keyboardInputService ?? throw new ArgumentNullException(nameof(keyboardInputService));
        _windowEnumerator = windowEnumerator ?? throw new ArgumentNullException(nameof(windowEnumerator));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _elevationDetector = elevationDetector ?? throw new ArgumentNullException(nameof(elevationDetector));
        _secureDesktopDetector = secureDesktopDetector ?? throw new ArgumentNullException(nameof(secureDesktopDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Control keyboard input on Windows. Supports type (text), press (key), key_down, key_up, combo, sequence, release_all, get_keyboard_layout, and wait_for_idle actions.
    /// </summary>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The keyboard action: type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout, or wait_for_idle.</param>
    /// <param name="app">Application window to target by title (partial match). The server automatically finds and activates the window.</param>
    /// <param name="text">Text to type (required for type action).</param>
    /// <param name="key">Key name to press (for press, key_down, key_up, combo actions). Examples: enter, tab, escape, f1, a, ctrl, shift, alt, win, copilot.</param>
    /// <param name="modifiers">Modifier keys: ctrl, shift, alt, win (comma-separated, for press and combo actions).</param>
    /// <param name="repeat">Number of times to repeat key press (default: 1, for press action).</param>
    /// <param name="sequence">JSON array of key sequence items, e.g., [{"key":"ctrl"},{"key":"c"}] (for sequence action).</param>
    /// <param name="interKeyDelayMs">Delay between keys in sequence (milliseconds).</param>
    /// <param name="expectedWindowTitle">Expected window title (partial match). If specified, operation fails if foreground window title doesn't match.</param>
    /// <param name="expectedProcessName">Expected process name. If specified, operation fails if foreground window's process doesn't match.</param>
    /// <param name="clearFirst">For type action only: If true, clears the current field content before typing by sending Ctrl+A (select all) followed by the new text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the keyboard operation including success status and operation details.</returns>
    [McpServerTool(Name = "keyboard_control", Title = "Keyboard Control", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Keyboard input to the CURRENTLY FOCUSED window/element. Best for: hotkeys (Win+R, Ctrl+S, Alt+Tab), special keys (Enter, Escape, Tab, arrows), typing into dialogs you just opened (e.g., Run dialog after Win+R). For typing text into a SPECIFIC UI element (e.g., Notepad's document area), use ui_automation(action='type', app='...') instead. Actions: type, press, key_down, key_up, sequence, release_all, get_keyboard_layout, wait_for_idle.")]
    [return: Description("The result includes success status, operation details, and 'target_window' (handle, title, process_name) showing which window received the input. If expectedWindowTitle/expectedProcessName was specified but didn't match, success=false with error_code='wrong_target_window'.")]
    public async Task<KeyboardControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The keyboard action: type, press, key_down, key_up, sequence, release_all, get_keyboard_layout, wait_for_idle")] string action,
        [Description("Application window to target by title (partial match, case-insensitive). Example: app='Visual Studio Code' or app='Notepad'. The server automatically finds and activates the window before the keyboard action.")] string? app = null,
        [Description("Text to type (required for type action)")] string? text = null,
        [Description("Key name to press (for press, key_down, key_up actions). Examples: enter, tab, escape, f1, a, ctrl, shift, alt, win, copilot")] string? key = null,
        [Description("Modifier keys: ctrl, shift, alt, win (comma-separated, for press action)")] string? modifiers = null,
        [Description("Number of times to repeat key press (default: 1, for press action)")] int repeat = 1,
        [Description("JSON array of key sequence items, e.g., [{\"key\":\"ctrl\"},{\"key\":\"c\"}] (for sequence action)")] string? sequence = null,
        [Description("Delay between keys in sequence (milliseconds)")] int? interKeyDelayMs = null,
        [Description("Expected window title (partial match). If specified, operation fails with 'wrong_target_window' if the foreground window title doesn't contain this text. Use this to prevent sending input to the wrong application.")] string? expectedWindowTitle = null,
        [Description("Expected process name (e.g., 'Code', 'chrome', 'notepad'). If specified, operation fails with 'wrong_target_window' if the foreground window's process doesn't match. Use this to prevent sending input to the wrong application.")] string? expectedProcessName = null,
        [Description("For 'type' action only: If true, clears the current field content before typing by sending Ctrl+A (select all) followed by the new text. Default is false.")] bool clearFirst = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = KeyboardOperationLogger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("KeyboardControl");
        clientLogger?.LogKeyboardOperationStarted(action ?? "null");

        _logger.LogOperationStart(correlationId, action ?? "null");

        // Create a linked token source with the configured timeout
        using var timeoutCts = new CancellationTokenSource(_configuration.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        try
        {
            // Resolve 'app' parameter to find and activate the target window
            if (!string.IsNullOrWhiteSpace(app))
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

                    var result = KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.WrongTargetWindow,
                        $"No window found matching app='{app}'. {suggestion}");
                    _logger.LogOperationFailure(correlationId, action ?? "null", result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                    return result;
                }

                // Activate the window before performing keyboard action
                var resolvedWindow = findResult.Windows![0];
                await _windowService.ActivateWindowAsync(nint.Parse(resolvedWindow.Handle), linkedToken);
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
                var result = KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.InvalidAction,
                    "Action parameter is required");
                _logger.LogOperationFailure(correlationId, "null", result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            var keyboardAction = ParseAction(action);
            if (keyboardAction == null)
            {
                var result = KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.InvalidAction,
                    $"Unknown action: '{action}'. Valid actions are: type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout, wait_for_idle");
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            KeyboardControlResult operationResult;

            switch (keyboardAction.Value)
            {
                case KeyboardAction.Type:
                    operationResult = await HandleTypeAsync(text, clearFirst, linkedToken);
                    break;

                case KeyboardAction.Press:
                    operationResult = await HandlePressAsync(key, modifiers, repeat, linkedToken);
                    break;

                case KeyboardAction.KeyDown:
                    operationResult = await HandleKeyDownAsync(key, linkedToken);
                    break;

                case KeyboardAction.KeyUp:
                    operationResult = await HandleKeyUpAsync(key, linkedToken);
                    break;

                case KeyboardAction.Sequence:
                    operationResult = await HandleSequenceAsync(sequence, interKeyDelayMs, linkedToken);
                    break;

                case KeyboardAction.ReleaseAll:
                    operationResult = await HandleReleaseAllAsync(linkedToken);
                    break;

                case KeyboardAction.GetKeyboardLayout:
                    operationResult = await HandleGetKeyboardLayoutAsync(linkedToken);
                    break;

                case KeyboardAction.WaitForIdle:
                    operationResult = await HandleWaitForIdleAsync(linkedToken);
                    break;

                default:
                    operationResult = KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            stopwatch.Stop();

            // For successful operations that send input, attach the target window info
            // This helps LLM agents verify the input went to the correct window
            if (operationResult.Success && keyboardAction.Value != KeyboardAction.GetKeyboardLayout && keyboardAction.Value != KeyboardAction.WaitForIdle)
            {
                operationResult = await AttachTargetWindowInfoAsync(operationResult, linkedToken);
            }

            if (operationResult.Success)
            {
                LogSuccess(correlationId, action, operationResult, stopwatch.ElapsedMilliseconds);
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
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.OperationTimeout,
                $"Operation timed out after {_configuration.TimeoutMs}ms");
            _logger.LogOperationFailure(correlationId, action ?? "null", errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested by caller, not timeout
            stopwatch.Stop();
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.UnexpectedError,
                "Operation was cancelled");
            _logger.LogOperationFailure(correlationId, action ?? "null", errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogOperationException(correlationId, action ?? "null", ex);
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.UnexpectedError,
                $"An unexpected error occurred: {ex.Message}");
            return errorResult;
        }
    }

    private async Task<KeyboardControlResult> HandleTypeAsync(string? text, bool clearFirst, CancellationToken cancellationToken)
    {
        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        // Check for elevated foreground window
        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        // If clearFirst is true, select all existing content first (Ctrl+A)
        // The new text will replace the selection
        if (clearFirst)
        {
            var selectAllResult = await _keyboardInputService.PressKeyAsync("a", ModifierKey.Ctrl, 1, cancellationToken);
            if (!selectAllResult.Success)
            {
                return KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.SendInputFailed,
                    $"Failed to select all before typing: {selectAllResult.Error}");
            }

            // Small delay to ensure selection is complete
            await Task.Delay(50, cancellationToken);
        }

        // Empty text is valid - returns success with 0 characters
        return await _keyboardInputService.TypeTextAsync(text ?? string.Empty, cancellationToken);
    }

    private async Task<KeyboardControlResult> HandlePressAsync(string? key, string? modifiers, int repeat, CancellationToken cancellationToken)
    {
        // Validate required parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for press action");
        }

        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        // Check for elevated foreground window
        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        var modifierKey = ParseModifiers(modifiers);
        return await _keyboardInputService.PressKeyAsync(key, modifierKey, repeat, cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleKeyDownAsync(string? key, CancellationToken cancellationToken)
    {
        // Validate required parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for key_down action");
        }

        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        // Check for elevated foreground window
        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        return await _keyboardInputService.KeyDownAsync(key, cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleKeyUpAsync(string? key, CancellationToken cancellationToken)
    {
        // Validate required parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for key_up action");
        }

        return await _keyboardInputService.KeyUpAsync(key, cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleSequenceAsync(string? sequenceJson, int? interKeyDelayMs, CancellationToken cancellationToken)
    {
        // Validate required parameter
        if (string.IsNullOrWhiteSpace(sequenceJson))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'sequence' parameter is required for sequence action");
        }

        // Check for secure desktop
        if (_secureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        // Check for elevated foreground window
        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        // Parse the sequence JSON
        IReadOnlyList<KeySequenceItem> sequence;
        try
        {
            sequence = JsonSerializer.Deserialize<List<KeySequenceItem>>(sequenceJson, McpJsonOptions.Default) ?? [];
        }
        catch (JsonException ex)
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidAction,
                $"Invalid sequence JSON: {ex.Message}");
        }

        return await _keyboardInputService.ExecuteSequenceAsync(sequence, interKeyDelayMs, cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleReleaseAllAsync(CancellationToken cancellationToken)
    {
        return await _keyboardInputService.ReleaseAllKeysAsync(cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleGetKeyboardLayoutAsync(CancellationToken cancellationToken)
    {
        return await _keyboardInputService.GetKeyboardLayoutAsync(cancellationToken);
    }

    private async Task<KeyboardControlResult> HandleWaitForIdleAsync(CancellationToken cancellationToken)
    {
        return await _keyboardInputService.WaitForIdleAsync(cancellationToken);
    }

    private static KeyboardAction? ParseAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "type" => KeyboardAction.Type,
            "press" => KeyboardAction.Press,
            "key_down" or "keydown" => KeyboardAction.KeyDown,
            "key_up" or "keyup" => KeyboardAction.KeyUp,
            "sequence" => KeyboardAction.Sequence,
            "release_all" or "releaseall" => KeyboardAction.ReleaseAll,
            "get_keyboard_layout" or "getkeyboardlayout" or "layout" => KeyboardAction.GetKeyboardLayout,
            "wait_for_idle" or "waitforidle" or "idle" => KeyboardAction.WaitForIdle,
            _ => null
        };
    }

    private static ModifierKey ParseModifiers(string? modifiers)
    {
        if (string.IsNullOrWhiteSpace(modifiers))
        {
            return ModifierKey.None;
        }

        var result = ModifierKey.None;
        var parts = modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            result |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                "alt" => ModifierKey.Alt,
                "win" or "windows" or "meta" => ModifierKey.Win,
                _ => ModifierKey.None
            };
        }

        return result;
    }

    /// <summary>
    /// Logs successful operation based on result type.
    /// </summary>
    private void LogSuccess(string correlationId, string action, KeyboardControlResult result, long durationMs)
    {
        if (result.CharactersTyped.HasValue)
        {
            _logger.LogTypeSuccess(correlationId, result.CharactersTyped.Value, durationMs);
        }
        else if (result.SequenceLength.HasValue)
        {
            _logger.LogSequenceSuccess(correlationId, result.SequenceLength.Value, durationMs);
        }
        else if (result.KeyboardLayout != null)
        {
            _logger.LogLayoutQuerySuccess(correlationId, result.KeyboardLayout.LanguageTag, durationMs);
        }
        else if (result.KeyPressed != null)
        {
            _logger.LogPressSuccess(correlationId, result.KeyPressed, null, durationMs);
        }
        else
        {
            // For release_all or other operations
            _logger.LogPressSuccess(correlationId, action, null, durationMs);
        }
    }

    /// <summary>
    /// Checks if the foreground window belongs to an elevated process.
    /// </summary>
    /// <returns>True if the foreground window is elevated; otherwise, false.</returns>
    private bool IsForegroundWindowElevated()
    {
        // Get current cursor position to check elevation
        NativeMethods.GetCursorPos(out var cursorPos);
        return _elevationDetector.IsTargetElevated(cursorPos.X, cursorPos.Y);
    }

    /// <summary>
    /// Verifies that the foreground window matches the expected target before sending input.
    /// This prevents input from being sent to the wrong application.
    /// </summary>
    /// <param name="expectedTitle">Expected window title (partial match, case-insensitive).</param>
    /// <param name="expectedProcessName">Expected process name (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result if window matches, failure result with WrongTargetWindow error if not.</returns>
    private async Task<KeyboardControlResult> VerifyTargetWindowAsync(string? expectedTitle, string? expectedProcessName, CancellationToken cancellationToken)
    {
        try
        {
            var foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.WrongTargetWindow,
                    "No foreground window found. Cannot verify target window.");
            }

            var windowInfo = await _windowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.WrongTargetWindow,
                    "Could not retrieve foreground window information.");
            }

            // Check expected title (partial, case-insensitive match)
            if (!string.IsNullOrEmpty(expectedTitle))
            {
                if (string.IsNullOrEmpty(windowInfo.Title) ||
                    !windowInfo.Title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    var result = KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.WrongTargetWindow,
                        $"Foreground window title '{windowInfo.Title}' does not contain expected text '{expectedTitle}'. Aborting to prevent input to wrong window.");
                    return result with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            // Check expected process name (case-insensitive match)
            if (!string.IsNullOrEmpty(expectedProcessName))
            {
                if (string.IsNullOrEmpty(windowInfo.ProcessName) ||
                    !windowInfo.ProcessName.Equals(expectedProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    var result = KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.WrongTargetWindow,
                        $"Foreground window process '{windowInfo.ProcessName}' does not match expected process '{expectedProcessName}'. Aborting to prevent input to wrong window.");
                    return result with { TargetWindow = TargetWindowInfo.FromFullWindowInfo(windowInfo) };
                }
            }

            // Window matches expectations
            return KeyboardControlResult.CreateSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.WrongTargetWindow,
                $"Failed to verify target window: {ex.Message}");
        }
    }

    /// <summary>
    /// Attaches information about the foreground window to the result.
    /// This helps LLM agents verify that input was sent to the correct window.
    /// </summary>
    /// <param name="result">The original result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result with target window info attached.</returns>
    private async Task<KeyboardControlResult> AttachTargetWindowInfoAsync(KeyboardControlResult result, CancellationToken cancellationToken)
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            // Release all held keys on disposal
            _keyboardInputService.ReleaseAllKeysAsync().GetAwaiter().GetResult();

            // Dispose the keyboard input service if it's disposable
            if (_keyboardInputService is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposed = true;
        }
    }
}
