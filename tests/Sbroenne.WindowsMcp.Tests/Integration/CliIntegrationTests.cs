using System.Text.Json;
using System.Diagnostics;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the <c>wincli</c> CLI (Phase 3). These prove the CLI is a faithful,
/// thin adapter over the same tool <c>ExecuteAsync</c> methods the MCP server uses: correct
/// argument parsing, correct exit codes (0 success / 1 tool error / 2 usage error), and output
/// that is byte-for-byte identical to the MCP server for the same operation.
/// </summary>
[Collection("UITestHarness")]
[Trait("Category", "Integration")]
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

    private async Task<string> DiscoverIdAsync(string? name, string? automationId)
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = name,
            AutomationId = automationId,
            RequireUnique = true
        });
        Assert.True(result.Success, result.ErrorMessage);
        return Assert.Single(result.Items!).Id;
    }

    /// <summary>Runs a CLI command in-process, capturing stdout, stderr, and the exit code.</summary>
    private static async Task<(int Code, string Stdout, string Stderr)> RunAsync(params string[] args)
    {
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        var parsed = ParsedArgs.Parse(args);
        var code = await CommandDispatcher.DispatchAsync(parsed, outWriter, errWriter, CancellationToken.None);
        return (code, outWriter.ToString(), errWriter.ToString());
    }

    private static bool SuccessOf(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
    }

    internal static async Task<(int Code, string Stdout, string Stderr)> RunSeparateProcessAsync(
        params string[] args)
    {
        var executable = Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            throw new TimeoutException($"CLI process {process.Id} did not exit within 60 seconds.");
        }
        return (process.ExitCode, await stdout, await stderr);
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
            "ui", "click", "--window", _windowHandle, "--element-id", await DiscoverIdAsync("Submit", null));

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiType_IntoHarnessField_Succeeds()
    {
        var (code, stdout, _) = await RunAsync(
            "ui", "type", "--window", _windowHandle,
            "--element-id", await DiscoverIdAsync(null, "UsernameInput"),
            "--text", "cli-user", "--clear-first");

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiClick_MissingWindow_ReturnsToolError()
    {
        var (code, stdout, _) = await RunAsync("ui", "click", "--element-id", "unknown");

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
            new { action = "find", automationId = "UsernameInput", controlType = "Edit", requireUnique = true },
            new { action = "type", elementId = "$prev", text = "batch-cli", clearFirst = true },
            new { action = "find", name = "Submit", controlType = "Button", requireUnique = true },
            new { action = "click", elementId = "$prev" },
        });

        var (code, stdout, _) = await RunAsync("ui", "batch", "--window", _windowHandle, "--steps", steps);

        Assert.Equal(0, code);
        Assert.True(SuccessOf(stdout), stdout);
    }

    [Fact]
    public async Task Cli_UiSnapshot_ForwardsAutomaticMode()
    {
        var (resetCode, resetOutput, _) = await RunAsync(
            "ui", "snapshot", "--window", _windowHandle, "--mode", "reset");
        var (autoCode, autoOutput, _) = await RunAsync(
            "ui", "snapshot", "--window", _windowHandle, "--mode", "auto");

        using var resetDocument = JsonDocument.Parse(resetOutput);
        using var autoDocument = JsonDocument.Parse(autoOutput);
        Assert.Equal(0, resetCode);
        Assert.Equal("full", resetDocument.RootElement.GetProperty("kind").GetString());
        Assert.Equal(0, autoCode);
        Assert.Equal("diff", autoDocument.RootElement.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Cli_UiSnapshot_SeparateProcessesDoNotShareRememberedViews()
    {
        var first = await RunSeparateProcessAsync(
            "ui", "snapshot", "--window", _windowHandle, "--mode", "auto");
        var second = await RunSeparateProcessAsync(
            "ui", "snapshot", "--window", _windowHandle, "--mode", "auto");

        using var firstDocument = JsonDocument.Parse(first.Stdout);
        using var secondDocument = JsonDocument.Parse(second.Stdout);
        Assert.Equal(0, first.Code);
        Assert.Equal("full", firstDocument.RootElement.GetProperty("kind").GetString());
        Assert.Equal(0, second.Code);
        Assert.Equal("full", secondDocument.RootElement.GetProperty("kind").GetString());
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
        await RunAsync("ui", "click", "--window", _windowHandle, "--element-id", await DiscoverIdAsync("Data Grid", null));
        await Task.Delay(150);

        var gridId = await DiscoverIdAsync(null, "ProductsDataGrid");
        var direct = await UIReadTableTool.ExecuteAsync(
            _windowHandle, gridId,
            maxRows: 200, maxColumns: 50, includeDiagnostics: false, CancellationToken.None);
        var directText = direct.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        var (code, stdout, _) = await RunAsync(
            "ui", "read-table", "--window", _windowHandle, "--element-id", gridId);

        Assert.Equal(0, code);
        Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
    }

    [Trait("Category", "RequiresDesktop")]
    [Fact]
    public async Task Cli_Clipboard_SetGetClear_RoundTrips()
    {
        var payload = $"wincli-clip-{Guid.NewGuid():N}";

        var (setCode, setOut, _) = await RunAsync("clipboard", "set", "--text", payload);
        Assert.Equal(0, setCode);
        Assert.True(SuccessOf(setOut), setOut);

        var (getCode, getOut, _) = await RunAsync("clipboard", "get");
        Assert.Equal(0, getCode);
        using (var doc = JsonDocument.Parse(getOut))
        {
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), getOut);
            Assert.Equal(payload, doc.RootElement.GetProperty("text").GetString());
        }

        var (clearCode, clearOut, _) = await RunAsync("clipboard", "clear");
        Assert.Equal(0, clearCode);
        Assert.True(SuccessOf(clearOut), clearOut);

        var (_, afterClear, _) = await RunAsync("clipboard", "get");
        using (var doc = JsonDocument.Parse(afterClear))
        {
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), afterClear);
            Assert.False(doc.RootElement.GetProperty("hasText").GetBoolean(), afterClear);
        }
    }

    [Trait("Category", "RequiresDesktop")]
    [Fact]
    public async Task Cli_ClipboardGet_MatchesMcpServerOutputExactly()
    {
        // Seed a deterministic value so both entry points observe the same clipboard state.
        await Sbroenne.WindowsMcp.Clipboard.Tools.ClipboardTool.ExecuteAsync(
            Sbroenne.WindowsMcp.Models.ClipboardAction.Set, "parity-probe", CancellationToken.None);

        var direct = await Sbroenne.WindowsMcp.Clipboard.Tools.ClipboardTool.ExecuteAsync(
            Sbroenne.WindowsMcp.Models.ClipboardAction.Get, text: null, CancellationToken.None);
        var directText = direct.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        var (code, stdout, _) = await RunAsync("clipboard", "get");

        Assert.Equal(0, code);
        Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
    }

    [Trait("Category", "RequiresDesktop")]
    [SkippableFact]
    public async Task Cli_FileOpen_DrivesOpenDialog()
    {
        DesktopInputTests.SkipUnlessEnabled();

        var testFilePath = Path.Combine(Path.GetTempPath(), $"wincli-open-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(testFilePath, "cli open content");
        try
        {
            await TestRetry.RunAsync(async _ =>
            {
                _fixture.BringToFront();
                await Task.Delay(500);

                var (code, stdout, _) = await RunAsync("file-open", "--window", _windowHandle, "--path", testFilePath);

                Assert.Equal(0, code);
                Assert.True(SuccessOf(stdout), stdout);

                await Task.Delay(300);
                var lastOpened = _fixture.Form!.Invoke(new Func<string?>(() => _fixture.Form!.LastOpenPath));
                Assert.Equal(testFilePath, lastOpened, ignoreCase: true);
            });
        }
        finally
        {
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    [Trait("Category", "RequiresDesktop")]
    [Fact]
    public async Task Cli_FileOpen_MatchesMcpServerOutputExactly()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), $"wincli-open-parity-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(testFilePath, "parity");
        try
        {
            // Both entry points must reject a nonexistent window handle identically.
            var direct = await UIOpenFileTool.ExecuteAsync("0", testFilePath, includeDiagnostics: false, CancellationToken.None);
            var directText = direct.Content
                .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                .Single().Text;

            var (_, stdout, _) = await RunAsync("file-open", "--window", "0", "--path", testFilePath);

            Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
        }
        finally
        {
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    [Fact]
    public async Task Cli_Macro_SaveListGetDelete_RoundTrips()
    {
        var name = "cli-macro-" + Guid.NewGuid().ToString("N");
        const string steps = "[{\"action\":\"find\",\"name\":\"Submit\",\"requireUnique\":true},{\"action\":\"click\",\"elementId\":\"$prev\"}]";
        try
        {
            var (saveCode, saveOut, _) = await RunAsync("macro", "save", "--name", name, "--steps", steps);
            Assert.Equal(0, saveCode);
            Assert.True(SuccessOf(saveOut), saveOut);

            var (listCode, listOut, _) = await RunAsync("macro", "list");
            Assert.Equal(0, listCode);
            Assert.Contains(name, listOut, StringComparison.Ordinal);

            var (getCode, getOut, _) = await RunAsync("macro", "get", "--name", name);
            Assert.Equal(0, getCode);
            using (var doc = JsonDocument.Parse(getOut))
            {
                Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), getOut);
                Assert.Equal(2, doc.RootElement.GetProperty("stepCount").GetInt32());
                Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("steps").ValueKind);
            }

            var (delCode, delOut, _) = await RunAsync("macro", "delete", "--name", name);
            Assert.Equal(0, delCode);
            Assert.True(SuccessOf(delOut), delOut);
        }
        finally
        {
            await RunAsync("macro", "delete", "--name", name);
        }
    }

    [Fact]
    public async Task Cli_MacroRun_MissingMacro_MatchesMcpServerOutputExactly()
    {
        var name = "missing-" + Guid.NewGuid().ToString("N");

        // Both entry points must produce identical output for a run against a nonexistent macro.
        var direct = await Sbroenne.WindowsMcp.Macros.Tools.UIMacroTool.ExecuteAsync(
            Sbroenne.WindowsMcp.Models.MacroAction.Run, name, steps: null, windowHandle: _windowHandle,
            stopOnError: true, withSnapshot: false, snapshotMode: "full", includeDiagnostics: false, CancellationToken.None);
        var directText = direct.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        var (code, stdout, _) = await RunAsync("macro", "run", "--name", name, "--window", _windowHandle);

        Assert.Equal(1, code);
        Assert.Equal(directText, stdout.TrimEnd('\r', '\n'));
    }

    [Fact]
    public void Cli_HelpAndTools_AreNonEmptyAndCoverAllGroups()
    {
        Assert.Contains("wincli", HelpText.Usage, StringComparison.Ordinal);
        foreach (var group in new[] { "app", "window", "ui", "keyboard", "mouse", "screenshot", "clipboard", "macro", "file-save", "file-open" })
        {
            Assert.Contains(group, HelpText.Usage, StringComparison.Ordinal);
            Assert.Contains(group, HelpText.Tools, StringComparison.Ordinal);
        }
    }
}
