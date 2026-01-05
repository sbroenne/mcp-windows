using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for typing text into UI elements.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UITypeTool
{
    private readonly UIAutomationService _automationService;
    private readonly ILogger<UITypeTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UITypeTool"/> class.
    /// </summary>
    public UITypeTool(UIAutomationService automationService, ILogger<UITypeTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Types text into a text field or other input element. Automatically activates the target window.
    /// WARNING: Do NOT use for Save As dialogs - use ui_file(windowHandle, filePath) instead. It handles path entry and Save button automatically.
    /// </summary>
    /// <remarks>
    /// Type text into Edit, Document, TextBox, or search fields. Auto-activates window, optionally clears existing text first.
    /// TO SAVE FILES: Use ui_file(windowHandle='...', filePath='C:/path/file.txt') - it handles the full Save As workflow automatically.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="text">Text to type. Required.</param>
    /// <param name="name">Element name (exact match, case-insensitive).</param>
    /// <param name="nameContains">Substring in element name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Edit, Document, TextBox, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="clearFirst">Clear existing text before typing (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the type operation including success status and element information.</returns>
    [McpServerTool(Name = "ui_type", Title = "Type Text into Element", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
        string text,
        string? name = null,
        string? nameContains = null,
        string? namePattern = null,
        string? controlType = null,
        string? automationId = null,
        string? className = null,
        int foundIndex = 1,
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
