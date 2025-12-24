namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents a UI element discovered via Windows UI Automation.
/// </summary>
public sealed record UIElementInfo
{
    /// <summary>
    /// Composite identifier for subsequent operations.
    /// Format: "window:{hwnd}|runtime:{id}|path:{treePath}"
    /// </summary>
    public required string ElementId { get; init; }

    /// <summary>
    /// Developer-assigned automation ID (may be null).
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Human-readable name from accessibility tree.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Control type (Button, Edit, Text, List, etc.).
    /// </summary>
    public required string ControlType { get; init; }

    /// <summary>
    /// Bounding rectangle in screen coordinates.
    /// </summary>
    public required BoundingRect BoundingRect { get; init; }

    /// <summary>
    /// Monitor-relative coordinates for use with mouse_control.
    /// </summary>
    public required MonitorRelativeRect MonitorRelativeRect { get; init; }

    /// <summary>
    /// Monitor index containing this element.
    /// </summary>
    public required int MonitorIndex { get; init; }

    /// <summary>
    /// Ready-to-use clickable point for mouse_control tool.
    /// Use these coordinates directly: mouse_control(action="click", x=ClickablePoint.X, y=ClickablePoint.Y, monitorIndex=ClickablePoint.MonitorIndex).
    /// </summary>
    public required ClickablePoint ClickablePoint { get; init; }

    /// <summary>
    /// Supported UI Automation patterns (Invoke, Toggle, Value, etc.).
    /// </summary>
    public required string[] SupportedPatterns { get; init; }

    /// <summary>
    /// Current value for elements with ValuePattern (text fields, etc.).
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Current toggle state for elements with TogglePattern.
    /// </summary>
    public string? ToggleState { get; init; }

    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// Whether the element is currently visible on screen (false = offscreen).
    /// </summary>
    public required bool IsOffscreen { get; init; }

    /// <summary>
    /// Child elements (only populated when hierarchy requested).
    /// </summary>
    public UIElementInfo[]? Children { get; init; }
}
