using System.Diagnostics;
using System.Drawing.Imaging;
using Sbroenne.WindowsMcp.Models;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Provides visual diff computation between screenshots using pixel-by-pixel comparison.
/// </summary>
public sealed class VisualDiffService : IVisualDiffService
{
    /// <inheritdoc />
    public Task<VisualDiffResult> ComputeDiffAsync(
        string beforeImageBase64,
        string afterImageBase64,
        string beforeImageName,
        string afterImageName,
        VisualDiffOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beforeImageBase64);
        ArgumentNullException.ThrowIfNull(afterImageBase64);
        ArgumentNullException.ThrowIfNull(beforeImageName);
        ArgumentNullException.ThrowIfNull(afterImageName);

        options ??= new VisualDiffOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Decode images from base64
            using var beforeBitmap = DecodeBase64ToBitmap(beforeImageBase64);
            using var afterBitmap = DecodeBase64ToBitmap(afterImageBase64);

            cancellationToken.ThrowIfCancellationRequested();

            // Check dimensions match
            if (beforeBitmap.Width != afterBitmap.Width || beforeBitmap.Height != afterBitmap.Height)
            {
                stopwatch.Stop();
                return Task.FromResult(new VisualDiffResult
                {
                    BeforeImage = beforeImageName,
                    AfterImage = afterImageName,
                    ChangedPixels = 0,
                    TotalPixels = 0,
                    ChangePercentage = 0,
                    Threshold = options.Threshold,
                    IsSignificantChange = false,
                    PixelTolerance = options.PixelTolerance,
                    DimensionsMismatch = true,
                    ComputationTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    Error = $"Image dimensions do not match: before={beforeBitmap.Width}x{beforeBitmap.Height}, after={afterBitmap.Width}x{afterBitmap.Height}"
                });
            }

            // Compute pixel-by-pixel difference
            var result = ComputePixelDiff(
                beforeBitmap,
                afterBitmap,
                beforeImageName,
                afterImageName,
                options,
                cancellationToken);

            stopwatch.Stop();
            return Task.FromResult(result with { ComputationTimeMs = (int)stopwatch.ElapsedMilliseconds });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(new VisualDiffResult
            {
                BeforeImage = beforeImageName,
                AfterImage = afterImageName,
                ChangedPixels = 0,
                TotalPixels = 0,
                ChangePercentage = 0,
                Threshold = options.Threshold,
                IsSignificantChange = false,
                PixelTolerance = options.PixelTolerance,
                ComputationTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Error = $"Failed to compute visual diff: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Decodes a base64 string to a Bitmap.
    /// </summary>
    private static Bitmap DecodeBase64ToBitmap(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    /// <summary>
    /// Computes pixel-by-pixel difference between two bitmaps.
    /// </summary>
    private static VisualDiffResult ComputePixelDiff(
        Bitmap before,
        Bitmap after,
        string beforeName,
        string afterName,
        VisualDiffOptions options,
        CancellationToken cancellationToken)
    {
        var width = before.Width;
        var height = before.Height;
        var totalPixels = width * height;
        var changedPixels = 0;

        // Create diff bitmap if generating diff image
        Bitmap? diffBitmap = options.GenerateDiffImage ? new Bitmap(width, height, PixelFormat.Format32bppArgb) : null;

        try
        {
            // Lock bits for fast pixel access
            var beforeRect = new Rectangle(0, 0, width, height);
            var afterRect = new Rectangle(0, 0, width, height);

            var beforeData = before.LockBits(beforeRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var afterData = after.LockBits(afterRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            BitmapData? diffData = null;
            if (diffBitmap is not null)
            {
                diffData = diffBitmap.LockBits(beforeRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            }

            try
            {
                unsafe
                {
                    var beforePtr = (byte*)beforeData.Scan0;
                    var afterPtr = (byte*)afterData.Scan0;
                    var diffPtr = diffData is not null ? (byte*)diffData.Scan0 : null;

                    var beforeStride = beforeData.Stride;
                    var afterStride = afterData.Stride;
                    var diffStride = diffData?.Stride ?? 0;

                    var highlightColor = options.HighlightColor;
                    var highlightA = (byte)((highlightColor >> 24) & 0xFF);
                    var highlightR = (byte)((highlightColor >> 16) & 0xFF);
                    var highlightG = (byte)((highlightColor >> 8) & 0xFF);
                    var highlightB = (byte)(highlightColor & 0xFF);

                    for (var y = 0; y < height; y++)
                    {
                        if (y % 100 == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        var beforeRow = beforePtr + (y * beforeStride);
                        var afterRow = afterPtr + (y * afterStride);
                        var diffRow = diffPtr is not null ? diffPtr + (y * diffStride) : null;

                        for (var x = 0; x < width; x++)
                        {
                            var offset = x * 4; // BGRA format

                            var beforeB = beforeRow[offset];
                            var beforeG = beforeRow[offset + 1];
                            var beforeR = beforeRow[offset + 2];

                            var afterB = afterRow[offset];
                            var afterG = afterRow[offset + 1];
                            var afterR = afterRow[offset + 2];

                            var isDifferent = Math.Abs(beforeR - afterR) > options.PixelTolerance ||
                                              Math.Abs(beforeG - afterG) > options.PixelTolerance ||
                                              Math.Abs(beforeB - afterB) > options.PixelTolerance;

                            if (isDifferent)
                            {
                                changedPixels++;
                            }

                            if (diffRow is not null)
                            {
                                if (isDifferent)
                                {
                                    // Blend highlight color with after image
                                    diffRow[offset] = BlendChannel(afterB, highlightB, highlightA);
                                    diffRow[offset + 1] = BlendChannel(afterG, highlightG, highlightA);
                                    diffRow[offset + 2] = BlendChannel(afterR, highlightR, highlightA);
                                    diffRow[offset + 3] = 255;
                                }
                                else
                                {
                                    // Copy after image pixel
                                    diffRow[offset] = afterB;
                                    diffRow[offset + 1] = afterG;
                                    diffRow[offset + 2] = afterR;
                                    diffRow[offset + 3] = 255;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                before.UnlockBits(beforeData);
                after.UnlockBits(afterData);
                if (diffData is not null && diffBitmap is not null)
                {
                    diffBitmap.UnlockBits(diffData);
                }
            }

            // Calculate change percentage
            var changePercentage = totalPixels > 0 ? (changedPixels / (double)totalPixels) * 100 : 0;
            var isSignificant = changePercentage > options.Threshold;

            // Encode diff image to base64 if generated
            string? diffImageBase64 = null;
            if (diffBitmap is not null && changedPixels > 0)
            {
                using var memoryStream = new MemoryStream();
                diffBitmap.Save(memoryStream, DrawingImageFormat.Png);
                diffImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            return new VisualDiffResult
            {
                BeforeImage = beforeName,
                AfterImage = afterName,
                DiffImage = changedPixels > 0 ? $"{Path.GetFileNameWithoutExtension(beforeName)}-diff.png" : null,
                DiffImageData = diffImageBase64,
                ChangedPixels = changedPixels,
                TotalPixels = totalPixels,
                ChangePercentage = Math.Round(changePercentage, 2),
                Threshold = options.Threshold,
                IsSignificantChange = isSignificant,
                PixelTolerance = options.PixelTolerance,
                DimensionsMismatch = false
            };
        }
        finally
        {
            diffBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Blends two color channels based on alpha value.
    /// </summary>
    private static byte BlendChannel(byte background, byte foreground, byte alpha)
    {
        var a = alpha / 255.0;
        return (byte)((foreground * a) + (background * (1 - a)));
    }
}
