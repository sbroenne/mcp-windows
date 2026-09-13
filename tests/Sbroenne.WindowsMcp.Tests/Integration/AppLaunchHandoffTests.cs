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
public sealed class AppLaunchHandoffTests : IAsyncLifetime
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");
    private McpClient? _client;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }

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
            var receipt = Path.Combine(directory, "receipt.json");
            Assert.True(File.Exists(receipt), "The owned primary must actually receive the request.");
            var received = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(receipt));
            Assert.NotNull(received);
            Assert.Equal(["owned local request"], received);
            Assert.Equal(code == 0, success);
            Assert.Equal(code == 0, result.RootElement.GetProperty("success").GetBoolean());
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
            await DeleteFixtureDirectoryAsync(directory);
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
            await DeleteFixtureDirectoryAsync(directory);
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 800)]
    [InlineData(true, 0)]
    [InlineData(true, 800)]
    public async Task ExecutableName_UsesActualImageNotWindowTitle(bool cli, int delay)
    {
        var directory = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"handoff-{Guid.NewGuid():N}")).FullName;
        using var receiver = Start(FixturePath, ["--handoff-primary", directory, "1"]);
        try
        {
            Assert.NotNull(await receiver.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            var (success, json) = await LaunchAsync(cli, Path.GetFileName(FixturePath),
                $"--handoff-secondary \"{directory}\" {delay} 0", AppContext.BaseDirectory);
            Assert.True(File.Exists(Path.Combine(directory, "receipt.json")));
            Assert.True(success, json);
            using var result = JsonDocument.Parse(json);
            Assert.Equal("possibleHandoff", result.RootElement.GetProperty("launchStatus").GetString());
            Assert.Equal(receiver.Id, result.RootElement.GetProperty("window").GetProperty("pid").GetInt32());
        }
        finally
        {
            await StopOwnedAsync(receiver);
            if (cli)
            {
                await RunCliAsync(["service", "stop"]);
            }
            await DeleteFixtureDirectoryAsync(directory);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewWindow_IsOwnedByLaunchedProcessNotExistingInstance(bool cli)
    {
        var root = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"handoff-{Guid.NewGuid():N}")).FullName;
        var first = Directory.CreateDirectory(Path.Combine(root, "first")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(root, "second")).FullName;
        using var existing = Start(FixturePath, ["--handoff-primary", first, "1"]);
        try
        {
            Assert.NotNull(await existing.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            var (success, json) = await LaunchAsync(cli, FixturePath, $"--handoff-primary \"{second}\" 1");
            Assert.True(success, json);
            using var result = JsonDocument.Parse(json);
            Assert.Equal("windowObserved", result.RootElement.GetProperty("launchStatus").GetString());
            var pid = result.RootElement.GetProperty("window").GetProperty("pid").GetInt32();
            Assert.NotEqual(existing.Id, pid);
            var ownerFile = Path.Combine(second, "owner.json");
            Assert.True(await TestWait.UntilAsync(() => File.Exists(ownerFile), TimeSpan.FromSeconds(10)));
            var owner = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(ownerFile));
            Assert.NotNull(owner);
            Assert.Equal(int.Parse(Assert.Single(owner), CultureInfo.InvariantCulture), pid);
            using var observedProcess = Process.GetProcessById(pid);
            Assert.False(observedProcess.HasExited);
            Assert.Contains("not verified", result.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
        }
        finally
        {
            await StopOwnedAsync(existing);
            var ownerFile = Path.Combine(second, "owner.json");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!File.Exists(ownerFile))
            {
                await Task.Delay(25, timeout.Token);
            }
            var owner = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(ownerFile));
            Assert.NotNull(owner);
            using var launched = Process.GetProcessById(int.Parse(Assert.Single(owner), CultureInfo.InvariantCulture));
            await StopOwnedAsync(launched);
            if (cli)
            {
                await RunCliAsync(["service", "stop"]);
            }
            await DeleteFixtureDirectoryAsync(root);
        }
    }

    private async Task<(bool Success, string Json)> LaunchAsync(bool cli, string path, string arguments, string? workingDirectory = null)
    {
        if (cli)
        {
            var options = new List<string> { "app", "--path", path, $"--args={arguments}", "--timeout", "3000" };
            if (workingDirectory is not null)
            {
                options.AddRange(["--working-dir", workingDirectory]);
            }
            var result = await RunCliAsync([.. options]);
            Assert.True(result.Code is 0 or 1, result.Error + result.Output);
            return (result.Code == 0, result.Output);
        }

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = Path.ChangeExtension(typeof(AppTool).Assembly.Location, ".exe"),
            Name = "owned-launch-test",
        });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        // Keep the server alive until fixture cleanup: disposing stdio transport terminates its child tree.
        _client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
        var response = await _client.CallToolAsync("app", new Dictionary<string, object?>
        {
            ["programPath"] = path,
            ["arguments"] = arguments,
            ["workingDirectory"] = workingDirectory,
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

    private static async Task DeleteFixtureDirectoryAsync(string directory)
    {
        Exception? lastError = null;
        var deleted = await TestWait.UntilAsync(() =>
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastError = ex;
                return false;
            }
        }, TimeSpan.FromSeconds(10));
        Assert.True(deleted, $"Owned fixture files remained locked after process exit: {lastError}");
    }
}
