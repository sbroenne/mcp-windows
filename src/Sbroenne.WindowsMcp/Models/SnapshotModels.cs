using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Controls how a UI snapshot uses the server's in-memory previous view.
/// </summary>
internal enum SnapshotMode
{
    Full,
    Auto,
    Reset
}

/// <summary>
/// One change between two compact UI trees.
/// </summary>
public sealed record SnapshotChange
{
    /// <summary>Change operation: add, remove, or update.</summary>
    [JsonPropertyName("op")]
    public required string Op { get; init; }

    /// <summary>Readable path identifying the changed element within the remembered tree.</summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>Complete added subtree for an add operation.</summary>
    [JsonPropertyName("node")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree? Node { get; init; }

    /// <summary>Current values for fields changed by an update operation.</summary>
    [JsonPropertyName("set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Set { get; init; }
}
