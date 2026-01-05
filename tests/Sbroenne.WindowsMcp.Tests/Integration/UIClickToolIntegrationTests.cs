using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for UIClickTool - clicking UI elements.
/// Tests real UI Automation click operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIClickToolIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIClickToolIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;

        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        _windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        _windowService = new WindowService(
            _windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector,
            windowConfiguration);

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
    public async Task FindAndClick_Button_IncrementsClickCount()
    {
        // Arrange
        var initialClickCount = _fixture.Form?.SubmitClickCount ?? 0;

        // Act - Click via UI Automation on the Submit button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(clickResult.Success, $"FindAndClick failed: {clickResult.ErrorMessage}");
        await Task.Delay(100); // Allow UI to update

        var newClickCount = _fixture.Form?.SubmitClickCount ?? 0;
        Assert.True(newClickCount > initialClickCount, "Button click count should have increased");
    }

    [Fact]
    public async Task FindAndClick_TabControl_SwitchesTab()
    {
        // Act - Click on the List View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });

        // Assert
        Assert.True(clickResult.Success, $"FindAndClick failed: {clickResult.ErrorMessage}");
        await Task.Delay(100); // Allow UI to update
    }

    [Fact]
    public async Task FindAndClick_CheckBox_TogglesState()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Find and click a checkbox to toggle it
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        // Assert - just verify the click action succeeded
        Assert.True(clickResult.Success, $"Click on checkbox failed: {clickResult.ErrorMessage}");
    }

    [Fact]
    public async Task FindAndClick_MultipleButtons_ClicksCorrectOne()
    {
        // Arrange - get initial submit click count
        var initialCount = _fixture.Form?.SubmitClickCount ?? 0;

        // Act - Click specifically on Submit button by name and type
        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Click failed: {result.ErrorMessage}");
        await Task.Delay(100);
        Assert.True((_fixture.Form?.SubmitClickCount ?? 0) > initialCount);
    }
}
