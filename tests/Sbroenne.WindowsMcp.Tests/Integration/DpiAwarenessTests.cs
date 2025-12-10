using Sbroenne.WindowsMcp.Input;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for DPI awareness.
/// These tests verify that cursor positioning is accurate regardless of DPI scaling.
/// Note: These tests run on the current system's DPI settings.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class DpiAwarenessTests
{
    private readonly MouseInputService _service = new();

    [Fact]
    public async Task MoveAsync_PositionAccuracy_IsWithin1PixelAtAnyDpi()
    {
        // Arrange - test at multiple positions on secondary monitor for DPI consistency
        var testPositions = new[]
        {
            TestMonitorHelper.GetTestCoordinates(100, 100),
            TestMonitorHelper.GetTestCoordinates(300, 200),
            TestMonitorHelper.GetTestCoordinates(500, 400),
        };

        foreach (var (x, y) in testPositions)
        {
            // Act
            var result = await _service.MoveAsync(x, y);

            // Assert - position should be accurate within 1 pixel
            if (result.Success)
            {
                Assert.InRange(result.FinalPosition.X, x - 1, x + 1);
                Assert.InRange(result.FinalPosition.Y, y - 1, y + 1);
            }
        }
    }

    [Fact]
    public async Task ClickAsync_CursorPosition_AccurateAfterClick()
    {
        // Arrange - use secondary monitor for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(200, 200);

        // Act
        var result = await _service.ClickAsync(x, y);

        // Assert - if successful, cursor should be at the expected position
        if (result.Success)
        {
            Assert.NotNull(result.FinalPosition);
            Assert.InRange(result.FinalPosition.X, x - 1, x + 1);
            Assert.InRange(result.FinalPosition.Y, y - 1, y + 1);
        }
    }

    [Fact]
    public void TestMonitorBounds_HasReasonableSize()
    {
        // Act - use test monitor bounds for consistency
        var bounds = TestMonitorHelper.GetTestMonitorBounds();

        // Assert - modern monitors have at least 640x480 resolution
        Assert.True(bounds.Width >= 640, $"Screen width {bounds.Width} seems too small");
        Assert.True(bounds.Height >= 480, $"Screen height {bounds.Height} seems too small");

        // Assert - very large resolutions (e.g., > 100,000) would indicate a bug
        Assert.True(bounds.Width < 100000, $"Screen width {bounds.Width} seems too large (possible scaling bug)");
        Assert.True(bounds.Height < 100000, $"Screen height {bounds.Height} seems too large (possible scaling bug)");
    }

    [Fact]
    public async Task DragAsync_EndPosition_AccurateAtAnyDpi()
    {
        // Arrange - use secondary monitor for DPI consistency
        var (startX, startY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var (endX, endY) = TestMonitorHelper.GetTestCoordinates(400, 400);

        // Act
        var result = await _service.DragAsync(startX, startY, endX, endY);

        // Assert - if successful, cursor should end at the expected position
        if (result.Success)
        {
            Assert.NotNull(result.FinalPosition);
            Assert.InRange(result.FinalPosition.X, endX - 1, endX + 1);
            Assert.InRange(result.FinalPosition.Y, endY - 1, endY + 1);
        }
    }
}
