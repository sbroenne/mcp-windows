using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for capturing a structured element tree ("snapshot") of a window.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UISnapshotTool
{
    /// <summary>
    /// Orient primitive: capture a compact element tree ("snapshot") of a window without guessing
    /// selectors first. Returns a hierarchy of elements (id, name, type, click coordinates, enabled)
    /// so you can see what's on screen, then act with ui_click/ui_type/ui_select using an element's
    /// name/automationId (or its returned id).
    /// </summary>
    /// <remarks>
    /// This is usually the FIRST call when automating an unfamiliar window - prefer it over blind
    /// ui_find guesses or screenshots. It is token-optimized: elements are returned in a compact,
    /// hierarchical form, depth-bounded, and (for Chromium/Electron) filtered to the leaner content view.
    /// To drill into a large window, pass parentElementId (from a prior snapshot/find) to scope the scan,
    /// or controlTypeFilter to keep only certain control types.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). If omitted, the foreground window is used.</param>
    /// <param name="parentElementId">Scope the snapshot to the subtree under this element id (from a prior snapshot or ui_find). Reduces size and tokens.</param>
    /// <param name="maxDepth">Maximum tree depth to traverse. Default (5) uses a framework-aware recommendation; explicit values are capped at 20.</param>
    /// <param name="controlTypeFilter">Comma-separated control types to keep (e.g. 'Button,Edit,MenuItem'). Others are pruned. Omit to keep all.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, elements scanned, detected framework) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload of the element tree. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_snapshot", Title = "Snapshot UI Tree", Destructive = false, ReadOnly = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        [DefaultValue(null)] string? windowHandle,
        [DefaultValue(null)] string? parentElementId,
        [DefaultValue(5)] int maxDepth,
        [DefaultValue(null)] string? controlTypeFilter,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "snapshot";

        if (string.IsNullOrWhiteSpace(windowHandle) && string.IsNullOrWhiteSpace(parentElementId))
        {
            // Both optional, but nudge callers toward an explicit target for determinism.
            // A null windowHandle falls back to the foreground window inside the service.
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.GetTreeAsync(
                windowHandle,
                parentElementId,
                maxDepth,
                controlTypeFilter,
                cancellationToken);

            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
