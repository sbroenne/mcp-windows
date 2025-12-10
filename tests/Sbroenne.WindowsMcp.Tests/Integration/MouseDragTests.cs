using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse drag operations.
/// These tests use a dedicated test harness window to verify drag events are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MouseDragTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseDragTests(MouseTestFixture fixture)
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
    public async Task DragAsync_LeftButton_VerifiedByHarness()
    {
        // Arrange - drag inside the drag panel (designed for drag testing)
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(200, 60);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Left);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Assert - harness detected the drag operation
        await Task.Delay(100); // Give time for events to propagate
        Assert.True(_fixture.GetDragDetected(), "Test harness did not detect the drag operation");
        _fixture.AssertDragDetected();
    }

    [Fact]
    public async Task DragAsync_RightButton_Succeeds()
    {
        // Arrange - drag inside the drag panel
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(200, 60);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Right);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Assert - cursor ends at end position
        Assert.InRange(result.FinalPosition.X, endX - 2, endX + 2);
        Assert.InRange(result.FinalPosition.Y, endY - 2, endY + 2);
    }

    [Fact]
    public async Task DragAsync_MiddleButton_Succeeds()
    {
        // Arrange - drag inside the drag panel
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(200, 60);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Middle);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task DragAsync_OutOfBoundsStart_ReturnsError()
    {
        // Arrange - start coordinates outside screen bounds, end in test window
        var startX = -99999;
        var startY = -99999;
        var (endX, endY) = _fixture.GetTestWindowCoordinates(200, 200);

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task DragAsync_OutOfBoundsEnd_ReturnsError()
    {
        // Arrange - start in test window, end coordinates outside screen bounds
        var (startX, startY) = _fixture.GetTestWindowCoordinates(100, 100);
        var endX = 999999;
        var endY = 999999;

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
    }

    [Fact]
    public async Task DragAsync_CursorEndsAtEndPosition()
    {
        // Arrange - drag inside the drag panel
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(300, 60);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY);

        // Assert - cursor should be at end position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(result.FinalPosition);
        Assert.InRange(result.FinalPosition.X, endX - 2, endX + 2);
        Assert.InRange(result.FinalPosition.Y, endY - 2, endY + 2);
    }

    [Fact]
    public async Task DragAsync_SameStartAndEnd_Succeeds()
    {
        // Arrange - same start and end point (essentially click and release)
        var (x, y) = _fixture.GetDragPanelCoordinates(100, 40);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(x, y, x, y);

        // Assert - should succeed but may not be detected as a drag (no movement)
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
    }

    [Fact]
    public async Task DragAsync_RecordsDragPositions()
    {
        // Arrange - drag inside the drag panel
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(300, 60);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Left);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Give time for events to propagate
        await Task.Delay(100);

        // Assert - harness recorded start position
        var dragStart = _fixture.GetDragStartPosition();
        Assert.NotNull(dragStart);

        // Assert - harness recorded end position (if drag was detected)
        if (_fixture.GetDragDetected())
        {
            var dragEnd = _fixture.GetDragEndPosition();
            Assert.NotNull(dragEnd);
        }
    }

    [Fact]
    public async Task DragAsync_LongDistance_VerifiedByHarness()
    {
        // Arrange - drag a longer distance to ensure it's detected
        var (startX, startY) = _fixture.GetDragPanelCoordinates(10, 10);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(400, 70);
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Left);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        // Give time for events to propagate
        await Task.Delay(100);

        // Assert - harness should definitely detect this long drag
        Assert.True(_fixture.GetDragDetected(), "Test harness did not detect the long drag operation");
    }
}
