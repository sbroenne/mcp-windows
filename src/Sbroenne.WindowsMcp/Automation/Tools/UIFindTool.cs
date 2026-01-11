using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for finding UI elements in Windows applications.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIFindTool
{
    /// <summary>
    /// Find UI elements. REQUIRED before clicking elements you haven't located yet. Returns element IDs for use with ui_click.
    /// </summary>
    /// <remarks>
    /// Finds UI elements by name, type, ID, or other criteria. Returns element IDs for clicking, typing, etc.
    /// You MUST call this tool or ui_click for every UI operation - never skip tool calls.
    /// REQUIRED: windowHandle (from window_management tool).
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="name">Element name (exact match, case-insensitive). For Electron apps, this is the ARIA label.</param>
    /// <param name="nameContains">Substring to search in element names (case-insensitive). Preferred for dialog buttons - e.g., 'Don\\'t save'.</param>
    /// <param name="namePattern">Regex pattern to match element names. Use for complex matching like 'Button [0-9]+' or 'Save|Cancel'.</param>
    /// <param name="controlType">Control type filter (Button, Edit, Text, CheckBox, ComboBox, Menu, MenuItem, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching (exact match, most reliable).</param>
    /// <param name="className">Element class name (e.g., 'Chrome_WidgetWin_1' for Chromium, 'Button' for Win32).</param>
    /// <param name="exactDepth">Exact depth to search (1=immediate children). Skips other depths, improves performance.</param>
    /// <param name="foundIndex">Return the Nth match (1-based, default: 1). Use 2 for second match, etc.</param>
    /// <param name="includeChildren">Include child elements in response (default: false).</param>
    /// <param name="sortByProminence">Sort results by size (largest first). Useful for disambiguation.</param>
    /// <param name="inRegion">Filter to region: 'x,y,width,height' in screen coordinates.</param>
    /// <param name="nearElement">Find elements near this elementId (results sorted by distance).</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing list of found elements with their properties and element IDs.</returns>
    [McpServerTool(Name = "ui_find", Title = "Find UI Elements", Destructive = false, OpenWorld = false)]
    public static async Task<string> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? name = null,
        [DefaultValue(null)] string? nameContains = null,
        [DefaultValue(null)] string? namePattern = null,
        [DefaultValue(null)] string? controlType = null,
        [DefaultValue(null)] string? automationId = null,
        [DefaultValue(null)] string? className = null,
        [DefaultValue(null)] int? exactDepth = null,
        [DefaultValue(1)] int foundIndex = 1,
        [DefaultValue(false)] bool includeChildren = false,
        [DefaultValue(false)] bool sortByProminence = false,
        [DefaultValue(null)] string? inRegion = null,
        [DefaultValue(null)] string? nearElement = null,
        [DefaultValue(5000)] int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        const string actionName = "find";

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
                ExactDepth = exactDepth,
                FoundIndex = Math.Max(1, foundIndex),
                IncludeChildren = includeChildren,
                SortByProminence = sortByProminence,
                InRegion = inRegion,
                NearElement = nearElement,
                TimeoutMs = Math.Clamp(timeoutMs, 0, 60000)
            };

            var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(query, cancellationToken);
            return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.SerializeToolError(actionName, ex);
        }
    }
}
