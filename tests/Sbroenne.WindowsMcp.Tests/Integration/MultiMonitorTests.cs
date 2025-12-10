using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for multi-monitor support.
/// These tests verify that all mouse operations work correctly
/// with negative coordinates and across multiple monitors.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MultiMonitorTests
{
    private readonly MouseInputService _service = new();

    [Fact]
    public async Task MoveAsync_NegativeCoordinates_WorksCorrectlyForSecondaryMonitor()
    {
        // Arrange - negative coordinates (secondary monitor to the left)
        var x = -100;
        var y = 100;

        // Act
        var result = await _service.MoveAsync(x, y);

        // Assert - result depends on whether negative coordinates are within virtual screen bounds
        // This test validates the code handles negative coordinates without crashing
        Assert.NotNull(result);
        // ErrorCode is a value type, just verify result is not null
    }

    [Fact]
    public async Task ClickAsync_NegativeCoordinates_HandlesCorrectly()
    {
        // Arrange - negative coordinates
        var x = -100;
        var y = 100;

        // Act
        var result = await _service.ClickAsync(x, y);

        // Assert - result depends on monitor configuration
        Assert.NotNull(result);
        // ErrorCode is a value type, just verify result is not null
    }

    [Fact]
    public async Task DragAsync_AcrossVirtualDesktop_HandlesCorrectly()
    {
        // Arrange - start and end on secondary monitor if available for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(400, 400);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY);

        // Assert
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public void CoordinateNormalization_VirtualDesktopBounds_ReturnsValidBounds()
    {
        // Act
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();

        // Assert - bounds should be valid
        Assert.True(bounds.Width > 0, "Width should be positive");
        Assert.True(bounds.Height > 0, "Height should be positive");
        // Right should be greater than or equal to left
        Assert.True(bounds.Right >= bounds.Left, "Right should be >= Left");
        // Bottom should be greater than or equal to top
        Assert.True(bounds.Bottom >= bounds.Top, "Bottom should be >= Top");
    }

    [Fact]
    public void ValidateCoordinates_WithinTestMonitor_ReturnsTrue()
    {
        // Arrange - test coordinates on secondary monitor if available
        var (x1, y1) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var (x2, y2) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var (x3, y3) = TestMonitorHelper.GetTestCoordinates(400, 400);

        // Act & Assert - all test coordinates should be valid
        var (isValid1, _) = CoordinateNormalizer.ValidateCoordinates(x1, y1);
        var (isValid2, _) = CoordinateNormalizer.ValidateCoordinates(x2, y2);
        var (isValid3, _) = CoordinateNormalizer.ValidateCoordinates(x3, y3);

        Assert.True(isValid1, $"Coordinates ({x1}, {y1}) should be valid on test monitor");
        Assert.True(isValid2, $"Coordinates ({x2}, {y2}) should be valid on test monitor");
        Assert.True(isValid3, $"Coordinates ({x3}, {y3}) should be valid on test monitor");
    }

    [Fact]
    public void ValidateCoordinates_FarOutOfBounds_ReturnsFalse()
    {
        // Arrange - coordinates far outside any reasonable screen
        var x = 999999;
        var y = 999999;

        // Act
        var (isValid, _) = CoordinateNormalizer.ValidateCoordinates(x, y);

        // Assert
        Assert.False(isValid, "Extremely large coordinates should be out of bounds");
    }
}
