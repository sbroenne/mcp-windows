using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for typing text into UI elements.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UITypeTool
{
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
    [McpServerTool(Name = "ui_type", Title = "Type Text into Element", Destructive = true, OpenWorld = false)]
    public static async Task<string> ExecuteAsync(
        string windowHandle,
        string text,
        [DefaultValue(null)] string? name = null,
        [DefaultValue(null)] string? nameContains = null,
        [DefaultValue(null)] string? namePattern = null,
        [DefaultValue(null)] string? controlType = null,
        [DefaultValue(null)] string? automationId = null,
        [DefaultValue(null)] string? className = null,
        [DefaultValue(1)] int foundIndex = 1,
        [DefaultValue(false)] bool clearFirst = false,
        CancellationToken cancellationToken = default)
    {
        const string actionName = "type";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.Fail(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        if (string.IsNullOrEmpty(text))
        {
            return WindowsToolsBase.Fail("text is required.");
        }

        try
        {
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

            var result = await WindowsToolsBase.UIAutomationService.FindAndTypeAsync(query, text, clearFirst, cancellationToken);
            return JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.SerializeToolError(actionName, ex);
        }
    }
}
