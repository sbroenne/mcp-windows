using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact representation of an annotated element for LLM-optimized responses.
/// The index corresponds to the numbered label drawn on the screenshot.
/// </summary>
public sealed record AnnotatedElement
{
    /// <summary>
    /// The numeric index of this element as shown on the annotated screenshot.
    /// Use this index to reference the element in subsequent operations.
    /// </summary>
    [JsonPropertyName("i")]
    public required int Index { get; init; }

    /// <summary>
    /// The name of the element.
    /// </summary>
    [JsonPropertyName("n")]
    public string? Name { get; init; }

    /// <summary>
    /// The control type (e.g., Button, Edit, CheckBox).
    /// </summary>
    [JsonPropertyName("t")]
    public required string Type { get; init; }

    /// <summary>
    /// The element ID for use in subsequent ui_automation operations.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Click coordinates as [x, y, monitorIndex] array for mouse_control.
    /// </summary>
    [JsonPropertyName("c")]
    public required int[] Click { get; init; }
}
