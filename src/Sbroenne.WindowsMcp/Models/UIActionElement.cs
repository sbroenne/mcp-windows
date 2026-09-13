using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>A compact observation of the same target before or after an action.</summary>
public sealed record UIActionElement
{
    /// <summary>The original opaque target ID, never a replacement found by name.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The name at the time of this observation, or null when unsupported.</summary>
    [JsonPropertyName("name")]
    public required string? Name { get; init; }

    /// <summary>The control type at the time of this observation.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Whether the target was enabled at the time of this observation.</summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }
}
