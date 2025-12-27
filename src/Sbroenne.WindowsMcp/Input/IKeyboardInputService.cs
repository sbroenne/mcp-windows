using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Interface for keyboard input operations.
/// </summary>
public interface IKeyboardInputService
{
    /// <summary>
    /// Types text using Unicode input (layout-independent).
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> TypeTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Presses a single key (with optional modifiers) and releases it.
    /// </summary>
    /// <param name="keyName">The name of the key to press.</param>
    /// <param name="modifiers">Modifier keys to hold during the press.</param>
    /// <param name="repeat">Number of times to repeat the key press.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> PressKeyAsync(string keyName, ModifierKey modifiers = ModifierKey.None, int repeat = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Presses a key down without releasing it.
    /// </summary>
    /// <param name="keyName">The name of the key to hold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> KeyDownAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously held key.
    /// </summary>
    /// <param name="keyName">The name of the key to release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> KeyUpAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases all currently held keys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> ReleaseAllKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a sequence of key presses.
    /// </summary>
    /// <param name="sequence">The sequence of keys to press.</param>
    /// <param name="interKeyDelayMs">Optional delay between keys in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> ExecuteSequenceAsync(IReadOnlyList<KeySequenceItem> sequence, int? interKeyDelayMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the current keyboard layout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing keyboard layout information.</returns>
    Task<KeyboardControlResult> GetKeyboardLayoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the UI thread of the foreground window to become idle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<KeyboardControlResult> WaitForIdleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the names of all currently held keys.
    /// </summary>
    /// <returns>A list of held key names.</returns>
    IReadOnlyList<string> GetHeldKeyNames();
}
