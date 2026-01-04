using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for finding UI elements in Windows applications.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIFindTool
{
    private readonly UIAutomationService _automationService;
    private readonly ILogger<UIFindTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIFindTool"/> class.
    /// </summary>
    public UIFindTool(UIAutomationService automationService, ILogger<UIFindTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Finds UI elements matching the specified criteria.
    /// Returns element IDs and details for use with other ui_* tools.
    /// </summary>
    [McpServerTool(Name = "ui_find", Title = "Find UI Elements", Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Find UI elements by name, type, ID, or other criteria. Returns element IDs for clicking, typing, etc. REQUIRED: windowHandle (from window_management tool).")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.")]
        string windowHandle,

        [Description("Element name (exact match, case-insensitive). For Electron apps, this is the ARIA label.")]
        string? name = null,

        [Description("Substring to search in element names (case-insensitive). Preferred for dialog buttons - e.g., 'Don\\'t save'.")]
        string? nameContains = null,

        [Description("Regex pattern to match element names. Use for complex matching like 'Button [0-9]+' or 'Save|Cancel'.")]
        string? namePattern = null,

        [Description("Control type filter (Button, Edit, Text, CheckBox, ComboBox, Menu, MenuItem, etc.)")]
        string? controlType = null,

        [Description("AutomationId for precise matching (exact match, most reliable).")]
        string? automationId = null,

        [Description("Element class name (e.g., 'Chrome_WidgetWin_1' for Chromium, 'Button' for Win32).")]
        string? className = null,

        [Description("Exact depth to search (1=immediate children). Skips other depths, improves performance.")]
        int? exactDepth = null,

        [Description("Return the Nth match (1-based, default: 1). Use 2 for second match, etc.")]
        int foundIndex = 1,

        [Description("Include child elements in response (default: false).")]
        bool includeChildren = false,

        [Description("Sort results by size (largest first). Useful for disambiguation.")]
        bool sortByProminence = false,

        [Description("Filter to region: 'x,y,width,height' in screen coordinates.")]
        string? inRegion = null,

        [Description("Find elements near this elementId (results sorted by distance).")]
        string? nearElement = null,

        [Description("Timeout in milliseconds (default: 5000).")]
        int timeoutMs = 5000,

        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_find",
                UIAutomationErrorType.InvalidParameter,
                "windowHandle is required. Get it from window_management(action='find').",
                null);
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
            ExactDepth = exactDepth,
            FoundIndex = Math.Max(1, foundIndex),
            IncludeChildren = includeChildren,
            SortByProminence = sortByProminence,
            InRegion = inRegion,
            NearElement = nearElement,
            TimeoutMs = Math.Clamp(timeoutMs, 0, 60000)
        };

        return await _automationService.FindElementsAsync(query, cancellationToken);
    }
}
