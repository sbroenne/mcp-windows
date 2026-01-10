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
using Sbroenne.WindowsMcp.Utilities;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling keyboard input on Windows.
/// </summary>
[McpServerToolType]
public sealed partial class KeyboardControlTool : IDisposable
{
    private const int UnspecifiedInt = int.MinValue;

    private readonly KeyboardInputService _keyboardInputService;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly ElevationDetector _elevationDetector;
    private readonly SecureDesktopDetector _secureDesktopDetector;
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
        KeyboardInputService keyboardInputService,
        WindowEnumerator windowEnumerator,
        WindowService windowService,
        ElevationDetector elevationDetector,
        SecureDesktopDetector secureDesktopDetector,
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
    /// ⚠️ TO SAVE FILES: STOP! Use ui_file(windowHandle, filePath) instead. keyboard_control does NOT handle Save As dialogs.
    /// Sends keyboard input to a specific window. The window is activated before input is sent.
    /// Best for: typing text, hotkeys (key='s', modifiers='ctrl'), special keys.
    /// For typing into a specific UI element by name/automationId, use ui_type instead.
    /// </summary>
    /// <remarks>
    /// Supports type (text), press (key), key_down, key_up, combo, sequence, release_all, get_keyboard_layout, and wait_for_idle actions. WARNING: Do NOT put modifiers in the 'key' parameter (e.g., 'Ctrl+S' is WRONG). Use key='s', modifiers='ctrl'. FOR SAVE: Use ui_file tool.
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="windowHandle">Window handle as decimal string (from app() or window_management 'find'). REQUIRED - ensures input goes to the correct window.</param>
    /// <param name="action">The keyboard action: type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout, or wait_for_idle.</param>
    /// <param name="text">Text to type (required for type action).</param>
    /// <param name="key">The MAIN key to press (for press, key_down, key_up actions). Examples: enter, tab, escape, f1, a, s, c, v, copilot. For Ctrl+S, this is 's' (not 'ctrl').</param>
    /// <param name="modifiers">Modifier keys HELD during the key press: ctrl, shift, alt, win (comma-separated). For Ctrl+S: key='s', modifiers='ctrl'. For Ctrl+Shift+S: key='s', modifiers='ctrl,shift'.</param>
    /// <param name="repeat">Number of times to repeat key press (default: 1, for press action).</param>
    /// <param name="sequence">JSON array for sequence action. Format: [{"key":"f","modifiers":"alt"},{"key":"s"}] for Alt+F then S. Modifiers: "ctrl", "shift", "alt", "win" (or numbers: 1,2,4,8).</param>
    /// <param name="interKeyDelayMs">Delay between keys in sequence (milliseconds).</param>
    /// <param name="clearFirst">For type action only: If true, clears the current field content before typing by sending Ctrl+A (select all) followed by the new text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result includes success status, operation details, and 'target_window' (handle, title, process_name) showing which window received the input.</returns>
    [McpServerTool(Name = "keyboard_control", Title = "Keyboard Control", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<KeyboardControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        string windowHandle,
        KeyboardAction action,
        string? text = null,
        string? key = null,
        string? modifiers = null,
        int repeat = 1,
        string? sequence = null,
        int interKeyDelayMs = UnspecifiedInt,
        bool clearFirst = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Validate windowHandle is provided
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "windowHandle is required. Get it from app() or window_management(action='find').");
        }

