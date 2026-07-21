using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Clipboard.Tools;

/// <summary>
/// MCP tool for reading and writing the Windows text clipboard.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class ClipboardTool
{
    /// <summary>
    /// 📋 READ/WRITE THE CLIPBOARD - the fastest way to move bulk text in and out of desktop apps.
    /// Prefer this over typing character-by-character or OCR when an app supports copy/paste.
    /// </summary>
    /// <remarks>
    /// WORKFLOW: To pull text OUT of an app, focus it, send keyboard_control(key='c', modifiers='ctrl')
    /// (or 'a' then 'c' to select all), then clipboard(action='get'). To push text INTO an app,
    /// clipboard(action='set', text='...') then keyboard_control(key='v', modifiers='ctrl').
    /// The clipboard is a shared OS resource, so this reads/writes whatever any app last placed there.
    /// </remarks>
    /// <param name="action">The clipboard action: get (read text), set (write text), or clear.</param>
    /// <param name="text">Text to place on the clipboard. Required for the 'set' action. An empty string clears the clipboard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload. For 'get', the payload carries 'text', 'length', and 'hasText'. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "clipboard", Title = "📋 Clipboard read/write", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        ClipboardAction action,
        [DefaultValue(null)] string? text,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = action switch
            {
                ClipboardAction.Get => await WindowsToolsBase.ClipboardService.GetTextAsync(cancellationToken),
                ClipboardAction.Set => await SetAsync(text, cancellationToken),
                ClipboardAction.Clear => await WindowsToolsBase.ClipboardService.ClearAsync(cancellationToken),
                _ => ClipboardResult.CreateFailure(action.ToString(), $"Unsupported clipboard action: {action}.")
            };

            return ToCallToolResult(result);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult("clipboard", ex);
        }
    }

    private static async Task<ClipboardResult> SetAsync(string? text, CancellationToken cancellationToken)
    {
        if (text is null)
        {
            return ClipboardResult.CreateFailure(
                "set",
                "text is required for the 'set' action. Pass an empty string to clear the clipboard, or use action='clear'.");
        }

        return await WindowsToolsBase.ClipboardService.SetTextAsync(text, cancellationToken);
    }

    private static CallToolResult ToCallToolResult(ClipboardResult result) =>
        new()
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions) }],
            IsError = !result.Success
        };
}
