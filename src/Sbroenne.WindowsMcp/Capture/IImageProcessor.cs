namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: format encoding.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Encodes a bitmap according to request parameters.
    /// </summary>
    /// <param name="source">Source bitmap from capture.</param>
    /// <param name="imageFormat">Output image format (jpeg/png).</param>
    /// <param name="quality">JPEG quality (1-100). Ignored for PNG.</param>
    /// <returns>Processed image with encoded data and metadata.</returns>
    ProcessedImage Process(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality);
}
