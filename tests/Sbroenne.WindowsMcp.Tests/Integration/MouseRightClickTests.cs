using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse right-click operations.
/// These tests use a dedicated test harness window to verify right-clicks are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseRightClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseRightClickTests(MouseTestFixture fixture)
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
        // Press Escape to close any context menu that might have been opened
        SendKeys.SendWait("{ESC}");
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RightClickAsync_OnPanel_VerifiedByHarness()
    {
        // Arrange - right-click on the right-click panel
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.RightClickAsync(panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness actually received the right-click
        var rightClickReceived = await _fixture.WaitForRightClickAsync(1);
        Assert.True(rightClickReceived, "Test harness did not receive the right-click");
        _fixture.AssertRightClickDetected(1);
    }

    [Fact]
    public async Task RightClickAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - right-click on the panel
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.RightClickAsync(panelCenter.X, panelCenter.Y);

        // Assert - API returns success and cursor is at expected position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, panelCenter.X - 2, panelCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, panelCenter.Y - 2, panelCenter.Y + 2);

        // Assert - harness verifies right-click was received
        var rightClickReceived = await _fixture.WaitForRightClickAsync(1);
        Assert.True(rightClickReceived, "Test harness did not receive the right-click");
    }

    [Fact]
    public async Task RightClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _fixture.MouseInputService.RightClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task RightClickAsync_MultipleRightClicks_AllVerifiedByHarness()
    {
        // Arrange - right-click the panel 3 times
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act - right-click 3 times
        for (var i = 0; i < 3; i++)
        {
            var result = await _fixture.MouseInputService.RightClickAsync(panelCenter.X, panelCenter.Y);
            Assert.True(result.Success, $"Right-click {i + 1} failed: {result.ErrorCode}: {result.Error}");
            await Task.Delay(50); // Small delay between clicks

            // Dismiss any context menu with Escape
            SendKeys.SendWait("{ESC}");
            await Task.Delay(50);
        }

        // Assert - harness received all 3 right-clicks
        var allRightClicksReceived = await _fixture.WaitForRightClickAsync(3);
        Assert.True(allRightClicksReceived, $"Expected 3 right-clicks but harness received {_fixture.GetRightClickCount()}");
    }

    [Fact]
    public async Task RightClickAsync_RecordsLastMouseButton()
    {
        // Arrange - right-click on the panel
        var panelCenter = _fixture.GetRightClickPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.RightClickAsync(panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness recorded the correct mouse button
        await _fixture.WaitForRightClickAsync(1);
        var lastButton = _fixture.GetLastMouseButton();
        Assert.NotNull(lastButton);
        Assert.Equal(MouseButtons.Right, lastButton);
    }
}
