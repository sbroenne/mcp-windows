namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents an annotated element with its index and properties for use in annotated screenshots.
/// The index corresponds to the numbered label drawn on the screenshot.
/// </summary>
public sealed record AnnotatedElement
{
    /// <summary>
    /// The numeric index of this element as shown on the annotated screenshot.
    /// Use this index to reference the element in subsequent operations.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// The name of the element.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The control type (e.g., Button, Edit, CheckBox).
    /// </summary>
    public required string ControlType { get; init; }

    /// <summary>
    /// The AutomationId if available.
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// The element ID for use in subsequent ui_automation operations.
    /// </summary>
    public required string ElementId { get; init; }

    /// <summary>
    /// Ready-to-use clickable point coordinates with monitor index.
    /// </summary>
    public required ClickablePoint ClickablePoint { get; init; }

    /// <summary>
    /// The bounding rectangle of the element.
    /// </summary>
    public required BoundingRect BoundingBox { get; init; }
}
