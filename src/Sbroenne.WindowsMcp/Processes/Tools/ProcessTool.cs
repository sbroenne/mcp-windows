using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Processes.Tools;

/// <summary>
/// MCP tool for listing and terminating running processes.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class ProcessTool
{
    /// <summary>
    /// 🧾 LIST OR KILL RUNNING PROCESSES - a Task-Manager-style view for agents.
    /// Keywords: task manager, running tasks, processes, kill, terminate, stop, end task, PID,
    /// memory usage, hung app, not responding, close background process.
    /// </summary>
    /// <remarks>
    /// WORKFLOW: Use action='list' (optionally --name to filter, --sort-by, --limit) to see what is
    /// running and grab a PID. Use action='kill' with either pid or name to terminate it; set
    /// force=true to also kill child processes (the whole process tree). Critical Windows processes
    /// and this automation server itself are protected and cannot be killed.
    /// </remarks>
    /// <param name="action">The action: list (enumerate processes) or kill (terminate by pid or name).</param>
    /// <param name="name">For 'list', a case-insensitive substring filter on the process name. For 'kill', the exact process name to terminate (all matching instances). The '.exe' suffix is optional.</param>
    /// <param name="pid">For 'kill', the process id to terminate. Takes precedence over 'name' when both are supplied.</param>
    /// <param name="sortBy">For 'list', the ordering: memory (largest working set first, default), name, or pid.</param>
    /// <param name="limit">For 'list', the maximum number of processes to return (1-500). Defaults to 20.</param>
    /// <param name="force">For 'kill', when true also terminates the entire child process tree. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result carrying the JSON payload. For 'list' the payload has 'processes' and 'count'; for 'kill' it has 'killed' and 'count'. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "process", Title = "🧾 List/Kill Processes", Destructive = true, OpenWorld = false)]
    public static partial Task<CallToolResult> ExecuteAsync(
        ProcessAction action,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] int? pid,
        [DefaultValue(ProcessSortBy.Memory)] ProcessSortBy sortBy,
        [DefaultValue(null)] int? limit,
        [DefaultValue(false)] bool force,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = action switch
            {
                ProcessAction.List => WindowsToolsBase.ProcessService.List(name, sortBy, limit),
                ProcessAction.Kill => WindowsToolsBase.ProcessService.Kill(pid, name, force),
                _ => ProcessResult.CreateFailure(action.ToString(), $"Unsupported process action: {action}.")
            };

            return Task.FromResult(ToCallToolResult(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(WindowsToolsBase.ErrorCallToolResult("process", ex));
        }
    }

    private static CallToolResult ToCallToolResult(ProcessResult result) =>
        new()
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions) }],
            IsError = !result.Success
        };
}
