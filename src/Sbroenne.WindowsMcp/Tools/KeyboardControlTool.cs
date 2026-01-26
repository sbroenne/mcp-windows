using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Serialization;
using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling keyboard input on Windows.
/// </summary>
[McpServerToolType]
public static partial class KeyboardControlTool
{
    /// <summary>
    /// ⚠️ TO SAVE FILES: STOP! Use file_save(windowHandle, filePath) instead - keyboard_control CANNOT handle Save As dialogs!
    /// Sends keyboard input to a specific window. The window is activated before input is sent.
    /// Best for: typing text, hotkeys (key='s', modifiers='ctrl'), special keys.
    /// For typing into a specific UI element by name/automationId, use ui_type instead.
    /// </summary>
    /// <remarks>
    /// Supports type (text), press (key), key_down, key_up, combo, sequence, release_all, get_keyboard_layout, and wait_for_idle actions. WARNING: Do NOT put modifiers in the 'key' parameter (e.g., 'Ctrl+S' is WRONG). Use key='s', modifiers='ctrl'. ⚠️ FOR SAVE: Use file_save tool - it handles Save As dialogs!
    /// </remarks>
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
    [McpServerTool(Name = "keyboard_control", Title = "Keyboard Control", Destructive = true, OpenWorld = false)]
    public static async partial Task<string> ExecuteAsync(
        string windowHandle,
        KeyboardAction action,
        [DefaultValue(null)] string? text,
        [DefaultValue(null)] string? key,
        [DefaultValue(null)] string? modifiers,
        [DefaultValue(1)] int repeat,
        [DefaultValue(null)] string? sequence,
        [DefaultValue(null)] int? interKeyDelayMs,
        [DefaultValue(false)] bool clearFirst,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate windowHandle is provided
            if (string.IsNullOrWhiteSpace(windowHandle))
            {
                return JsonSerializer.Serialize(
                    KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.MissingRequiredParameter,
                        "windowHandle is required. Get it from app() or window_management(action='find')."),
                    WindowsToolsBase.JsonOptions);
            }

