using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse middle-click operations.
/// These tests interact with the actual Windows input system.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseMiddleClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseInputService _mouseInputService;

    public MouseMiddleClickTests()
    {
        // Save original cursor position to restore after each test
        _originalPosition = Coordinates.FromCurrent();
        _mouseInputService = new MouseInputService();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _mouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MiddleClickAsync_AtCurrentPosition_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (safeX, safeY) = TestMonitorHelper.GetTestCoordinates(100, 100);
        await _mouseInputService.MoveAsync(safeX, safeY);

        // Act
        var result = await _mouseInputService.MiddleClickAsync(null, null);

        // Assert
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            Assert.InRange(result.FinalPosition.X, safeX - 1, safeX + 1);
            Assert.InRange(result.FinalPosition.Y, safeY - 1, safeY + 1);
        }
    }

    [Fact]
    public async Task MiddleClickAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(175, 175);

        // Act
        var result = await _mouseInputService.MiddleClickAsync(targetX, targetY);

        // Assert
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
            Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);
        }
    }

    [Fact]
    public async Task MiddleClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _mouseInputService.MiddleClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }
}
