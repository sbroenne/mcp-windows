using System.Reflection;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>Static help, version, and command-reference text for the CLI.</summary>
internal static class HelpText
{
    public const string ElementIdLifetime = """
        CLI ELEMENT-ID LIFETIME
        IDs identify observations in the persistent CLI daemon, not OS handles.
        Reuse IDs from ui find/ui snapshot in later CLI invocations while that owner remains alive.
        Restart, eviction, or replacement makes an ID stale: rediscover, never repair it by name.
        MCP owners and the CLI daemon do not share IDs. Treat IDs as opaque strings.
        Targeted click/type/select/read-table and state waits require --element-id.
        Selectors belong only to discovery and appear/disappear waits.
        ui read --window <handle> without --element-id explicitly reads the whole window.
        Batch $prev requires an unambiguous preceding result. Saved macros must discover
        fresh targets on replay, then use elementId="$prev", never persisted IDs.
        """;

    public static string Version
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return $"wincli {v?.ToString(3) ?? "1.0.0"}";
        }
    }

    public const string Usage = """
        wincli - Windows automation CLI (UI automation, mouse, keyboard, windows, screenshots).

        The token-efficient sibling of the Windows MCP server. Every command emits the same JSON
        payload the MCP server returns. Exit codes: 0 success, 1 tool error, 2 usage error.

        USAGE
          wincli <group> [<action>] [--option value] [--flag]

        DISCOVERY
          wincli --help                 Show this help.
          wincli tools                  List every command with its key options.
          wincli tools --json           Machine-readable tool manifest (names + JSON input schemas).
          wincli guidance               Print the full automation guide (recommended read first).
          wincli --version              Print the version.

        COMMON WORKFLOW
          1. wincli window find --title Notepad          -> get a window handle
          2. wincli ui snapshot --window <handle>        -> see the element tree
          3. wincli ui click --window <handle> --element-id <observed-id> --with-snapshot

        CLI STATE
          CLI invocations share a persistent daemon and its observed IDs.
          A stale ID fails safely. Discover again after control replacement or daemon restart.
          Unknown options are rejected before invoking tools for every command group.

        GROUPS
          service      Manage the persistent CLI daemon (start, status, stop).
          app          Launch an application.
          window       Manage windows (find, list, activate, move, close, ...).
          ui           UI automation (snapshot, find, click, type, select, read, read-table, wait, batch).
          keyboard     Send keystrokes (type, press, sequence, ...).
          mouse        Mouse input (move, click, drag, scroll, ...).
          screenshot   Capture screens/windows/regions (annotated element discovery by default).
          clipboard    Read/write the Windows clipboard (get, set, clear).
          macro        Record & replay UI workflows (save, run, list, get, delete).
          file-save    Save the active document (handles the Save As dialog).
          file-open    Open an existing file (handles the Open dialog).
          process      List or kill running processes (task-manager style).

        Run 'wincli tools' for the full option reference.
        """;

    public const string Tools = """
        wincli command reference
        ========================

        service start|status|stop
            Start, inspect, or stop the user-owned CLI daemon. Normal commands auto-start it.
            Restart invalidates prior element IDs and snapshot tokens, not target applications.

        app --path <exe> [--args <a>|--arguments <a>] [--working-dir <d>] [--no-wait] [--timeout-ms <n>]
            Launch an application and return its window handle.
            For child arguments beginning with --, use --args="--new-window target"
            (or --arguments="--new-window target"). Separate --args "--new-window target"
            and missing values are usage errors; they never launch the application.

        window <action> [options]
            actions: list, find, activate, get_foreground, minimize, maximize, restore, close,
                     move, resize, set_bounds, wait_for, move_to_monitor, get_state,
                     wait_for_state, move_and_activate, ensure_visible
            options: --handle --title --process --filter --regex --include-all-desktops
                     --x --y --width --height --timeout-ms --target --monitor-index
                     --state --exclude-title --discard-changes

        ui snapshot --window <h> [--max-depth <n>] [--control-type <t>]
                    [--mode full|auto|reset] [--since <snapshot-token>]
            Use full for one complete inspection (default). For repeated checks of the same window
            use auto from the first check; full is not remembered. Use reset,
            then auto, to begin a new comparison within a persistent MCP connection.
            CLI auto without --since returns a full baseline. Pass the returned token with --since
            for a later diff; a missing/mismatched baseline safely returns a full view.
        ui find     --window <h> [--name|--name-contains|--name-pattern|--control-type|
                     --automation-id|--class-name ...] [--found-index <n>] [--include-children]
                     [--sort-by-prominence] [--in-region x,y,w,h]
                     [--visible-only] [--enabled-only] [--content-view-only]
                     [--scope window|active_dialog] [--require-unique]
                     [--timeout-ms <n>]
        ui click    --window <h> --element-id <id>
                     [--double-click] [--with-snapshot] [--snapshot-mode full|auto|reset]
        ui type     --window <h> --text <s> --element-id <id> [--clear-first]
                     [--input-mode auto|keyboard|value] [--with-snapshot]
                     [--snapshot-mode full|auto|reset]
        ui select   --window <h> --value <s> --element-id <control-id> [--with-snapshot] [--snapshot-mode full|auto|reset]
        ui read     --window <h> [--element-id <id>] [--include-children] [--language <c>] [--format raw|article]
        ui read-table --window <h> --element-id <grid-id> [--max-rows <n>] [--max-columns <n>]
        ui wait     [--window <h>] [--mode appear|disappear] [selectors] [--timeout-ms <n>]
        ui wait     --mode state --element-id <id> --desired-state <state> [--timeout-ms <n>]
        ui batch    --window <h> --steps '<json>' | --steps-file <path>
                     [--continue-on-error] [--with-snapshot] [--snapshot-mode full|auto|reset]
            For --with-snapshot, use full once, auto for repeated checks, or reset to begin a new comparison.
            Add --since <token> for checked post-action diffs on click/type/select/batch/macro.
            selectors: --name --name-contains --name-pattern --control-type --automation-id --class-name

        keyboard <action> --window <h> [options]
            actions: type, press, key_down, key_up, sequence, release_all,
                     get_keyboard_layout, wait_for_idle
            options: --text --key --modifiers --repeat --sequence --inter-key-delay-ms --clear-first

        mouse <action> [options]
            actions: move, click, double_click, right_click, middle_click, drag, polyline, scroll, get_position
            options: --x --y --end-x --end-y --points --direction --amount --modifiers --button
                     --target --monitor-index --window --expected-window-title --expected-process-name

        screenshot [action] [options]
            actions: capture (default), list_monitors
            options: --window --target --monitor-index --region-x --region-y --region-width
                     --region-height --annotate/--no-annotate --include-cursor --image-format
                     --quality --output-mode --output-path --include-image

        file-save --window <h> [--path <file>]
            Save the active document; drives the Save As dialog when needed.

        file-open --window <h> --path <file> [--trigger-mode shortcut|wait] [--timeout-ms <n>]
            Open an existing file. shortcut sends Ctrl+O; wait handles a native dialog opened by
            a prior semantic click in a browser or desktop app.

        clipboard <action> [--text <s>]
            actions: get (read clipboard text), set (write --text), clear
            Fast bulk text IO. Pair with keyboard copy/paste: focus app, keyboard c --modifiers ctrl,
            then clipboard get; or clipboard set --text '...' then keyboard v --modifiers ctrl.

        process <action> [options]
            actions: list ([--name <filter>] [--sort-by memory|name|pid] [--limit <n>]),
                     kill (--pid <n> | --name <exe> [--force])
            Task-manager style listing and termination. --force also kills the child process tree.
            Critical Windows processes and the automation server itself are protected.

        macro <action> [options]
            actions: save (--name --steps '<json>'|--steps-file <path>), run (--name --window <h>
                     [--continue-on-error] [--with-snapshot] [--snapshot-mode full|auto|reset]),
                     list, get (--name), delete (--name)
            Persist a ui_batch steps array under a name, then replay it later against any window.
            Replay uses the identical batch engine, so a macro run == the equivalent ui batch call.

        Diagnostics (not global): --include-diagnostics (alias --diagnostics) is supported by
            ui operations, macro run, file-open, and file-save for available diagnostic details.
            Their command aliases have the same support. Other command groups reject these flags.
        Machine-readable: run 'wincli tools --json' for the full tool manifest (names + JSON schemas).
        This is the MCP schema, not a CLI flag schema; use the kebab-case options above.

        """ + ElementIdLifetime;
}
