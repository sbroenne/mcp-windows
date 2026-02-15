using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for saving files to disk. Handles Save As dialogs automatically.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIFileTool
{
    /// <summary>
    /// 💾 SAVE FILE TO DISK - The ONLY tool for saving documents. Automatically handles: Ctrl+S, Save As dialogs, filename entry, and overwrite prompts.
    /// ⚠️ DO NOT use keyboard_control(key='s', modifiers='ctrl') for saving - it CANNOT handle Save As dialogs and will get stuck!
    /// NOTE: English Windows only (detects 'Save As' dialog titles).
    /// </summary>
    /// <remarks>
    /// WHEN TO USE: Any time you need to save a file in ANY application (Notepad, Word, VS Code, etc.).
    /// WHAT IT DOES: Sends Ctrl+S, waits for Save As dialog, enters filename, clicks Save, handles overwrite confirmation.
    /// WHY NOT KEYBOARD: keyboard_control sends Ctrl+S but CANNOT detect or interact with the Save As dialog that appears.
    /// </remarks>
    /// <param name="windowHandle">Window handle (from app or window_management 'find'). REQUIRED. Pass the APPLICATION window handle, not a dialog.</param>
    /// <param name="filePath">Full path to save to (e.g., C:/Users/User/doc.txt). REQUIRED for new files. Forward/back slashes both work.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [McpServerTool(Name = "file_save", Title = "💾 SAVE FILE (handles Save As dialogs)", Destructive = true, OpenWorld = false)]
    public static async partial Task<string> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? filePath,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "save";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.Fail(
                "windowHandle is required. Get it from window_management(action='find'). Pass the APPLICATION window, not a dialog.");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.SaveAsync(windowHandle, filePath, cancellationToken);
            return WindowsToolsBase.SerializeUIResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.SerializeToolError(actionName, ex);
        }
    }
}
