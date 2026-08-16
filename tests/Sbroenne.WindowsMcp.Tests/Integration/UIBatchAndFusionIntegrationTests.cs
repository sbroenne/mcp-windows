using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for Phase 1 agent-ergonomics features:
/// ui_batch (multi-step sequences) and perceive/act fusion (withSnapshot) on ui_click/ui_type/ui_select.
/// Tests drive the real tool entry points against the controlled WinForms harness.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIBatchAndFusionIntegrationTests
{
    private readonly UITestHarnessFixture _fixture;
    private readonly string _windowHandle;

    public UIBatchAndFusionIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        _windowHandle = _fixture.TestWindowHandleString;
    }

    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    private static string ExtractText(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().Single().Text;

    private static BatchResult ParseBatch(CallToolResult result) =>
        JsonSerializer.Deserialize<BatchResult>(ExtractText(result), ParseOptions)!;

    [Fact]
    public async Task Batch_FillFormAndSubmit_AllStepsSucceed()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "batch-user", clearFirst = true },
            new { action = "type", automationId = "PasswordInput", controlType = "Edit", text = "batch-pass", clearFirst = true },
            new { action = "click", name = "Submit", controlType = "Button" },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Batch failed: {ExtractText(result)}");
        Assert.False(result.IsError);
        Assert.Equal(3, batch.StepsRun);
        Assert.Equal(3, batch.StepsSucceeded);
        Assert.All(batch.Steps, s => Assert.True(s.Success, $"Step {s.Index} ({s.Action}) failed: {s.Error}"));
    }

    [Fact]
    public async Task Batch_StopOnError_HaltsAtFailingStep()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "abc" },
            new { action = "click", name = "NoSuchButton_ZZZ", controlType = "Button" },
            new { action = "click", name = "Submit", controlType = "Button" },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.False(batch.Success);
        Assert.True(result.IsError);
        Assert.Equal(2, batch.StepsRun); // stopped after the failing second step
        Assert.Equal(1, batch.StepsSucceeded);
        Assert.True(batch.Stopped);
    }

    [Fact]
    public async Task Batch_ContinueOnError_RunsEveryStep()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "click", name = "NoSuchButton_ZZZ", controlType = "Button" },
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "still-runs" },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: false, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.False(batch.Success);
        Assert.Equal(2, batch.StepsRun);
        Assert.Equal(1, batch.StepsSucceeded);
    }

    [Fact]
    public async Task Batch_FindThenTypeUsingPrev_ChainsElementId()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "find", automationId = "UsernameInput", controlType = "Edit" },
            new { action = "type", elementId = "$prev", text = "chained", clearFirst = true },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Chained batch failed: {ExtractText(result)}");
        Assert.NotNull(batch.Steps[0].ElementId);
    }

    [Fact]
    public async Task Batch_WithSnapshot_AttachesPostActionTree()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "snap" },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: true, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Batch failed: {ExtractText(result)}");
        Assert.NotNull(batch.PostActionTree);
        Assert.True(batch.PostActionTree!.Length >= 1);
    }

    /// <summary>
    /// Window-relative center of the Submit button, read on the UI thread.
    /// </summary>
    private (int X, int Y) SubmitCenter()
    {
        var form = _fixture.Form!;
        var point = (Point)form.Invoke(() => form.SubmitButtonWindowRelativeCenter);
        return (point.X, point.Y);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Batch_MouseClickStep_ClicksAtWindowRelativeCoordinates()
    {
        var (x, y) = SubmitCenter();
        var before = _fixture.Form!.SubmitMouseInputCount;

        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "mouse", mouseAction = "click", x, y },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Mouse batch failed: {ExtractText(result)}");

        await Task.Delay(150);
        Assert.True(_fixture.Form!.SubmitMouseInputCount > before, "Submit button received no physical mouse input from the batch mouse step.");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Batch_PolylineStep_RunsAsSingleStep()
    {
        var (x, y) = SubmitCenter();

        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "polyline", points = new[] { new[] { x, y }, new[] { x + 20, y + 10 }, new[] { x + 40, y } } },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Polyline batch failed: {ExtractText(result)}");
        Assert.Equal(1, batch.StepsRun);
        Assert.Equal("polyline", batch.Steps[0].Action);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Batch_MouseStepWithWrongProcessGuard_FailsAndHaltsBatch()
    {
        var (x, y) = SubmitCenter();

        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "mouse", mouseAction = "click", x, y, expectedProcessName = "definitely_not_this_process_zzz" },
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "should-not-run" },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.False(batch.Success);
        Assert.True(result.IsError);
        Assert.Equal(1, batch.StepsRun); // halted before the type step
        Assert.Equal(0, batch.StepsSucceeded);
        Assert.True(batch.Stopped);
        Assert.Contains("does not match expected process", batch.Steps[0].Error, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Batch_ClickStepWithDoubleClick_DeliversTwoMouseUps()
    {
        var before = _fixture.Form!.SubmitMouseInputCount;

        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "click", name = "Submit", controlType = "Button", doubleClick = true },
        });

        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, steps, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = ParseBatch(result);
        Assert.True(batch.Success, $"Double-click batch failed: {ExtractText(result)}");
        Assert.Equal("double-clicked", batch.Steps[0].Summary);

        // The tool waits for an observable change before returning, which the first mouse-up already causes;
        // poll briefly for the second so the assertion does not race the message pump.
        Assert.True(
            await TestWait.UntilAsync(() => _fixture.Form!.SubmitMouseInputCount >= before + 2),
            $"Expected at least 2 physical mouse-ups, saw {_fixture.Form!.SubmitMouseInputCount - before}.");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task UiClick_DoubleClick_DeliversTwoMouseUps()
    {
        var before = _fixture.Form!.SubmitMouseInputCount;

        var result = await UIClickTool.ExecuteAsync(
            _windowHandle,
            name: "Submit",
            nameContains: null,
            namePattern: null,
            controlType: "Button",
            automationId: null,
            className: null,
            elementId: null,
            foundIndex: 1,
            withSnapshot: false,
            includeDiagnostics: false,
            doubleClick: true,
            CancellationToken.None);

        var text = ExtractText(result);
        Assert.True(JsonDocument.Parse(text).RootElement.GetProperty("success").GetBoolean(), $"Double-click failed: {text}");

        // The tool waits for an observable change before returning, which the first mouse-up already causes;
        // poll briefly for the second so the assertion does not race the message pump.
        Assert.True(
            await TestWait.UntilAsync(() => _fixture.Form!.SubmitMouseInputCount >= before + 2),
            $"Expected at least 2 physical mouse-ups, saw {_fixture.Form!.SubmitMouseInputCount - before}.");
    }

    [Fact]
    public async Task Batch_InvalidJson_FailsGracefully()
    {
        var result = await UIBatchTool.ExecuteAsync(
            _windowHandle, "not json", stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not valid JSON", ExtractText(result));
    }

    [Fact]
    public async Task Click_WithSnapshot_AttachesPostActionTree()
    {
        var result = await UIClickTool.ExecuteAsync(
            _windowHandle,
            name: "Submit",
            nameContains: null,
            namePattern: null,
            controlType: "Button",
            automationId: null,
            className: null,
            elementId: null,
            foundIndex: 1,
            withSnapshot: true,
            includeDiagnostics: false,
            doubleClick: false,
            CancellationToken.None);

        var text = ExtractText(result);
        var doc = JsonDocument.Parse(text);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), $"Click failed: {text}");
        Assert.True(doc.RootElement.TryGetProperty("postActionTree", out var tree), "Expected postActionTree on fused click result.");
        Assert.True(tree.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Type_WithoutSnapshot_OmitsPostActionTree()
    {
        var result = await UITypeTool.ExecuteAsync(
            _windowHandle,
            text: "no-snapshot",
            name: null,
            nameContains: null,
            namePattern: null,
            controlType: "Edit",
            automationId: "UsernameInput",
            className: null,
            elementId: null,
            foundIndex: 1,
            clearFirst: true,
            withSnapshot: false,
            includeDiagnostics: false,
            CancellationToken.None);

        var doc = JsonDocument.Parse(ExtractText(result));
        Assert.False(doc.RootElement.TryGetProperty("postActionTree", out _), "postActionTree should be absent when withSnapshot=false.");
    }
}
