using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse scroll operations.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MouseScrollTests
{
    private readonly MouseInputService _service = new();

    [Fact]
    public async Task ScrollAsync_DownAtCurrentPosition_Succeeds()
    {
        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Down, 1, null, null);

        // Assert - operation should succeed even if target is elevated
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_UpAtCurrentPosition_Succeeds()
    {
        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Up, 1, null, null);

        // Assert
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_LeftAtCurrentPosition_Succeeds()
    {
        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Left, 1, null, null);

        // Assert
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_RightAtCurrentPosition_Succeeds()
    {
        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Right, 1, null, null);

        // Assert
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(200, 200);

        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Down, 1, x, y);

        // Assert - if successful, cursor should be at specified position
        if (result.Success)
        {
            Assert.NotNull(result.FinalPosition);
            Assert.InRange(result.FinalPosition.X, x - 1, x + 1);
            Assert.InRange(result.FinalPosition.Y, y - 1, y + 1);
        }
    }

    [Fact]
    public async Task ScrollAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var x = -99999;
        var y = -99999;

        // Act
        var result = await _service.ScrollAsync(ScrollDirection.Down, 1, x, y);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task ScrollAsync_MultipleClicks_Succeeds()
    {
        // Act - scroll 5 clicks
        var result = await _service.ScrollAsync(ScrollDirection.Down, 5, null, null);

        // Assert
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_ZeroAmount_Succeeds()
    {
        // Act - scroll 0 clicks (effectively no scroll)
        var result = await _service.ScrollAsync(ScrollDirection.Down, 0, null, null);

        // Assert - should succeed even with 0 amount
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }
}
