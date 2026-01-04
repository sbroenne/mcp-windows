using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for waiting for UI elements to appear or disappear.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIWaitTool
{
    private readonly IUIAutomationService _automationService;
    private readonly ILogger<UIWaitTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIWaitTool"/> class.
    /// </summary>
    public UIWaitTool(IUIAutomationService automationService, ILogger<UIWaitTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Waits for a UI element to appear or disappear.
    /// </summary>
    [McpServerTool(Name = "ui_wait", Title = "Wait for UI Element", Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Wait for an element to appear, disappear, or reach a state. Use after async operations, dialogs, loading spinners.")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.")]
        string windowHandle,

        [Description("Wait mode: 'appear' (default), 'disappear', 'enabled', 'disabled', 'visible', 'offscreen'.")]
        string mode = "appear",

        [Description("Element name (exact match, case-insensitive).")]
        string? name = null,

        [Description("Substring in element name (case-insensitive).")]
        string? nameContains = null,

        [Description("Regex pattern for element name matching.")]
        string? namePattern = null,

        [Description("Control type (Button, Edit, ProgressBar, etc.)")]
        string? controlType = null,

        [Description("AutomationId for precise matching.")]
        string? automationId = null,

        [Description("Element class name.")]
        string? className = null,

        [Description("Exact depth to search (1=immediate children).")]
        int? exactDepth = null,

        [Description("Return Nth match (1-based, default: 1).")]
        int foundIndex = 1,

        [Description("Timeout in milliseconds (default: 5000).")]
        int timeoutMs = 5000,

        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_wait",
                UIAutomationErrorType.InvalidParameter,
                "windowHandle is required. Get it from window_management(action='find').",
                null);
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            mode = "appear";
        }

        // Validate at least one search criterion
        var hasSearchCriteria = !string.IsNullOrEmpty(name) ||
                                !string.IsNullOrEmpty(nameContains) ||
                                !string.IsNullOrEmpty(namePattern) ||
                                !string.IsNullOrEmpty(controlType) ||
                                !string.IsNullOrEmpty(automationId) ||
                                !string.IsNullOrEmpty(className);

        if (!hasSearchCriteria)
        {
            return UIAutomationResult.CreateFailure(
                "ui_wait",
                UIAutomationErrorType.InvalidParameter,
                "Provide at least one search criterion: name, nameContains, controlType, or automationId.",
                null);
        }

        var timeoutClamped = Math.Clamp(timeoutMs, 0, 60000);

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
            TimeoutMs = timeoutClamped
        };

        return mode.ToLowerInvariant() switch
        {
            "disappear" => await _automationService.WaitForElementDisappearAsync(query, timeoutClamped, cancellationToken),
            "enabled" or "disabled" or "visible" or "offscreen" => await HandleStateWaitAsync(query, mode, timeoutClamped, cancellationToken),
            _ => await _automationService.WaitForElementAsync(query, timeoutClamped, cancellationToken)
        };
    }

    private async Task<UIAutomationResult> HandleStateWaitAsync(ElementQuery query, string desiredState, int timeoutMs, CancellationToken cancellationToken)
    {
        // First find the element
        var findResult = await _automationService.FindElementsAsync(query, cancellationToken);
        if (!findResult.Success || findResult.Items?.Length == 0)
        {
            return UIAutomationResult.CreateFailure(
                "ui_wait",
                UIAutomationErrorType.ElementNotFound,
                findResult.ErrorMessage ?? "Element not found.",
                null);
        }

        var elementId = findResult.Items![0].Id;
        return await _automationService.WaitForElementStateAsync(elementId, desiredState, timeoutMs, cancellationToken);
    }
}
