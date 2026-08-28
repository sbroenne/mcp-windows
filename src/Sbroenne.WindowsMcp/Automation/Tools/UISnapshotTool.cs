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
    /// MODE RULE (REQUIRED): when a task asks you to inspect or compare the same window before and
    /// after a change, you MUST explicitly pass mode='auto' on BOTH snapshot calls (or reset first,
    /// then auto). Never omit mode or choose full for a repeated check: a full call is not remembered.
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
    /// hierarchical and depth-bounded form.
    /// Use full for a one-time inspection. If the task will check the same window or subtree more than
    /// once, use auto on the first check and every later check; do not begin with full because full is
    /// not remembered. This running server returns only useful changes when that is smaller. Automatic complete
    /// responses omit proven layout-only Chromium/Electron wrappers while retaining named and actionable
    /// controls. Use reset when starting a new comparison: it forgets and replaces the remembered view.
    /// No saved-view id is required.
    /// Separate wincli invocations start fresh and therefore safely return a complete simplified view.
    /// To drill into a large window, pass parentElementId (from a prior snapshot/find) to scope the scan,
    /// or controlTypeFilter to retain matching controls and the ancestors needed to reach them.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). If omitted, the foreground window is used.</param>
    /// <param name="parentElementId">Revisit a known subtree using an element id from an earlier snapshot or find. Use only after discovering that id; omit it to inspect the whole window.</param>
    /// <param name="maxDepth">Maximum tree depth to traverse. Default (5) uses a framework-aware recommendation; explicit values are capped at 20.</param>
    /// <param name="controlTypeFilter">Comma-separated control types to keep (e.g. 'Button,Edit,MenuItem'). Others are pruned. Omit to keep all.</param>
    /// <param name="mode">REQUIRED for before/after or other repeated checks: explicitly use auto on BOTH the first and later snapshots of the same window or subtree. Never omit mode or use full for repeated checks. Use reset first only when replacing an older comparison, then auto. Use full only for a one-time complete inspection (default); full is not remembered.</param>
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