            // Parse windowHandle
            if (!long.TryParse(windowHandle, out var handleValue) || handleValue == 0)
            {
                return JsonSerializer.Serialize(
                    KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.InvalidAction,
                        $"Invalid windowHandle '{windowHandle}'. Must be a non-zero decimal number from app() or window_management."),
                    WindowsToolsBase.JsonOptions);
            }

            var handle = new IntPtr(handleValue);

            // Create a linked token source with the configured timeout
            using var timeoutCts = new CancellationTokenSource(WindowsToolsBase.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var linkedToken = linkedCts.Token;

            // Activate the target window before sending keyboard input
            var activationResult = await WindowsToolsBase.WindowService.ActivateWindowAsync(handle, linkedToken);
            if (!activationResult.Success)
            {
                return JsonSerializer.Serialize(
                    KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.WrongTargetWindow,
                        $"Failed to activate window {windowHandle}: {activationResult.Error}"),
                    WindowsToolsBase.JsonOptions);
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

            // For successful operations that send input, attach the target window info
            if (operationResult.Success && action != KeyboardAction.GetKeyboardLayout && action != KeyboardAction.WaitForIdle)
            {
                operationResult = await AttachTargetWindowInfoAsync(operationResult, linkedToken);
            }

            return JsonSerializer.Serialize(operationResult, WindowsToolsBase.JsonOptions);
        }
        catch (OperationCanceledException)
        {
            return JsonSerializer.Serialize(
                KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.OperationTimeout,
                    $"Operation timed out after {WindowsToolsBase.TimeoutMs}ms"),
                WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.SerializeToolError("keyboard_control", ex);
        }
    }

    private static async Task<KeyboardControlResult> HandleTypeAsync(string? text, bool clearFirst, CancellationToken cancellationToken)
    {
        // Check for secure desktop
        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
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
        if (clearFirst)
        {
            var selectAllResult = await WindowsToolsBase.KeyboardInputService.PressKeyAsync("a", ModifierKey.Ctrl, 1, cancellationToken);
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
        var normalizedText = PathNormalizer.NormalizeWindowsPath(text);

        return await WindowsToolsBase.KeyboardInputService.TypeTextAsync(normalizedText, cancellationToken);
    }

    private static async Task<KeyboardControlResult> HandlePressAsync(string? key, string? modifiers, int repeat, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for press action");
        }

        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        var modifierKey = ParseModifiers(modifiers);
        var result = await WindowsToolsBase.KeyboardInputService.PressKeyAsync(key, modifierKey, repeat, cancellationToken);

        if (result.Success)
        {
            result = AddFileSaveHintIfNeeded(key, modifierKey, result);
        }

        return result;
    }

    private static KeyboardControlResult AddFileSaveHintIfNeeded(string key, ModifierKey modifiers, KeyboardControlResult result)
    {
        var keyLower = key.ToLowerInvariant();
        var isCtrlS = keyLower == "s" && modifiers.HasFlag(ModifierKey.Ctrl);
        var isAltF = keyLower == "f" && modifiers.HasFlag(ModifierKey.Alt);

        if (isCtrlS)
        {
            return result with
            {
                Hint = "⚠️ WARNING: keyboard_control CANNOT handle Save As dialogs! Use file_save(windowHandle, filePath) instead - it handles the entire save workflow automatically."
            };
        }

        if (isAltF)
        {
            return result with
            {
                Hint = "⚠️ For save operations, use file_save(windowHandle, filePath) - it handles Save As dialogs automatically!"
            };
        }

        return result;
    }

    private static async Task<KeyboardControlResult> HandleKeyDownAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for key_down action");
        }

        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        return await WindowsToolsBase.KeyboardInputService.KeyDownAsync(key, cancellationToken);
    }

    private static async Task<KeyboardControlResult> HandleKeyUpAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for key_up action");
        }

        return await WindowsToolsBase.KeyboardInputService.KeyUpAsync(key, cancellationToken);
    }

    private static async Task<KeyboardControlResult> HandleSequenceAsync(string? sequenceJson, int? interKeyDelayMs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sequenceJson))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'sequence' parameter is required for sequence action");
        }

        if (WindowsToolsBase.SecureDesktopDetector.IsSecureDesktopActive())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.SecureDesktopActive,
                "Cannot send keyboard input when secure desktop (UAC prompt, lock screen) is active");
        }

        if (IsForegroundWindowElevated())
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "Cannot send keyboard input to an elevated (administrator) window. Run this tool as administrator or interact with a non-elevated window.");
        }

        IReadOnlyList<KeySequenceItem> sequence;
        try
        {
            sequence = JsonSerializer.Deserialize<List<KeySequenceItem>>(sequenceJson, McpJsonOptions.Default) ?? [];
        }
        catch (JsonException ex)
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidAction,
                $"Invalid sequence JSON: {ex.Message}. CORRECT FORMAT: JSON array with 'key' property.");
        }

        var result = await WindowsToolsBase.KeyboardInputService.ExecuteSequenceAsync(sequence, interKeyDelayMs, cancellationToken);

        if (result.Success)
        {
            result = AddSequenceFileSaveHintIfNeeded(sequence, result);
        }

        return result;
    }

    private static KeyboardControlResult AddSequenceFileSaveHintIfNeeded(IReadOnlyList<KeySequenceItem> sequence, KeyboardControlResult result)
    {
        var hasFileMenuPattern = sequence.Count > 0 &&
            sequence[0].Key.Equals("f", StringComparison.OrdinalIgnoreCase) &&
            sequence[0].Modifiers.HasFlag(ModifierKey.Alt);

        var hasCtrlS = sequence.Any(item =>
            item.Key.Equals("s", StringComparison.OrdinalIgnoreCase) &&
            item.Modifiers.HasFlag(ModifierKey.Ctrl));

        var hasLegacySaveAs = sequence.Count >= 2 &&
            hasFileMenuPattern &&
            sequence.Any(item => item.Key.Equals("a", StringComparison.OrdinalIgnoreCase));

        var hasLegacySave = sequence.Count >= 2 &&
            hasFileMenuPattern &&
            sequence.Skip(1).Any(item => item.Key.Equals("s", StringComparison.OrdinalIgnoreCase));

        if (hasCtrlS || hasLegacySaveAs || hasLegacySave)
        {
            return result with
            {
                Hint = "⚠️ WARNING: For file saving, use file_save(windowHandle, filePath) - it handles Save As dialogs automatically!"
            };
        }

        if (hasFileMenuPattern)
        {
            return result with
            {
                Hint = "⚠️ For save operations, use file_save(windowHandle, filePath) instead of menu navigation!"
            };
        }

        return result;
    }

    private static async Task<KeyboardControlResult> HandleReleaseAllAsync(CancellationToken cancellationToken)
    {
        return await WindowsToolsBase.KeyboardInputService.ReleaseAllKeysAsync(cancellationToken);
    }

    private static async Task<KeyboardControlResult> HandleGetKeyboardLayoutAsync(CancellationToken cancellationToken)
    {
        return await WindowsToolsBase.KeyboardInputService.GetKeyboardLayoutAsync(cancellationToken);
    }

    private static async Task<KeyboardControlResult> HandleWaitForIdleAsync(CancellationToken cancellationToken)
    {
        return await WindowsToolsBase.KeyboardInputService.WaitForIdleAsync(cancellationToken);
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

    private static bool IsForegroundWindowElevated()
    {
        NativeMethods.GetCursorPos(out var cursorPos);
        return WindowsToolsBase.ElevationDetector.IsTargetElevated(cursorPos.X, cursorPos.Y);
    }

    private static async Task<KeyboardControlResult> AttachTargetWindowInfoAsync(KeyboardControlResult result, CancellationToken cancellationToken)
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
}