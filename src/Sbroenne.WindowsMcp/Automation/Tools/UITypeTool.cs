using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>MCP tool for typing into an observed UI element.</summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UITypeTool
{
    /// <summary>Type text into an input discovered by ui_find or ui_snapshot. Use file_save for Save As dialogs. Keywords: type, input, text, fill, edit, field.</summary>
    /// <param name="windowHandle">Explicit target window handle.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="elementId">Required opaque ID of the input from discovery in this owner.</param>
    /// <param name="clearFirst">Clear existing text first.</param>
    /// <param name="withSnapshot">Attach a post-action snapshot.</param>
    /// <param name="snapshotMode">full for one verification, auto for repeated checks, reset for a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics.</param>
    /// <param name="inputMode">auto, keyboard, or value. Auto uses keyboard input for Chromium/Electron.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="snapshotSince">Previous snapshot token for a checked post-action diff.</param>
    [McpServerTool(Name = "ui_type", Title = "Type Text into Element", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string text,
        string elementId,
        [DefaultValue(false)] bool clearFirst,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        [DefaultValue("auto")] string inputMode,
        CancellationToken cancellationToken,
        [DefaultValue(null)] string? snapshotSince = null)
    {
        if (string.IsNullOrWhiteSpace(windowHandle) || string.IsNullOrWhiteSpace(elementId))
        {
            return WindowsToolsBase.FailResult("windowHandle and elementId are required. Discover the input with ui_find or ui_snapshot.");
        }
        if (string.IsNullOrEmpty(text))
        {
            return WindowsToolsBase.FailResult("text is required.");
        }
        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult($"snapshotMode must be full, auto, or reset (got '{snapshotMode}').");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.TypeIntoElementAsync(
                elementId, text, clearFirst, windowHandle, inputMode, cancellationToken);
            result = await WindowsToolsBase.WithPostActionSnapshotAsync(
                result, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken, snapshotSince);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.ErrorCallToolResult("type", ex);
        }
    }
}
