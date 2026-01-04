using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for finding UI elements in Windows applications.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIFindTool
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
    /// Finds UI elements matching the specified criteria. Returns element IDs and details for use with other ui_* tools.
    /// </summary>
    /// <remarks>
    /// Finds UI elements by name, type, ID, or other criteria. Returns element IDs for clicking, typing, etc. REQUIRED: windowHandle (from window_management tool).
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
    [McpServerTool(Name = "ui_find", Title = "Find UI Elements", Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
        string? name = null,
        string? nameContains = null,
        string? namePattern = null,
        string? controlType = null,
        string? automationId = null,
        string? className = null,
        int? exactDepth = null,
        int foundIndex = 1,
        bool includeChildren = false,
        bool sortByProminence = false,
        string? inRegion = null,
        string? nearElement = null,
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
