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

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for controlling keyboard input on Windows.
/// </summary>
[McpServerToolType]
public sealed partial class KeyboardControlTool : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IKeyboardInputService _keyboardInputService;
    private readonly IElevationDetector _elevationDetector;
    private readonly ISecureDesktopDetector _secureDesktopDetector;
    private readonly KeyboardOperationLogger _logger;
    private readonly KeyboardConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardControlTool"/> class.
    /// </summary>
    /// <param name="keyboardInputService">The keyboard input service.</param>
    /// <param name="elevationDetector">The elevation detector.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="logger">The operation logger.</param>
    /// <param name="configuration">The keyboard configuration.</param>
    public KeyboardControlTool(
        IKeyboardInputService keyboardInputService,
        IElevationDetector elevationDetector,
        ISecureDesktopDetector secureDesktopDetector,
        KeyboardOperationLogger logger,
        KeyboardConfiguration configuration)
    {
        _keyboardInputService = keyboardInputService ?? throw new ArgumentNullException(nameof(keyboardInputService));
        _elevationDetector = elevationDetector ?? throw new ArgumentNullException(nameof(elevationDetector));
        _secureDesktopDetector = secureDesktopDetector ?? throw new ArgumentNullException(nameof(secureDesktopDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Control keyboard input on Windows. Supports type (text), press (key), key_down, key_up, combo, sequence, release_all, and get_keyboard_layout actions.
    /// </summary>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The keyboard action: type, press, key_down, key_up, combo, sequence, release_all, or get_keyboard_layout.</param>
    /// <param name="text">Text to type (required for type action).</param>
    /// <param name="key">Key name to press (for press, key_down, key_up, combo actions). Examples: enter, tab, escape, f1, a, ctrl, shift, alt, win, copilot.</param>
    /// <param name="modifiers">Modifier keys: ctrl, shift, alt, win (comma-separated, for press and combo actions).</param>
    /// <param name="repeat">Number of times to repeat key press (default: 1, for press action).</param>
    /// <param name="sequence">JSON array of key sequence items, e.g., [{"key":"ctrl"},{"key":"c"}] (for sequence action).</param>
    /// <param name="interKeyDelayMs">Delay between keys in sequence (milliseconds).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the keyboard operation including success status and operation details.</returns>
    [McpServerTool(Name = "keyboard_control", Title = "Keyboard Control", Destructive = true, UseStructuredContent = true)]
    [return: Description("The result of the keyboard operation including success status, characters typed, key pressed, keyboard layout info, and error details if failed.")]
    public async Task<KeyboardControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        string action,
        string? text = null,
        string? key = null,
        string? modifiers = null,
        int repeat = 1,
        string? sequence = null,
        int? interKeyDelayMs = null,
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
                    $"Unknown action: '{action}'. Valid actions are: type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout");
                _logger.LogOperationFailure(correlationId, action, result.ErrorCode.ToString(), result.Error ?? "Unknown error", stopwatch.ElapsedMilliseconds);
                return result;
            }

            KeyboardControlResult operationResult;

            switch (keyboardAction.Value)
            {
                case KeyboardAction.Type:
                    operationResult = await HandleTypeAsync(text, linkedToken);
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

                case KeyboardAction.Combo:
                    operationResult = await HandleComboAsync(key, modifiers, linkedToken);
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

                default:
                    operationResult = KeyboardControlResult.CreateFailure(
                        KeyboardControlErrorCode.InvalidAction,
                        $"Unknown action: '{action}'");
                    break;
            }

            stopwatch.Stop();

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
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogOperationException(correlationId, action ?? "null", ex);
            var errorResult = KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.UnexpectedError,
                $"An unexpected error occurred: {ex.Message}");
            return errorResult;
        }
    }

    private async Task<KeyboardControlResult> HandleTypeAsync(string? text, CancellationToken cancellationToken)
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

    private async Task<KeyboardControlResult> HandleComboAsync(string? key, string? modifiers, CancellationToken cancellationToken)
    {
        // Validate required parameter
        if (string.IsNullOrWhiteSpace(key))
        {
            return KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.MissingRequiredParameter,
                "The 'key' parameter is required for combo action");
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
        return await _keyboardInputService.PressKeyAsync(key, modifierKey, 1, cancellationToken);
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
            sequence = JsonSerializer.Deserialize<List<KeySequenceItem>>(sequenceJson, JsonOptions) ?? [];
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

    private static KeyboardAction? ParseAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "type" => KeyboardAction.Type,
            "press" => KeyboardAction.Press,
            "key_down" or "keydown" => KeyboardAction.KeyDown,
            "key_up" or "keyup" => KeyboardAction.KeyUp,
            "combo" => KeyboardAction.Combo,
            "sequence" => KeyboardAction.Sequence,
            "release_all" or "releaseall" => KeyboardAction.ReleaseAll,
            "get_keyboard_layout" or "getkeyboardlayout" or "layout" => KeyboardAction.GetKeyboardLayout,
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
