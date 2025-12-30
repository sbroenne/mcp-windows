using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse scroll operations.
/// These tests use a dedicated test harness window to verify scroll events are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MouseScrollTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseScrollTests(MouseTestFixture fixture)
    {
        _fixture = fixture;
        _originalPosition = Coordinates.FromCurrent();

        // Reset harness state before each test
        _fixture.Reset();
    }

    public void Dispose()
    {
        _fixture.MouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ScrollAsync_DownOnScrollPanel_VerifiedByHarness()
    {
        // Arrange - scroll on the scroll panel
        var panelCenter = _fixture.GetScrollPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Focus the scroll panel first
        await _fixture.MouseInputService.ClickAsync(panelCenter.X, panelCenter.Y);
        await Task.Delay(50);
        _fixture.Reset(); // Reset after the click

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Down, 1, panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Assert - harness received the scroll event
        var scrollReceived = await _fixture.WaitForScrollEventAsync(1);
        Assert.True(scrollReceived, "Test harness did not receive the scroll event");
        _fixture.AssertScrollDetected(1);

        // Assert - scroll delta should be negative (down)
        var scrollDelta = _fixture.GetTotalScrollDelta();
        Assert.True(scrollDelta < 0, $"Expected negative scroll delta for down scroll, got {scrollDelta}");
    }

    [Fact]
    public async Task ScrollAsync_UpOnScrollPanel_VerifiedByHarness()
    {
        // Arrange - scroll on the scroll panel
        var panelCenter = _fixture.GetScrollPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Focus the scroll panel first
        await _fixture.MouseInputService.ClickAsync(panelCenter.X, panelCenter.Y);
        await Task.Delay(50);
        _fixture.Reset(); // Reset after the click

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Up, 1, panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Assert - harness received the scroll event
        var scrollReceived = await _fixture.WaitForScrollEventAsync(1);
        Assert.True(scrollReceived, "Test harness did not receive the scroll event");

        // Assert - scroll delta should be positive (up)
        var scrollDelta = _fixture.GetTotalScrollDelta();
        Assert.True(scrollDelta > 0, $"Expected positive scroll delta for up scroll, got {scrollDelta}");
    }

    [Fact]
    public async Task ScrollAsync_LeftInTestWindow_Succeeds()
    {
        // Arrange - scroll inside the dedicated test window
        // Note: Horizontal scroll events may not be captured by the basic scroll panel
        var (x, y) = _fixture.GetTestWindowCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Left, 1, x, y);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_RightInTestWindow_Succeeds()
    {
        // Arrange - scroll inside the dedicated test window
        // Note: Horizontal scroll events may not be captured by the basic scroll panel
        var (x, y) = _fixture.GetTestWindowCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Right, 1, x, y);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ScrollAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - scroll on the scroll panel with specific coordinates
        var panelCenter = _fixture.GetScrollPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Down, 1, panelCenter.X, panelCenter.Y);

        // Assert - cursor should be at specified position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(result.FinalPosition);
        Assert.InRange(result.FinalPosition.X, panelCenter.X - 2, panelCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, panelCenter.Y - 2, panelCenter.Y + 2);
    }

    [Fact]
    public async Task ScrollAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var x = -99999;
        var y = -99999;

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Down, 1, x, y);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task ScrollAsync_MultipleClicks_VerifiedByHarness()
    {
        // Arrange - scroll 5 clicks on the scroll panel
        var panelCenter = _fixture.GetScrollPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Focus the scroll panel first
        await _fixture.MouseInputService.ClickAsync(panelCenter.X, panelCenter.Y);
        await Task.Delay(50);
        _fixture.Reset(); // Reset after the click

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Down, 5, panelCenter.X, panelCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Assert - harness received scroll events
        var scrollReceived = await _fixture.WaitForScrollEventAsync(1);
        Assert.True(scrollReceived, "Test harness did not receive any scroll events");

        // Assert - total scroll delta should be significant
        var scrollDelta = _fixture.GetTotalScrollDelta();
        Assert.True(scrollDelta != 0, "Scroll delta should not be zero after 5 scroll clicks");
    }

    [Fact]
    public async Task ScrollAsync_ZeroAmount_Succeeds()
    {
        // Arrange - scroll 0 clicks (effectively no scroll)
        _fixture.Reset(); // Ensure clean state
        var (x, y) = _fixture.GetTestWindowCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ScrollAsync(ScrollDirection.Down, 0, x, y);

        // Assert - should succeed even with 0 amount
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Harness should not receive any scroll events for 0 amount
        await Task.Delay(100);
        var scrollCount = _fixture.GetScrollEventCount();
        Assert.Equal(0, scrollCount);
    }

    [Fact]
    public async Task ScrollAsync_AccumulatesScrollDelta()
    {
        // Arrange - scroll multiple times and verify delta accumulates
        var panelCenter = _fixture.GetScrollPanelCenter();
        _fixture.EnsureTestWindowForeground();

        // Focus the scroll panel first
        await _fixture.MouseInputService.ClickAsync(panelCenter.X, panelCenter.Y);
        await Task.Delay(50);
        _fixture.Reset();

        async Task ScrollAndWaitAsync(ScrollDirection direction, int expectedScrollEventCount)
        {
            // First attempt
            await _fixture.MouseInputService.ScrollAsync(direction, 1, panelCenter.X, panelCenter.Y);

            if (await _fixture.WaitForScrollEventAsync(expectedScrollEventCount, TimeSpan.FromSeconds(2)))
            {
                return;
            }

            // Retry once with stronger preconditions (focus + hover)
            _fixture.EnsureTestWindowForeground();
            await _fixture.MouseInputService.MoveAsync(panelCenter.X, panelCenter.Y);
            await Task.Delay(50);
            await _fixture.MouseInputService.ScrollAsync(direction, 1, panelCenter.X, panelCenter.Y);

            var ok = await _fixture.WaitForScrollEventAsync(expectedScrollEventCount, TimeSpan.FromSeconds(2));
            if (!ok)
            {
                var history = string.Join(" | ", _fixture.GetEventHistory().TakeLast(15));
                throw new Xunit.Sdk.XunitException(
                    $"Expected at least {expectedScrollEventCount} scroll events after {direction} scroll, got {_fixture.GetScrollEventCount()}. " +
                    $"Recent events: {history}");
            }
        }

        // Act - scroll down twice, then up once
        await ScrollAndWaitAsync(ScrollDirection.Down, expectedScrollEventCount: 1);
        await ScrollAndWaitAsync(ScrollDirection.Down, expectedScrollEventCount: 2);
        await ScrollAndWaitAsync(ScrollDirection.Up, expectedScrollEventCount: 3);

        // Assert - scroll count should reflect 3 events
        var scrollCount = _fixture.GetScrollEventCount();
        Assert.True(scrollCount >= 3, $"Expected at least 3 scroll events, got {scrollCount}");
    }
}
