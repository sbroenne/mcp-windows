using System.Drawing.Imaging;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: format encoding.
/// </summary>
public sealed class ImageProcessor : IImageProcessor
{
    /// <inheritdoc />
    public ProcessedImage Process(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality)
    {
        ArgumentNullException.ThrowIfNull(source);

        var width = source.Width;
        var height = source.Height;

        // Encode to requested format
        var data = imageFormat switch
        {
            Models.ImageFormat.Jpeg => EncodeToJpeg(source, quality),
            Models.ImageFormat.Png => EncodeToPng(source),
            _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, "Unsupported image format")
        };

        return new ProcessedImage(
            data,
            width,
            height,
            width,
            height,
            imageFormat.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Encodes a bitmap to JPEG format with specified quality.
    /// </summary>
    /// <param name="bitmap">Bitmap to encode.</param>
    /// <param name="quality">Quality level (1-100).</param>
    /// <returns>Encoded JPEG data.</returns>
    public static byte[] EncodeToJpeg(Bitmap bitmap, int quality)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        // Clamp quality to valid range
        quality = Math.Clamp(quality, 1, 100);

        var jpegEncoder = GetEncoder(DrawingImageFormat.Jpeg);
        if (jpegEncoder is null)
        {
            throw new InvalidOperationException("JPEG encoder not found");
        }

        using var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, jpegEncoder, encoderParams);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Encodes a bitmap to PNG format.
    /// </summary>
    /// <param name="bitmap">Bitmap to encode.</param>
    /// <returns>Encoded PNG data.</returns>
    public static byte[] EncodeToPng(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, DrawingImageFormat.Png);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Gets the image encoder for a given format.
    /// </summary>
    private static ImageCodecInfo? GetEncoder(DrawingImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(c => c.FormatID == format.Guid);
    }
}
