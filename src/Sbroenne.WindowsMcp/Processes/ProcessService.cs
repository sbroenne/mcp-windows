using SysProcess = System.Diagnostics.Process;

namespace Sbroenne.WindowsMcp.Processes;

/// <summary>
/// Lists and terminates running processes. This is a "dumb actuator" over
/// <see cref="System.Diagnostics.Process"/>: it enumerates and kills, it does not interpret.
/// </summary>
/// <remarks>
/// A small denylist of critical Windows processes (plus the current process) is refused for
/// termination so an agent cannot take the machine down or kill the automation server itself.
/// CPU usage is intentionally not reported: an accurate percentage requires sampling over time,
/// which would add latency and violate the project's no-blocking-sleep timing rules.
/// </remarks>
public sealed class ProcessService
{
    /// <summary>Maximum number of processes a single <c>list</c> may return.</summary>
    public const int MaxListLimit = 500;

    private const int DefaultListLimit = 20;

    // Names (lower-case, without .exe) that must never be terminated.
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "idle", "system idle process", "registry", "memory compression",
        "smss", "csrss", "wininit", "winlogon", "services", "lsass",
    };

    /// <summary>
    /// Lists running processes, optionally filtered by name and ordered/limited for token economy.
    /// </summary>
    /// <param name="nameFilter">Case-insensitive substring; only processes whose name contains it are returned.</param>
    /// <param name="sortBy">Ordering of the returned list.</param>
    /// <param name="limit">Maximum rows to return (1..<see cref="MaxListLimit"/>). Null uses the default.</param>
    /// <returns>A result carrying the matched processes.</returns>
    public ProcessResult List(string? nameFilter = null, ProcessSortBy sortBy = ProcessSortBy.Memory, int? limit = null)
    {
        var effectiveLimit = Math.Clamp(limit ?? DefaultListLimit, 1, MaxListLimit);

        var summaries = new List<ProcessSummary>();
        foreach (var process in SysProcess.GetProcesses())
        {
            using (process)
            {
                var summary = TryDescribe(process);
                if (summary is null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(nameFilter) &&
                    summary.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                summaries.Add(summary);
            }
        }

        var ordered = sortBy switch
        {
            ProcessSortBy.Name => summaries.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Pid),
            ProcessSortBy.Pid => summaries.OrderBy(p => p.Pid),
            _ => summaries.OrderByDescending(p => p.MemoryMb).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
        };

        return ProcessResult.CreateListSuccess(ordered.Take(effectiveLimit).ToList());
    }

    /// <summary>
    /// Terminates a process by PID, or every process matching a name.
    /// </summary>
    /// <param name="pid">The process id to terminate. Takes precedence over <paramref name="name"/>.</param>
    /// <param name="name">The process name to terminate (all matches). Ignored when <paramref name="pid"/> is set.</param>
    /// <param name="force">When true, also terminate the entire child process tree.</param>
    /// <returns>A result describing the processes that were terminated, or a failure.</returns>
    public ProcessResult Kill(int? pid = null, string? name = null, bool force = false)
    {
        if (pid is not null)
        {
            return KillByPid(pid.Value, force);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return KillByName(name, force);
        }

        return ProcessResult.CreateFailure("kill", "Provide either a pid or a name to terminate.");
    }

    private ProcessResult KillByPid(int pid, bool force)
    {
        SysProcess process;
        try
        {
            process = SysProcess.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return ProcessResult.CreateFailure("kill", $"No running process has pid {pid}.");
        }

        using (process)
        {
            var summary = TryDescribe(process);
            var displayName = summary?.Name ?? "process";

            if (IsProtected(pid, displayName))
            {
                return ProcessResult.CreateFailure(
                    "kill", $"Refusing to terminate protected process '{displayName}' (pid {pid}).");
            }

            try
            {
                process.Kill(entireProcessTree: force);
            }
            catch (Exception ex)
            {
                return ProcessResult.CreateFailure(
                    "kill", $"Failed to terminate '{displayName}' (pid {pid}): {ex.Message}.");
            }

            return ProcessResult.CreateKillSuccess([summary ?? new ProcessSummary(pid, displayName, 0)]);
        }
    }

    private ProcessResult KillByName(string name, bool force)
    {
        var normalized = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        var matches = SysProcess.GetProcessesByName(normalized);
        if (matches.Length == 0)
        {
            return ProcessResult.CreateFailure("kill", $"No running process is named '{name}'.");
        }

        var killed = new List<ProcessSummary>();
        var errors = new List<string>();

        foreach (var process in matches)
        {
            using (process)
            {
                var summary = TryDescribe(process);
                var pid = summary?.Pid ?? SafePid(process);
                var displayName = summary?.Name ?? normalized;

                if (IsProtected(pid, displayName))
                {
                    errors.Add($"'{displayName}' (pid {pid}) is protected");
                    continue;
                }

                try
                {
                    process.Kill(entireProcessTree: force);
                    killed.Add(summary ?? new ProcessSummary(pid, displayName, 0));
                }
                catch (Exception ex)
                {
                    errors.Add($"pid {pid}: {ex.Message}");
                }
            }
        }

        if (killed.Count == 0)
        {
            return ProcessResult.CreateFailure(
                "kill", $"Could not terminate any process named '{name}': {string.Join("; ", errors)}.");
        }

        return ProcessResult.CreateKillSuccess(killed);
    }

    private static bool IsProtected(int pid, string name)
    {
        if (pid <= 4 || pid == Environment.ProcessId)
        {
            return true;
        }

        var trimmed = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        return ProtectedNames.Contains(trimmed);
    }

    private static ProcessSummary? TryDescribe(SysProcess process)
    {
        try
        {
            var memoryMb = Math.Round(process.WorkingSet64 / (1024d * 1024d), 1, MidpointRounding.AwayFromZero);
            return new ProcessSummary(process.Id, process.ProcessName, memoryMb);
        }
        catch (Exception)
        {
            // Some system/protected processes deny access to their metrics; skip them.
            return null;
        }
    }

    private static int SafePid(SysProcess process)
    {
        try
        {
            return process.Id;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
