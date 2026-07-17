using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Structured tabular data extracted from a grid/table/list control via the
/// UI Automation Grid and Table patterns. Returned by the <c>ui_read_table</c> tool
/// so agents get rows and columns as JSON instead of scraping text or running OCR.
/// </summary>
public sealed record UITableData
{
    /// <summary>
    /// Total number of rows reported by the control (before any truncation).
    /// </summary>
    [JsonPropertyName("rowCount")]
    public required int RowCount { get; init; }

    /// <summary>
    /// Total number of columns reported by the control (before any truncation).
    /// </summary>
    [JsonPropertyName("columnCount")]
    public required int ColumnCount { get; init; }

    /// <summary>
    /// Column header labels, when the control exposes the Table pattern. Null when the
    /// control is grid-only (no header metadata) or headers could not be read.
    /// </summary>
    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Headers { get; init; }

    /// <summary>
    /// The extracted cell text as a row-major array of rows, each row an array of cell
    /// strings ordered by column. Empty string represents an empty or unreadable cell.
    /// </summary>
    [JsonPropertyName("rows")]
    public required string[][] Rows { get; init; }

    /// <summary>
    /// True when the control had more rows and/or columns than were returned because a
    /// maxRows/maxColumns cap was hit. Increase the caps to read the remainder.
    /// </summary>
    [JsonPropertyName("truncated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Truncated { get; init; }
}
