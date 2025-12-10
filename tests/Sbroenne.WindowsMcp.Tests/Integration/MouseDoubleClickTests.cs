using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse double-click operations.
/// These tests use a dedicated test harness window to verify double-clicks are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseDoubleClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseDoubleClickTests(MouseTestFixture fixture)
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
    public async Task DoubleClickAsync_OnButton_VerifiedByHarness()
    {
        // Arrange - double-click on the test button
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DoubleClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness actually received the double-click
        var doubleClickReceived = await _fixture.WaitForDoubleClickAsync(1);
        Assert.True(doubleClickReceived, "Test harness did not receive the double-click");
        _fixture.AssertButtonDoubleClicked(1);
    }

    [Fact]
    public async Task DoubleClickAsync_WithCoordinates_MovesCursorFirst()
    {
        // Arrange - double-click on the test button
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DoubleClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success and cursor is at expected position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, buttonCenter.X - 2, buttonCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, buttonCenter.Y - 2, buttonCenter.Y + 2);

        // Assert - harness verifies double-click was received
        var doubleClickReceived = await _fixture.WaitForDoubleClickAsync(1);
        Assert.True(doubleClickReceived, "Test harness did not receive the double-click");
    }

    [Fact]
    public async Task DoubleClickAsync_BothClicksAtSamePosition_CursorDoesNotMove()
    {
        // Arrange - double-click on the test button
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();
        await _fixture.MouseInputService.MoveAsync(buttonCenter.X, buttonCenter.Y);

        // Get position before double-click
        var posBefore = Coordinates.FromCurrent();

        // Act
        var result = await _fixture.MouseInputService.DoubleClickAsync(null, null);

        // Assert - cursor should not move during double-click
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, posBefore.X - 2, posBefore.X + 2);
        Assert.InRange(result.FinalPosition.Y, posBefore.Y - 2, posBefore.Y + 2);

        // Assert - harness received double-click
        var doubleClickReceived = await _fixture.WaitForDoubleClickAsync(1);
        Assert.True(doubleClickReceived, "Test harness did not receive the double-click");
    }

    [Fact]
    public async Task DoubleClickAsync_SendsValidDoubleClickSequence()
    {
        // This test verifies that the double-click sends all 4 events (2x down+up)
        // Arrange - double-click on the test button
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DoubleClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness confirms double-click was recognized
        // Note: A double-click event on a button means 4 mouse events were sent correctly
        var doubleClickReceived = await _fixture.WaitForDoubleClickAsync(1);
        Assert.True(doubleClickReceived, "Test harness did not recognize the double-click sequence");
    }

    [Fact]
    public async Task DoubleClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _fixture.MouseInputService.DoubleClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
        Assert.NotNull(result.Error);
        Assert.Contains("out of bounds", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DoubleClickAsync_MultipleDoubleClicks_AllVerifiedByHarness()
    {
        // Arrange - double-click the button 3 times
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act - double-click 3 times
        for (var i = 0; i < 3; i++)
        {
            var result = await _fixture.MouseInputService.DoubleClickAsync(buttonCenter.X, buttonCenter.Y);
            Assert.True(result.Success, $"Double-click {i + 1} failed: {result.ErrorCode}: {result.Error}");
            await Task.Delay(100); // Delay between double-clicks
        }

        // Assert - harness received all 3 double-clicks
        var allDoubleClicksReceived = await _fixture.WaitForDoubleClickAsync(3);
        Assert.True(allDoubleClicksReceived, $"Expected 3 double-clicks but harness received {_fixture.GetButtonDoubleClickCount()}");
    }
}
