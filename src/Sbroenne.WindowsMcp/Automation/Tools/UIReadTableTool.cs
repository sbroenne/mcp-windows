using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>MCP tool for reading an observed table.</summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIReadTableTool
{
    /// <summary>Read structured rows and columns from a grid discovered by ui_find or ui_snapshot. Never searches for another grid. Keywords: table, grid, rows, columns, extract, structured data.</summary>
    /// <param name="windowHandle">Explicit target window handle.</param>
    /// <param name="elementId">Required opaque ID of the grid itself.</param>
    /// <param name="maxRows">Maximum rows to read.</param>
    /// <param name="maxColumns">Maximum columns to read.</param>
    /// <param name="includeDiagnostics">Include diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [McpServerTool(Name = "ui_read_table", Title = "Read Table/Grid as Rows", Destructive = false, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string elementId,
        [DefaultValue(200)] int maxRows,
        [DefaultValue(50)] int maxColumns,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(windowHandle) || string.IsNullOrWhiteSpace(elementId))
        {
            return WindowsToolsBase.FailResult("windowHandle and elementId are required. Discover the grid with ui_find or ui_snapshot.");
        }

        try
        {
            var result = await WindowsToolsBase.UIAutomationService.ReadTableAsync(
                elementId, windowHandle, maxRows, maxColumns, cancellationToken);
            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.ErrorCallToolResult("read_table", ex);
        }
    }
}
