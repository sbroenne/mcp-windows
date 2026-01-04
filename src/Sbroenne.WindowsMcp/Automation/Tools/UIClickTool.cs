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
public sealed partial class UIClickTool
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
    /// Click a button, link, menu item, or other element. Auto-activates window. Preferred over mouse_control for UI interactions.
    /// </summary>
    /// <remarks>
    /// Clicks a UI element. Automatically activates the target window before clicking.
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
    [McpServerTool(Name = "ui_click", Title = "Click UI Element", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
        string? name = null,
        string? nameContains = null,
        string? namePattern = null,
        string? controlType = null,
        string? automationId = null,
        string? className = null,
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
