using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for file operations (save, open dialogs).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIFileTool
{
    private readonly IUIAutomationService _automationService;
    private readonly ILogger<UIFileTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIFileTool"/> class.
    /// </summary>
    public UIFileTool(IUIAutomationService automationService, ILogger<UIFileTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles file operations like saving and opening files.
    /// </summary>
    [McpServerTool(Name = "ui_file", Title = "File Operations", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Save or open files. Triggers dialogs, auto-fills paths, handles confirmations. Pass APPLICATION window (not dialog).")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED. Pass the app window, not a dialog.")]
        string windowHandle,

        [Description("File path to save to. Use backslashes for Windows format (e.g., C:\\Users\\User\\file.txt). Required for save action if dialog appears.")]
        string? filePath = null,

        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_file",
                UIAutomationErrorType.InvalidParameter,
                "windowHandle is required. Get it from window_management(action='find'). Pass the APPLICATION window, not a dialog.",
                null);
        }

        return await _automationService.SaveAsync(windowHandle, filePath, cancellationToken);
    }
}
