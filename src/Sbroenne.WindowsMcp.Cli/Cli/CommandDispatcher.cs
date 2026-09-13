using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Clipboard.Tools;
using Sbroenne.WindowsMcp.Macros.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Processes.Tools;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>
/// Routes a parsed command line to the matching tool. Every handler delegates to the exact same
/// <c>ExecuteAsync</c> method the MCP server registers. Targeting validation rejects removed
/// selectors and unknown arguments rather than silently widening an operation.
/// </summary>
internal static class CommandDispatcher
{
    internal static Task<int> DispatchAsync(
        ParsedArgs args, TextWriter output, TextWriter error, CancellationToken ct) =>
        Emit.WithWritersAsync(output, error, () => DispatchAsync(args, ct));

    public static async Task<int> DispatchAsync(ParsedArgs args, CancellationToken ct)
    {
        var optionError = ValidateNonUiOptions(args);
        if (optionError is not null)
        {
            return Emit.Usage(optionError);
        }
        switch (args.Group)
        {
            case "app":
                return await AppAsync(args, ct);
            case "window":
            case "window-management":
                return await WindowAsync(args, ct);
            case "keyboard":
                return await KeyboardAsync(args, ct);
            case "mouse":
                return await MouseAsync(args, ct);
            case "screenshot":
                return await ScreenshotAsync(args, ct);
            case "ui":
                return await UiAsync(args, ct);
            case "clipboard":
            case "clip":
                return await ClipboardAsync(args, ct);
            case "macro":
            case "ui-macro":
                return await MacroAsync(args, ct);
            case "file-save":
            case "filesave":
            case "save":
                return await FileSaveAsync(args, ct);
            case "file-open":
            case "fileopen":
            case "open":
                return await FileOpenAsync(args, ct);
            case "process":
            case "proc":
                return await ProcessAsync(args, ct);
            default:
                return Emit.Usage($"unknown command '{args.Group}'.");
        }
    }

    internal static string? ValidateNonUiOptions(ParsedArgs args)
    {
        const string WindowOptions = "window handle";
        const string Diagnostics = "include-diagnostics diagnostics";
        var options = args.Group switch
        {
            "app" => "path program program-path args arguments working-dir cwd working-directory no-wait wait-for-window timeout-ms timeout",
            "window" or "window-management" => $"{WindowOptions} title process process-name filter regex include-all-desktops all-desktops x y width height timeout-ms timeout target monitor-index monitor state exclude-title discard-changes",
            "keyboard" => $"{WindowOptions} text key modifiers repeat sequence inter-key-delay-ms delay-ms delay clear-first clear",
            "mouse" => $"{WindowOptions} target x y end-x endx end-y endy direction amount modifiers button monitor-index monitor expected-window-title expected-title expected-process-name expected-process points",
            "screenshot" => $"{WindowOptions} action no-annotate annotate target monitor-index monitor region-x region-y region-width region-height include-cursor cursor image-format format quality output-mode output-path out include-image",
            "file-save" or "filesave" or "save" => $"{WindowOptions} path file-path file {Diagnostics}",
            "file-open" or "fileopen" or "open" => $"{WindowOptions} path file-path file {Diagnostics} trigger-mode trigger timeout-ms timeout",
            "process" or "proc" => "name pid sort-by sort limit force",
            "clipboard" or "clip" => "text",
            "macro" or "ui-macro" => $"{WindowOptions} steps steps-file name continue-on-error no-stop-on-error stop-on-error with-snapshot snapshot snapshot-mode {Diagnostics} since",
            _ => null,
        };
        if (options is null)
        {
            return null;
        }

        var allowed = options.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = args.OptionNames.FirstOrDefault(option => !allowed.Contains(option));
        return unknown is null ? null : $"Unknown --{unknown} option for {args.Group}. See wincli tools for supported options.";
    }

    private static string? Window(ParsedArgs a) => a.GetString("window", "handle");

