using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
/// </summary>
public sealed class ImageProcessor
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

    /// <inheritdoc />
    public ProcessedImage ProcessWithScaling(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality,
        int maxDimension)
    {
        ArgumentNullException.ThrowIfNull(source);

        var originalWidth = source.Width;
        var originalHeight = source.Height;

        // Check if scaling is needed
        if (maxDimension <= 0 || (originalWidth <= maxDimension && originalHeight <= maxDimension))
        {
            // No scaling needed
            return Process(source, imageFormat, quality);
        }

        // Calculate scaled dimensions preserving aspect ratio
        var (scaledWidth, scaledHeight) = CalculateScaledDimensions(originalWidth, originalHeight, maxDimension);

        // Create scaled bitmap
        using var scaledBitmap = new Bitmap(scaledWidth, scaledHeight, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(scaledBitmap))
        {
            // High-quality scaling for best LLM readability
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            graphics.DrawImage(source, 0, 0, scaledWidth, scaledHeight);
        }

        // Encode the scaled bitmap
        var data = imageFormat switch
        {
            Models.ImageFormat.Jpeg => EncodeToJpeg(scaledBitmap, quality),
            Models.ImageFormat.Png => EncodeToPng(scaledBitmap),
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

    /// <summary>
    /// Calculates scaled dimensions preserving aspect ratio.
    /// </summary>
    private static (int Width, int Height) CalculateScaledDimensions(int width, int height, int maxDimension)
    {
        if (width <= maxDimension && height <= maxDimension)
        {
            return (width, height);
        }

        double ratio;
        if (width > height)
        {
            ratio = (double)maxDimension / width;
        }
        else
        {
            ratio = (double)maxDimension / height;
        }

        return ((int)(width * ratio), (int)(height * ratio));
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
