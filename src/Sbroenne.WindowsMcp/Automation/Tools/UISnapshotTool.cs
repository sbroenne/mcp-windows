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
    /// Keywords: snapshot, element tree, structure, overview, inspect window, list elements,
    /// what's on screen, accessibility tree, orient, discover UI, dump window.
    /// </summary>
    /// <remarks>
    /// This is usually the FIRST call when automating an unfamiliar window - prefer it over blind
    /// ui_find guesses or screenshots. It is token-optimized: elements are returned in a compact,
    /// hierarchical form, depth-bounded, and (for Chromium/Electron) filtered to the leaner content view.
    /// Existing calls return a complete tree. Set mode='auto' to let this running server remember the
    /// previous view and return only useful changes when that is smaller. The first auto call returns a
    /// complete tree. Set mode='reset' to forget and replace the remembered view. No saved-view id is
    /// required. Separate wincli invocations start fresh and therefore safely return a complete tree.
    /// To drill into a large window, pass parentElementId (from a prior snapshot/find) to scope the scan,
    /// or controlTypeFilter to retain matching controls and the ancestors needed to reach them.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). If omitted, the foreground window is used.</param>
    /// <param name="parentElementId">Scope the snapshot to the subtree under this element id (from a prior snapshot or ui_find). Reduces size and tokens.</param>
    /// <param name="maxDepth">Maximum tree depth to traverse. Default (5) uses a framework-aware recommendation; explicit values are capped at 20.</param>
    /// <param name="controlTypeFilter">Comma-separated control types to keep (e.g. 'Button,Edit,MenuItem'). Others are pruned. Omit to keep all.</param>
    /// <param name="mode">Snapshot mode: full (default complete tree), auto (changes after the first view), or reset (forget and return a new complete view).</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, elements scanned, detected framework) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload of the element tree. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_snapshot", Title = "Snapshot UI Tree", Destructive = false, ReadOnly = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        [DefaultValue(null)] string? windowHandle,
        [DefaultValue(null)] string? parentElementId,
        [DefaultValue(5)] int maxDepth,
        [DefaultValue(null)] string? controlTypeFilter,
        [DefaultValue("full")] string mode,
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
            if (!SnapshotStateService.TryParseMode(mode, out var parsedMode))
            {
                return WindowsToolsBase.FailResult(
                    $"mode must be one of: full, auto, reset (got '{mode}').");
            }

            var result = await WindowsToolsBase.CaptureSnapshotAsync(
                windowHandle,
                parentElementId,
                maxDepth,
                controlTypeFilter,
                parsedMode,
                cancellationToken);

            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }

    /// <summary>Calls <see cref="ExecuteAsync(string?, string?, int, string?, string, bool, CancellationToken)"/> in full mode.</summary>
    public static Task<CallToolResult> ExecuteAsync(
        string? windowHandle,
        string? parentElementId,
        int maxDepth,
        string? controlTypeFilter,
        bool includeDiagnostics,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            windowHandle,
            parentElementId,
            maxDepth,
            controlTypeFilter,
            "full",
            includeDiagnostics,
            cancellationToken);
}
