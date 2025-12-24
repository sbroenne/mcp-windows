namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents a point that can be clicked, with monitor-relative coordinates
/// ready for direct use with mouse_control tool.
/// </summary>
/// <remarks>
/// This provides a convenient "copy-paste ready" coordinate set for LLMs
/// to use directly with mouse_control without any calculation needed.
/// </remarks>
public sealed record ClickablePoint
{
    /// <summary>
    /// X coordinate relative to monitor's left edge (use directly with mouse_control).
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y coordinate relative to monitor's top edge (use directly with mouse_control).
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// Monitor index for use with mouse_control's monitorIndex parameter.
    /// </summary>
    public required int MonitorIndex { get; init; }

    /// <summary>
    /// Creates a ClickablePoint from monitor-relative coordinates.
    /// </summary>
    /// <param name="x">X coordinate relative to monitor.</param>
    /// <param name="y">Y coordinate relative to monitor.</param>
    /// <param name="monitorIndex">Index of the monitor.</param>
    /// <returns>A new ClickablePoint instance.</returns>
    public static ClickablePoint Create(int x, int y, int monitorIndex) =>
        new() { X = x, Y = y, MonitorIndex = monitorIndex };

    /// <summary>
    /// Creates a ClickablePoint at the center of a MonitorRelativeRect.
    /// </summary>
    /// <param name="rect">The bounding rectangle.</param>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <returns>A ClickablePoint at the center of the rectangle.</returns>
    public static ClickablePoint FromCenter(MonitorRelativeRect rect, int monitorIndex)
    {
        ArgumentNullException.ThrowIfNull(rect);
        return new() { X = rect.CenterX, Y = rect.CenterY, MonitorIndex = monitorIndex };
    }
}