        // Parse windowHandle
        if (!long.TryParse(windowHandle, out var handleValue) || handleValue == 0)
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidAction,
                $"Invalid windowHandle '{windowHandle}'. Must be a non-zero decimal number from app() or window_management.");
        }

        var handle = new IntPtr(handleValue);

        var correlationId = KeyboardOperationLogger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("KeyboardControl");
        clientLogger?.LogKeyboardOperationStarted(action.ToString());

        _logger.LogOperationStart(correlationId, action.ToString());

        // Create a linked token source with the configured timeout
        using var timeoutCts = new CancellationTokenSource(_configuration.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        try
        {
            // Activate the target window before sending keyboard input
            var activationResult = await _windowService.ActivateWindowAsync(handle, linkedToken);
            if (!activationResult.Success)
            {
                _logger.LogOperationFailure(correlationId, action.ToString(), "WindowActivationFailed", activationResult.Error ?? "Failed to activate window", stopwatch.ElapsedMilliseconds);
                return KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.WrongTargetWindow,
                    $"Failed to activate window {windowHandle}: {activationResult.Error}");
            }

            // Small delay to let the window settle after activation
            await Task.Delay(50, linkedToken);

            KeyboardControlResult operationResult;

            switch (action)
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
                    operationResult = await HandleSequenceAsync(
                        sequence,
                        interKeyDelayMs == UnspecifiedInt ? null : interKeyDelayMs,
                        linkedToken);
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
            if (operationResult.Success && action != KeyboardAction.GetKeyboardLayout && action != KeyboardAction.WaitForIdle)
            {
                operationResult = await AttachTargetWindowInfoAsync(operationResult, linkedToken);
            }

            if (operationResult.Success)
            {
                LogSuccess(correlationId, action.ToString(), operationResult, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogOperationFailure(correlationId, action.ToString(), operationResult.ErrorCode.ToString(), operationResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            }

            return operationResult;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.OperationTimeout,
                $"Operation timed out after {_configuration.TimeoutMs}ms");
            _logger.LogOperationFailure(correlationId, action.ToString(), errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested by caller, not timeout
            stopwatch.Stop();
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.UnexpectedError,
                "Operation was cancelled");
            _logger.LogOperationFailure(correlationId, action.ToString(), errorResult.ErrorCode.ToString(), errorResult.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
            return errorResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogOperationException(correlationId, action.ToString(), ex);
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

        // Normalize Windows file paths: convert forward slashes to backslashes
        // This handles cases where LLMs provide paths like D:/folder/file.txt
        var normalizedText = PathNormalizer.NormalizeWindowsPath(text);

        // Empty text is valid - returns success with 0 characters
        return await _keyboardInputService.TypeTextAsync(normalizedText, cancellationToken);
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
        var result = await _keyboardInputService.PressKeyAsync(key, modifierKey, repeat, cancellationToken);

        // Add hints for common save/file operations to guide LLMs toward dedicated tools
        if (result.Success)
        {
            result = AddFileSaveHintIfNeeded(key, modifierKey, result);
        }

        return result;
    }

    /// <summary>
    /// Adds a hint to the result if the key combination is a file save operation.
    /// This guides LLMs to use the dedicated ui_file tool for reliable file operations.
    /// </summary>
    private static KeyboardControlResult AddFileSaveHintIfNeeded(string key, ModifierKey modifiers, KeyboardControlResult result)
    {
        var keyLower = key.ToLowerInvariant();
        var isCtrlS = keyLower == "s" && modifiers.HasFlag(ModifierKey.Ctrl);
        var isAltF = keyLower == "f" && modifiers.HasFlag(ModifierKey.Alt);

        if (isCtrlS)
        {
            return result with
            {
                Hint = "TIP: For more reliable file saving, use ui_file(action='save', windowHandle, filePath) which handles Save dialogs automatically. Keyboard shortcuts may not work in all applications or may open dialogs that need additional handling."
            };
        }

        if (isAltF)
        {
            return result with
            {
                Hint = "TIP: For file operations, use ui_file tool instead of menu navigation. ui_file(action='save', windowHandle, filePath) handles Save/SaveAs dialogs automatically and works across different application UI styles."
            };
        }

        return result;
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
            // Provide helpful error message with correct format examples
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidAction,
                $"Invalid sequence JSON: {ex.Message}. " +
                "CORRECT FORMAT: JSON array of objects with 'key' property. " +
                "Examples: " +
                "[{\"key\":\"a\"}] for single key, " +
                "[{\"key\":\"s\",\"modifiers\":1}] for Ctrl+S (modifiers: 1=ctrl, 2=shift, 4=alt, 8=win), " +
                "[{\"key\":\"f\",\"modifiers\":4},{\"key\":\"s\"}] for Alt+F then S (menu navigation). " +
                "TIP: For saving files, use ui_file tool instead.");
        }

        var result = await _keyboardInputService.ExecuteSequenceAsync(sequence, interKeyDelayMs, cancellationToken);

        // Add hints for common save/file operations to guide LLMs toward dedicated tools
        if (result.Success)
        {
            result = AddSequenceFileSaveHintIfNeeded(sequence, result);
        }

        return result;
    }

    /// <summary>
    /// Adds a hint to the result if the key sequence appears to be a file save operation.
    /// This guides LLMs to use the dedicated ui_file tool for reliable file operations.
    /// </summary>
    private static KeyboardControlResult AddSequenceFileSaveHintIfNeeded(IReadOnlyList<KeySequenceItem> sequence, KeyboardControlResult result)
    {
        // Check for Alt+F (File menu) pattern at the start of the sequence
        var hasFileMenuPattern = sequence.Count > 0 &&
            sequence[0].Key.Equals("f", StringComparison.OrdinalIgnoreCase) &&
            sequence[0].Modifiers.HasFlag(ModifierKey.Alt);

        // Check for Ctrl+S anywhere in the sequence
        var hasCtrlS = sequence.Any(item =>
            item.Key.Equals("s", StringComparison.OrdinalIgnoreCase) &&
            item.Modifiers.HasFlag(ModifierKey.Ctrl));

        // Check for Alt+F, A pattern (legacy Save As)
        var hasLegacySaveAs = sequence.Count >= 2 &&
            hasFileMenuPattern &&
            sequence.Any(item => item.Key.Equals("a", StringComparison.OrdinalIgnoreCase));

        // Check for Alt+F, S pattern (legacy Save)
        var hasLegacySave = sequence.Count >= 2 &&
            hasFileMenuPattern &&
            sequence.Skip(1).Any(item => item.Key.Equals("s", StringComparison.OrdinalIgnoreCase));

        if (hasCtrlS || hasLegacySaveAs || hasLegacySave)
        {
            return result with
            {
                Hint = "TIP: For more reliable file saving, use ui_file(action='save', windowHandle, filePath) which handles Save dialogs automatically. Keyboard shortcuts and menu navigation may not work reliably in modern applications with ribbon UIs."
            };
        }

        if (hasFileMenuPattern)
        {
            return result with
            {
                Hint = "TIP: For file operations like Save, SaveAs, or Open, use the ui_file tool instead of menu navigation. It handles dialogs automatically and works across different application UI styles."
            };
        }

        return result;
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
