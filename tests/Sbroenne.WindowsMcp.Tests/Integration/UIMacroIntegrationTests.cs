using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Macros;
using Sbroenne.WindowsMcp.Macros.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for <c>ui_macro</c> record &amp; replay. Saves a macro (a ui_batch steps array),
/// then replays it against the controlled WinForms harness and verifies the workflow ran through the
/// identical batch engine. Uses a temporary macro directory so it never touches the user's macros.
/// </summary>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class UIMacroIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly string _windowHandle;
    private readonly string _macroDir;
    private readonly MacroService _service;

    public UIMacroIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        _windowHandle = _fixture.TestWindowHandleString;

        _macroDir = Path.Combine(Path.GetTempPath(), "mcp-windows-macro-it-" + Guid.NewGuid().ToString("N"));
        _service = new MacroService(_macroDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_macroDir))
            {
                Directory.Delete(_macroDir, recursive: true);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    private static string ExtractText(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().Single().Text;

    [Fact]
    public async Task Save_ThenRun_ReplaysWorkflowThroughBatchEngine()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "macro-user", clearFirst = true },
            new { action = "type", automationId = "PasswordInput", controlType = "Edit", text = "macro-pass", clearFirst = true },
            new { action = "click", name = "Submit", controlType = "Button" },
        });

        var save = await _service.SaveAsync("login-flow", steps);
        Assert.True(save.Success);
        Assert.Equal(3, save.StepCount);

        // Load the persisted steps and replay them through the batch engine, exactly as the
        // ui_macro 'run' action does internally.
        var stepsJson = await _service.LoadStepsJsonAsync("login-flow");
        Assert.NotNull(stepsJson);

        var result = await Sbroenne.WindowsMcp.Automation.Tools.UIBatchTool.ExecuteAsync(
            _windowHandle, stepsJson!, stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        var batch = JsonSerializer.Deserialize<BatchResult>(ExtractText(result), ParseOptions)!;
        Assert.True(batch.Success, $"Macro replay failed: {ExtractText(result)}");
        Assert.Equal(3, batch.StepsRun);
        Assert.Equal(3, batch.StepsSucceeded);
    }

    [Fact]
    public async Task Tool_Run_MissingMacro_ReturnsError()
    {
        var result = await UIMacroTool.ExecuteAsync(
            MacroAction.Run, "does-not-exist", steps: null, windowHandle: _windowHandle,
            stopOnError: true, withSnapshot: false, includeDiagnostics: false, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("does not exist", ExtractText(result), StringComparison.Ordinal);
    }
}
