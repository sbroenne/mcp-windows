using System.Diagnostics;
using System.Text.Json;
using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>Real CLI processes; no input injection, foreground activation, browsers, or accounts.</summary>
[Trait("Category", "Integration")]
public sealed class CliSafetyProcessTests : IAsyncLifetime
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await RunAsync(["service", "stop"]);
    }

    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public async Task App_EqualsArguments_AreRecordedExactlyByLocalChild(string option)
    {
        await AssertRecordedAsync($"--{option}=--new-window \"local target\"", null, ["--new-window", "local target"]);
    }

    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public async Task App_OrdinaryUrl_IsRecordedWithoutOpeningIt(string option)
    {
        await AssertRecordedAsync($"--{option}", "https://example.invalid/local", ["https://example.invalid/local"]);
    }

    [Theory]
    [InlineData("args", "--new-window target")]
    [InlineData("arguments", "--new-window target")]
    [InlineData("args", null)]
    [InlineData("arguments", null)]
    [InlineData("args", "--no-wait")]
    [InlineData("arguments", "--no-wait")]
    public async Task App_AmbiguousOrMissingValue_ReturnsUsageWithoutLaunching(string option, string? value)
    {
        var output = Path.Combine(AppContext.BaseDirectory, $"cli-arguments-{Guid.NewGuid():N}.json");
        try
        {
            var arguments = new List<string> { "app", "--path", FixturePath, "--no-wait", $"--{option}" };
            if (value is not null)
            {
                arguments.Add(value);
            }

            var result = await RunAsync(arguments, output);
            Assert.Equal(2, result.Code);
            Assert.Empty(result.Stdout);
            Assert.Contains($"--{option}=", result.Stderr, StringComparison.Ordinal);
            Assert.False(File.Exists(output));
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Theory]
    [InlineData("state")]
    [InlineData("STATE")]
    [InlineData(" state ")]
    public async Task Ui_StateWaitWithoutId_RequiresObservedTarget(string mode)
    {
        var result = await RunAsync(["ui", "wait", "--window", "0", "--mode", mode, "--name", "Inert", "--desired-state", "enabled"]);

        Assert.Equal(2, result.Code);
        Assert.Contains("--element-id", result.Stderr, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("tools")]
    [InlineData("guidance")]
    public async Task Discovery_ExplainsElementIdLifetime(string command)
    {
        var result = await RunAsync([command]);

        Assert.Equal(0, result.Code);
        Assert.Contains("daemon", result.Stdout, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertRecordedAsync(string option, string? value, string[] expected)
    {
        var output = Path.Combine(AppContext.BaseDirectory, $"cli-arguments-{Guid.NewGuid():N}.json");
        try
        {
            var recordArgument = $" --record-arguments=\"{output}\"";
            var arguments = new List<string> { "app", "--path", FixturePath, "--no-wait",
                value is null ? option + recordArgument : option };
            if (value is not null)
            {
                arguments.Add(value + recordArgument);
            }

            var result = await RunAsync(arguments);
            Assert.True(result.Code == 0, result.Stdout + result.Stderr);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!File.Exists(output))
            {
                await Task.Delay(25, timeout.Token);
            }

            var actual = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(output, timeout.Token));
            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(output);
        }
    }

    private static Process Start(string executable, IEnumerable<string> arguments, string? output = null)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (output is not null)
        {
            info.Environment["WINCLI_TEST_ARGUMENT_OUTPUT"] = output;
        }

        return Process.Start(info) ?? throw new InvalidOperationException($"Could not start {executable}.");
    }

    private static async Task<(int Code, string Stdout, string Stderr)> RunAsync(IEnumerable<string> arguments, string? output = null)
    {
        using var process = Start(Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe"), arguments, output);
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(10));
            return (process.ExitCode, await stdout, await stderr);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
    }
}
