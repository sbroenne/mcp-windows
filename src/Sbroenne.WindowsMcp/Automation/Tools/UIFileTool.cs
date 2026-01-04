using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for file operations (save, open dialogs).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIFileTool
{
    private readonly UIAutomationService _automationService;
    private readonly ILogger<UIFileTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIFileTool"/> class.
    /// </summary>
    public UIFileTool(UIAutomationService automationService, ILogger<UIFileTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles file operations like saving and opening files.
    /// </summary>
    /// <remarks>
    /// Save or open files. Triggers dialogs, auto-fills paths, handles confirmations. Pass APPLICATION window (not dialog).
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED. Pass the app window, not a dialog.</param>
    /// <param name="filePath">File path to save to. Use backslashes for Windows format (e.g., C:\\Users\\User\\file.txt). Required for save action if dialog appears.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the file operation including success status.</returns>
    [McpServerTool(Name = "ui_file", Title = "File Operations", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
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
