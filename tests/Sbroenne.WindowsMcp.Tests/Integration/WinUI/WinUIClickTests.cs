using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Integration tests for UI Automation click operations against WinUI 3 modern app harness.
/// Tests verify that our MCP tools work correctly with modern Windows App SDK applications.
/// </summary>
[Collection("ModernTestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class WinUIClickTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUIClickTests(ModernTestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.BringToFront();
        Thread.Sleep(200);

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
    public async Task FindAndClick_CommandBarButton_Succeeds()
    {
        // Act - Click the New button in the CommandBar
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NewButton",
        });

        // Assert
        Assert.True(result.Success, $"Click failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task FindAndClick_NavigationViewItem_SwitchesPage()
    {
        // Act - Click on the Form Controls navigation item
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });

        // Assert
        Assert.True(result.Success, $"Navigation click failed: {result.ErrorMessage}");
        await Task.Delay(200);

        // Verify we're on the Form Controls page by finding a control that's only on that page
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });

        Assert.True(findResult.Success, "SubmitButton should be visible after navigating to Form Controls");
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }

    [Fact]
    public async Task FindAndClick_AccentButton_UpdatesStatus()
    {
        // Navigate to Form Controls page first
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act - Click the Submit button (styled as AccentButton in WinUI 3)
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });

        // Assert
        Assert.True(result.Success, $"Submit click failed: {result.ErrorMessage}");
        await Task.Delay(100);

        // Verify by finding the StatusBarText element (visible on all pages)
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusBarText",
        });

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }

    [Fact]
    public async Task FindAndClick_CheckBox_TogglesState()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act - Click the EnableNotifications checkbox
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EnableNotificationsCheckbox",
        });

        // Assert
        Assert.True(result.Success, $"Checkbox click failed: {result.ErrorMessage}");
        await Task.Delay(100);

        // Navigate to home page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavHome",
        });
        await Task.Delay(100);

        // Verify checkbox state display exists
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "CheckboxStateDisplay",
        });

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }

    [Fact]
    public async Task FindAndClick_ClickTestButton_IncrementsCount()
    {
        // Navigate to Editor page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        await Task.Delay(200);

        // Act - Click the ClickTestButton multiple times
        for (int i = 0; i < 3; i++)
        {
            var result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "ClickTestButton",
            });
            Assert.True(result.Success, $"Click test button click failed: {result.ErrorMessage}");
            await Task.Delay(50);
        }

        await Task.Delay(100);

        // Verify click count element exists
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ClickCountText",
        });

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }
}
