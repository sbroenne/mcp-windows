namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output image format for screenshots.
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// JPEG format (lossy, smaller file size). Default for LLM optimization.
    /// </summary>
    Jpeg,

    /// <summary>
    /// PNG format (lossless, larger file size). Use for pixel-perfect capture.
    /// </summary>
    Png
}
