using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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
    /// ✅ WORKS ON ELEVATED WINDOWS - use this when keyboard_control fails with "elevated window" error.
    /// WARNING: Do NOT use for Save As dialogs - use file_save(windowHandle, filePath) instead. It handles path entry and Save button automatically.
    /// Keywords: type, type text, enter text, input, fill, fill field, write text, set text,
    /// text field, edit box, form field, elevated window, admin window.
    /// </summary>
    /// <remarks>
    /// Type text into Edit, Document, TextBox, or search fields. Auto-activates window, optionally clears existing text first.
    /// ✅ Works on elevated/admin windows where keyboard_control fails. For Notepad, use controlType="Document" (not "Edit").
    /// TO SAVE FILES: Use file_save(windowHandle='...', filePath='C:/path/file.txt') - it handles the full Save As workflow automatically.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="text">Text to type. Required.</param>
    /// <param name="name">Element name (exact match, case-insensitive).</param>
    /// <param name="nameContains">Substring in element name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Edit, Document, TextBox, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="elementId">Stable element id from a prior ui_find/ui_snapshot. When provided, types into that exact element directly and ignores the name/type selectors (avoids re-querying).</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="clearFirst">Clear existing text before typing (default: false).</param>
    /// <param name="withSnapshot">When true, attach a post-action snapshot so you can verify the new state without another tool call. Default: false.</param>
    /// <param name="snapshotMode">Post-action snapshot mode when withSnapshot=true: full for one verification (default), auto for repeated checks of the same window, or reset when this action starts a new comparison.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload describing the type operation's success status and element information. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_type", Title = "Type Text into Element", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string text,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(null)] string? elementId,
        [DefaultValue(1)] int foundIndex,
        [DefaultValue(false)] bool clearFirst,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "type";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        if (string.IsNullOrEmpty(text))
        {
            return WindowsToolsBase.FailResult("text is required.");
        }

        var foundIndexError = WindowsToolsBase.ValidateFoundIndex(foundIndex);
        if (foundIndexError is not null)
        {
            return foundIndexError;
        }

        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult(
                $"snapshotMode must be one of: full, auto, reset (got '{snapshotMode}').");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                var byIdResult = await WindowsToolsBase.UIAutomationService.TypeIntoElementAsync(elementId, text, clearFirst, windowHandle, cancellationToken);
                byIdResult = await WindowsToolsBase.WithPostActionSnapshotAsync(
                    byIdResult, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken);
                return WindowsToolsBase.ToCallToolResult(byIdResult, includeDiagnostics);
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

            var result = await WindowsToolsBase.UIAutomationService.FindAndTypeAsync(query, text, clearFirst, cancellationToken);
            result = await WindowsToolsBase.WithPostActionSnapshotAsync(
                result, windowHandle, withSnapshot, parsedSnapshotMode, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }

    /// <summary>Calls the snapshot-aware overload with a complete post-action snapshot.</summary>
    public static Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string text,
        string? name,
        string? nameContains,
        string? namePattern,
        string? controlType,
        string? automationId,
        string? className,
        string? elementId,
        int foundIndex,
        bool clearFirst,
        bool withSnapshot,
        bool includeDiagnostics,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            windowHandle, text, name, nameContains, namePattern, controlType, automationId, className,
            elementId, foundIndex, clearFirst, withSnapshot, "full", includeDiagnostics,
            cancellationToken);
}
