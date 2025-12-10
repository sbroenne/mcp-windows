using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse cursor movement operations.
/// These tests use a dedicated Notepad window to avoid interfering with user's work.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseMoveTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseMoveTests(MouseTestFixture fixture)
    {
        _fixture = fixture;
        // Save original cursor position to restore after each test
        _originalPosition = Coordinates.FromCurrent();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _fixture.MouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MoveAsync_ValidCoordinates_ReturnsSuccessWithFinalPosition()
    {
        // Arrange - use test window coordinates
        var (targetX, targetY) = _fixture.GetTestWindowCoordinates(100, 100);

        // Act
        var result = await _fixture.MouseInputService.MoveAsync(targetX, targetY);

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
        var result = await _fixture.MouseInputService.MoveAsync(targetX, targetY);

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
        var result = await _fixture.MouseInputService.MoveAsync(targetX, targetY);

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
        // Arrange - use test window for DPI consistency
        var bounds = _fixture.TestWindowBounds;
        // Test multiple positions within the test window
        var testCoordinates = new[]
        {
            _fixture.GetTestWindowCoordinates(50, 50),
            _fixture.GetTestWindowCenter(),
            _fixture.GetTestWindowCoordinates(bounds.Width - 50, bounds.Height - 50),
        };

        foreach (var (targetX, targetY) in testCoordinates)
        {
            // Act
            var result = await _fixture.MouseInputService.MoveAsync(targetX, targetY);

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
    public async Task MoveAsync_BoundaryCoordinates_WorksAtWindowEdges()
    {
        // Arrange - use test window edges
        var bounds = _fixture.TestWindowBounds;

        // Test top-left corner of window
        var result1 = await _fixture.MouseInputService.MoveAsync(bounds.Left + 5, bounds.Top + 5);
        Assert.True(result1.Success);

        // Test bottom-right corner of window (minus margin to stay in window)
        var result2 = await _fixture.MouseInputService.MoveAsync(bounds.Right - 5, bounds.Bottom - 5);
        Assert.True(result2.Success);
    }

    [Fact]
    public async Task MoveAsync_RapidMoves_AllSucceed()
    {
        // Arrange - test 10 rapid move operations within test window
        var bounds = _fixture.TestWindowBounds;
        var random = new Random(42); // Fixed seed for reproducibility
        var moveCount = 10;

        // Act & Assert
        for (int i = 0; i < moveCount; i++)
        {
            // Stay within window bounds with 10px margin
            var targetX = random.Next(bounds.Left + 10, bounds.Right - 10);
            var targetY = random.Next(bounds.Top + 10, bounds.Bottom - 10);

            var result = await _fixture.MouseInputService.MoveAsync(targetX, targetY);
            Assert.True(result.Success, $"Move {i + 1} to ({targetX}, {targetY}) should succeed");
        }
    }
}
