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
    /// ✅ SAVE FILES - Use this tool to save documents. Handles Ctrl+S, Save As dialogs, filename entry, and Save button automatically.
    /// ALWAYS use this instead of keyboard_control(key='s', modifiers='ctrl') which does NOT handle Save As dialogs.
    /// NOTE: Only works on English Windows (detects 'Save As' dialog titles and 'Yes'/'Replace' button text).
    /// </summary>
    /// <remarks>
    /// Pass the APPLICATION window handle (from app or window_management), not a dialog handle.
    /// Pass filePath with full path (e.g., 'C:/Users/User/doc.txt'). Forward slashes auto-converted.
    /// Handles: Ctrl+S trigger, Save As dialog detection, filename field entry, Save button click, overwrite prompts.
    /// English Windows only: detects dialogs by English titles like 'Save As' and buttons like 'Yes'/'Replace'.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from app or window_management 'find'). REQUIRED. Pass the app window, not a dialog.</param>
    /// <param name="filePath">File path to save to. Both forward slashes and backslashes work (e.g., D:/folder/file.txt or D:\\folder\\file.txt). Required for Save As.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the file operation including success status.</returns>
    [McpServerTool(Name = "ui_file", Title = "SAVE FILE to Disk", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
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
