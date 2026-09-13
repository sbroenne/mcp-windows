using System.Diagnostics;
using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class CliHelpIntegrationTests
{
    [Theory]
    [InlineData("app", "--path")]
    [InlineData("keyboard", "--modifiers")]
    [InlineData("file-save", "--path")]
    [InlineData("service", "start|status|stop")]
    public async Task Help_DoesNotRequireActionArguments(string command, string expectedOption)
    {
        var (code, stdout, stderr) = await RunAsync(command, "--help");

        Assert.Equal(0, code);
        Assert.Empty(stderr);
        Assert.Contains(expectedOption, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"success\"", stdout, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> Commands =>
        CliCommandCatalog.ToolToCommand.Values.Select(command => new object[] { command });

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task EveryCommand_HasHelpWithoutExecuting(string command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var arguments = command.Split(' ').Append("--help").ToArray();
        var (code, stdout, stderr) = await RunAsync(arguments);

        Assert.Equal(0, code);
        Assert.Empty(stderr);
        Assert.Contains(command.Split(' ')[0] + " ", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"success\"", stdout, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("save", "file-save")]
    [InlineData("filesave", "file-save")]
    [InlineData("open", "file-open")]
    [InlineData("fileopen", "file-open")]
    [InlineData("clip", "clipboard")]
    [InlineData("proc", "process")]
    [InlineData("window-management", "window")]
    [InlineData("ui-macro", "macro")]
    public async Task AliasHelp_UsesCanonicalCommand(string alias, string canonical)
    {
        var (code, stdout, stderr) = await RunAsync(alias, "--help");

        Assert.Equal(0, code);
        Assert.Empty(stderr);
        Assert.StartsWith(canonical + " ", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveHelp_WithWindow_DoesNotAttemptSave()
    {
        var (code, stdout, stderr) = await RunAsync("file-save", "--window", "0", "--help");

        Assert.Equal(0, code);
        Assert.Empty(stderr);
        Assert.StartsWith("file-save ", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"action\":\"save\"", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HelpText_AsAnOptionValue_IsNotAHelpRequest()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await CommandDispatcher.DispatchAsync(
            ParsedArgs.Parse(["keyboard", "type", "--text=--help"]),
            output, error, CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Empty(error.ToString());
        Assert.Contains("\"success\":false", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("windowHandle", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCommandHelp_ReportsUsageError()
    {
        var (code, stdout, stderr) = await RunAsync("not-a-command", "--help");

        Assert.Equal(2, code);
        Assert.Empty(stdout);
        Assert.Contains("unknown command", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task KeyboardHelp_ContainsUsableShortcutExample()
    {
        var (code, stdout, _) = await RunAsync("keyboard", "press", "--help");

        Assert.Equal(0, code);
        Assert.Contains("keyboard press --window <h> --key A --modifiers Ctrl", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("mouse <action>", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_UsesRequestWritersWhenDispatchedInProcess()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await CommandDispatcher.DispatchAsync(
            ParsedArgs.Parse(["file-save", "--window", "0", "--help"]),
            output, error, CancellationToken.None);

        Assert.Equal(0, code);
        Assert.Empty(error.ToString());
        Assert.StartsWith("file-save ", output.ToString(), StringComparison.Ordinal);
    }

    private static async Task<(int Code, string Stdout, string Stderr)> RunAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start wincli.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return (process.ExitCode, await stdout, await stderr);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }
}
