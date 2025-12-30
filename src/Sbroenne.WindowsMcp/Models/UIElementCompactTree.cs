using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact representation of a UI element for tree responses (with children).
/// Reduces token count compared to full UIElementInfo while preserving hierarchy.
/// Use get_element_details action to fetch full details when needed.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON serialization overhead:
/// - id: Element ID for subsequent operations
/// - n: Name (human-readable)
/// - t: Type (control type)
/// - c: Click coordinates [x, y, monitorIndex]
/// - e: Enabled status
/// - ch: Children (nested tree elements)
/// </remarks>
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
    [JsonPropertyName("n")]
    public string? Name { get; init; }

    /// <summary>
    /// Control type (Button, Edit, Text, etc.).
    /// </summary>
    [JsonPropertyName("t")]
    public required string Type { get; init; }

    /// <summary>
    /// Click coordinates as [x, y, monitorIndex]. Use with mouse_control:
    /// mouse_control(action='click', x=c[0], y=c[1], monitorIndex=c[2])
    /// </summary>
    [JsonPropertyName("c")]
    public int[]? Click { get; init; }

    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    [JsonPropertyName("e")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Child elements in the UI tree.
    /// </summary>
    [JsonPropertyName("ch")]
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
