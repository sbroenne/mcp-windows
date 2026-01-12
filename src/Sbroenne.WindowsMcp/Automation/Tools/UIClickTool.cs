using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
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
    /// </summary>
    /// <remarks>
    /// Clicks a UI element. Automatically activates the target window before clicking.
    /// You MUST use this tool for every click operation - each click requires a separate tool call.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="name">Element name (exact match, case-insensitive). For Electron apps, the ARIA label.</param>
    /// <param name="nameContains">Substring in element name (case-insensitive). Preferred for dialog buttons like 'Don\\'t save'.</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Button, MenuItem, Hyperlink, ListItem, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name (e.g., 'Chrome_WidgetWin_1').</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the click operation including success status and element information.</returns>
    [McpServerTool(Name = "ui_click", Title = "Click UI Element", Destructive = true, OpenWorld = false)]
    public static async partial Task<string> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(1)] int foundIndex,
        CancellationToken cancellationToken)
    {
        const string actionName = "click";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.Fail(
                "windowHandle is required. Get it from window_management(action='find').");
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

            var result = await WindowsToolsBase.UIAutomationService.FindAndClickAsync(query, cancellationToken);
            return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.SerializeToolError(actionName, ex);
        }
    }
}