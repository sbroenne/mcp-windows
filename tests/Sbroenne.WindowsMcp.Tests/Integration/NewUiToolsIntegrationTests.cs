using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the newly exposed UI Automation capabilities:
/// ui_snapshot (GetTreeAsync), ui_wait (WaitForElement*), ui_select (FindAndSelectAsync),
/// and elementId reuse (ClickElementAsync / TypeIntoElementAsync).
/// Tests run against the controlled WinForms harness.
/// </summary>
[Collection("UITestHarness")]
public sealed class NewUiToolsIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public NewUiToolsIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();

        _windowHandle = _fixture.TestWindowHandleString;

        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();
        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            mouseService,
            keyboardService,
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    [Fact]
    public async Task Snapshot_ReturnsTreeContainingKnownControls()
    {
        var result = await _automationService.GetTreeAsync(_windowHandle, null, 5, null, CancellationToken.None);

        Assert.True(result.Success, $"Snapshot failed: {result.ErrorMessage}");
        Assert.NotNull(result.Tree);
        Assert.True(result.Tree!.Length >= 1, "Expected at least one root element in the snapshot tree.");
    }

    [Fact]
    public async Task Snapshot_WithControlTypeFilter_Succeeds()
    {
        var result = await _automationService.GetTreeAsync(_windowHandle, null, 5, "Button", CancellationToken.None);

        Assert.True(result.Success, $"Filtered snapshot failed: {result.ErrorMessage}");
        Assert.NotNull(result.Tree);
    }

    [Fact]
    public async Task Wait_ForExistingElement_Appears()
    {
        var query = new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        };

        var result = await _automationService.WaitForElementAsync(query, 3000, CancellationToken.None);

        Assert.True(result.Success, $"Wait-for-appear failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task Wait_ForAbsentElement_DisappearsImmediately()
    {
        var query = new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "ThisElementDoesNotExist_XYZ",
            ControlType = "Button",
        };

        var result = await _automationService.WaitForElementDisappearAsync(query, 3000, CancellationToken.None);

        Assert.True(result.Success, $"Wait-for-disappear failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task Wait_ForElementState_Enabled_Succeeds()
    {
        var elementId = await ResolveElementIdAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        var result = await _automationService.WaitForElementStateAsync(elementId, "enabled", 3000, CancellationToken.None);

        Assert.True(result.Success, $"Wait-for-state failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task Select_ComboBoxValue_Succeeds()
    {
        var query = new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "CategoryCombo",
            ControlType = "ComboBox",
        };

        var result = await _automationService.FindAndSelectAsync(query, "Science", CancellationToken.None);

        Assert.True(result.Success, $"Select failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task ClickElement_ByElementId_Succeeds()
    {
        var elementId = await ResolveElementIdAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        var result = await _automationService.ClickElementAsync(elementId, _windowHandle, CancellationToken.None);

        Assert.True(result.Success, $"Click-by-id failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task TypeIntoElement_ByElementId_Succeeds()
    {
        var elementId = await ResolveElementIdAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        var result = await _automationService.TypeIntoElementAsync(elementId, "hello-by-id", clearFirst: true, _windowHandle, CancellationToken.None);

        Assert.True(result.Success, $"Type-by-id failed: {result.ErrorMessage}");
    }

    private async Task<string> ResolveElementIdAsync(ElementQuery query)
    {
        var findResult = await _automationService.FindElementsAsync(query, CancellationToken.None);
        Assert.True(findResult.Success, $"Setup find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.True(findResult.Items!.Length > 0, "Setup find returned no elements.");
        return findResult.Items![0].Id;
    }
}
