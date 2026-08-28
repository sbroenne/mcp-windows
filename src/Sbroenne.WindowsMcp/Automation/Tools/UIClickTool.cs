using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for clicking UI elements.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIClickTool
{
    /// <summary>
    /// Click a UI element. REQUIRED for all click operations - you must call this tool to click anything. Auto-activates window.
    /// Keywords: click, press button, tap, select, push, activate button, click button, click link,
    /// click menu, check box, toggle, invoke element, UI element, control.
    /// </summary>
    /// <remarks>
    /// Clicks a UI element. Automatically activates the target window before clicking.
    /// You MUST use this tool for every click operation - each click requires a separate tool call.
    /// Works for Electron/Chromium elements: links, buttons, tabs, menu items exposed through UIA names or ARIA labels.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="name">Element name (exact match, case-insensitive). For Electron apps and Chromium browsers, this is often the visible label or ARIA label.</param>
    /// <param name="nameContains">Substring in element name (case-insensitive). Preferred for dialog buttons like 'Don\\'t save'.</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Button, MenuItem, Hyperlink, ListItem, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name (e.g., 'Chrome_WidgetWin_1' for Chromium).</param>
    /// <param name="elementId">Stable element id from a prior ui_find/ui_snapshot. When provided, clicks that exact element directly and ignores the name/type selectors (avoids re-querying).</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="withSnapshot">When true, attach a post-action snapshot so you can verify the new state without another tool call. Default: false.</param>
    /// <param name="snapshotMode">Post-action snapshot mode when withSnapshot=true: full for one verification (default), auto for repeated checks of the same window, or reset when this action starts a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="doubleClick">Double-click the element instead of single-clicking. Use for list/grid items that open on double-click - no coordinates needed. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload describing the click operation's success status and element information. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_click", Title = "Click UI Element", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(null)] string? elementId,
        [DefaultValue(1)] int foundIndex,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        [DefaultValue(false)] bool doubleClick,
        CancellationToken cancellationToken)
    {
        const string actionName = "click";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        var foundIndexError = WindowsToolsBase.ValidateFoundIndex(foundIndex);
        if (foundIndexError is not null)
        {
            return foundIndexError;
        }

        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult(
                $"snapshotMode must be one of: full, auto, reset (got '{snapshotMode}').");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                var byIdResult = doubleClick
                    ? await WindowsToolsBase.UIAutomationService.DoubleClickElementAsync(elementId, windowHandle, cancellationToken)
                    : await WindowsToolsBase.UIAutomationService.ClickElementAsync(elementId, windowHandle, cancellationToken);
                byIdResult = await WindowsToolsBase.WithPostActionSnapshotAsync(
                    byIdResult, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken);
                return WindowsToolsBase.ToCallToolResult(byIdResult, includeDiagnostics);
            }

            var query = new ElementQuery
            {
                WindowHandle = windowHandle,
                Name = name,
                NameContains = nameContains,
                NamePattern = namePattern,
                ControlType = controlType,
                AutomationId = automationId,
                ClassName = className,
                FoundIndex = Math.Max(1, foundIndex)
            };

            var result = doubleClick
                ? await WindowsToolsBase.UIAutomationService.FindAndDoubleClickAsync(query, cancellationToken)
                : await WindowsToolsBase.UIAutomationService.FindAndClickAsync(query, cancellationToken);
            result = await WindowsToolsBase.WithPostActionSnapshotAsync(
                result, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }

    /// <summary>Calls the snapshot-aware overload with a complete post-action snapshot.</summary>
    public static Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string? name,
        string? nameContains,
        string? namePattern,
        string? controlType,
        string? automationId,
        string? className,
        string? elementId,
        int foundIndex,
        bool withSnapshot,
        bool includeDiagnostics,
        bool doubleClick,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            windowHandle, name, nameContains, namePattern, controlType, automationId, className,
            elementId, foundIndex, withSnapshot, "full", includeDiagnostics, doubleClick,
            cancellationToken);
}
