using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for extracting structured tabular data (rows/columns) from grid, table, and
/// details/report list-view controls via the UI Automation Grid and Table patterns.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIReadTableTool
{
    /// <summary>
    /// Reads a grid/table/list control as structured rows and columns.
    /// </summary>
    /// <remarks>
    /// Extracts tabular data (DataGrid, Table, WPF DataGrid, WinForms DataGridView, details/report
    /// ListView, spreadsheet-like grids) as JSON rows and columns using the UIA Grid/Table patterns -
    /// no OCR, no text scraping. Point it at the grid element (via selectors or elementId) or at the
    /// window: if no selector matches a grid, the first grid-capable descendant of the window is used.
    /// Returns rowCount/columnCount, optional column headers, and a row-major rows array. Large grids
    /// are capped by maxRows/maxColumns and report truncated=true. For non-tabular text use ui_read.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="name">Element name (exact match, case-insensitive) to locate the grid.</param>
    /// <param name="nameContains">Substring in element name (case-insensitive) to locate the grid.</param>
    /// <param name="namePattern">Regex pattern for element name matching to locate the grid.</param>
    /// <param name="controlType">Control type of the grid (Table, DataGrid, List, etc.).</param>
    /// <param name="automationId">AutomationId of the grid for precise matching.</param>
    /// <param name="className">Element class name of the grid.</param>
    /// <param name="elementId">Stable element id from a prior ui_find/ui_snapshot. When provided, reads that exact element (or its first grid-capable descendant) and ignores the name/type selectors.</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1) when selectors match multiple elements.</param>
    /// <param name="maxRows">Maximum rows to read (default: 200). Rows beyond this are omitted and truncated=true is returned.</param>
    /// <param name="maxColumns">Maximum columns to read (default: 50). Columns beyond this are omitted and truncated=true is returned.</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result whose JSON payload carries a <c>table</c> object (rowCount, columnCount, headers, rows, truncated). <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_read_table", Title = "Read Table/Grid as Rows", Destructive = false, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(null)] string? elementId,
        [DefaultValue(1)] int foundIndex,
        [DefaultValue(200)] int maxRows,
        [DefaultValue(50)] int maxColumns,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "read_table";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        var foundIndexError = WindowsToolsBase.ValidateFoundIndex(foundIndex);
        if (foundIndexError is not null)
        {
            return foundIndexError;
        }

        try
        {
            var automationService = WindowsToolsBase.UIAutomationService;

            string? elementIdToRead = null;
            if (!string.IsNullOrWhiteSpace(elementId))
            {
                elementIdToRead = elementId;
            }
            else if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(nameContains) || !string.IsNullOrEmpty(namePattern) ||
                !string.IsNullOrEmpty(controlType) || !string.IsNullOrEmpty(automationId) || !string.IsNullOrEmpty(className))
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

                var findResult = await automationService.FindElementsAsync(query, cancellationToken);
                if (findResult.Success && findResult.Items?.Length > 0)
                {
                    elementIdToRead = findResult.Items[0].Id;
                }
            }

            var result = await automationService.ReadTableAsync(elementIdToRead, windowHandle, maxRows, maxColumns, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
