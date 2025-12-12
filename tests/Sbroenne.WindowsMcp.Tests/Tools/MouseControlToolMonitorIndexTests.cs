using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Tests.Fixtures;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Tools;

/// <summary>
/// Integration tests for MouseControlTool monitorIndex validation and monitor context features.
/// Tests User Story 1 (Explicit Monitor Targeting), User Story 2 (Monitor Info in Responses),
/// and User Story 3 (Query Current Position) using real services without mocking.
/// </summary>
public sealed class MouseControlToolMonitorIndexTests : IClassFixture<MultiMonitorFixture>
{
    private readonly MultiMonitorFixture _fixture;
    private readonly MouseControlTool _tool;

    public MouseControlToolMonitorIndexTests(MultiMonitorFixture fixture)
    {
        _fixture = fixture;

        // Create real services for integration testing
        var mouseService = new MouseInputService();
        var monitorService = new MonitorService();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var logger = new MouseOperationLogger(NullLogger<MouseOperationLogger>.Instance);
        var config = MouseConfiguration.FromEnvironment();

        _tool = new MouseControlTool(
            mouseService,
            monitorService,
            elevationDetector,
            secureDesktopDetector,
            logger,
            config);
    }

    /// <summary>
    /// Helper to create a RequestContext for testing.
    /// Uses unsafe FormatterServices to bypass constructor and set required fields.
    /// </summary>
#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is obsolete but necessary for struct instantiation without constructor
    private static RequestContext<CallToolRequestParams> CreateMockContext()
    {
        // RequestContext is a struct with required Server property
        // For unit testing validation logic that doesn't need the context internals,
        // we use FormatterServices to create an instance without calling constructor
        var contextType = typeof(RequestContext<CallToolRequestParams>);
        var context = (RequestContext<CallToolRequestParams>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);

        // Set the Server property to satisfy ArgumentNullException.ThrowIfNull
        // Find and set the backing field
        var serverProp = contextType.GetProperty("Server");
        if (serverProp != null)
        {
            var backingField = contextType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("Server"));

            if (backingField != null)
            {
                var mockServer = new object(); // Minimal non-null object to pass ThrowIfNull check
                var boxed = (object)context;
                backingField.SetValue(boxed, mockServer);
                context = (RequestContext<CallToolRequestParams>)boxed;
            }
        }

        return context;
    }
#pragma warning restore SYSLIB0050

    [Fact]
    public async Task ExecuteAsync_ClickWithCoordinatesNoMonitorIndex_ReturnsMissingParameterError()
    {
        // Arrange
        var (x, y) = _fixture.GetMonitorCenter(0);

        // Act - monitorIndex parameter defaults to 0, but we're testing the new validation
        // Note: The current implementation has monitorIndex with default=0, so this test
        // will initially PASS (wrong behavior). After T007 implementation, behavior depends
        // on whether we can make monitorIndex nullable or detect if it was explicitly provided.
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click",
            x: x,
            y: y);

        // Assert - Validation should fail because monitorIndex is required with coordinates
        Assert.False(result.Success);
        Assert.Equal("missing_required_parameter", result.ErrorCodeString);
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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click",
            x: x,
            y: y,
            monitorIndex: invalidIndex);

        // Assert - Validation should fail because monitorIndex is out of range
        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCodeString);
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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click",
            x: x,
            y: y,
            monitorIndex: monitorIndex);

        // Assert - Validation should fail because coordinates are out of bounds
        Assert.False(result.Success);
        Assert.Equal("coordinates_out_of_bounds", result.ErrorCodeString);
        Assert.Contains($"Coordinates ({x}, {y}) out of bounds", result.Error!);
        Assert.NotNull(result.ErrorDetails);
        Assert.True(result.ErrorDetails!.ContainsKey("valid_bounds"));
        Assert.True(result.ErrorDetails!.ContainsKey("provided_coordinates"));
    }

    [Fact]
    public async Task ExecuteAsync_ClickWithoutCoordinates_DoesNotRequireMonitorIndex()
    {
        // Arrange - click at current cursor position (no x/y provided)

        // Act
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click");
        // Note: monitorIndex parameter omitted - should use default (0)

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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "move",
            x: x,
            y: y,
            monitorIndex: monitorIndex);

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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "drag",
            x: startX,  // Note: drag uses x/y for start position
            y: startY,
            endX: endX,
            endY: endY);
        // monitorIndex omitted - defaults to 0

        // Assert - Validation should fail because monitorIndex is required with coordinates
        Assert.False(result.Success);
        Assert.Equal("missing_required_parameter", result.ErrorCodeString);
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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "move",
            x: x,
            y: y,
            monitorIndex: monitorIndex);

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
    public async Task ExecuteAsync_SuccessfulClick_IncludesMonitorInfo()
    {
        // Arrange
        var monitorIndex = 0;
        var (x, y) = _fixture.GetSafeCoordinates(monitorIndex);
        var monitor = _fixture.GetMonitor(monitorIndex)!;

        // Act
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click",
            x: x,
            y: y,
            monitorIndex: monitorIndex);

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
    public async Task ExecuteAsync_CoordinatelessClick_DoesNotIncludeMonitorInfo()
    {
        // Arrange - click at current position (no coordinates, no monitorIndex)

        // Act
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "click");

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
        var monitor = _fixture.GetMonitor(0);

        // Act
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "get_position");

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
        var result = await _tool.ExecuteAsync(
            context: CreateMockContext(),
            action: "get_position");

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
