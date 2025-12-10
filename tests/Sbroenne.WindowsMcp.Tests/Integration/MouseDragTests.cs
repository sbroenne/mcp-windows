using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse drag operations.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MouseDragTests
{
    private readonly MouseInputService _service = new();

    [Fact]
    public async Task DragAsync_LeftButton_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(300, 300);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY, MouseButton.Left);

        // Assert - operation should succeed even if target is elevated
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task DragAsync_RightButton_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(300, 300);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY, MouseButton.Right);

        // Assert - operation should succeed even if target is elevated
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task DragAsync_MiddleButton_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(300, 300);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY, MouseButton.Middle);

        // Assert - operation should succeed even if target is elevated
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task DragAsync_OutOfBoundsStart_ReturnsError()
    {
        // Arrange - start coordinates outside screen bounds, end on test monitor
        var startX = -99999;
        var startY = -99999;
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(200, 200);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task DragAsync_OutOfBoundsEnd_ReturnsError()
    {
        // Arrange - start on test monitor, end coordinates outside screen bounds
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var endX = 999999;
        var endY = 999999;

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task DragAsync_CursorEndsAtEndPosition()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(400, 400);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY);

        // Assert - if successful, cursor should be near end position
        if (result.Success)
        {
            Assert.NotNull(result.FinalPosition);
            // Allow 1 pixel tolerance for position accuracy
            Assert.InRange(result.FinalPosition.X, endX - 1, endX + 1);
            Assert.InRange(result.FinalPosition.Y, endY - 1, endY + 1);
        }
    }

    [Fact]
    public async Task DragAsync_SameStartAndEnd_Succeeds()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(200, 200);

        // Act
        var result = await _service.DragAsync(x, y, x, y);

        // Assert - should succeed (essentially a click and release)
        Assert.True(
            result.ErrorCode == MouseControlErrorCode.Success ||
            result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or elevated process target, got {result.ErrorCode}: {result.ErrorMessage}");
    }
}
