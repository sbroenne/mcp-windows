using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="CaptureRegion"/> validation.
/// </summary>
public sealed class CaptureRegionValidationTests
{
    [Theory]
    [InlineData(0, 0, 100, 100, true)]
    [InlineData(100, 100, 800, 600, true)]
    [InlineData(-100, -100, 400, 300, true)] // Negative coordinates are valid (multi-monitor)
    [InlineData(0, 0, 1, 1, true)] // Minimum valid size
    public void IsValid_WithPositiveDimensions_ReturnsTrue(int x, int y, int width, int height, bool expected)
    {
        // Arrange
        var region = new CaptureRegion(x, y, width, height);

        // Act
        var result = region.IsValid();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 0, 0, 100)] // Zero width
    [InlineData(0, 0, 100, 0)] // Zero height
    [InlineData(0, 0, 0, 0)] // Zero both
    [InlineData(0, 0, -100, 100)] // Negative width
    [InlineData(0, 0, 100, -100)] // Negative height
    [InlineData(0, 0, -1, -1)] // Both negative
    public void IsValid_WithInvalidDimensions_ReturnsFalse(int x, int y, int width, int height)
    {
        // Arrange
        var region = new CaptureRegion(x, y, width, height);

        // Act
        var result = region.IsValid();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(100, 100, 10000)]
    [InlineData(1920, 1080, 2073600)] // Full HD
    [InlineData(3840, 2160, 8294400)] // 4K
    [InlineData(7680, 4320, 33177600)] // 8K
    public void TotalPixels_CalculatesCorrectly(int width, int height, long expected)
    {
        // Arrange
        var region = new CaptureRegion(0, 0, width, height);

        // Act
        var result = region.TotalPixels;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TotalPixels_LargeValues_DoesNotOverflow()
    {
        // Arrange - values that would overflow int32 multiplication
        var region = new CaptureRegion(0, 0, 50000, 50000);

        // Act
        var result = region.TotalPixels;

        // Assert - should be 2,500,000,000 (2.5 billion)
        Assert.Equal(2_500_000_000L, result);
    }

    [Fact]
    public void CaptureRegion_IsImmutableRecord()
    {
        // Arrange
        var region = new CaptureRegion(100, 200, 300, 400);

        // Assert - values match constructor
        Assert.Equal(100, region.X);
        Assert.Equal(200, region.Y);
        Assert.Equal(300, region.Width);
        Assert.Equal(400, region.Height);
    }

    [Fact]
    public void CaptureRegion_Equality_WorksCorrectly()
    {
        // Arrange
        var region1 = new CaptureRegion(100, 200, 300, 400);
        var region2 = new CaptureRegion(100, 200, 300, 400);
        var region3 = new CaptureRegion(100, 200, 300, 401);

        // Assert
        Assert.Equal(region1, region2);
        Assert.NotEqual(region1, region3);
    }

    [Fact]
    public void CaptureRegion_WithExpression_CreatesNewInstance()
    {
        // Arrange
        var original = new CaptureRegion(100, 200, 300, 400);

        // Act
        var modified = original with { Width = 500 };

        // Assert
        Assert.Equal(300, original.Width);
        Assert.Equal(500, modified.Width);
        Assert.Equal(original.X, modified.X);
        Assert.Equal(original.Y, modified.Y);
        Assert.Equal(original.Height, modified.Height);
    }
}
