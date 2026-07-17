using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Macros.Tools;

/// <summary>
/// MCP tool for recording and replaying UI automation macros. A macro is a saved <c>ui_batch</c>
/// steps array; running one replays it through the identical batch engine.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIMacroTool
{
    /// <summary>
    /// 🔁 RECORD &amp; REPLAY a reusable UI workflow. Save a ui_batch steps array under a name, then
    /// replay it against any window later - so a multi-step task (open a form, fill fields, submit)
    /// becomes a single named call. Also list, inspect, and delete saved macros.
    /// </summary>
    /// <remarks>
    /// WORKFLOW: build and verify a sequence with ui_batch, then ui_macro(action='save', name='login',
    /// steps='[...]') to persist it. Later ui_macro(action='run', name='login', windowHandle='...')
    /// replays the exact steps via the batch engine (same semantics, same "$prev" chaining, same
    /// stopOnError behaviour). Use action='list' to see saved macros, action='get' to inspect one,
    /// action='delete' to remove one. Macros persist on disk across sessions.
    /// - save:   requires name + steps (a ui_batch JSON array).
    /// - run:    requires name + windowHandle; optional stopOnError (default true), withSnapshot.
    /// - get:    requires name; returns the saved steps.
    /// - list:   returns all saved macro names with step counts.
    /// - delete: requires name.
    /// </remarks>
    /// <param name="action">The macro action: save, run, get, list, or delete.</param>
    /// <param name="name">Macro name (letters, digits, '-', '_', '.'). Required for save, run, get, delete.</param>
    /// <param name="steps">ui_batch steps JSON array. Required for save.</param>
    /// <param name="windowHandle">Target window handle for replay. Required for run.</param>
    /// <param name="stopOnError">For run: stop at the first failing step (default: true).</param>
    /// <param name="withSnapshot">For run: attach the window's element tree after replay (default: false).</param>
    /// <param name="includeDiagnostics">Reserved for parity; responses are already compact. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result with the JSON payload. For run it is the ui_batch result; otherwise the macro management result. <c>IsError</c> reflects success.</returns>
    [McpServerTool(Name = "ui_macro", Title = "🔁 Record & Replay UI Macros", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        MacroAction action,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? steps,
        [DefaultValue(null)] string? windowHandle,
        [DefaultValue(true)] bool stopOnError,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = WindowsToolsBase.MacroService;

            switch (action)
            {
                case MacroAction.Save:
                    return ToCallToolResult(await service.SaveAsync(name ?? "", steps ?? "", cancellationToken));

                case MacroAction.List:
                    return ToCallToolResult(await service.ListAsync(cancellationToken));

                case MacroAction.Get:
                    return ToCallToolResult(await service.GetAsync(name ?? "", cancellationToken));

                case MacroAction.Delete:
                    return ToCallToolResult(await service.DeleteAsync(name ?? "", cancellationToken));

                case MacroAction.Run:
                    return await RunAsync(service, name, windowHandle, stopOnError, withSnapshot, includeDiagnostics, cancellationToken);

                default:
                    return ToCallToolResult(MacroResult.Failure(action.ToString(), $"Unsupported macro action: {action}."));
            }
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult("ui_macro", ex);
        }
    }

    private static async Task<CallToolResult> RunAsync(
        MacroService service,
        string? name,
        string? windowHandle,
        bool stopOnError,
        bool withSnapshot,
        bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ToCallToolResult(MacroResult.Failure("run", "name is required to run a macro."));
        }

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return ToCallToolResult(MacroResult.Failure("run",
                "windowHandle is required to run a macro. Get it from window_management(action='find')."));
        }

        var stepsJson = await service.LoadStepsJsonAsync(name, cancellationToken);
        if (stepsJson is null)
        {
            return ToCallToolResult(MacroResult.Failure("run",
                $"Macro '{name}' does not exist or is corrupt. Use ui_macro(action='list') to see saved macros."));
        }

        // Replay through the identical batch engine so a macro run == the equivalent ui_batch call.
        return await UIBatchTool.ExecuteAsync(
            windowHandle, stepsJson, stopOnError, withSnapshot, includeDiagnostics, cancellationToken);
    }

    private static CallToolResult ToCallToolResult(MacroResult result) =>
        new()
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions) }],
            IsError = result.IsError
        };
}
