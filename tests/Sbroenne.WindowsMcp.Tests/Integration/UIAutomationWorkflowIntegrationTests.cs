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
/// Integration tests for workflows combining multiple UI tools.
/// Tests realistic user workflows against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIAutomationWorkflowIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIAutomationWorkflowIntegrationTests(UITestHarnessFixture fixture)
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
    public async Task CombinedWorkflow_TypeAndClick_Succeeds()
    {
        // This test simulates a realistic workflow:
        // 1. Type text in the text box
        // 2. Click a button

        // Ensure the window is in the foreground
        _fixture.BringToFront();
        await Task.Delay(200);

        // Step 1: Type text using automationId to target specific textbox
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Workflow test",
            clearFirst: true);
        Assert.True(typeResult.Success, $"Type failed: {typeResult.ErrorMessage}");

        // Wait for UI to stabilize
        await Task.Delay(100);

        // Step 2: Click the Submit button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });
        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");

        // Verify results - give more time for click to register
        await Task.Delay(200);
        Assert.Equal("Workflow test", _fixture.Form?.UsernameText);
        Assert.True((_fixture.Form?.SubmitClickCount ?? 0) > 0, $"Button click count was: {_fixture.Form?.SubmitClickCount ?? 0}");
    }

    [Fact]
    public async Task Workflow_TypeReadAndClick_Works()
    {
        // Arrange - Type some text
        var testText = "Read this back";
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);
        await Task.Delay(100);

        // Act - Read the text back
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        Assert.True(findResult.Success);
        var textBoxId = findResult.Items![0].Id;

        var readResult = await _automationService.GetTextAsync(textBoxId, _windowHandle, false);

        // Assert
        Assert.True(readResult.Success);
        Assert.Equal(testText, readResult.Text);
    }

    [Fact]
    public async Task Workflow_WaitAndClick_Succeeds()
    {
        // Arrange - Wait for a button that exists
        var waitResult = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            },
            timeoutMs: 5000);

        Assert.True(waitResult.Success);

        // Act - Click it
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(clickResult.Success);
        await Task.Delay(100);
        Assert.True((_fixture.Form?.SubmitClickCount ?? 0) > 0);
    }

    [Fact]
    public async Task Workflow_FindNavigateAndClick_Succeeds()
    {
        // Arrange - Find tab control
        var tabFindResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        Assert.True(tabFindResult.Success);

        // Act - Click to navigate to Form Controls tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success);
        await Task.Delay(100);

        // Find and click a checkbox on that tab
        var checkboxResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        // Assert
        Assert.True(checkboxResult.Success, $"Failed to click checkbox: {checkboxResult.ErrorMessage}");
    }
}
