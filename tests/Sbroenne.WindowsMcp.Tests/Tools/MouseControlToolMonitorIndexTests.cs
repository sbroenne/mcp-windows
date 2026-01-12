using System.Runtime.Versioning;
using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Fixtures;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Tools;

/// <summary>
/// Integration tests for MouseControlTool monitorIndex validation and monitor context features.
/// Tests User Story 1 (Explicit Monitor Targeting), User Story 2 (Monitor Info in Responses),
/// and User Story 3 (Query Current Position) using real services without mocking.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MouseControlToolMonitorIndexTests : IClassFixture<MultiMonitorFixture>
{
    private readonly MultiMonitorFixture _fixture;

    public MouseControlToolMonitorIndexTests(MultiMonitorFixture fixture)
    {
        _fixture = fixture;
    }

    private static MouseControlResult DeserializeResult(string json)
    {
        return JsonSerializer.Deserialize<MouseControlResult>(json, WindowsToolsBase.JsonOptions)!;
    }

    [Fact]
    public async Task ExecuteAsync_ClickWithCoordinatesNoMonitorIndex_ReturnsMissingParameterError()
    {
        // Arrange
        var (x, y) = _fixture.GetMonitorCenter(0);

        // Act - monitorIndex not provided, should fail validation
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Validation should fail because monitorIndex is required with coordinates
        Assert.False(result.Success);
        Assert.Contains("monitorIndex", result.Error!);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails!.ContainsKey("valid_indices"));
    }

    [Fact]
    public async Task ExecuteAsync_ClickWithInvalidMonitorIndex_ReturnsInvalidCoordinatesError()
    {
        // Arrange
        var (x, y) = _fixture.GetMonitorCenter(0);
        var invalidIndex = _fixture.MonitorCount + 10; // Way beyond valid range

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: invalidIndex,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Validation should fail because monitorIndex is out of range
        Assert.False(result.Success);
        Assert.Contains($"Invalid monitorIndex: {invalidIndex}", result.Error!);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails!.ContainsKey("valid_indices"));
        Assert.True(result.ErrorDetails!.ContainsKey("provided_index"));
    }

    [Fact]
    public async Task ExecuteAsync_ClickWithOutOfBoundsCoordinates_ReturnsOutOfBoundsError()
    {
        // Arrange
        var monitorIndex = 0;
        var (x, y) = _fixture.GetOutOfBoundsCoordinates(monitorIndex);

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: monitorIndex,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Validation should fail because coordinates are out of bounds
        Assert.False(result.Success);
        Assert.Contains($"Coordinates ({x}, {y}) out of bounds", result.Error!);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails!.ContainsKey("valid_bounds"));
        Assert.True(result.ErrorDetails!.ContainsKey("provided_coordinates"));
    }

    [Fact]
    [Trait("Category", "RequiresDisplay")]
    public async Task ExecuteAsync_ClickWithoutCoordinates_DoesNotRequireMonitorIndex()
    {
        // Arrange - click at current cursor position (no x/y provided)

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: null,
            y: null,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - coordinate-less actions should work without explicit monitorIndex
        Assert.True(result.Success);
        Assert.NotNull(result.FinalPosition);
    }

    [Fact]
    public async Task ExecuteAsync_MoveWithCoordinatesAndValidMonitorIndex_Succeeds()
    {
        // Arrange
        var monitorIndex = 0;
        var monitor = _fixture.GetMonitor(monitorIndex)!;
        var (x, y) = _fixture.GetSafeCoordinates(monitorIndex);

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Move,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: monitorIndex,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Real integration test, cursor actually moved
        Assert.True(result.Success);
        Assert.NotNull(result.FinalPosition);
        Assert.Equal(monitor.Width, result.MonitorWidth);
        Assert.Equal(monitor.Height, result.MonitorHeight);
    }

    [Fact]
    public async Task ExecuteAsync_DragWithCoordinatesNoMonitorIndex_ReturnsMissingParameterError()
    {
        // Arrange
        var (startX, startY) = _fixture.GetSafeCoordinates(0);
        var (endX, endY) = _fixture.GetMonitorCenter(0);

        // Act - Attempt drag without monitorIndex (should fail validation)
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Drag,
            target: null,
            x: startX,  // Note: drag uses x/y for start position
            y: startY,
            endX: endX,
            endY: endY,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Validation should fail because monitorIndex is required with coordinates
        Assert.False(result.Success);
        Assert.Contains("monitorIndex", result.Error!);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulMove_IncludesMonitorInfo()
    {
        // Arrange
        var monitorIndex = 0;
        var (x, y) = _fixture.GetSafeCoordinates(monitorIndex);
        var monitor = _fixture.GetMonitor(monitorIndex)!;

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Move,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: monitorIndex,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Success response should include monitor context
        Assert.True(result.Success);
        Assert.NotNull(result.MonitorIndex);
        Assert.Equal(monitorIndex, result.MonitorIndex);
        Assert.NotNull(result.MonitorWidth);
        Assert.Equal(monitor.Width, result.MonitorWidth);
        Assert.NotNull(result.MonitorHeight);
        Assert.Equal(monitor.Height, result.MonitorHeight);
    }

    [Fact]
    [Trait("Category", "RequiresDisplay")]
    public async Task ExecuteAsync_SuccessfulClick_IncludesMonitorInfo()
    {
        // Arrange
        var monitorIndex = 0;
        var (x, y) = _fixture.GetSafeCoordinates(monitorIndex);
        var monitor = _fixture.GetMonitor(monitorIndex)!;

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: x,
            y: y,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: monitorIndex,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Success response should include monitor context
        Assert.True(result.Success);
        Assert.NotNull(result.MonitorIndex);
        Assert.Equal(monitorIndex, result.MonitorIndex);
        Assert.NotNull(result.MonitorWidth);
        Assert.Equal(monitor.Width, result.MonitorWidth);
        Assert.NotNull(result.MonitorHeight);
        Assert.Equal(monitor.Height, result.MonitorHeight);
    }

    [Fact]
    [Trait("Category", "RequiresDisplay")]
    public async Task ExecuteAsync_CoordinatelessClick_DoesNotIncludeMonitorInfo()
    {
        // Arrange - click at current position (no coordinates, no monitorIndex)

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.Click,
            target: null,
            x: null,
            y: null,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Coordinate-less operations don't have explicit monitor context
        Assert.True(result.Success);
        Assert.Null(result.MonitorIndex);
        Assert.Null(result.MonitorWidth);
        Assert.Null(result.MonitorHeight);
    }

    // =========================
    // US3: Query Current Position Tests
    // =========================

    [Fact]
    public async Task ExecuteAsync_GetPosition_ReturnsCurrentPositionWithMonitorInfo()
    {
        // Arrange - get_position should return current cursor position with monitor context

        // Act
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.GetPosition,
            target: null,
            x: null,
            y: null,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Should return success with coordinates and monitor context
        Assert.True(result.Success, $"get_position should succeed. Error: {result.Error}");
        Assert.NotNull(result.FinalPosition);
        Assert.NotNull(result.MonitorIndex);
        Assert.InRange(result.MonitorIndex.Value, 0, _fixture.MonitorCount - 1);
        Assert.NotNull(result.MonitorWidth);
        Assert.True(result.MonitorWidth.Value > 0);
        Assert.NotNull(result.MonitorHeight);
        Assert.True(result.MonitorHeight.Value > 0);
    }

    [Fact]
    public async Task ExecuteAsync_GetPosition_DeterminesCorrectMonitor()
    {
        // Act - get_position should determine which monitor contains the cursor
        var resultJson = await MouseControlTool.ExecuteAsync(
            action: MouseAction.GetPosition,
            target: null,
            x: null,
            y: null,
            endX: null,
            endY: null,
            direction: null,
            amount: 1,
            modifiers: null,
            button: null,
            monitorIndex: null,
            expectedWindowTitle: null,
            expectedProcessName: null,
            windowHandle: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert - Should identify a valid monitor and return its dimensions
        Assert.True(result.Success);
        Assert.NotNull(result.MonitorIndex);
        Assert.InRange(result.MonitorIndex.Value, 0, _fixture.MonitorCount - 1);

        // Verify the returned dimensions match the identified monitor
        var identifiedMonitor = _fixture.GetMonitor(result.MonitorIndex.Value)!;
        Assert.Equal(identifiedMonitor.Width, result.MonitorWidth);
        Assert.Equal(identifiedMonitor.Height, result.MonitorHeight);

        // Verify coordinates are within monitor bounds
        Assert.InRange(result.FinalPosition.X, 0, identifiedMonitor.Width - 1);
        Assert.InRange(result.FinalPosition.Y, 0, identifiedMonitor.Height - 1);
    }
}


