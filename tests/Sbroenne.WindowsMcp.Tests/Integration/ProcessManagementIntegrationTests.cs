using System.Text.Json;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Processes;
using SysProcess = System.Diagnostics.Process;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for process listing and termination. These spawn their own short-lived child
/// processes and only ever terminate those children (by pid, or by a uniquely-named copy for the
/// by-name path), so they never touch unrelated processes on a shared machine. They cover both the
/// <see cref="ProcessService"/> directly and the <c>wincli process</c> CLI adapter.
/// </summary>
public sealed class ProcessManagementIntegrationTests
{
    private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Spawns a headless process that stays alive for ~30s (a pinging cmd shell).</summary>
    private static SysProcess StartLongChild(string? executablePath = null)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executablePath ?? "cmd.exe",
            Arguments = "/c ping -n 30 127.0.0.1",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var process = SysProcess.Start(psi);
        Assert.NotNull(process);
        return process!;
    }

    [Fact]
    public void List_IncludesASpawnedChild()
    {
        using var child = StartLongChild();
        try
        {
            var service = new ProcessService();
            var result = service.List(nameFilter: "cmd", limit: ProcessService.MaxListLimit);

            Assert.True(result.Success);
            Assert.Contains(result.Processes!, p => p.Pid == child.Id);
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
            }
        }
    }

    [Fact]
    public void Kill_ByPid_TerminatesSpawnedChild()
    {
        var child = StartLongChild();
        var service = new ProcessService();

        var result = service.Kill(pid: child.Id, force: true);

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Killed);
        Assert.Contains(result.Killed!, p => p.Pid == child.Id);

        Assert.True(child.WaitForExit(5000), "child process did not exit after kill");
        Assert.True(child.HasExited);
        child.Dispose();
    }

    [Fact]
    public void Kill_ByName_TerminatesOnlyTheUniquelyNamedCopy()
    {
        // Copy cmd.exe to a unique name so a by-name kill cannot collateral-hit other shells.
        var systemCmd = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        var uniqueName = $"wincli-proc-test-{Guid.NewGuid():N}";
        var uniqueExe = Path.Combine(Path.GetTempPath(), uniqueName + ".exe");
        File.Copy(systemCmd, uniqueExe, overwrite: true);

        SysProcess? child = null;
        try
        {
            child = StartLongChild(uniqueExe);
            var service = new ProcessService();

            var result = service.Kill(name: uniqueName, force: true);

            Assert.True(result.Success, result.Error);
            Assert.Contains(result.Killed!, p => p.Pid == child.Id);
            Assert.True(child.WaitForExit(5000), "uniquely-named child did not exit after kill");
        }
        finally
        {
            if (child is not null)
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                }
                child.Dispose();
            }

            TryDelete(uniqueExe);
        }
    }

    [Fact]
    public async Task Cli_ProcessList_ReturnsSuccessJson()
    {
        var (code, stdout, _) = await RunAsync("process", "list", "--limit", "2");

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), stdout);
        Assert.True(doc.RootElement.GetProperty("count").GetInt32() <= 2, stdout);
    }

    [Fact]
    public async Task Cli_ProcessKill_ByPid_TerminatesChild()
    {
        var child = StartLongChild();
        try
        {
            var (code, stdout, _) = await RunAsync(
                "process", "kill", "--pid", child.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), "--force");

            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(stdout);
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), stdout);
            Assert.True(child.WaitForExit(5000), "child did not exit after CLI kill");
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
            }
            child.Dispose();
        }
    }

    [Fact]
    public async Task Cli_ProcessKill_WithoutTarget_ReturnsToolError()
    {
        var (code, stdout, _) = await RunAsync("process", "kill");

        Assert.Equal(1, code);
        using var doc = JsonDocument.Parse(stdout);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean(), stdout);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a lingering temp copy is harmless.
        }
    }

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
}
