using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>MCP tool for clicking an observed UI element.</summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIClickTool
{
    /// <summary>Click or double-click an element discovered by ui_find or ui_snapshot. Rediscover stale IDs; selectors are not accepted. Keywords: click, button, press, toggle, activate, double-click.</summary>
    /// <param name="windowHandle">Explicit target window handle.</param>
    /// <param name="elementId">Required opaque ID returned by discovery in this owner.</param>
    /// <param name="withSnapshot">Attach a post-action snapshot.</param>
    /// <param name="snapshotMode">full for one verification, auto for repeated checks, reset for a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics.</param>
    /// <param name="doubleClick">Perform a double-click.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="snapshotSince">Previous snapshot token for a checked post-action diff.</param>
    [McpServerTool(Name = "ui_click", Title = "Click UI Element", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string elementId,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        [DefaultValue(false)] bool doubleClick,
        CancellationToken cancellationToken,
        [DefaultValue(null)] string? snapshotSince = null)
    {
        if (string.IsNullOrWhiteSpace(windowHandle) || string.IsNullOrWhiteSpace(elementId))
        {
            return WindowsToolsBase.FailResult("windowHandle and elementId are required. Discover the target with ui_find or ui_snapshot.");
        }

        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult($"snapshotMode must be full, auto, or reset (got '{snapshotMode}').");
        }

        try
        {
            var result = doubleClick
                ? await WindowsToolsBase.UIAutomationService.DoubleClickElementAsync(elementId, windowHandle, cancellationToken)
                : await WindowsToolsBase.UIAutomationService.ClickElementAsync(elementId, windowHandle, cancellationToken);
            result = await WindowsToolsBase.WithPostActionSnapshotAsync(
                result, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken, snapshotSince);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.ErrorCallToolResult("click", ex);
        }
    }
}
