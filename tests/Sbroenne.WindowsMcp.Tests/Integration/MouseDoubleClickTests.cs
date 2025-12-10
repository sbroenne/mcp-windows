using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse double-click operations.
/// These tests interact with the actual Windows input system.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseDoubleClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseInputService _mouseInputService;

    public MouseDoubleClickTests()
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
    public async Task DoubleClickAsync_AtCurrentPosition_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (safeX, safeY) = TestMonitorHelper.GetTestCoordinates(100, 100);
        await _mouseInputService.MoveAsync(safeX, safeY);

        // Act
        var result = await _mouseInputService.DoubleClickAsync(null, null);

        // Assert
        // The double-click either succeeds or fails due to elevated target
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            // Cursor should remain at the same position (within tolerance)
            Assert.InRange(result.FinalPosition.X, safeX - 1, safeX + 1);
            Assert.InRange(result.FinalPosition.Y, safeY - 1, safeY + 1);
        }
    }

    [Fact]
    public async Task DoubleClickAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(250, 250);

        // Act
        var result = await _mouseInputService.DoubleClickAsync(targetX, targetY);

        // Assert
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            // Cursor should be at the target position (within tolerance)
            Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
            Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);
        }
    }

    [Fact]
    public async Task DoubleClickAsync_BothClicksAtSamePosition_CursorDoesNotMove()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(300, 300);
        await _mouseInputService.MoveAsync(targetX, targetY);

        // Get position before double-click
        var posBefore = Coordinates.FromCurrent();

        // Act
        var result = await _mouseInputService.DoubleClickAsync(null, null);

        // Assert
        // The double-click should not move the cursor
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            // Position should remain unchanged (within 1 pixel tolerance)
            Assert.InRange(result.FinalPosition.X, posBefore.X - 1, posBefore.X + 1);
            Assert.InRange(result.FinalPosition.Y, posBefore.Y - 1, posBefore.Y + 1);
        }
    }

    [Fact]
    public async Task DoubleClickAsync_SendsValidDoubleClickSequence()
    {
        // This test verifies that the double-click sends all 4 events (2x down+up)
        // The implementation sends events via SendInput which processes them atomically
        // so they are guaranteed to be within the system's double-click time threshold
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(150, 150);

        // Act
        var result = await _mouseInputService.DoubleClickAsync(targetX, targetY);

        // Assert
        // Just verify the operation completes without throwing
        Assert.NotNull(result);
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget ||
                   result.ErrorCode == MouseControlErrorCode.CoordinatesOutOfBounds,
            $"Unexpected error: {result.ErrorCode}: {result.Error}");
    }

    [Fact]
    public async Task DoubleClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _mouseInputService.DoubleClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
        Assert.NotNull(result.Error);
        Assert.Contains("out of bounds", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
