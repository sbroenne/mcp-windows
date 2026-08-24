using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for continuous multi-point strokes. These verify the property that motivated the
/// feature: one press, N moves, one release - no pen lift at the vertices, which N segment-drags cannot give.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class MouseStrokeTests : DesktopInputTestBase, IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;

    public MouseStrokeTests(MouseTestFixture fixture)
    {
        _fixture = fixture;
        _originalPosition = Coordinates.FromCurrent();
        _fixture.Reset();
    }

    public void Dispose()
    {
        _fixture.MouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
    }

    private Coordinates[] BuildZigzag()
    {
        var (x0, y0) = _fixture.GetDragPanelCoordinates(20, 20);
        var (x1, y1) = _fixture.GetDragPanelCoordinates(90, 60);
        var (x2, y2) = _fixture.GetDragPanelCoordinates(160, 20);
        var (x3, y3) = _fixture.GetDragPanelCoordinates(230, 60);

        return
        [
            new Coordinates(x0, y0),
            new Coordinates(x1, y1),
            new Coordinates(x2, y2),
            new Coordinates(x3, y3)
        ];
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task StrokeAsync_FourPoints_PressesOnceAndEndsAtLastPoint()
    {
        // Arrange
        var points = BuildZigzag();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.StrokeAsync(points, MouseButton.Left, ModifierKey.None);

        // Assert - API reports success and the cursor finished on the last vertex
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");
        Assert.InRange(result.FinalPosition.X, points[^1].X - 2, points[^1].X + 2);
        Assert.InRange(result.FinalPosition.Y, points[^1].Y - 2, points[^1].Y + 2);

        await Task.Delay(150); // let queued mouse messages drain

        // Assert - exactly one press for the whole stroke; N segment-drags would report N
        Assert.Equal(1, _fixture.GetDragPressCount());
        Assert.True(_fixture.GetDragDetected(), "Test harness did not detect the stroke as a drag");
        Assert.Equal(0, _fixture.GetButtonClickCount());
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task StrokeAsync_SinglePoint_FailsWithoutSendingInput()
    {
        var (x, y) = _fixture.GetDragPanelCoordinates(20, 20);

        var result = await _fixture.MouseInputService.StrokeAsync([new Coordinates(x, y)]);

        Assert.False(result.Success);
        Assert.Equal(0, _fixture.GetDragPressCount());
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task StrokeAsync_OutOfBoundsVertex_FailsBeforePressing()
    {
        var (x, y) = _fixture.GetDragPanelCoordinates(20, 20);

        var result = await _fixture.MouseInputService.StrokeAsync(
            [new Coordinates(x, y), new Coordinates(int.MaxValue, int.MaxValue)]);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);

        // The whole point of validating up front: nothing was pressed, so no button can be left stuck down.
        Assert.Equal(0, _fixture.GetDragPressCount());
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task DragAsync_StillReportsSingleDrag_AfterStrokeRefactor()
    {
        // Regression: DragAsync now delegates to StrokeAsync; it must stay a single press/release.
        var (startX, startY) = _fixture.GetDragPanelCoordinates(20, 20);
        var (endX, endY) = _fixture.GetDragPanelCoordinates(200, 60);
        _fixture.EnsureTestWindowForeground();

        var result = await _fixture.MouseInputService.DragAsync(startX, startY, endX, endY, MouseButton.Left);

        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.ErrorMessage}");

        await Task.Delay(150);
        Assert.Equal(1, _fixture.GetDragPressCount());
        Assert.True(_fixture.GetDragDetected(), "Test harness did not detect the drag operation");
    }
}
