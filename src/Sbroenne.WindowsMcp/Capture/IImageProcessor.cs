namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
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

    /// <summary>
    /// Scales and encodes a bitmap for LLM-optimized output.
    /// </summary>
    /// <param name="source">Source bitmap from capture.</param>
    /// <param name="imageFormat">Output image format (jpeg/png).</param>
    /// <param name="quality">JPEG quality (1-100). Ignored for PNG.</param>
    /// <param name="maxDimension">Maximum width or height. Image scaled proportionally if larger. 0 = no scaling.</param>
    /// <returns>Processed image with encoded data, dimensions, and original dimensions if scaled.</returns>
    ProcessedImage ProcessWithScaling(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality,
        int maxDimension);
}
