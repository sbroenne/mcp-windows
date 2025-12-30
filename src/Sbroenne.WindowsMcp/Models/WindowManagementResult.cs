using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result of a window management operation.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - ok: Success
/// - ec: Error code
/// - err: Error message
/// - w: Window (single)
/// - ws: Windows (list)
/// - n: Count
/// - msg: Message
/// </remarks>
public sealed record WindowManagementResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("ok")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error code if operation failed.
    /// </summary>
    [JsonIgnore]
    public WindowManagementErrorCode ErrorCode { get; init; } = WindowManagementErrorCode.None;

    /// <summary>
    /// Gets the error code string for JSON serialization.
    /// </summary>
    [JsonPropertyName("ec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCodeString => ErrorCode == WindowManagementErrorCode.None
        ? null
        : ErrorCode.ToString();

    /// <summary>
    /// Gets the error message if operation failed.
    /// </summary>
    [JsonPropertyName("err")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Gets the single window info (for find, activate, get_foreground, etc.).
    /// Uses compact format for reduced token count.
    /// </summary>
    [JsonPropertyName("w")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WindowInfoCompact? Window { get; init; }

    /// <summary>
    /// Gets the list of windows (for list action).
    /// Uses compact format for reduced token count.
    /// </summary>
    [JsonPropertyName("ws")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<WindowInfoCompact>? Windows { get; init; }

    /// <summary>
    /// Gets the number of windows found/affected.
    /// </summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }

    /// <summary>
    /// Gets an informational message for successful results.
    /// </summary>
    [JsonPropertyName("msg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful result for a list operation.
    /// </summary>
    /// <param name="windows">The list of windows.</param>
    /// <returns>A successful result with the window list.</returns>
    public static WindowManagementResult CreateListSuccess(IReadOnlyList<WindowInfo> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        return new WindowManagementResult
        {
            Success = true,
            Windows = windows.Select(WindowInfoCompact.FromFull).ToList(),
            Count = windows.Count
        };
    }

    /// <summary>
    /// Creates a successful result for a list operation (compact version).
    /// </summary>
    /// <param name="windows">The list of compact windows.</param>
    /// <returns>A successful result with the window list.</returns>
    public static WindowManagementResult CreateListSuccess(IReadOnlyList<WindowInfoCompact> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        return new WindowManagementResult
        {
            Success = true,
            Windows = windows,
            Count = windows.Count
        };
    }

    /// <summary>
    /// Creates a successful result for a single window operation.
    /// </summary>
    /// <param name="window">The window info.</param>
    /// <param name="message">Optional success message.</param>
    /// <returns>A successful result with the window info.</returns>
    public static WindowManagementResult CreateWindowSuccess(WindowInfo window, string? message = null)
    {
        return new WindowManagementResult
        {
            Success = true,
            Window = WindowInfoCompact.FromFull(window),
            Message = message
        };
    }

    /// <summary>
    /// Creates a successful result for a single window operation (compact version).
    /// </summary>
    /// <param name="window">The compact window info.</param>
    /// <param name="message">Optional success message.</param>
    /// <returns>A successful result with the window info.</returns>
    public static WindowManagementResult CreateWindowSuccess(WindowInfoCompact window, string? message = null)
    {
        return new WindowManagementResult
        {
            Success = true,
            Window = window,
            Message = message
        };
    }

    /// <summary>
    /// Creates a successful result with just a message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful result with a message.</returns>
    public static WindowManagementResult CreateSuccess(string message)
    {
        return new WindowManagementResult
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failure result with error details.</returns>
    public static WindowManagementResult CreateFailure(WindowManagementErrorCode errorCode, string errorMessage)
    {
        return new WindowManagementResult
        {
            Success = false,
            ErrorCode = errorCode,
            Error = errorMessage
        };
    }

    /// <summary>
    /// Creates a "window not found" result (not an error, just empty result).
    /// </summary>
    /// <param name="searchTerm">The search term that was used.</param>
    /// <returns>A successful result indicating no windows matched.</returns>
    public static WindowManagementResult CreateNotFound(string searchTerm)
    {
        return new WindowManagementResult
        {
            Success = true,
            Windows = [],
            Count = 0,
            Message = $"No windows found matching '{searchTerm}'"
        };
    }
}
