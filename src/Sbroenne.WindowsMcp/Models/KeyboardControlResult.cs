using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents the result of a keyboard control operation.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - ok: Success
/// - ec: Error Code
/// - err: Error message
/// - cnt: Characters typed count
/// - k: Key pressed
/// - held: Held keys
/// - seq: Sequence length
/// - kbl: Keyboard layout
/// - tw: Target window
/// - msg: Message
/// </remarks>
public sealed record KeyboardControlResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("ok")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets the error code if the operation failed.
    /// </summary>
    [JsonPropertyName("ec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public KeyboardControlErrorCode ErrorCode { get; init; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    [JsonPropertyName("err")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets the number of characters typed (for Type action).
    /// </summary>
    [JsonPropertyName("cnt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CharactersTyped { get; init; }

    /// <summary>
    /// Gets or sets the key that was pressed (for Press, KeyDown, KeyUp actions).
    /// </summary>
    [JsonPropertyName("k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyPressed { get; init; }

    /// <summary>
    /// Gets or sets the list of keys that are currently held.
    /// </summary>
    [JsonPropertyName("held")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HeldKeys { get; init; }

    /// <summary>
    /// Gets or sets the number of keys executed in a sequence.
    /// </summary>
    [JsonPropertyName("seq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SequenceLength { get; init; }

    /// <summary>
    /// Gets or sets the keyboard layout information (for GetKeyboardLayout action).
    /// </summary>
    [JsonPropertyName("kbl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public KeyboardLayoutInfo? KeyboardLayout { get; init; }

    /// <summary>
    /// Gets or sets information about the window that received the keyboard input.
    /// This helps LLM agents verify that input was sent to the correct window.
    /// </summary>
    [JsonPropertyName("tw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TargetWindowInfo? TargetWindow { get; init; }

    /// <summary>
    /// Gets or sets a message describing the result (e.g., for wait_for_idle action).
    /// </summary>
    [JsonPropertyName("msg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful result for a type operation.
    /// </summary>
    /// <param name="charactersTyped">Number of characters typed.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateTypeSuccess(int charactersTyped)
    {
        return new KeyboardControlResult
        {
            Success = true,
            CharactersTyped = charactersTyped
        };
    }

    /// <summary>
    /// Creates a successful result for a key press operation.
    /// </summary>
    /// <param name="keyPressed">The key that was pressed.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreatePressSuccess(string keyPressed)
    {
        return new KeyboardControlResult
        {
            Success = true,
            KeyPressed = keyPressed
        };
    }

    /// <summary>
    /// Creates a successful result for a key down operation.
    /// </summary>
    /// <param name="keyPressed">The key that was pressed.</param>
    /// <param name="heldKeys">List of all currently held keys.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateKeyDownSuccess(string keyPressed, IReadOnlyList<string> heldKeys)
    {
        return new KeyboardControlResult
        {
            Success = true,
            KeyPressed = keyPressed,
            HeldKeys = heldKeys
        };
    }

    /// <summary>
    /// Creates a successful result for a key up operation.
    /// </summary>
    /// <param name="keyReleased">The key that was released.</param>
    /// <param name="heldKeys">List of remaining held keys.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateKeyUpSuccess(string keyReleased, IReadOnlyList<string> heldKeys)
    {
        return new KeyboardControlResult
        {
            Success = true,
            KeyPressed = keyReleased,
            HeldKeys = heldKeys
        };
    }

    /// <summary>
    /// Creates a successful result for a release all operation.
    /// </summary>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateReleaseAllSuccess()
    {
        return new KeyboardControlResult
        {
            Success = true,
            HeldKeys = []
        };
    }

    /// <summary>
    /// Creates a successful result for a sequence operation.
    /// </summary>
    /// <param name="sequenceLength">Number of keys executed in the sequence.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateSequenceSuccess(int sequenceLength)
    {
        return new KeyboardControlResult
        {
            Success = true,
            SequenceLength = sequenceLength
        };
    }

    /// <summary>
    /// Creates a successful result for a get keyboard layout operation.
    /// </summary>
    /// <param name="layoutInfo">The keyboard layout information.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateLayoutSuccess(KeyboardLayoutInfo layoutInfo)
    {
        return new KeyboardControlResult
        {
            Success = true,
            KeyboardLayout = layoutInfo
        };
    }

    /// <summary>
    /// Creates a successful result for a wait for idle operation.
    /// </summary>
    /// <param name="message">A message describing the idle state.</param>
    /// <returns>A successful KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateWaitForIdleSuccess(string message)
    {
        return new KeyboardControlResult
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed KeyboardControlResult.</returns>
    public static KeyboardControlResult CreateFailure(KeyboardControlErrorCode errorCode, string error)
    {
        return new KeyboardControlResult
        {
            Success = false,
            ErrorCode = errorCode,
            Error = error
        };
    }

    /// <summary>
    /// Creates a generic successful result (used internally for pre-flight checks).
    /// </summary>
    /// <returns>A successful KeyboardControlResult.</returns>
    internal static KeyboardControlResult CreateSuccess()
    {
        return new KeyboardControlResult
        {
            Success = true
        };
    }
}
