namespace Sbroenne.WindowsMcp.Cli;

/// <summary>
/// Records, in one place, which <c>wincli</c> command invokes each MCP tool. This is the CLI side of
/// the "single definition" contract: the MCP tool surface is the source of truth (see
/// <c>Sbroenne.WindowsMcp.Catalog.ToolCatalog</c>), and a unit test asserts this map stays in exact
/// sync with it - so adding a new MCP tool without a matching CLI command fails the build.
/// </summary>
internal static class CliCommandCatalog
{
    /// <summary>Maps every MCP tool name to the <c>wincli</c> command line that drives it.</summary>
    public static readonly IReadOnlyDictionary<string, string> ToolToCommand =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["app"] = "app",
            ["window_management"] = "window",
            ["keyboard_control"] = "keyboard",
            ["mouse_control"] = "mouse",
            ["screenshot_control"] = "screenshot",
            ["clipboard"] = "clipboard",
            ["ui_macro"] = "macro",
            ["file_save"] = "file-save",
            ["file_open"] = "file-open",
            ["ui_snapshot"] = "ui snapshot",
            ["ui_find"] = "ui find",
            ["ui_click"] = "ui click",
            ["ui_type"] = "ui type",
            ["ui_select"] = "ui select",
            ["ui_read"] = "ui read",
            ["ui_read_table"] = "ui read-table",
            ["ui_wait"] = "ui wait",
            ["ui_batch"] = "ui batch",
        };
}
