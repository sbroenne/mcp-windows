using System.Text.Json;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the structured table result shape (<see cref="UITableData"/>) and the
/// <see cref="UIAutomationResult.CreateSuccessWithTable"/> factory. These run without a desktop.
/// </summary>
public sealed class TableResultTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private static UITableData SampleTable(bool truncated = false) => new()
    {
        RowCount = truncated ? 500 : 2,
        ColumnCount = 3,
        Headers = ["ID", "Name", "Status"],
        Rows =
        [
            ["1", "Alpha", "Active"],
            ["2", "Beta", "Pending"],
        ],
        Truncated = truncated ? true : null,
    };

    [Fact]
    public void CreateSuccessWithTable_PopulatesTableAndSuccess()
    {
        var table = SampleTable();

        var result = UIAutomationResult.CreateSuccessWithTable("read_table", table);

        Assert.True(result.Success);
        Assert.Equal("read_table", result.Action);
        Assert.Same(table, result.Table);
        Assert.Null(result.Text);
        Assert.Null(result.Items);
    }

    [Fact]
    public void CreateSuccessWithTable_NonTruncated_HintReportsRowAndColumnCount()
    {
        var result = UIAutomationResult.CreateSuccessWithTable("read_table", SampleTable());

        Assert.NotNull(result.UsageHint);
        Assert.Contains("2 rows", result.UsageHint);
        Assert.Contains("3 columns", result.UsageHint);
        Assert.DoesNotContain("truncated", result.UsageHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateSuccessWithTable_Truncated_HintMentionsTruncationAndTotals()
    {
        var result = UIAutomationResult.CreateSuccessWithTable("read_table", SampleTable(truncated: true));

        Assert.NotNull(result.UsageHint);
        Assert.Contains("truncated", result.UsageHint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500", result.UsageHint);
        Assert.Contains("maxRows", result.UsageHint);
    }

    [Fact]
    public void CreateSuccessWithTable_NullTable_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => UIAutomationResult.CreateSuccessWithTable("read_table", null!));
    }

    [Fact]
    public void UITableData_SerializesWithCamelCaseAndOmitsNulls()
    {
        var json = JsonSerializer.Serialize(SampleTable(), SerializerOptions);

        Assert.Contains("\"rowCount\":2", json);
        Assert.Contains("\"columnCount\":3", json);
        Assert.Contains("\"headers\":[\"ID\",\"Name\",\"Status\"]", json);
        Assert.Contains("\"rows\":[[\"1\",\"Alpha\",\"Active\"],[\"2\",\"Beta\",\"Pending\"]]", json);
        // Truncated is null here and must be omitted.
        Assert.DoesNotContain("truncated", json);
    }

    [Fact]
    public void UITableData_Truncated_SerializesTruncatedTrue()
    {
        var json = JsonSerializer.Serialize(SampleTable(truncated: true), SerializerOptions);

        Assert.Contains("\"truncated\":true", json);
    }

    [Fact]
    public void UITableData_NullHeaders_OmitsHeadersProperty()
    {
        var table = new UITableData
        {
            RowCount = 1,
            ColumnCount = 1,
            Headers = null,
            Rows = [["only"]],
        };

        var json = JsonSerializer.Serialize(table, SerializerOptions);

        Assert.DoesNotContain("headers", json);
        Assert.Contains("\"rows\":[[\"only\"]]", json);
    }
}
