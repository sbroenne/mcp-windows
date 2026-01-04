namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Options for visual diff computation.
/// </summary>
public sealed record VisualDiffOptions
{
    /// <summary>
    /// Gets the threshold percentage for determining if a change is significant (0-100).
    /// </summary>
    public double Threshold { get; init; } = 0.5;

    /// <summary>
    /// Gets the per-channel tolerance for pixel comparison (0-255).
    /// Pixels with differences smaller than this are considered identical.
    /// </summary>
    public int PixelTolerance { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether to generate a diff image highlighting changed pixels.
    /// </summary>
    public bool GenerateDiffImage { get; init; } = false;

    /// <summary>
    /// Gets the color to highlight changed pixels in the diff image (RGBA).
    /// </summary>
    public uint HighlightColor { get; init; } = 0xFF0000FF; // Red: #FF0000FF
}
