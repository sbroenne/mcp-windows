using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse middle-click operations.
/// These tests use a dedicated test harness window to verify middle-clicks are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseMiddleClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseMiddleClickTests(MouseTestFixture fixture)
    {
        _fixture = fixture;
        // Save original cursor position to restore after each test
        _originalPosition = Coordinates.FromCurrent();

        // Reset harness state before each test
        _fixture.Reset();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _fixture.MouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MiddleClickAsync_OnPanel_VerifiedByHarness()
    {
        // Arrange - middle-click on the right-click panel (which also tracks middle clicks)
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.MiddleClickAsync(panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness actually received the middle-click
        var middleClickReceived = await _fixture.WaitForMiddleClickAsync(1);
        Assert.True(middleClickReceived, "Test harness did not receive the middle-click");
        _fixture.AssertMiddleClickDetected(1);
    }

    [Fact]
    public async Task MiddleClickAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - middle-click on the panel
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.MiddleClickAsync(panelCenter.X, panelCenter.Y);

        // Assert - API returns success and cursor is at expected position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, panelCenter.X - 2, panelCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, panelCenter.Y - 2, panelCenter.Y + 2);

        // Assert - harness verifies middle-click was received
        var middleClickReceived = await _fixture.WaitForMiddleClickAsync(1);
        Assert.True(middleClickReceived, "Test harness did not receive the middle-click");
    }

    [Fact]
    public async Task MiddleClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _fixture.MouseInputService.MiddleClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task MiddleClickAsync_MultipleMiddleClicks_AllVerifiedByHarness()
    {
        // Arrange - middle-click the panel 3 times
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act - middle-click 3 times
        for (var i = 0; i < 3; i++)
        {
            var result = await _fixture.MouseInputService.MiddleClickAsync(panelCenter.X, panelCenter.Y);
            Assert.True(result.Success, $"Middle-click {i + 1} failed: {result.ErrorCode}: {result.Error}");
            await Task.Delay(50); // Small delay between clicks
        }

        // Assert - harness received all 3 middle-clicks
        var allMiddleClicksReceived = await _fixture.WaitForMiddleClickAsync(3);
        Assert.True(allMiddleClicksReceived, $"Expected 3 middle-clicks but harness received {_fixture.GetMiddleClickCount()}");
    }

    [Fact]
    public async Task MiddleClickAsync_OnButton_VerifiedByHarness()
    {
        // Arrange - middle-click on the test button (which tracks middle clicks via MouseDown)
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.MiddleClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received the middle-click
        var middleClickReceived = await _fixture.WaitForMiddleClickAsync(1);
        Assert.True(middleClickReceived, "Test harness did not receive the middle-click on button");
    }
}
