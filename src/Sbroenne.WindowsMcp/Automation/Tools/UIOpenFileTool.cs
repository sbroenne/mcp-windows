using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for opening files through an application's standard Open dialog.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIOpenFileTool
{
    /// <summary>
    /// 📂 OPEN A FILE via the app's Open dialog - the counterpart to file_save. Sends Ctrl+O,
    /// waits for the Open dialog, types the path into the File name field, and clicks Open.
    /// NOTE: English Windows only; the file must already exist.
    /// </summary>
    /// <remarks>
    /// WHEN TO USE: To load an existing document into an app that uses the standard Windows Open
    /// dialog (Notepad, WordPad, most editors). WHY NOT KEYBOARD: like file_save, this drives the
    /// dialog reliably instead of leaving keyboard_control stuck on a dialog it cannot see.
    /// </remarks>
    /// <param name="windowHandle">Application window handle (from app or window_management 'find'). REQUIRED. Pass the APPLICATION window, not a dialog.</param>
    /// <param name="filePath">Absolute path of an existing file to open (e.g., C:/Users/User/doc.txt). Forward/back slashes both work. REQUIRED.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload describing the open operation's success status. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "file_open", Title = "📂 OPEN FILE (handles Open dialogs)", Destructive = false, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string filePath,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "open";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find'). Pass the APPLICATION window, not a dialog.");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return WindowsToolsBase.FailResult(
                "filePath is required. Provide the absolute path of an existing file to open.");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.OpenFileAsync(windowHandle, filePath, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
