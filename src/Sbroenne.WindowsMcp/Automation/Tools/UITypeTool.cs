using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for typing text into UI elements.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UITypeTool
{
    private readonly IUIAutomationService _automationService;
    private readonly ILogger<UITypeTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UITypeTool"/> class.
    /// </summary>
    public UITypeTool(IUIAutomationService automationService, ILogger<UITypeTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Types text into a text field or other input element. Automatically activates the target window.
    /// </summary>
    [McpServerTool(Name = "ui_type", Title = "Type Text into Element", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Type text into Edit, Document, TextBox, or search fields. Auto-activates window, optionally clears existing text first.")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.")]
        string windowHandle,

        [Description("Text to type. Required.")]
        string text,

        [Description("Element name (exact match, case-insensitive).")]
        string? name = null,

        [Description("Substring in element name (case-insensitive).")]
        string? nameContains = null,

        [Description("Regex pattern for element name matching.")]
        string? namePattern = null,

        [Description("Control type (Edit, Document, TextBox, etc.)")]
        string? controlType = null,

        [Description("AutomationId for precise matching.")]
        string? automationId = null,

        [Description("Element class name.")]
        string? className = null,

        [Description("Return Nth match (1-based, default: 1).")]
        int foundIndex = 1,

        [Description("Clear existing text before typing (default: false).")]
        bool clearFirst = false,

        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_type",
                UIAutomationErrorType.InvalidParameter,
                "windowHandle is required. Get it from window_management(action='find').",
                null);
        }

        if (string.IsNullOrEmpty(text))
        {
            return UIAutomationResult.CreateFailure(
                "ui_type",
                UIAutomationErrorType.InvalidParameter,
                "text is required.",
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

        return await _automationService.FindAndTypeAsync(query, text, clearFirst, cancellationToken);
    }
}
