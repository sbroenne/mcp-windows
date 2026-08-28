using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for selecting a value in a combo box, list, or similar selection control.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UISelectTool
{
    /// <summary>
    /// Select a value in a combo box, drop-down, list box, or tab control. Prefer this over
    /// click-then-click sequences for selection controls - it uses the proper UI Automation
    /// selection patterns (SelectionItem/ExpandCollapse) so it is reliable across frameworks.
    /// Keywords: select, choose, pick, dropdown, combo box, list box, tab, option, set value,
    /// choose option, selection control, expand and select.
    /// </summary>
    /// <remarks>
    /// Locate the selection control with the selectors (name/automationId/controlType such as ComboBox,
    /// List, Tab), then pass the visible option text as 'value'. The tool expands the control if needed,
    /// finds the matching item, and selects it. For free-text combo boxes where you need to type an
    /// arbitrary value, use ui_type instead.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). REQUIRED.</param>
    /// <param name="value">The visible text of the option to select (e.g. 'Germany', 'Landscape'). Required.</param>
    /// <param name="name">Element name of the selection control (exact match, case-insensitive).</param>
    /// <param name="nameContains">Substring in the control's name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for the control's name.</param>
    /// <param name="controlType">Control type (ComboBox, List, Tab, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="foundIndex">Return Nth matching control (1-based, default: 1).</param>
    /// <param name="withSnapshot">When true, attach a post-action snapshot so you can verify the new state without another tool call. Default: false.</param>
    /// <param name="snapshotMode">Post-action snapshot mode when withSnapshot=true: full for one verification (default), auto for repeated checks of the same window, or reset when this action starts a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload describing the select operation's success status and element information. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_select", Title = "Select Value in Control", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string value,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(1)] int foundIndex,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "select";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        if (string.IsNullOrEmpty(value))
        {
            return WindowsToolsBase.FailResult("value is required (the option text to select).");
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

            var result = await WindowsToolsBase.UIAutomationService.FindAndSelectAsync(query, value, cancellationToken);
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
        string value,
        string? name,
        string? nameContains,
        string? namePattern,
        string? controlType,
        string? automationId,
        string? className,
        int foundIndex,
        bool withSnapshot,
        bool includeDiagnostics,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            windowHandle, value, name, nameContains, namePattern, controlType, automationId,
            className, foundIndex, withSnapshot, "full", includeDiagnostics, cancellationToken);
}
