using System.Text.Json;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the <c>wincli</c> CLI (Phase 3). These prove the CLI is a faithful,
/// thin adapter over the same tool <c>ExecuteAsync</c> methods the MCP server uses: correct
/// argument parsing, correct exit codes (0 success / 1 tool error / 2 usage error), and output
/// that is byte-for-byte identical to the MCP server for the same operation.
/// </summary>
[Collection("UITestHarness")]
public sealed class CliIntegrationTests
{
    private readonly UITestHarnessFixture _fixture;
    private readonly string _windowHandle;

    public CliIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        _windowHandle = _fixture.TestWindowHandleString;
    }

    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Runs a CLI command in-process, capturing stdout, stderr, and the exit code.</summary>
    private static async Task<(int Code, string Stdout, string Stderr)> RunAsync(params string[] args)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        try
        {
            var parsed = ParsedArgs.Parse(args);
            var code = await CommandDispatcher.DispatchAsync(parsed, CancellationToken.None);
            return (code, outWriter.ToString(), errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private static bool SuccessOf(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
    }

    [Fact]
    public async Task Cli_WindowList_ReturnsSuccessJsonAndZeroExit()
    {
        var (code, stdout, _) = await RunAsync("window", "list");

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiClick_OnHarnessButton_Succeeds()
    {
        var (code, stdout, _) = await RunAsync(
            "ui", "click", "--window", _windowHandle, "--name", "Submit", "--control-type", "Button");

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiType_IntoHarnessField_Succeeds()
    {
        var (code, stdout, _) = await RunAsync(
            "ui", "type", "--window", _windowHandle,
            "--automation-id", "UsernameInput", "--control-type", "Edit",
            "--text", "cli-user", "--clear-first");

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiClick_MissingWindow_ReturnsToolError()
    {
        var (code, stdout, _) = await RunAsync("ui", "click", "--name", "Submit");

        Assert.Equal(1, code);
        Assert.False(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UnknownGroup_ReturnsUsageError()
    {
        var (code, stdout, stderr) = await RunAsync("bogus");

        Assert.Equal(2, code);
        Assert.Empty(stdout);
        Assert.Contains("unknown command", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cli_MouseInvalidAction_ReturnsUsageError()
    {
        var (code, _, stderr) = await RunAsync("mouse", "wiggle");

        Assert.Equal(2, code);
        Assert.Contains("valid action", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cli_UiBatch_MissingSteps_ReturnsUsageError()
    {
        var (code, _, stderr) = await RunAsync("ui", "batch", "--window", _windowHandle);

        Assert.Equal(2, code);
        Assert.Contains("steps", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cli_UiBatch_RunsMultiStepSequence()
    {
        var steps = JsonSerializer.Serialize(new object[]
        {
            new { action = "type", automationId = "UsernameInput", controlType = "Edit", text = "batch-cli", clearFirst = true },
            new { action = "click", name = "Submit", controlType = "Button" },
        });

        var (code, stdout, _) = await RunAsync("ui", "batch", "--window", _windowHandle, "--steps", steps);

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiFind_MatchesMcpServerOutputExactly()
    {
        // Parity: the CLI must emit the identical JSON payload the MCP tool returns for the same call.
        var direct = await UIFindTool.ExecuteAsync(
            _windowHandle, "Submit", null, null, "Button", null, null,
            exactDepth: null, foundIndex: 1, includeChildren: false, sortByProminence: false,
            inRegion: null, nearElement: null, visibleOnly: null, contentViewOnly: null,
            timeoutMs: 5000, includeDiagnostics: false, CancellationToken.None);
        var directText = direct.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        var (code, stdout, _) = await RunAsync(
            "ui", "find", "--window", _windowHandle, "--name", "Submit", "--control-type", "Button");

        Assert.Equal(0, code);
        Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
    }

    [Trait("Category", "RequiresDesktop")]
    [Fact]
    public async Task Cli_UiReadTable_MatchesMcpServerOutputExactly()
    {
        // Ensure the Data Grid tab is realized so the grid exposes its rows.
        await RunAsync("ui", "click", "--window", _windowHandle, "--name", "Data Grid", "--control-type", "TabItem");
        await Task.Delay(150);

        var direct = await UIReadTableTool.ExecuteAsync(
            _windowHandle, name: null, nameContains: null, namePattern: null,
            controlType: null, automationId: "ProductsDataGrid", className: null, elementId: null,
            foundIndex: 1, maxRows: 200, maxColumns: 50, includeDiagnostics: false, CancellationToken.None);
        var directText = direct.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        var (code, stdout, _) = await RunAsync(
            "ui", "read-table", "--window", _windowHandle, "--automation-id", "ProductsDataGrid");

        Assert.Equal(0, code);
        Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
    }

    [Fact]
    public void Cli_HelpAndTools_AreNonEmptyAndCoverAllGroups()
    {
        Assert.Contains("wincli", HelpText.Usage, StringComparison.Ordinal);
        foreach (var group in new[] { "app", "window", "ui", "keyboard", "mouse", "screenshot", "file-save" })
        {
            Assert.Contains(group, HelpText.Usage, StringComparison.Ordinal);
            Assert.Contains(group, HelpText.Tools, StringComparison.Ordinal);
        }
    }
}
