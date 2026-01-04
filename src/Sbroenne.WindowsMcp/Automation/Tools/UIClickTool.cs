using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for clicking UI elements.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIClickTool
{
    private readonly UIAutomationService _automationService;
    private readonly WindowService _windowService;
    private readonly ILogger<UIClickTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIClickTool"/> class.
    /// </summary>
    public UIClickTool(
        UIAutomationService automationService,
        WindowService windowService,
        ILogger<UIClickTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _windowService = windowService;
        _logger = logger;
    }

    /// <summary>
    /// Clicks a UI element. Automatically activates the target window before clicking.
    /// </summary>
    [McpServerTool(Name = "ui_click", Title = "Click UI Element", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Click a button, link, menu item, or other element. Auto-activates window. Preferred over mouse_control for UI interactions.")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.")]
        string windowHandle,

        [Description("Element name (exact match, case-insensitive). For Electron apps, the ARIA label.")]
        string? name = null,

        [Description("Substring in element name (case-insensitive). Preferred for dialog buttons like 'Don\\'t save'.")]
        string? nameContains = null,

        [Description("Regex pattern for element name matching.")]
        string? namePattern = null,

        [Description("Control type (Button, MenuItem, Hyperlink, ListItem, etc.)")]
        string? controlType = null,

        [Description("AutomationId for precise matching.")]
        string? automationId = null,

        [Description("Element class name (e.g., 'Chrome_WidgetWin_1').")]
        string? className = null,

        [Description("Return Nth match (1-based, default: 1).")]
        int foundIndex = 1,

        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_click",
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
            FoundIndex = Math.Max(1, foundIndex)
        };

        return await _automationService.FindAndClickAsync(query, cancellationToken);
    }
}
