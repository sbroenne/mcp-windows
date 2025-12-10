using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
/// </summary>
public sealed class ImageProcessor : IImageProcessor
{
    /// <inheritdoc />
    public ProcessedImage Process(
        Bitmap source,
        Models.ImageFormat imageFormat,
        int quality,
        int maxWidth,
        int maxHeight)
    {
        ArgumentNullException.ThrowIfNull(source);

        var originalWidth = source.Width;
        var originalHeight = source.Height;

        // Calculate scaled dimensions
        var (newWidth, newHeight) = CalculateScaledDimensions(
            originalWidth,
            originalHeight,
            maxWidth,
            maxHeight);

        // Scale if needed
        Bitmap outputBitmap;
        if (newWidth != originalWidth || newHeight != originalHeight)
        {
            outputBitmap = ScaleBitmap(source, newWidth, newHeight);
        }
        else
        {
            // No scaling needed - use source directly (don't dispose it here, caller owns it)
            outputBitmap = source;
        }

        try
        {
            // Encode to requested format
            var data = imageFormat switch
            {
                Models.ImageFormat.Jpeg => EncodeToJpeg(outputBitmap, quality),
                Models.ImageFormat.Png => EncodeToPng(outputBitmap),
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, "Unsupported image format")
            };

            return new ProcessedImage(
                data,
                newWidth,
                newHeight,
                originalWidth,
                originalHeight,
                imageFormat.ToString().ToLowerInvariant());
        }
        finally
        {
            // Only dispose if we created a new bitmap (scaled)
            if (outputBitmap != source)
            {
                outputBitmap.Dispose();
            }
        }
    }

    /// <summary>
    /// Calculates scaled dimensions while preserving aspect ratio.
    /// </summary>
    /// <param name="originalWidth">Original width in pixels.</param>
    /// <param name="originalHeight">Original height in pixels.</param>
    /// <param name="maxWidth">Maximum width constraint (0 = no constraint).</param>
    /// <param name="maxHeight">Maximum height constraint (0 = no constraint).</param>
    /// <returns>Scaled dimensions that fit within constraints.</returns>
    public static (int width, int height) CalculateScaledDimensions(
        int originalWidth,
        int originalHeight,
        int maxWidth,
        int maxHeight)
    {
        // No constraints - return original size
        if (maxWidth <= 0 && maxHeight <= 0)
        {
            return (originalWidth, originalHeight);
        }

        // Calculate the scaling ratios
        double widthRatio = maxWidth > 0 ? (double)maxWidth / originalWidth : double.MaxValue;
        double heightRatio = maxHeight > 0 ? (double)maxHeight / originalHeight : double.MaxValue;

        // Use the smaller ratio to ensure both constraints are met
        double ratio = Math.Min(widthRatio, heightRatio);

        // Don't upscale - only downscale
        if (ratio >= 1.0)
        {
            return (originalWidth, originalHeight);
        }

        // Calculate new dimensions
        var newWidth = Math.Max(1, (int)(originalWidth * ratio));
        var newHeight = Math.Max(1, (int)(originalHeight * ratio));

        return (newWidth, newHeight);
    }

    /// <summary>
    /// Scales a bitmap using high-quality bicubic interpolation.
    /// </summary>
    /// <param name="source">Source bitmap to scale.</param>
    /// <param name="newWidth">Target width.</param>
    /// <param name="newHeight">Target height.</param>
    /// <returns>New scaled bitmap. Caller is responsible for disposing.</returns>
    public static Bitmap ScaleBitmap(Bitmap source, int newWidth, int newHeight)
    {
        ArgumentNullException.ThrowIfNull(source);

        var scaledBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

        try
        {
            using var graphics = Graphics.FromImage(scaledBitmap);

            // High quality scaling settings
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            graphics.DrawImage(source, 0, 0, newWidth, newHeight);

            return scaledBitmap;
        }
        catch
        {
            scaledBitmap.Dispose();
            throw;
        }
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
