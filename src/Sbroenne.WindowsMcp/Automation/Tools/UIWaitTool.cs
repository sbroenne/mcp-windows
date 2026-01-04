using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for waiting for UI elements to appear or disappear.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIWaitTool
{
    private readonly UIAutomationService _automationService;
    private readonly ILogger<UIWaitTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIWaitTool"/> class.
    /// </summary>
    public UIWaitTool(UIAutomationService automationService, ILogger<UIWaitTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Waits for a UI element to appear or disappear.
    /// </summary>
    /// <remarks>
    /// Wait for an element to appear, disappear, or reach a state. Use after async operations, dialogs, loading spinners.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="mode">Wait mode: 'appear' (default), 'disappear', 'enabled', 'disabled', 'visible', 'offscreen'.</param>
    /// <param name="name">Element name (exact match, case-insensitive).</param>
    /// <param name="nameContains">Substring in element name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Button, Edit, ProgressBar, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="exactDepth">Exact depth to search (1=immediate children).</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result indicating whether the wait condition was satisfied.</returns>
    [McpServerTool(Name = "ui_wait", Title = "Wait for UI Element", Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
        string mode = "appear",
        string? name = null,
        string? nameContains = null,
        string? namePattern = null,
        string? controlType = null,
        string? automationId = null,
        string? className = null,
        int? exactDepth = null,
        int foundIndex = 1,
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
