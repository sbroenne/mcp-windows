using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Processes;
using SysProcess = System.Diagnostics.Process;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ProcessService"/> covering the read-only listing paths and the
/// non-destructive validation branches of <c>kill</c>. These never terminate a real process, so
/// they are safe to run anywhere and need no desktop. Actual termination is covered by the
/// integration tests, which spawn and kill their own child processes.
/// </summary>
public sealed class ProcessServiceTests
{
    private readonly ProcessService _service = new();

    [Fact]
    public void List_ReturnsRunningProcesses_IncludingTheCurrentOne()
    {
        var currentName = SysProcess.GetCurrentProcess().ProcessName;

        var result = _service.List(nameFilter: currentName);

        Assert.True(result.Success);
        Assert.NotNull(result.Processes);
        Assert.NotEmpty(result.Processes!);
        Assert.All(result.Processes!, p => Assert.Contains(currentName, p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void List_RespectsLimit()
    {
        var result = _service.List(limit: 3);

        Assert.True(result.Success);
        Assert.NotNull(result.Processes);
        Assert.True(result.Processes!.Count <= 3, $"expected <= 3, got {result.Processes!.Count}");
        Assert.Equal(result.Processes!.Count, result.Count);
    }

    [Fact]
    public void List_ClampsNonPositiveLimitToAtLeastOne()
    {
        var result = _service.List(limit: -10);

        Assert.True(result.Success);
        Assert.NotNull(result.Processes);
        Assert.True(result.Processes!.Count <= 1);
    }

    [Fact]
    public void List_SortByMemory_IsDescending()
    {
        var result = _service.List(sortBy: ProcessSortBy.Memory, limit: 50);

        var memory = result.Processes!.Select(p => p.MemoryMb).ToList();
        for (var i = 1; i < memory.Count; i++)
        {
            Assert.True(memory[i - 1] >= memory[i], "memory ordering must be non-increasing");
        }
    }

    [Fact]
    public void List_SortByPid_IsAscending()
    {
        var result = _service.List(sortBy: ProcessSortBy.Pid, limit: 50);

        var pids = result.Processes!.Select(p => p.Pid).ToList();
        for (var i = 1; i < pids.Count; i++)
        {
            Assert.True(pids[i - 1] <= pids[i], "pid ordering must be non-decreasing");
        }
    }

    [Fact]
    public void Kill_WithNeitherPidNorName_Fails()
    {
        var result = _service.Kill();

        Assert.False(result.Success);
        Assert.Contains("pid or a name", result.Error);
    }

    [Fact]
    public void Kill_UnknownPid_Fails()
    {
        // A pid that is astronomically unlikely to exist.
        var result = _service.Kill(pid: 999_999_999);

        Assert.False(result.Success);
        Assert.Contains("999999999", result.Error);
    }

    [Fact]
    public void Kill_ProtectedProcessByName_IsRefused_AndNeverTerminated()
    {
        // csrss.exe is a critical Windows process present on every session; the service must refuse
        // it rather than attempt (and fail) a real kill.
        var result = _service.Kill(name: "csrss");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        // Still running afterwards.
        Assert.NotEmpty(SysProcess.GetProcessesByName("csrss"));
    }
}
