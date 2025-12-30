using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact representation of a UI element for list responses.
/// Reduces token count by ~75% compared to full UIElementInfo.
/// Use get_element_details action to fetch full details when needed.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON serialization overhead:
/// - id: Element ID for subsequent operations
/// - n: Name (human-readable)
/// - t: Type (control type)
/// - c: Click coordinates [x, y, monitorIndex]
/// - e: Enabled status
/// </remarks>
public sealed record UIElementCompact
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
    public required int[] Click { get; init; }

    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    [JsonPropertyName("e")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Creates a compact element from a full UIElementInfo.
    /// </summary>
    /// <param name="full">The full element info.</param>
    /// <returns>A compact representation.</returns>
    public static UIElementCompact FromFull(UIElementInfo full)
    {
        ArgumentNullException.ThrowIfNull(full);

        return new UIElementCompact
        {
            Id = full.ElementId,
            Name = full.Name,
            Type = full.ControlType,
            Click = [full.ClickablePoint.X, full.ClickablePoint.Y, full.ClickablePoint.MonitorIndex],
            Enabled = full.IsEnabled
        };
    }
}
