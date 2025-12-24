namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Bounding rectangle relative to monitor origin (for mouse_control).
/// </summary>
public sealed record MonitorRelativeRect
{
    /// <summary>
    /// X coordinate relative to monitor left edge.
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y coordinate relative to monitor top edge.
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// Width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Center X for clicking.
    /// </summary>
    public int CenterX => X + Width / 2;

    /// <summary>
    /// Center Y for clicking.
    /// </summary>
    public int CenterY => Y + Height / 2;
}
