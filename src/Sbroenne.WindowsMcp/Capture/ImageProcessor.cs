using System.Drawing.Imaging;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
/// </summary>
public sealed class ImageProcessor : IImageProcessor
{
    /// <summary>
    /// Maximum width for output images. Images wider than this will be scaled down.
    /// 1024px width balances readability with token consumption for LLM vision APIs.
    /// </summary>
    private const int MaxWidth = 1024;

    /// <summary>
    /// Maximum height for output images. Images taller than this will be scaled down.
    /// </summary>
    private const int MaxHeight = 768;

    /// <inheritdoc />
    public ProcessedImage Process(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality)
    {
        ArgumentNullException.ThrowIfNull(source);

        var originalWidth = source.Width;
        var originalHeight = source.Height;

        // Scale down if needed
        var (scaledBitmap, scaledWidth, scaledHeight) = ScaleIfNeeded(source);
        var bitmapToEncode = scaledBitmap ?? source;

        try
        {
            // Encode to requested format
            var data = imageFormat switch
            {
                Models.ImageFormat.Jpeg => EncodeToJpeg(bitmapToEncode, quality),
                Models.ImageFormat.Png => EncodeToPng(bitmapToEncode),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, "Unsupported image format")
            };

            return new ProcessedImage(
                data,
                scaledWidth,
                scaledHeight,
                originalWidth,
                originalHeight,
                imageFormat.ToString().ToLowerInvariant());
        }
        finally
        {
            // Dispose the scaled bitmap if we created one
            scaledBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Scales the bitmap down if it exceeds the maximum dimensions.
    /// </summary>
    /// <returns>Tuple of (scaled bitmap or null if no scaling, final width, final height).</returns>
    private static (Bitmap? ScaledBitmap, int Width, int Height) ScaleIfNeeded(Bitmap source)
    {
        var width = source.Width;
        var height = source.Height;

        // Check if scaling is needed
        if (width <= MaxWidth && height <= MaxHeight)
        {
            return (null, width, height);
        }

        // Calculate scale factor to fit within max dimensions while maintaining aspect ratio
        var scaleX = (double)MaxWidth / width;
        var scaleY = (double)MaxHeight / height;
        var scale = Math.Min(scaleX, scaleY);

        var newWidth = (int)(width * scale);
        var newHeight = (int)(height * scale);

        // Create scaled bitmap with high-quality interpolation
        var scaled = new Bitmap(newWidth, newHeight);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, newWidth, newHeight);
        }

        return (scaled, newWidth, newHeight);
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
