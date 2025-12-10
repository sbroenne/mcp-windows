namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Scales and encodes a bitmap according to request parameters.
    /// </summary>
    /// <param name="source">Source bitmap from capture.</param>
    /// <param name="imageFormat">Output image format (jpeg/png).</param>
    /// <param name="quality">JPEG quality (1-100). Ignored for PNG.</param>
    /// <param name="maxWidth">Maximum width (0 = no constraint).</param>
    /// <param name="maxHeight">Maximum height (0 = no constraint).</param>
    /// <returns>Processed image with encoded data and metadata.</returns>
    ProcessedImage Process(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality,
        int maxWidth,
        int maxHeight);
}
