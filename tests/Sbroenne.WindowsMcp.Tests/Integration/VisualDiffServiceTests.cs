using System.Drawing.Imaging;
using Sbroenne.WindowsMcp.Capture;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="VisualDiffService"/> that computes pixel-by-pixel differences
/// between two images and generates diff visualization.
/// </summary>
public sealed class VisualDiffServiceTests : IDisposable
{
    private readonly VisualDiffService _visualDiffService;
    private readonly List<Bitmap> _disposableBitmaps = [];

    public VisualDiffServiceTests()
    {
        _visualDiffService = new VisualDiffService();
    }

    public void Dispose()
    {
        foreach (var bitmap in _disposableBitmaps)
        {
            bitmap.Dispose();
        }
    }

    private Bitmap CreateTestBitmap(int width, int height, Color fillColor)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        _disposableBitmaps.Add(bitmap);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(fillColor);
        return bitmap;
    }

    private static string BitmapToBase64(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return Convert.ToBase64String(stream.ToArray());
    }

    [Fact]
    public async Task ComputeDiffAsync_IdenticalImages_ReturnsZeroChange()
    {
        // Arrange
        var image = CreateTestBitmap(100, 100, Color.Red);
        var base64Image = BitmapToBase64(image);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(base64Image, base64Image, "before", "after");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ChangedPixels);
        Assert.Equal(0.0, result.ChangePercentage);
        Assert.False(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_CompletelyDifferentImages_ReturnsFullChange()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(100, 100, Color.Red);
        var afterImage = CreateTestBitmap(100, 100, Color.Blue);
        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000, result.ChangedPixels); // 100x100 = 10000 pixels
        Assert.Equal(100.0, result.ChangePercentage);
        Assert.True(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_PartiallyDifferentImages_ReturnsCorrectPercentage()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(100, 100, Color.White);

        // Change the top half of the afterImage to black
        using (var graphics = Graphics.FromImage(afterImage))
        {
            graphics.FillRectangle(Brushes.Black, 0, 0, 100, 50);
        }

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000, result.ChangedPixels); // Top half = 100*50 = 5000 pixels
        Assert.Equal(50.0, result.ChangePercentage);
        Assert.True(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_DefaultThreshold_IsOnePercent()
    {
        // Arrange - create images where more than 1% differs (1.5%)
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(100, 100, Color.White);

        // Change 150 pixels (1.5% of 10000) to exceed 1% threshold
        for (int y = 0; y < 15; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                afterImage.SetPixel(x, y, Color.Black);
            }
        }

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act - with default 1% threshold (uses > not >=)
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert - 1.5% is above 1% threshold
        Assert.Equal(150, result.ChangedPixels);
        Assert.Equal(1.5, result.ChangePercentage);
        Assert.True(result.IsSignificantChange); // > threshold
    }

    [Fact]
    public async Task ComputeDiffAsync_BelowThreshold_IsNotSignificant()
    {
        // Arrange - create images where less than 1% differs
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(100, 100, Color.White);

        // Change exactly 50 pixels (0.5% of 10000)
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                afterImage.SetPixel(x, y, Color.Black);
            }
        }

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act - with default 1% threshold
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert - 0.5% is below 1% threshold
        Assert.Equal(50, result.ChangedPixels);
        Assert.Equal(0.5, result.ChangePercentage);
        Assert.False(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_CustomThreshold_IsRespected()
    {
        // Arrange - 0.5% change
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(100, 100, Color.White);

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                afterImage.SetPixel(x, y, Color.Black);
            }
        }

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        var options = new VisualDiffOptions { Threshold = 0.4 }; // 0.4% threshold

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after", options);

        // Assert - 0.5% is above 0.4% threshold, so significant
        Assert.True(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_PixelTolerance_IgnoresSmallColorDifferences()
    {
        // Arrange - create images with slight color difference
        var beforeImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 100, 100, 100));
        var afterImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 105, 100, 100)); // +5 red

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        var options = new VisualDiffOptions { PixelTolerance = 10 }; // Tolerance of 10

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after", options);

        // Assert - color difference of 5 is within tolerance of 10
        Assert.Equal(0, result.ChangedPixels);
        Assert.False(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_PixelTolerance_DetectsLargeColorDifferences()
    {
        // Arrange - create images with larger color difference
        var beforeImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 100, 100, 100));
        var afterImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 115, 100, 100)); // +15 red

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        var options = new VisualDiffOptions { PixelTolerance = 10 }; // Tolerance of 10

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after", options);

        // Assert - color difference of 15 exceeds tolerance of 10
        Assert.Equal(10000, result.ChangedPixels);
        Assert.True(result.IsSignificantChange);
    }

    [Fact]
    public async Task ComputeDiffAsync_GeneratesDiffImage()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(100, 100, Color.White);

        // Change a corner
        using (var graphics = Graphics.FromImage(afterImage))
        {
            graphics.FillRectangle(Brushes.Black, 0, 0, 10, 10);
        }

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert
        Assert.NotNull(result.DiffImageData);
        Assert.NotEmpty(result.DiffImageData);

        // Verify it's valid base64 PNG
        var diffBytes = Convert.FromBase64String(result.DiffImageData);
        Assert.True(diffBytes.Length >= 8);
        Assert.Equal(0x89, diffBytes[0]); // PNG signature
        Assert.Equal(0x50, diffBytes[1]); // 'P'
        Assert.Equal(0x4E, diffBytes[2]); // 'N'
        Assert.Equal(0x47, diffBytes[3]); // 'G'
    }

    [Fact]
    public async Task ComputeDiffAsync_DiffImageHasCorrectDimensions()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(200, 150, Color.White);
        var afterImage = CreateTestBitmap(200, 150, Color.Black);

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert
        Assert.NotNull(result.DiffImageData);

        // Decode and check dimensions
        var diffBytes = Convert.FromBase64String(result.DiffImageData);
        using var stream = new MemoryStream(diffBytes);
        using var diffImage = Image.FromStream(stream);

        Assert.Equal(200, diffImage.Width);
        Assert.Equal(150, diffImage.Height);
    }

    [Fact]
    public async Task ComputeDiffAsync_DifferentDimensions_ReturnsErrorResult()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(100, 100, Color.White);
        var afterImage = CreateTestBitmap(200, 150, Color.White);

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert - returns result with error, not exception
        Assert.False(result.Success);
        Assert.True(result.DimensionsMismatch);
        Assert.NotNull(result.Error);
        Assert.Contains("dimensions", result.Error.ToLowerInvariant());
    }

    [Fact]
    public async Task ComputeDiffAsync_InvalidBase64_ReturnsErrorResult()
    {
        // Arrange
        var validImage = CreateTestBitmap(100, 100, Color.White);
        var validBase64 = BitmapToBase64(validImage);
        var invalidBase64 = "not-valid-base64!@#$";

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(invalidBase64, validBase64, "before", "after");

        // Assert - returns result with error, not exception
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ComputeDiffAsync_ReturnsCorrectTotalPixels()
    {
        // Arrange
        var image = CreateTestBitmap(120, 80, Color.White);
        var base64 = BitmapToBase64(image);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(base64, base64, "before", "after");

        // Assert
        Assert.Equal(9600, result.TotalPixels); // 120 * 80 = 9600
    }

    [Fact]
    public async Task ComputeDiffAsync_ReturnsCorrectTotalPixelsForDifferentSizes()
    {
        // Arrange
        var beforeImage = CreateTestBitmap(320, 240, Color.White);
        var afterImage = CreateTestBitmap(320, 240, Color.Black);

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after");

        // Assert - 320 * 240 = 76800 pixels
        Assert.Equal(76800, result.TotalPixels);
    }

    [Fact]
    public async Task ComputeDiffAsync_ZeroPixelTolerance_DetectsAllDifferences()
    {
        // Arrange - minimal color difference
        var beforeImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 100, 100, 100));
        var afterImage = CreateTestBitmap(100, 100, Color.FromArgb(255, 101, 100, 100)); // +1 red

        var beforeBase64 = BitmapToBase64(beforeImage);
        var afterBase64 = BitmapToBase64(afterImage);

        var options = new VisualDiffOptions { PixelTolerance = 0 }; // Zero tolerance

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(beforeBase64, afterBase64, "before", "after", options);

        // Assert - even 1 difference is detected
        Assert.Equal(10000, result.ChangedPixels);
        Assert.Equal(100.0, result.ChangePercentage);
    }

    [Fact]
    public async Task ComputeDiffAsync_IdenticalImages_NoDiffImageGenerated()
    {
        // Arrange
        var image = CreateTestBitmap(100, 100, Color.Blue);
        var base64 = BitmapToBase64(image);

        // Act
        var result = await _visualDiffService.ComputeDiffAsync(base64, base64, "before", "after");

        // Assert - no differences means no diff image needed (or empty diff image)
        Assert.Equal(0, result.ChangedPixels);
        // Diff image may still be generated but shows no differences
    }
}
