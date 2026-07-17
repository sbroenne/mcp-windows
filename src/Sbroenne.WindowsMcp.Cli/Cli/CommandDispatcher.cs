using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>
/// Routes a parsed command line to the matching tool. Every handler delegates to the exact same
/// <c>ExecuteAsync</c> method the MCP server registers, so the CLI and the MCP server are guaranteed
/// to behave identically - the CLI is only a thin argument-to-tool adapter.
/// </summary>
internal static class CommandDispatcher
{
    public static async Task<int> DispatchAsync(ParsedArgs args, CancellationToken ct)
    {
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
            case "file-save":
            case "filesave":
            case "save":
                return await FileSaveAsync(args, ct);
            default:
                return Emit.Usage($"unknown command '{args.Group}'.");
        }
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

    private static async Task<int> UiAsync(ParsedArgs a, CancellationToken ct)
    {
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
                        diag,
                        ct);
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
                        a.GetInt("timeout-ms", "timeout") ?? 5000,
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "click":
                {
                    var result = await UIClickTool.ExecuteAsync(
                        window,
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetString("element-id"),
                        a.GetInt("found-index", "index") ?? 1,
                        a.GetFlag("with-snapshot", "snapshot"),
                        diag,
                        ct);
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
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetString("element-id"),
                        a.GetInt("found-index", "index") ?? 1,
                        a.GetFlag("clear-first", "clear"),
                        a.GetFlag("with-snapshot", "snapshot"),
                        diag,
                        ct);
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
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetInt("found-index", "index") ?? 1,
                        a.GetFlag("with-snapshot", "snapshot"),
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            case "read":
                {
                    var result = await UIReadTool.ExecuteAsync(
                        window,
                        a.GetString("name"),
                        a.GetString("name-contains"),
                        a.GetString("name-pattern"),
                        a.GetString("control-type"),
                        a.GetString("automation-id"),
                        a.GetString("class-name"),
                        a.GetString("element-id"),
                        a.GetInt("found-index", "index") ?? 1,
                        a.GetFlag("include-children", "children"),
                        a.GetString("language", "lang"),
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
                        diag,
                        ct);
                    return Emit.Result(result);
                }

            default:
                return Emit.Usage(
                    "ui requires an operation: snapshot, find, click, type, select, read, wait, or batch.");
        }
    }
}
