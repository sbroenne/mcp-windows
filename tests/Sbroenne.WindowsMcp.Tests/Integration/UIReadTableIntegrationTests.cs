using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for structured grid/table extraction (Phase 2). Exercises the real UIA
/// Grid/Table patterns against the harness DataGridView and details-view ListView.
/// </summary>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class UIReadTableIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationThread _staThread;
    private readonly UIAutomationService _automationService;
    private readonly string _windowHandle;

    public UIReadTableIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        _windowHandle = _fixture.TestWindowHandleString;

        _staThread = new UIAutomationThread();
        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            new MouseInputService(),
            new KeyboardInputService(),
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    private async Task SwitchToTabAsync(string tabName)
    {
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = tabName,
            ControlType = "TabItem",
        });
        Assert.True(result.Success, $"Failed to switch to '{tabName}' tab: {result.ErrorMessage}");
        await Task.Delay(150);
    }

    [Fact]
    public async Task ReadTable_DataGridView_ReturnsHeadersAndRows()
    {
        await SwitchToTabAsync("Data Grid");

        var result = await _automationService.ReadTableAsync(
            elementId: null, _windowHandle, maxRows: 200, maxColumns: 50, CancellationToken.None);

        Assert.True(result.Success, $"ReadTable failed: {result.ErrorMessage}");
        var table = result.Table;
        Assert.NotNull(table);

        // 5 product rows (add-row disabled), 5 columns.
        Assert.Equal(5, table!.ColumnCount);
        Assert.True(table.RowCount >= 5, $"Expected >=5 rows, got {table.RowCount}.");
        Assert.NotNull(table.Headers);
        Assert.Contains("Product Name", table.Headers!);
        Assert.Contains("Price", table.Headers!);

        // Data cells resolved: the first data row should contain the first product.
        var flattened = table.Rows.SelectMany(r => r).ToArray();
        Assert.Contains("Laptop Pro 15", flattened);
        Assert.Contains("Wireless Mouse", flattened);
        Assert.Null(table.Truncated);
    }

    [Fact]
    public async Task ReadTable_DetailsListView_ReturnsRows()
    {
        await SwitchToTabAsync("List View");

        var find = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ItemsListView",
        });
        Assert.True(find.Success && find.Items?.Length > 0, "ItemsListView not found.");

        var result = await _automationService.ReadTableAsync(
            find.Items![0].Id, _windowHandle, maxRows: 200, maxColumns: 50, CancellationToken.None);

        Assert.True(result.Success, $"ReadTable failed: {result.ErrorMessage}");
        Assert.NotNull(result.Table);
        Assert.True(result.Table!.RowCount >= 5, $"Expected >=5 rows, got {result.Table.RowCount}.");
        var flattened = result.Table.Rows.SelectMany(r => r).ToArray();
        Assert.Contains("Project Alpha", flattened);
    }

    [Fact]
    public async Task ReadTable_Truncated_WhenMaxRowsBelowTotal()
    {
        await SwitchToTabAsync("Data Grid");

        var result = await _automationService.ReadTableAsync(
            elementId: null, _windowHandle, maxRows: 2, maxColumns: 50, CancellationToken.None);

        Assert.True(result.Success, $"ReadTable failed: {result.ErrorMessage}");
        Assert.NotNull(result.Table);
        Assert.Equal(2, result.Table!.Rows.Length);
        Assert.True(result.Table.RowCount >= 5);
        Assert.True(result.Table.Truncated);
    }

    [Fact]
    public async Task ReadTable_NonGridElement_ReturnsPatternNotSupported()
    {
        // The Submit button is not a grid; extraction should fail cleanly with a recovery hint.
        var find = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });
        Assert.True(find.Success && find.Items?.Length > 0, "Submit button not found.");

        var result = await _automationService.ReadTableAsync(
            find.Items![0].Id, _windowHandle, maxRows: 200, maxColumns: 50, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.PatternNotSupported, result.ErrorType);
        Assert.NotNull(result.RecoverySuggestion);
    }

    [Fact]
    public async Task ReadTableTool_DataGrid_EmitsTableJson()
    {
        await SwitchToTabAsync("Data Grid");

        var callResult = await UIReadTableTool.ExecuteAsync(
            _windowHandle, name: null, nameContains: null, namePattern: null,
            controlType: null, automationId: "ProductsDataGrid", className: null, elementId: null,
            foundIndex: 1, maxRows: 200, maxColumns: 50, includeDiagnostics: false, CancellationToken.None);

        Assert.False(callResult.IsError == true, "Tool reported an error.");
        var text = callResult.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Single().Text;

        using var doc = JsonDocument.Parse(text);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean(), text);
        var table = doc.RootElement.GetProperty("table");
        Assert.Equal(5, table.GetProperty("columnCount").GetInt32());
        Assert.True(table.GetProperty("rows").GetArrayLength() >= 5);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        GC.SuppressFinalize(this);
    }
}
