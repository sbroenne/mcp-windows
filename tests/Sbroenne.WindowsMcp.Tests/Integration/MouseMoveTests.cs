using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse cursor movement operations.
/// These tests interact with the actual Windows input system.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseMoveTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseInputService _mouseInputService;

    public MouseMoveTests()
    {
        // Save original cursor position to restore after each test
        var currentPos = Coordinates.FromCurrent();
        _originalPosition = currentPos;
        _mouseInputService = new MouseInputService();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _mouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MoveAsync_ValidCoordinates_ReturnsSuccessWithFinalPosition()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(100, 100);

        // Act
        var result = await _mouseInputService.MoveAsync(targetX, targetY);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MouseControlErrorCode.Success, result.ErrorCode);
        // Allow 1 pixel tolerance due to DPI scaling and rounding
        Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
        Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);
    }

    [Fact]
    public async Task MoveAsync_NegativeCoordinates_WorksCorrectlyForSecondaryMonitor()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();

        // Only run if virtual desktop has negative coordinates (multi-monitor setup with left monitor)
        if (bounds.Left >= 0)
        {
            // Skip test - no secondary monitor on the left
            return;
        }

        var targetX = bounds.Left + 50; // Should be negative
        var targetY = bounds.Top + 50;

        // Act
        var result = await _mouseInputService.MoveAsync(targetX, targetY);

        // Assert - allow 1 pixel tolerance due to DPI scaling
        Assert.True(result.Success);
        Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
        Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);
    }

    [Fact]
    public async Task MoveAsync_OutOfBoundsCoordinates_ReturnsCoordinatesOutOfBoundsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        // Use coordinates well outside the virtual desktop
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _mouseInputService.MoveAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("out of bounds", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        // Verify bounds are returned for error context
        Assert.NotNull(result.ScreenBounds);
        Assert.Equal(bounds, result.ScreenBounds);
    }

    [Fact]
    public async Task MoveAsync_PositionAccuracy_MatchesRequestedCoordinatesWithin1Pixel()
    {
        // Arrange - use test monitor for DPI consistency
        var bounds = TestMonitorHelper.GetTestMonitorBounds();
        // Test multiple positions across the test monitor
        var testCoordinates = new[]
        {
            TestMonitorHelper.GetTestCoordinates(100, 100),
            TestMonitorHelper.GetTestMonitorCenter(),
            (X: bounds.Right - 100, Y: bounds.Bottom - 100),
        };

        foreach (var (targetX, targetY) in testCoordinates)
        {
            // Act
            var result = await _mouseInputService.MoveAsync(targetX, targetY);

            // Assert - verify position is within 1 pixel of target (DPI rounding tolerance)
            Assert.True(result.Success, $"Move to ({targetX}, {targetY}) should succeed");
            Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
            Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);

            // Verify with actual cursor position using the public API
            var actualPos = Coordinates.FromCurrent();
            Assert.InRange(actualPos.X, targetX - 1, targetX + 1);
            Assert.InRange(actualPos.Y, targetY - 1, targetY + 1);
        }
    }

    [Fact]
    public async Task MoveAsync_BoundaryCoordinates_WorksAtScreenEdges()
    {
        // Arrange - use preferred test monitor
        var bounds = TestMonitorHelper.GetTestMonitorBounds();

        // Test top-left corner
        var result1 = await _mouseInputService.MoveAsync(bounds.Left, bounds.Top);
        Assert.True(result1.Success);

        // Test bottom-right corner (minus 1 to stay on screen)
        var result2 = await _mouseInputService.MoveAsync(bounds.Right - 1, bounds.Bottom - 1);
        Assert.True(result2.Success);
    }

    [Fact]
    public async Task MoveAsync_RapidMoves_AllSucceed()
    {
        // Arrange - test 10 rapid move operations on preferred test monitor
        var bounds = TestMonitorHelper.GetTestMonitorBounds();
        var random = new Random(42); // Fixed seed for reproducibility
        var moveCount = 10;

        // Act & Assert
        for (int i = 0; i < moveCount; i++)
        {
            var targetX = random.Next(bounds.Left, bounds.Right);
            var targetY = random.Next(bounds.Top, bounds.Bottom);

            var result = await _mouseInputService.MoveAsync(targetX, targetY);
            Assert.True(result.Success, $"Move {i + 1} to ({targetX}, {targetY}) should succeed");
        }
    }
}
