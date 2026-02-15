using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact representation of a UI element for tree responses (with children).
/// Reduces token count compared to full UIElementInfo while preserving hierarchy.
/// Use get_element_details action to fetch full details when needed.
/// </summary>
public sealed record UIElementCompactTree
{
    /// <summary>
    /// Element ID for subsequent operations (use with elementId parameter).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name from accessibility tree.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Control type (Button, Edit, Text, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Click coordinates as [x, y, monitorIndex]. Use with mouse_control:
    /// mouse_control(action='click', x=c[0], y=c[1], monitorIndex=c[2])
    /// </summary>
    [JsonPropertyName("click")]
    public int[]? Click { get; init; }

    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Child elements in the UI tree.
    /// </summary>
    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree[]? Children { get; init; }

    /// <summary>
    /// Creates a compact tree element from a full UIElementInfo (recursive).
    /// </summary>
    /// <param name="full">The full element info.</param>
    /// <returns>A compact tree representation.</returns>
    public static UIElementCompactTree FromFull(UIElementInfo full)
    {
        ArgumentNullException.ThrowIfNull(full);

        return new UIElementCompactTree
        {
            Id = full.ElementId,
            Name = full.Name,
            Type = full.ControlType,
            Click = full.ClickablePoint != null
                ? [full.ClickablePoint.X, full.ClickablePoint.Y, full.ClickablePoint.MonitorIndex]
                : null,
            Enabled = full.IsEnabled,
            Children = full.Children?.Select(FromFull).ToArray()
        };
    }
}
