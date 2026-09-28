using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
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
    /// 💾 SAVE FILE TO DISK - Sends the requested save shortcut, fills a supported Save As dialog, and clicks Save once.
    /// Overwrite prompts and errors are left open for the caller to inspect and answer explicitly. Do not blindly repeat a failed save.
    /// NOTE: English Windows only (detects 'Save As' dialog titles).
    /// Keywords: save, save file, save as, save document, write file, store, persist, export,
    /// ctrl+s, save dialog, overwrite, filename.
    /// </summary>
    /// <remarks>
    /// Use for applications with supported native Save As dialogs. Use ui_find/ui_read to inspect any
    /// remaining prompt and ui_click or keyboard_control for an explicit response chosen by the caller.
    /// </remarks>
    /// <param name="windowHandle">Window handle (from app or window_management 'find'). REQUIRED. Pass the APPLICATION window handle, not a dialog.</param>
    /// <param name="filePath">Full destination path. Fills a Save As dialog; it does not retarget Ctrl+S for an already named document. To save an existing document under a different path, also set triggerMode='save_as'. Forward/back slashes both work.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="triggerMode">shortcut (default): Ctrl+S for the current document. save_as: F12 in Word/PowerPoint, Ctrl+Shift+S elsewhere, to choose a different destination without first saving over the original. wait: handle an already open owned Save As dialog without sending another shortcut. If the application shows an in-window Save As page instead of a dialog, inspect it and open its file chooser explicitly, then use wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload describing the save operation's success status. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "file_save", Title = "💾 SAVE FILE (handles Save As dialogs)", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? filePath,
        [DefaultValue(false)] bool includeDiagnostics,
        [DefaultValue("shortcut")] string triggerMode,
        CancellationToken cancellationToken)
    {
        const string actionName = "save";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find'). Pass the APPLICATION window, not a dialog.");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.SaveAsync(windowHandle, filePath, triggerMode, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
