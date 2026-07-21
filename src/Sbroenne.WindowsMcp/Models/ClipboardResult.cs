using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents the result of a clipboard operation (get / set / clear).
/// </summary>
public sealed record ClipboardResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the action that was performed (get, set, or clear).
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>
    /// Gets the clipboard text. Populated for <c>get</c> operations. Null when the clipboard
    /// contained no text (e.g., empty or a non-text format).
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// Gets the number of characters read from or written to the clipboard.
    /// </summary>
    [JsonPropertyName("length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Length { get; init; }

    /// <summary>
    /// Gets a value indicating whether the clipboard currently holds text.
    /// Populated for <c>get</c> operations so agents can branch without a second call.
    /// </summary>
    [JsonPropertyName("hasText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasText { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error. Mirrors <see cref="Success"/> inverted.
    /// </summary>
    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError => !Success;

    /// <summary>Creates a successful result for a <c>get</c> operation.</summary>
    /// <param name="text">The text read from the clipboard (null when no text present).</param>
    /// <returns>A successful <see cref="ClipboardResult"/>.</returns>
    public static ClipboardResult CreateGetSuccess(string? text) => new()
    {
        Success = true,
        Action = "get",
        Text = text,
        Length = text?.Length ?? 0,
        HasText = !string.IsNullOrEmpty(text)
    };

    /// <summary>Creates a successful result for a <c>set</c> operation.</summary>
    /// <param name="length">Number of characters written.</param>
    /// <returns>A successful <see cref="ClipboardResult"/>.</returns>
    public static ClipboardResult CreateSetSuccess(int length) => new()
    {
        Success = true,
        Action = "set",
        Length = length
    };

    /// <summary>Creates a successful result for a <c>clear</c> operation.</summary>
    /// <returns>A successful <see cref="ClipboardResult"/>.</returns>
    public static ClipboardResult CreateClearSuccess() => new()
    {
        Success = true,
        Action = "clear"
    };

    /// <summary>Creates a failure result.</summary>
    /// <param name="action">The action that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed <see cref="ClipboardResult"/>.</returns>
    public static ClipboardResult CreateFailure(string action, string error) => new()
    {
        Success = false,
        Action = action,
        Error = error
    };
}
