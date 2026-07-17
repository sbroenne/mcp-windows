using System.Reflection;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>Static help, version, and command-reference text for the CLI.</summary>
internal static class HelpText
{
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
          wincli guidance               Print the full automation guide (recommended read first).
          wincli --version              Print the version.

        COMMON WORKFLOW
          1. wincli window find --title Notepad          -> get a window handle
          2. wincli ui snapshot --window <handle>        -> see the element tree
          3. wincli ui click --window <handle> --name OK --with-snapshot

        GROUPS
          app          Launch an application.
          window       Manage windows (find, list, activate, move, close, ...).
          ui           UI automation (snapshot, find, click, type, select, read, read-table, wait, batch).
          keyboard     Send keystrokes (type, press, sequence, ...).
          mouse        Mouse input (move, click, drag, scroll, ...).
          screenshot   Capture screens/windows/regions (annotated element discovery by default).
          file-save    Save the active document (handles the Save As dialog).

        Run 'wincli tools' for the full option reference.
        """;

    public const string Tools = """
        wincli command reference
        ========================

        app --path <exe> [--args <a>] [--working-dir <d>] [--no-wait] [--timeout-ms <n>]
            Launch an application and return its window handle.

        window <action> [options]
            actions: list, find, activate, get_foreground, minimize, maximize, restore, close,
                     move, resize, set_bounds, wait_for, move_to_monitor, get_state,
                     wait_for_state, move_and_activate, ensure_visible
            options: --handle --title --process --filter --regex --include-all-desktops
                     --x --y --width --height --timeout-ms --target --monitor-index
                     --state --exclude-title --discard-changes

        ui snapshot --window <h> [--parent <id>] [--max-depth <n>] [--control-type <t>]
        ui find     --window <h> [--name|--name-contains|--name-pattern|--control-type|
                     --automation-id|--class-name ...] [--found-index <n>] [--include-children]
                     [--sort-by-prominence] [--in-region x,y,w,h] [--near-element <id>]
                     [--visible-only] [--content-view-only] [--timeout-ms <n>]
        ui click    --window <h> [selectors|--element-id <id>] [--found-index <n>] [--with-snapshot]
        ui type     --window <h> --text <s> [selectors|--element-id <id>] [--clear-first] [--with-snapshot]
        ui select   --window <h> --value <s> [selectors] [--with-snapshot]
        ui read     --window <h> [selectors|--element-id <id>] [--include-children] [--language <c>]
        ui read-table --window <h> [selectors|--element-id <id>] [--max-rows <n>] [--max-columns <n>]
        ui wait     [--window <h>] [--mode appear|disappear|...] [--element-id <id>]
                     [--desired-state <s>] [selectors] [--timeout-ms <n>]
        ui batch    --window <h> --steps '<json>' | --steps-file <path>
                     [--continue-on-error] [--with-snapshot]
            selectors: --name --name-contains --name-pattern --control-type --automation-id --class-name

        keyboard <action> --window <h> [options]
            actions: type, press, key_down, key_up, sequence, release_all,
                     get_keyboard_layout, wait_for_idle
            options: --text --key --modifiers --repeat --sequence --inter-key-delay-ms --clear-first

        mouse <action> [options]
            actions: move, click, double_click, right_click, middle_click, drag, scroll, get_position
            options: --x --y --end-x --end-y --direction --amount --modifiers --button
                     --target --monitor-index --window --expected-window-title --expected-process-name

        screenshot [action] [options]
            actions: capture (default), list_monitors
            options: --window --target --monitor-index --region-x --region-y --region-width
                     --region-height --annotate/--no-annotate --include-cursor --image-format
                     --quality --output-mode --output-path --include-image

        file-save --window <h> [--path <file>]
            Save the active document; drives the Save As dialog when needed.

        Global: add --include-diagnostics to any command for timing/diagnostic details.
        """;
}
