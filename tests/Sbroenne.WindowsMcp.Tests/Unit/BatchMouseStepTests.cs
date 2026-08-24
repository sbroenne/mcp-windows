using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Desktop-free tests for the ui_batch mouse/polyline step vocabulary and the mouse_control polyline
/// point list. Every case here fails validation before any Windows input is sent, so these run in CI.
/// </summary>
public sealed class BatchMouseStepTests
{
    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions StepParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string ExtractText(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().Single().Text;

    private static BatchResult ParseBatch(CallToolResult result) =>
        JsonSerializer.Deserialize<BatchResult>(ExtractText(result), ParseOptions)!;

    private static Task<CallToolResult> RunBatchAsync(string steps) =>
        UIBatchTool.ExecuteAsync("12345", steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

    [Fact]
    public void BatchStep_MouseStep_DeserializesCoordinatesAndButton()
    {
        var json = """
            [{"action":"mouse","mouseAction":"drag","x":480,"y":420,"endX":481,"endY":650,"button":"right","modifiers":"shift"}]
            """;

        var steps = JsonSerializer.Deserialize<BatchStep[]>(json, StepParseOptions)!;

        var step = Assert.Single(steps);
        Assert.Equal("mouse", step.Action);
        Assert.Equal("drag", step.MouseAction);
        Assert.Equal(480, step.X);
        Assert.Equal(420, step.Y);
        Assert.Equal(481, step.EndX);
        Assert.Equal(650, step.EndY);
        Assert.Equal("right", step.Button);
        Assert.Equal("shift", step.Modifiers);
    }

    [Fact]
    public void BatchStep_PolylineStep_DeserializesPointPairs()
    {
        var json = """
            [{"action":"polyline","points":[[650,430],[750,480],[750,620]],"button":"left"}]
            """;

        var steps = JsonSerializer.Deserialize<BatchStep[]>(json, StepParseOptions)!;

        var step = Assert.Single(steps);
        Assert.Equal("polyline", step.Action);
        Assert.NotNull(step.Points);
        Assert.Equal(3, step.Points!.Length);
        Assert.Equal([650, 430], step.Points[0]);
        Assert.Equal([750, 620], step.Points[2]);
    }

    [Fact]
    public void BatchStep_ClickStep_DeserializesDoubleClickFlag()
    {
        var json = """
            [{"action":"click","name":"Items","doubleClick":true}]
            """;

        var steps = JsonSerializer.Deserialize<BatchStep[]>(json, StepParseOptions)!;

        Assert.True(Assert.Single(steps).DoubleClick);
    }

    [Fact]
    public async Task Batch_MouseStep_WithoutMouseAction_FailsWithGuidance()
    {
        var result = await RunBatchAsync("""[{"action":"mouse","x":10,"y":10}]""");

        var batch = ParseBatch(result);
        Assert.False(batch.Success);
        Assert.True(result.IsError);

        var step = Assert.Single(batch.Steps);
        Assert.False(step.Success);
        Assert.Contains("requires 'mouseAction'", step.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Batch_MouseStep_UnknownMouseAction_ListsValidActions()
    {
        var result = await RunBatchAsync("""[{"action":"mouse","mouseAction":"wiggle","x":10,"y":10}]""");

        var step = Assert.Single(ParseBatch(result).Steps);
        Assert.False(step.Success);
        Assert.Contains("invalid mouseAction 'wiggle'", step.Error, StringComparison.Ordinal);
        Assert.Contains("polyline", step.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Batch_UnknownAction_ListsMouseAndPolyline()
    {
        var result = await RunBatchAsync("""[{"action":"teleport"}]""");

        var step = Assert.Single(ParseBatch(result).Steps);
        Assert.False(step.Success);
        Assert.Contains("mouse, polyline", step.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("polyline", "drag")]
    [InlineData("double_click", "doubleClick")]
    [InlineData("DOUBLE_CLICK", "doubleClick")]
    [InlineData("doubleclick", "doubleClick")]
    public async Task Batch_MouseStep_AcceptsKnownMouseActionSpellings(string mouseAction, string _)
    {
        // A recognized action gets past parsing and fails later (invalid window handle), never with
        // "invalid mouseAction" - that is what distinguishes accepted spellings from rejected ones.
        var result = await RunBatchAsync($$"""[{"action":"mouse","mouseAction":"{{mouseAction}}","x":10,"y":10}]""");

        var step = Assert.Single(ParseBatch(result).Steps);
        Assert.DoesNotContain("invalid mouseAction", step.Error ?? "", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[[1]]", "exactly two numbers")]
    [InlineData("[[1,2,3]]", "exactly two numbers")]
    [InlineData("[]", "non-empty JSON array")]
    [InlineData("not json", "not valid JSON")]
    public async Task MouseControl_Polyline_MalformedPoints_FailsCleanly(string points, string expectedFragment)
    {
        var result = await MouseControlTool.ExecuteAsync(
            action: Models.MouseAction.Polyline,
            target: "primary_screen",
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
            points: points,
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(expectedFragment, ExtractText(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MouseControl_Polyline_SinglePoint_RequiresAtLeastTwo()
    {
        var result = await MouseControlTool.ExecuteAsync(
            action: Models.MouseAction.Polyline,
            target: "primary_screen",
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
            points: "[[10,10]]",
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("at least 2", ExtractText(result), StringComparison.Ordinal);
    }
}
