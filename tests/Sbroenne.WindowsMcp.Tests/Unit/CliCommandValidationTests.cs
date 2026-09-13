using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class CliCommandValidationTests
{
    [Theory]
    [InlineData("app", null)]
    [InlineData("window", "close")]
    [InlineData("window-management", "close")]
    [InlineData("keyboard", "type")]
    [InlineData("mouse", "click")]
    [InlineData("screenshot", null)]
    [InlineData("clipboard", "clear")]
    [InlineData("clip", "clear")]
    [InlineData("macro", "delete")]
    [InlineData("ui-macro", "delete")]
    [InlineData("file-save", null)]
    [InlineData("filesave", null)]
    [InlineData("save", null)]
    [InlineData("file-open", null)]
    [InlineData("fileopen", null)]
    [InlineData("open", null)]
    [InlineData("process", "kill")]
    [InlineData("proc", "kill")]
    public async Task UnknownOption_ReturnsUsageBeforeAnyToolDispatch(string group, string? action)
    {
        var arguments = new List<string> { group };
        if (action is not null)
        {
            arguments.Add(action);
        }
        arguments.Add("--typo");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var code = await CommandDispatcher.DispatchAsync(ParsedArgs.Parse([.. arguments]), output, error, CancellationToken.None);

        Assert.Equal(2, code);
        Assert.Empty(output.ToString());
        Assert.Contains("--typo", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task App_WithPathAndUnknownOption_DoesNotLaunch()
    {
        // A nonexistent executable is deliberately safe even if validation regresses.
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await CommandDispatcher.DispatchAsync(
            ParsedArgs.Parse(["app", "--path", "nonexistent-review-test.exe", "--typo"]),
            output, error, CancellationToken.None);

        Assert.Equal(2, code);
        Assert.Empty(output.ToString());
        Assert.Contains("--typo", error.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("app --program sample.exe --arguments=--new-window --cwd . --no-wait --timeout 500")]
    [InlineData("window-management move --window 123 --x 1 --y 2 --width 30 --height 40")]
    [InlineData("keyboard press --handle 123 --key enter --delay 1 --clear")]
    [InlineData("mouse drag --handle 123 --x 1 --y 2 --endx 3 --endy 4 --expected-title sample")]
    [InlineData("screenshot --action capture --out image.png --no-annotate --cursor --format png")]
    [InlineData("open --handle 123 --file example.txt --trigger none --timeout 500 --diagnostics")]
    [InlineData("save --handle 123 --file-path example.txt --diagnostics")]
    [InlineData("proc list --sort memory --limit 5 --force")]
    [InlineData("ui-macro run --name sample --handle 123 --snapshot --since token --no-stop-on-error")]
    [InlineData("clip set --text sample")]
    public void SupportedOptionsAndAliases_RemainAccepted(string command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var parsed = ParsedArgs.Parse(command.Split(' '));

        Assert.Null(CommandDispatcher.ValidateNonUiOptions(parsed));
    }
}
