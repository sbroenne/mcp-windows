using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents the result of a <c>process</c> tool operation (list / kill).
/// </summary>
public sealed record ProcessResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>Gets the action that was performed (<c>list</c> or <c>kill</c>).</summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>
    /// Gets the matched processes. Populated for <c>list</c> operations.
    /// </summary>
    [JsonPropertyName("processes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ProcessSummary>? Processes { get; init; }

    /// <summary>
    /// Gets the number of processes returned (for <c>list</c>) or terminated (for <c>kill</c>).
    /// </summary>
    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; init; }

    /// <summary>
    /// Gets the processes that were terminated. Populated for <c>kill</c> operations.
    /// </summary>
    [JsonPropertyName("killed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ProcessSummary>? Killed { get; init; }

    /// <summary>Gets the error message when the operation failed.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error. Mirrors <see cref="Success"/> inverted.
    /// </summary>
    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError => !Success;

    /// <summary>Creates a successful result for a <c>list</c> operation.</summary>
    /// <param name="processes">The matched processes.</param>
    /// <returns>A successful <see cref="ProcessResult"/>.</returns>
    public static ProcessResult CreateListSuccess(IReadOnlyList<ProcessSummary> processes)
    {
        ArgumentNullException.ThrowIfNull(processes);
        return new()
        {
            Success = true,
            Action = "list",
            Processes = processes,
            Count = processes.Count
        };
    }

    /// <summary>Creates a successful result for a <c>kill</c> operation.</summary>
    /// <param name="killed">The processes that were terminated.</param>
    /// <returns>A successful <see cref="ProcessResult"/>.</returns>
    public static ProcessResult CreateKillSuccess(IReadOnlyList<ProcessSummary> killed)
    {
        ArgumentNullException.ThrowIfNull(killed);
        return new()
        {
            Success = true,
            Action = "kill",
            Killed = killed,
            Count = killed.Count
        };
    }

    /// <summary>Creates a failure result.</summary>
    /// <param name="action">The action that failed.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed <see cref="ProcessResult"/>.</returns>
    public static ProcessResult CreateFailure(string action, string error) => new()
    {
        Success = false,
        Action = action,
        Error = error
    };
}
