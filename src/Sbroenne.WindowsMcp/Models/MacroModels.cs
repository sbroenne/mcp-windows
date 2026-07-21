using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>Operation performed by the <c>ui_macro</c> tool.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MacroAction>))]
public enum MacroAction
{
    /// <summary>Persist a named macro from a ui_batch steps array.</summary>
    [JsonStringEnumMemberName("save")]
    Save,

    /// <summary>List the saved macros (names, step counts, timestamps).</summary>
    [JsonStringEnumMemberName("list")]
    List,

    /// <summary>Return the steps of a single saved macro.</summary>
    [JsonStringEnumMemberName("get")]
    Get,

    /// <summary>Replay a saved macro against a window via the ui_batch engine.</summary>
    [JsonStringEnumMemberName("run")]
    Run,

    /// <summary>Delete a saved macro.</summary>
    [JsonStringEnumMemberName("delete")]
    Delete,
}

/// <summary>Lightweight description of a saved macro (no step bodies), used by <c>list</c>.</summary>
public sealed record MacroSummary
{
    /// <summary>Macro name (also the on-disk file stem).</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Number of steps the macro contains.</summary>
    [JsonPropertyName("stepCount")]
    public required int StepCount { get; init; }

    /// <summary>UTC time the macro was last saved (ISO 8601).</summary>
    [JsonPropertyName("savedAtUtc")]
    public required string SavedAtUtc { get; init; }
}

/// <summary>
/// The full on-disk representation of a saved macro. <see cref="Steps"/> is the raw ui_batch
/// steps array so replay is byte-for-byte what was recorded.
/// </summary>
public sealed record MacroDefinition
{
    /// <summary>Macro name (also the on-disk file stem).</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Number of steps the macro contains.</summary>
    [JsonPropertyName("stepCount")]
    public required int StepCount { get; init; }

    /// <summary>UTC time the macro was last saved (ISO 8601).</summary>
    [JsonPropertyName("savedAtUtc")]
    public required string SavedAtUtc { get; init; }

    /// <summary>The ui_batch steps array (verbatim), replayed by the batch engine.</summary>
    [JsonPropertyName("steps")]
    public required JsonElement Steps { get; init; }
}

/// <summary>Result of a <c>ui_macro</c> management operation (save/list/get/delete).</summary>
public sealed record MacroResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>The action performed: save, list, get, or delete.</summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>The macro name the operation targeted (save/get/delete).</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>Number of steps involved (save/get).</summary>
    [JsonPropertyName("stepCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StepCount { get; init; }

    /// <summary>The saved macros (list action).</summary>
    [JsonPropertyName("macros")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MacroSummary>? Macros { get; init; }

    /// <summary>The macro's steps array (get action).</summary>
    [JsonPropertyName("steps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Steps { get; init; }

    /// <summary>Error message when the operation failed.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>True when the operation failed (drives the tool's IsError flag).</summary>
    [JsonIgnore]
    public bool IsError => !Success;

    /// <summary>Creates a failure result carrying <paramref name="error"/>.</summary>
    public static MacroResult Failure(string action, string error) =>
        new() { Success = false, Action = action, Error = error };
}
