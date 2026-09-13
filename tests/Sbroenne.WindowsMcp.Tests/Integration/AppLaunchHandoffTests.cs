using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("WindowManagement")]
[Trait("Category", "RequiresDesktop")]
public sealed class AppLaunchHandoffTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");

    public static TheoryData<bool, int, int, int> HandoffCases
    {
        get
        {
            var cases = new TheoryData<bool, int, int, int>();
            foreach (var cli in new[] { false, true })
            {
                foreach (var delay in new[] { 0, 800 })
                {
                    foreach (var code in new[] { 0, 7 })
                    {
                        foreach (var windows in new[] { 1, 2 })
                        {
                            cases.Add(cli, delay, code, windows);
                        }
                    }
                }
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(HandoffCases))]
    public async Task ExistingReceiver_ExitStatusAndAmbiguityArePreserved(bool cli, int delay, int code, int windowCount)
    {
        var directory = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"handoff-{Guid.NewGuid():N}")).FullName;
        using var receiver = Start(FixturePath, ["--handoff-primary", directory, windowCount.ToString(CultureInfo.InvariantCulture)]);
        try
        {
            var handles = JsonSerializer.Deserialize<string[]>((await receiver.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)))!)!;
            Assert.Equal(windowCount, handles.Length);

            var (success, json) = await LaunchAsync(cli, FixturePath, $"--handoff-secondary \"{directory}\" {delay} {code}");
            using var result = JsonDocument.Parse(json);
            Assert.Equal(code == 0, success);
            Assert.Equal(code == 0, result.RootElement.GetProperty("success").GetBoolean());
            var receipt = Path.Combine(directory, "receipt.json");
            Assert.True(File.Exists(receipt), "The owned primary must actually receive the request.");
            var received = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(receipt));
            Assert.NotNull(received);
            Assert.Equal(["owned local request"], received);
            if (code != 0)
            {
                Assert.Contains("code 7", result.RootElement.GetProperty("error").GetString(), StringComparison.Ordinal);
                Assert.False(result.RootElement.TryGetProperty("window", out _));
                return;
            }

            Assert.Equal("possibleHandoff", result.RootElement.GetProperty("launchStatus").GetString());
            Assert.Contains("not verified", result.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
            Assert.DoesNotContain("focused and ready", json, StringComparison.OrdinalIgnoreCase);
            if (windowCount == 1)
            {
                Assert.Equal(handles[0], result.RootElement.GetProperty("window").GetProperty("handle").GetString());
            }
            else
            {
                Assert.False(result.RootElement.TryGetProperty("window", out _));
            }
        }
        finally
        {
            await StopOwnedAsync(receiver);
            if (cli)
            {
                await RunCliAsync(["service", "stop"]);
            }
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 800, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 800, false)]
    [InlineData(false, 0, true)]
    [InlineData(true, 800, true)]
    public async Task ZeroExit_WithoutMatchingExecutableReceiver_DoesNotClaimHandoff(bool cli, int delay, bool otherPath)
    {
        var directory = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"handoff-{Guid.NewGuid():N}")).FullName;
        Process? receiver = null;
        try
        {
            if (otherPath)
            {
                foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.*"))
                {
                    File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
                }
                receiver = Start(Path.Combine(directory, Path.GetFileName(FixturePath)), ["--handoff-primary", directory, "1"]);
                Assert.NotNull(await receiver.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            }

            var (success, json) = await LaunchAsync(cli, FixturePath, $"--exit {delay} 0");
            using var result = JsonDocument.Parse(json);
            Assert.False(success);
            Assert.False(result.RootElement.GetProperty("success").GetBoolean());
            Assert.False(result.RootElement.TryGetProperty("window", out _));
            Assert.Contains("not verified", result.RootElement.GetProperty("error").GetString(), StringComparison.Ordinal);
        }
        finally
        {
            if (receiver is not null)
            {
                await StopOwnedAsync(receiver);
                receiver.Dispose();
            }
            if (cli)
            {
                await RunCliAsync(["service", "stop"]);
            }
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<(bool Success, string Json)> LaunchAsync(bool cli, string path, string arguments)
    {
        if (cli)
        {
            var result = await RunCliAsync(["app", "--path", path, $"--args={arguments}", "--timeout", "3000"]);
            Assert.True(result.Code is 0 or 1, result.Error + result.Output);
            return (result.Code == 0, result.Output);
        }

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = Path.ChangeExtension(typeof(AppTool).Assembly.Location, ".exe"),
            Name = "owned-launch-test",
        });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
        var response = await client.CallToolAsync("app", new Dictionary<string, object?>
        {
            ["programPath"] = path,
            ["arguments"] = arguments,
            ["timeoutMs"] = 3000,
        }, cancellationToken: timeout.Token);
        return (response.IsError != true, Assert.IsType<TextContentBlock>(Assert.Single(response.Content)).Text);
    }

    private static Process Start(string path, IEnumerable<string> arguments)
    {
        var info = new ProcessStartInfo(path)
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
        return Process.Start(info) ?? throw new InvalidOperationException($"Could not start {path}.");
    }

    private static async Task<(int Code, string Output, string Error)> RunCliAsync(string[] arguments)
    {
        using var process = Start(Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe"), arguments);
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            return (process.ExitCode, await stdout.WaitAsync(TimeSpan.FromSeconds(10)), await stderr.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            await StopOwnedAsync(process);
        }
    }

    private static async Task StopOwnedAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
