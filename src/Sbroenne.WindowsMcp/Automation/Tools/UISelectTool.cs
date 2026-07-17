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
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
