using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>MCP tool for selecting an option in an observed control.</summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UISelectTool
{
    /// <summary>Select an option in a combo box, list, or tab control discovered by ui_find or ui_snapshot. Expands the control when needed. Keywords: select, choose, option, dropdown, combo box, list, tab.</summary>
    /// <param name="windowHandle">Explicit target window handle.</param>
    /// <param name="value">Visible text of the option to select.</param>
    /// <param name="elementId">Required opaque ID of the containing selection control, not the hidden option.</param>
    /// <param name="withSnapshot">Attach a post-action snapshot.</param>
    /// <param name="snapshotMode">full for one verification, auto for repeated checks, reset for a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="snapshotSince">Previous snapshot token for a checked post-action diff.</param>
    [McpServerTool(Name = "ui_select", Title = "Select Value in Control", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string value,
        string elementId,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken,
        [DefaultValue(null)] string? snapshotSince = null)
    {
        if (string.IsNullOrWhiteSpace(windowHandle) || string.IsNullOrWhiteSpace(elementId))
        {
            return WindowsToolsBase.FailResult("windowHandle and elementId are required. Discover the selection control with ui_find or ui_snapshot.");
        }
        if (string.IsNullOrEmpty(value))
        {
            return WindowsToolsBase.FailResult("value is required (the option text to select).");
        }
        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult($"snapshotMode must be full, auto, or reset (got '{snapshotMode}').");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.SelectElementAsync(elementId, value, windowHandle, cancellationToken);
            result = await WindowsToolsBase.WithPostActionSnapshotAsync(
                result, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken, snapshotSince);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.ErrorCallToolResult("select", ex);
        }
    }
}
