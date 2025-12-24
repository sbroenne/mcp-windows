namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents a bounding rectangle in screen coordinates.
/// </summary>
public sealed record BoundingRect
{
    /// <summary>
    /// Screen X coordinate (left edge).
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Screen Y coordinate (top edge).
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
    /// Center X coordinate (for clicking).
    /// </summary>
    public int CenterX => X + Width / 2;

    /// <summary>
    /// Center Y coordinate (for clicking).
    /// </summary>
    public int CenterY => Y + Height / 2;

    /// <summary>
    /// Creates a BoundingRect from coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>A new BoundingRect instance.</returns>
    public static BoundingRect FromCoordinates(double x, double y, double width, double height)
    {
        return new BoundingRect
        {
            X = (int)x,
            Y = (int)y,
            Width = (int)width,
            Height = (int)height
        };
    }
}
