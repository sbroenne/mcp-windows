using System.Diagnostics;
using System.Runtime.InteropServices;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Structured grid/table extraction for the UI Automation service. Uses the UIA Grid and Table
/// patterns to return rows/columns as data instead of relying on OCR or text scraping.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <summary>
    /// Default maximum rows returned when the caller does not specify a cap.
    /// </summary>
    private const int DefaultMaxTableRows = 200;

    /// <summary>
    /// Default maximum columns returned when the caller does not specify a cap.
    /// </summary>
    private const int DefaultMaxTableColumns = 50;

    /// <summary>
    /// Extracts structured tabular data from a grid/table/list control via the UIA Grid pattern.
    /// </summary>
    /// <param name="elementId">Stable element id of the grid (or a container holding it). When null, the window root is used.</param>
    /// <param name="windowHandle">Window handle used to resolve the root when <paramref name="elementId"/> is null.</param>
    /// <param name="maxRows">Maximum number of rows to read (&lt;= 0 uses the default).</param>
    /// <param name="maxColumns">Maximum number of columns to read (&lt;= 0 uses the default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result carrying the extracted <see cref="UITableData"/>, or a failure describing why no grid was found.</returns>
    public async Task<UIAutomationResult> ReadTableAsync(string? elementId, string? windowHandle, int maxRows, int maxColumns, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var rowCap = maxRows <= 0 ? DefaultMaxTableRows : maxRows;
        var columnCap = maxColumns <= 0 ? DefaultMaxTableColumns : maxColumns;

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                UIA.IUIAutomationElement? targetElement;

                if (!string.IsNullOrEmpty(elementId))
                {
                    targetElement = ElementIdGenerator.ResolveToAutomationElement(elementId);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "read_table",
                            UIAutomationErrorType.ElementNotFound,
                            $"Element with ID '{elementId}' not found or stale.",
                            CreateDiagnostics(stopwatch));
                    }
                }
                else
                {
                    targetElement = GetRootElement(windowHandle);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "read_table",
                            UIAutomationErrorType.WindowNotFound,
                            "No window found. Provide a valid windowHandle or an elementId.",
                            CreateDiagnostics(stopwatch));
                    }
                }

                var gridElement = FindGridElement(targetElement);
                if (gridElement == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "read_table",
                        UIAutomationErrorType.PatternNotSupported,
                        "No grid/table control was found on the target. The element and its descendants do not expose the UIA Grid pattern.",
                        CreateDiagnostics(stopwatch),
                        "Point ui_read_table at a data grid, table, or details/report list-view. Use ui_snapshot to inspect the tree, or ui_read for plain text.");
                }

                var elevationCheck = CheckElevatedTarget(gridElement);
                if (!elevationCheck.Success)
                {
                    return elevationCheck with { Action = "read_table" };
                }

                var gridPattern = gridElement.GetPattern<UIA.IUIAutomationGridPattern>(UIA3PatternIds.Grid);
                if (gridPattern == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "read_table",
                        UIAutomationErrorType.PatternNotSupported,
                        "The resolved element stopped exposing the Grid pattern before it could be read.",
                        CreateDiagnostics(stopwatch));
                }

                var totalRows = Math.Max(0, gridPattern.CurrentRowCount);
                var totalColumns = Math.Max(0, gridPattern.CurrentColumnCount);
                var rowsToRead = Math.Min(totalRows, rowCap);
                var columnsToRead = Math.Min(totalColumns, columnCap);

                var headers = TryReadColumnHeaders(gridElement, columnsToRead);

                var rows = new string[rowsToRead][];
                for (var r = 0; r < rowsToRead; r++)
                {
                    var row = new string[columnsToRead];
                    for (var c = 0; c < columnsToRead; c++)
                    {
                        row[c] = ReadCellText(gridPattern, r, c);
                    }

                    rows[r] = row;
                }

                // Some providers (notably the Win32/WinForms ListView) expose the Grid pattern and
                // report the correct row/column counts, but GetItem() returns empty text for the
                // sub-item columns. Fall back to reading each row element's child cells directly.
                if (IsMissingSubItemText(rows, columnsToRead))
                {
                    FillRowsFromRowElements(gridElement, rows, rowsToRead, columnsToRead);
                }

                var truncated = totalRows > rowsToRead || totalColumns > columnsToRead;
                var table = new UITableData
                {
                    RowCount = totalRows,
                    ColumnCount = totalColumns,
                    Headers = headers,
                    Rows = rows,
                    Truncated = truncated ? true : null
                };

                return UIAutomationResult.CreateSuccessWithTable("read_table", table, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogReadTableError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "read_table",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "ReadTable"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogReadTableError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "read_table",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <summary>
    /// Returns the target element if it exposes the Grid pattern, otherwise the first descendant
    /// (control view) that does. Null when no grid-capable element exists in the subtree.
    /// </summary>
    private UIA.IUIAutomationElement? FindGridElement(UIA.IUIAutomationElement root)
    {
        if (SupportsGridPattern(root))
        {
            return root;
        }

        UIA.IUIAutomationElementArray? descendants;
        try
        {
            descendants = root.FindAll(UIA.TreeScope.TreeScope_Descendants, Uia.TrueCondition);
        }
        catch (COMException)
        {
            return null;
        }

        if (descendants == null)
        {
            return null;
        }

        var count = Math.Min(descendants.Length, MaxElementsToScan);
        for (var i = 0; i < count; i++)
        {
            UIA.IUIAutomationElement candidate;
            try
            {
                candidate = descendants.GetElement(i);
            }
            catch (COMException)
            {
                continue;
            }

            if (SupportsGridPattern(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool SupportsGridPattern(UIA.IUIAutomationElement element)
    {
        try
        {
            return element.GetPattern<UIA.IUIAutomationGridPattern>(UIA3PatternIds.Grid) != null;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads column header labels via the Table pattern, or null when the control has no header metadata.
    /// </summary>
    private static string[]? TryReadColumnHeaders(UIA.IUIAutomationElement gridElement, int columnsToRead)
    {
        if (columnsToRead <= 0)
        {
            return null;
        }

        try
        {
            var tablePattern = gridElement.GetPattern<UIA.IUIAutomationTablePattern>(UIA3PatternIds.Table);
            if (tablePattern == null)
            {
                return null;
            }

            var headerElements = tablePattern.GetCurrentColumnHeaders();
            if (headerElements == null || headerElements.Length == 0)
            {
                return null;
            }

            var count = Math.Min(headerElements.Length, columnsToRead);
            var headers = new string[count];
            for (var i = 0; i < count; i++)
            {
                headers[i] = GetCellText(headerElements.GetElement(i));
            }

            return headers;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Detects the degenerate case where a grid reports multiple columns but every sub-item column
    /// (index &gt;= 1) came back empty — typical of Win32 ListView Grid providers.
    /// </summary>
    private static bool IsMissingSubItemText(string[][] rows, int columnsToRead)
    {
        if (columnsToRead <= 1 || rows.Length == 0)
        {
            return false;
        }

        foreach (var row in rows)
        {
            for (var c = 1; c < row.Length; c++)
            {
                if (!string.IsNullOrEmpty(row[c]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Rebuilds row text by enumerating each row element's child cells. Used as a fallback for
    /// controls whose Grid pattern does not surface sub-item text via GetItem.
    /// </summary>
    private void FillRowsFromRowElements(UIA.IUIAutomationElement gridElement, string[][] rows, int rowsToRead, int columnsToRead)
    {
        UIA.IUIAutomationElementArray? rowElements;
        try
        {
            rowElements = gridElement.FindAll(UIA.TreeScope.TreeScope_Children, Uia.TrueCondition);
        }
        catch (COMException)
        {
            return;
        }

        if (rowElements == null)
        {
            return;
        }

        var rowIndex = 0;
        var elementCount = rowElements.Length;
        for (var i = 0; i < elementCount && rowIndex < rowsToRead; i++)
        {
            UIA.IUIAutomationElement rowElement;
            try
            {
                rowElement = rowElements.GetElement(i);
                if (!IsRowControlType(rowElement.GetControlTypeId()))
                {
                    continue;
                }
            }
            catch (COMException)
            {
                continue;
            }

            var cells = ReadRowCells(rowElement, columnsToRead);
            if (cells != null)
            {
                rows[rowIndex] = cells;
            }

            rowIndex++;
        }
    }

    private static bool IsRowControlType(int controlTypeId)
        => controlTypeId is UIA3ControlTypeIds.DataItem or UIA3ControlTypeIds.ListItem or UIA3ControlTypeIds.TreeItem;

    /// <summary>
    /// Reads a row's cell text from its child elements, padding/truncating to <paramref name="columnsToRead"/>.
    /// Falls back to the row element's own text when it exposes no child cells.
    /// </summary>
    private string[] ReadRowCells(UIA.IUIAutomationElement rowElement, int columnsToRead)
    {
        var cells = new string[columnsToRead];
        UIA.IUIAutomationElementArray? children;
        try
        {
            children = rowElement.FindAll(UIA.TreeScope.TreeScope_Children, Uia.TrueCondition);
        }
        catch (COMException)
        {
            children = null;
        }

        if (children == null || children.Length == 0)
        {
            // No child cells: use the row element's own text for the first column.
            cells[0] = GetCellText(rowElement);
            return cells;
        }

        var count = Math.Min(children.Length, columnsToRead);
        for (var i = 0; i < count; i++)
        {
            try
            {
                cells[i] = GetCellText(children.GetElement(i));
            }
            catch (COMException)
            {
                cells[i] = string.Empty;
            }
        }

        return cells;
    }

    /// <summary>
    /// Reads the text of a single grid cell, tolerating spanned/virtualized cells that fail to resolve.
    /// </summary>
    private static string ReadCellText(UIA.IUIAutomationGridPattern gridPattern, int row, int column)
    {
        try
        {
            var cell = gridPattern.GetItem(row, column);
            return cell == null ? string.Empty : GetCellText(cell);
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts the most meaningful text for a cell: value first, then name, then aggregated text.
    /// </summary>
    private static string GetCellText(UIA.IUIAutomationElement cell)
    {
        try
        {
            var value = cell.TryGetValue();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            var name = cell.GetName();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var text = cell.GetText();
            return string.IsNullOrEmpty(text) ? string.Empty : text;
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }
}
