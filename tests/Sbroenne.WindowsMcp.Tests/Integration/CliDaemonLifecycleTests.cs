using System.Diagnostics;
using System.Text.Json;
using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>Exercises the real CLI-only owner without interacting with desktop applications.</summary>
[Trait("Category", "Integration")]
public sealed class CliDaemonLifecycleTests
{
    [Fact]
    public async Task ConcurrentStart_Status_Stop_Restart_UseOnePersistentOwner()
    {
        await RunAsync("service", "stop");
        try
        {
            var starts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunAsync("service", "start")));
            foreach (var start in starts)
            {
                Assert.True(start.Code == 0, start.Error + start.Output);
            }

            var statuses = await Task.WhenAll(RunAsync("service", "status"), RunAsync("service", "status"));
            using var first = JsonDocument.Parse(statuses[0].Output);
            using var second = JsonDocument.Parse(statuses[1].Output);
            Assert.Equal("running", first.RootElement.GetProperty("state").GetString());
            var pid = first.RootElement.GetProperty("processId").GetInt32();
            Assert.Equal(pid, second.RootElement.GetProperty("processId").GetInt32());
            var generation = first.RootElement.GetProperty("generation").GetString();

            Assert.Equal(0, (await RunAsync("service", "stop")).Code);
            using var stopped = JsonDocument.Parse((await RunAsync("service", "status")).Output);
            Assert.Equal("stopped", stopped.RootElement.GetProperty("state").GetString());
            Assert.Equal(0, (await RunAsync("service", "start")).Code);
            using var restarted = JsonDocument.Parse((await RunAsync("service", "status")).Output);
            Assert.NotEqual(generation, restarted.RootElement.GetProperty("generation").GetString());
        }
        finally
        {
            await RunAsync("service", "stop");
        }
    }

    [Fact]
    public async Task InformationalCommands_DoNotStartOwner()
    {
        await RunAsync("service", "stop");
        foreach (var command in new[] { "--help", "--version", "tools", "guidance" })
        {
            Assert.Equal(0, (await RunAsync(command)).Code);
        }

        using var status = JsonDocument.Parse((await RunAsync("service", "status")).Output);
        Assert.Equal("stopped", status.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public async Task DotnetLaunch_UsesSameOwnerAsExecutableLaunch()
    {
        await RunAsync("service", "stop");
        try
        {
            var started = await RunProcessAsync("dotnet",
                [typeof(CommandDispatcher).Assembly.Location, "service", "start"]);
            Assert.True(started.Code == 0, started.Error + started.Output);
            using var dotnetOwner = JsonDocument.Parse(started.Output);
            using var executableOwner = JsonDocument.Parse((await RunAsync("service", "status")).Output);
            Assert.Equal(
                dotnetOwner.RootElement.GetProperty("generation").GetString(),
                executableOwner.RootElement.GetProperty("generation").GetString());
        }
        finally
        {
            await RunAsync("service", "stop");
        }
    }

    [Fact]
    public async Task DifferentCallerDirectories_ResolveRelativePathsAndShareOneOwner()
    {
        var root = Path.Combine(AppContext.BaseDirectory, $"cli-working-directories-{Guid.NewGuid():N}");
        var fixture = Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");
        var executable = Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe");
        string? generation = null;
        try
        {
            foreach (var name in new[] { "first", "second" })
            {
                var directory = Directory.CreateDirectory(Path.Combine(root, name)).FullName;
                var launched = await RunProcessAsync(executable,
                    ["app", "--path", Path.GetRelativePath(directory, fixture), "--no-wait",
                        $"--args={name} --record-arguments=arguments.json"], directory);
                Assert.True(launched.Code == 0, launched.Error + launched.Output);
                var recorded = Path.Combine(directory, "arguments.json");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                while (!File.Exists(recorded))
                {
                    await Task.Delay(25, timeout.Token);
                }

                var actual = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(recorded, timeout.Token));
                Assert.NotNull(actual);
                Assert.Equal([name], actual);
                using var status = JsonDocument.Parse((await RunAsync("service", "status")).Output);
                var currentGeneration = status.RootElement.GetProperty("generation").GetString();
                if (generation is not null)
                {
                    Assert.Equal(generation, currentGeneration);
                }

                generation = currentGeneration;
            }
        }
        finally
        {
            await RunAsync("service", "stop");
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Find_ThenReadAndStateWaitAcrossProcesses_StopDoesNotCloseApplication()
    {
        var info = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        info.ArgumentList.Add("--inert-window");
        using var fixture = Process.Start(info)!;
        try
        {
            var handle = await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(string.IsNullOrWhiteSpace(handle));
            var found = await RunAsync("ui", "find", "--window", handle!, "--name", "Inert",
                "--control-type", "Button", "--visible-only", "false", "--content-view-only", "false");
            Assert.True(found.Code == 0, found.Error + found.Output);
            using var document = JsonDocument.Parse(found.Output);
            var id = document.RootElement.GetProperty("items")[0].GetProperty("id").GetString()!;
            var read = await RunAsync("ui", "read", "--window", handle!, "--element-id", id);
            Assert.True(read.Code == 0, read.Error + read.Output);
            var wait = await RunAsync("ui", "wait", "--window", handle!, "--mode", "state",
                "--element-id", id, "--desired-state", "enabled");
            Assert.True(wait.Code == 0, wait.Error + wait.Output);

            await RunAsync("service", "stop");
            Assert.False(fixture.HasExited);
            var stale = await RunAsync("ui", "read", "--window", handle!, "--element-id", id);
            Assert.Equal(1, stale.Code);
        }
        finally
        {
            await RunAsync("service", "stop");
            if (!fixture.HasExited)
            {
                fixture.Kill();
                await fixture.WaitForExitAsync();
            }
        }
    }

    private static Task<(int Code, string Output, string Error)> RunAsync(params string[] arguments) =>
        RunProcessAsync(Path.ChangeExtension(typeof(CommandDispatcher).Assembly.Location, ".exe"), arguments);

    private static async Task<(int Code, string Output, string Error)> RunProcessAsync(
        string executable, string[] arguments, string? workingDirectory = null)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (workingDirectory is not null)
        {
            info.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
        // A daemon must not inherit these handles and keep client pipelines open forever.
        await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(10));
        return (process.ExitCode, await stdout, await stderr);
    }
}