    private static async Task<int> AppAsync(ParsedArgs a, CancellationToken ct)
    {
        var path = a.GetString("path", "program", "program-path");
        if (string.IsNullOrWhiteSpace(path))
        {
            return Emit.Usage("app requires --path <executable>.");
        }

        var waitForWindow = a.Has("no-wait") ? false : a.GetBool("wait-for-window", true);
        var result = await AppTool.ExecuteAsync(
            path,
            a.GetString("args", "arguments"),
            a.GetString("working-dir", "cwd", "working-directory"),
            waitForWindow,
            a.GetInt("timeout-ms", "timeout"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> WindowAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<WindowAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"window requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<WindowAction>())}.");
        }

        var result = await WindowManagementTool.ExecuteAsync(
            action,
            a.GetString("handle", "window"),
            a.GetString("title"),
            a.GetString("process", "process-name"),
            a.GetString("filter"),
            a.GetBool("regex"),
            a.GetFlag("include-all-desktops", "all-desktops"),
            a.GetInt("x"),
            a.GetInt("y"),
            a.GetInt("width"),
            a.GetInt("height"),
            a.GetInt("timeout-ms", "timeout"),
            a.GetString("target"),
            a.GetInt("monitor-index", "monitor"),
            a.GetString("state"),
            a.GetString("exclude-title"),
            a.GetBool("discard-changes"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> KeyboardAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<KeyboardAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"keyboard requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<KeyboardAction>())}.");
        }

        var result = await KeyboardControlTool.ExecuteAsync(
            Window(a) ?? string.Empty,
            action,
            a.GetString("text"),
            a.GetString("key"),
            a.GetString("modifiers"),
            a.GetInt("repeat") ?? 1,
            a.GetString("sequence"),
            a.GetInt("inter-key-delay-ms", "delay-ms", "delay"),
            a.GetFlag("clear-first", "clear"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> MouseAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<MouseAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"mouse requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<MouseAction>())}.");
        }

        var result = await MouseControlTool.ExecuteAsync(
            action,
            a.GetString("target"),
            a.GetInt("x"),
            a.GetInt("y"),
            a.GetInt("end-x", "endx"),
            a.GetInt("end-y", "endy"),
            a.GetString("direction"),
            a.GetInt("amount") ?? 1,
            a.GetString("modifiers"),
            a.GetString("button"),
            a.GetInt("monitor-index", "monitor"),
            a.GetString("expected-window-title", "expected-title"),
            a.GetString("expected-process-name", "expected-process"),
            Window(a),
            a.GetString("points"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> ScreenshotAsync(ParsedArgs a, CancellationToken ct)
    {
        // Action may come as a positional token (screenshot capture) or --action; default null -> capture.
        var action = a.Action ?? a.GetString("action");
        action = action?.Replace('-', '_');

        var annotate = a.Has("no-annotate") ? false : a.GetBool("annotate", true);

        var result = await ScreenshotControlTool.ExecuteAsync(
            action,
            annotate,
            a.GetString("target"),
            a.GetInt("monitor-index", "monitor"),
            Window(a),
            a.GetInt("region-x"),
            a.GetInt("region-y"),
            a.GetInt("region-width"),
            a.GetInt("region-height"),
            a.GetFlag("include-cursor", "cursor"),
            a.GetString("image-format", "format"),
            a.GetInt("quality"),
            a.GetString("output-mode"),
            a.GetString("output-path", "out"),
            a.GetBool("include-image"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> FileSaveAsync(ParsedArgs a, CancellationToken ct)
    {
        var result = await UIFileTool.ExecuteAsync(
            Window(a) ?? string.Empty,
            a.GetString("path", "file-path", "file"),
            a.GetFlag("include-diagnostics", "diagnostics"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> FileOpenAsync(ParsedArgs a, CancellationToken ct)
    {
        var result = await UIOpenFileTool.ExecuteAsync(
            Window(a) ?? string.Empty,
            a.GetString("path", "file-path", "file") ?? string.Empty,
            a.GetFlag("include-diagnostics", "diagnostics"),
            a.GetString("trigger-mode", "trigger") ?? "shortcut",
            a.GetInt("timeout-ms", "timeout") ?? 5000,
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> ProcessAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<ProcessAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"process requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<ProcessAction>())}.");
        }

        var sortBy = ProcessSortBy.Memory;
        var sortRaw = a.GetString("sort-by", "sort");
        if (sortRaw is not null && !EnumHelper.TryParse<ProcessSortBy>(sortRaw, out sortBy))
        {
            return Emit.Usage(
                $"process --sort-by must be one of: {string.Join(", ", EnumHelper.Tokens<ProcessSortBy>())}.");
        }

        var result = await ProcessTool.ExecuteAsync(
            action,
            a.GetString("name"),
            a.GetInt("pid"),
            sortBy,
            a.GetInt("limit"),
            a.GetFlag("force"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> ClipboardAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<ClipboardAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"clipboard requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<ClipboardAction>())}.");
        }

        var result = await ClipboardTool.ExecuteAsync(
            action,
            a.GetString("text"),
            ct);
        return Emit.Result(result);
    }

    private static async Task<int> MacroAsync(ParsedArgs a, CancellationToken ct)
    {
        if (!EnumHelper.TryParse<MacroAction>(a.Action, out var action))
        {
            return Emit.Usage(
                $"macro requires a valid action. One of: {string.Join(", ", EnumHelper.Tokens<MacroAction>())}.");
        }

        var steps = a.GetString("steps");
        var stepsFile = a.GetString("steps-file");
        if (steps is null && stepsFile is not null && File.Exists(stepsFile))
        {
            steps = await File.ReadAllTextAsync(stepsFile, ct);
        }

        var stopOnError = a.Has("continue-on-error") || a.Has("no-stop-on-error")
            ? false
            : a.GetBool("stop-on-error", true);

        var result = await UIMacroTool.ExecuteAsync(
            action,
            a.GetString("name"),
            steps,
            Window(a),
            stopOnError,
            a.GetFlag("with-snapshot", "snapshot"),
            a.GetString("snapshot-mode") ?? "full",
            a.GetFlag("include-diagnostics", "diagnostics"),
            ct,
            a.GetString("since"));
        return Emit.Result(result);
    }

    private static async Task<int> UiAsync(ParsedArgs a, CancellationToken ct)
    {
        var targetingError = ValidateUiTargeting(a);
        if (targetingError is not null)
        {
            return Emit.Usage(targetingError);
        }

        var window = Window(a) ?? string.Empty;
        var diag = a.GetFlag("include-diagnostics", "diagnostics");

        switch (a.Action)
        {
            case "snapshot":
                {
                    var result = await UISnapshotTool.ExecuteAsync(
                        Window(a),
                        a.GetString("parent-element-id", "parent"),
                        a.GetInt("max-depth", "depth") ?? 5,
                        a.GetString("control-type-filter", "control-type"),
                        a.GetString("mode") ?? "full",
                        diag,
                        ct,
                        a.GetString("since"));
                    return Emit.Result(result);
                }

            case "find":
                {
                    var result = await UIFindTool.ExecuteAsync(
                        window,
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetInt("exact-depth"),
                        a.GetInt("found-index", "index") ?? 1,
                        a.GetFlag("include-children", "children"),
                        a.GetFlag("sort-by-prominence", "prominence"),
                        a.GetString("in-region", "region"),
                        a.GetString("near-element", "near"),
                        a.GetNullableBool("visible-only"),
                        a.GetNullableBool("content-view-only"),
                        a.GetString("parent-element-id", "parent"),
                        a.GetString("scope") ?? "window",
                        a.GetFlag("require-unique", "unique"),
                        a.GetNullableBool("enabled-only"),
                        a.GetInt("timeout-ms", "timeout") ?? 5000,
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "click":
                {
                    var result = await UIClickTool.ExecuteAsync(
                        window,
                        a.GetString("element-id")!,
                        a.GetFlag("with-snapshot", "snapshot"),
                        a.GetString("snapshot-mode") ?? "full",
                        diag,
                        a.GetFlag("double-click", "dblclick"),
                        ct,
                        a.GetString("since"));
                    return Emit.Result(result);
                }

            case "type":
                {
                    var text = a.GetString("text");
                    if (text is null)
                    {
                        return Emit.Usage("ui type requires --text <value>.");
                    }

                    var result = await UITypeTool.ExecuteAsync(
                        window,
                        text,
                        a.GetString("element-id")!,
                        a.GetFlag("clear-first", "clear"),
                        a.GetFlag("with-snapshot", "snapshot"),
                        a.GetString("snapshot-mode") ?? "full",
                        diag,
                        a.GetString("input-mode") ?? "auto",
                        ct,
                        a.GetString("since"));
                    return Emit.Result(result);
                }

            case "select":
                {
                    var value = a.GetString("value");
                    if (value is null)
                    {
                        return Emit.Usage("ui select requires --value <optionText>.");
                    }

                    var result = await UISelectTool.ExecuteAsync(
                        window,
                        value,
                        a.GetString("element-id")!,
                        a.GetFlag("with-snapshot", "snapshot"),
                        a.GetString("snapshot-mode") ?? "full",
                        diag,
                        ct,
                        a.GetString("since"));
                    return Emit.Result(result);
                }

            case "read":
                {
                    var result = await UIReadTool.ExecuteAsync(
                        window,
                        a.GetString("element-id"),
                        a.GetFlag("include-children", "children"),
                        a.GetString("language", "lang"),
                        a.GetString("format"),
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "read-table":
                {
                    var result = await UIReadTableTool.ExecuteAsync(
                        window,
                        a.GetString("element-id")!,
                        a.GetInt("max-rows", "rows") ?? 200,
                        a.GetInt("max-columns", "cols") ?? 50,
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "wait":
                {
                    var result = await UIWaitTool.ExecuteAsync(
                        Window(a),
                        a.GetString("mode") ?? "appear",
                        a.GetString("element-id"),
                        a.GetString("desired-state", "state"),
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetString("parent-element-id", "parent"),
                        a.GetString("scope") ?? "window",
                        a.GetFlag("require-unique", "unique"),
                        a.GetNullableBool("enabled-only"),
                        a.GetInt("timeout-ms", "timeout") ?? 5000,
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "batch":
                {
                    var steps = a.GetString("steps");
                    var stepsFile = a.GetString("steps-file");
                    if (steps is null && stepsFile is not null && File.Exists(stepsFile))
                    {
                        steps = await File.ReadAllTextAsync(stepsFile, ct);
                    }

                    if (string.IsNullOrWhiteSpace(steps))
                    {
                        return Emit.Usage("ui batch requires --steps '<json array>' or --steps-file <path>.");
                    }

                    var stopOnError = a.Has("continue-on-error") || a.Has("no-stop-on-error")
                        ? false
                        : a.GetBool("stop-on-error", true);

                    var result = await UIBatchTool.ExecuteAsync(
                        window,
                        steps,
                        stopOnError,
                        a.GetFlag("with-snapshot", "snapshot"),
                        a.GetString("snapshot-mode") ?? "full",
                        diag,
                        ct,
                        a.GetString("since"));
                    return Emit.Result(result);
                }

            default:
                return Emit.Usage(
                    "ui requires an operation: snapshot, find, click, type, select, read, read-table, wait, or batch.");
        }

    }

    private static string? ValidateUiTargeting(ParsedArgs a)
    {
        foreach (var option in new[] { "element-id", "id" })
        {
            if (a.Has(option) && string.IsNullOrWhiteSpace(a.GetString(option)))
            {
                return $"--{option} requires a non-empty element ID value. Omit the ID only for an explicit whole-window read.";
            }
        }
        const string Common = "window handle include-diagnostics diagnostics";
        const string Selectors = "name name-contains name-pattern control-type automation-id class-name parent-element-id parent scope require-unique unique enabled-only";
        const string Snapshots = "with-snapshot snapshot snapshot-mode since";
        var accepted = a.Action switch
        {
            "click" => $"element-id double-click dblclick {Snapshots}",
            "type" => $"element-id text clear-first clear input-mode {Snapshots}",
            "select" => $"element-id value {Snapshots}",
            "read" => "element-id include-children children language lang format",
            "read-table" => "element-id max-rows rows max-columns cols",
            "wait" => $"{Selectors} element-id desired-state state mode timeout-ms timeout",
            "find" => $"{Selectors} exact-depth found-index index include-children children sort-by-prominence prominence in-region region near-element near visible-only content-view-only timeout-ms timeout",
            "snapshot" => "parent-element-id parent max-depth depth control-type-filter control-type mode since",
            "batch" => $"steps steps-file continue-on-error no-stop-on-error stop-on-error {Snapshots}",
            _ => ""
        };
        var allowed = (Common + " " + accepted).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = a.OptionNames.FirstOrDefault(option => !allowed.Contains(option));
        if (unknown is not null)
        {
            return $"Unknown or removed --{unknown} option for ui {a.Action}. Selectors are only supported by discovery and appear/disappear waits.";
        }
        var stateWait = a.Action == "wait" &&
            string.Equals(a.GetString("mode")?.Trim(), "state", StringComparison.OrdinalIgnoreCase);
        var targeted = a.Action is "click" or "type" or "select" or "read-table" || stateWait;
        if (targeted || a.Action == "read")
        {
            foreach (var option in new[]
            {
                    "name", "name-contains", "name-pattern", "control-type", "automation-id", "class-name",
                    "found-index", "index", "parent-element-id", "parent", "scope", "require-unique", "unique",
                    "near-element", "near", "enabled-only", "visible-only",
                })
            {
                if (a.Has(option))
                {
                    return $"--{option} is a discovery selector, not an action target. Use ui find then --element-id.";
                }
            }
        }
        if (targeted && string.IsNullOrWhiteSpace(a.GetString("element-id")))
        {
            return "This action requires --element-id from ui find or ui snapshot.";
        }
        return null;
    }
}
