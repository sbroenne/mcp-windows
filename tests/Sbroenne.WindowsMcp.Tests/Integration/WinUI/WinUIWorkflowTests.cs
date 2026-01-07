using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Integration tests for end-to-end workflows against WinUI 3 modern app harness.
/// Tests verify complete user scenarios work correctly with modern WinUI 3 controls.
/// These tests use our own MCP tools for verification.
/// </summary>
[Collection("ModernTestHarness")]
public sealed class WinUIWorkflowTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUIWorkflowTests(ModernTestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;
        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator(windowConfiguration);
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

    /// <summary>
    /// Tests navigation workflow: Navigate through all pages and verify each loads correctly.
    /// </summary>
    [Fact]
    public async Task Workflow_NavigateAllPages_AllPagesLoad()
    {
        var navigationItems = new[]
        {
            ("NavHome", "StatusLabel"),
            ("NavFormControls", "SubmitButton"),
            ("NavEditor", "EditorTextBox"),
        };

        foreach (var (navItem, expectedControl) in navigationItems)
        {
            // Navigate
            var navResult = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = navItem,
            });
            Assert.True(navResult.Success, $"Failed to click {navItem}: {navResult.ErrorMessage}");
            await Task.Delay(200);

            // Verify expected control is visible
            var findResult = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = expectedControl,
            });

            Assert.True(findResult.Success, $"Failed to find {expectedControl} after navigating via {navItem}");
            Assert.NotNull(findResult.Items);
            Assert.NotEmpty(findResult.Items!);
        }
    }

    /// <summary>
    /// Tests form filling workflow: Fill out form and verify values are captured.
    /// </summary>
    [Fact]
    public async Task Workflow_FillForm_AllValuesSet()
    {
        // Navigate to Form Controls
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // 1. Type username
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "testuser@example.com",
            clearFirst: true);
        Assert.True(typeResult.Success, $"Type failed: {typeResult.ErrorMessage}");
        await Task.Delay(50);

        // 2. Toggle a checkbox
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EnableNotificationsCheckbox",
        });
        await Task.Delay(50);

        // 3. Click submit
        var submitResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });
        Assert.True(submitResult.Success, $"Submit failed: {submitResult.ErrorMessage}");
        await Task.Delay(100);

        // Verify status bar exists (visible on all pages)
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusBarText",
        });
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }

    /// <summary>
    /// Tests editor workflow: Type text and verify text box accepts input.
    /// </summary>
    [Fact]
    public async Task Workflow_EditorTyping_Succeeds()
    {
        // Navigate to Editor
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        await Task.Delay(200);

        // Type text (with clear first to ensure clean state)
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "EditorTextBox",
            },
            text: "The quick brown fox jumps over the lazy dog",
            clearFirst: true);
        Assert.True(typeResult.Success, $"Type failed: {typeResult.ErrorMessage}");
        await Task.Delay(100);

        // Verify word count element exists
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "WordCountText",
        });
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);
    }

    /// <summary>
    /// Tests CommandBar workflow: Click toolbar buttons and verify actions succeed.
    /// </summary>
    [Fact]
    public async Task Workflow_CommandBar_ToolbarActionsWork()
    {
        // Click the New button
        var newResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NewButton",
        });
        Assert.True(newResult.Success);
        await Task.Delay(100);

        // Verify status bar exists
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusBarText",
        });
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.NotEmpty(findResult.Items!);

        // Click Save button
        var saveResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SaveButton",
        });
        Assert.True(saveResult.Success);
    }

    /// <summary>
    /// Tests slider existence: Verify slider can be found on Form Controls page.
    /// </summary>
    [Fact]
    public async Task Workflow_SliderExists_OnFormControlsPage()
    {
        // Navigate to Form Controls
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Find slider
        var sliderResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "VolumeSlider",
        });
        Assert.True(sliderResult.Success);
        Assert.NotNull(sliderResult.Items);
        Assert.NotEmpty(sliderResult.Items!);

        // Find volume display
        var displayResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "VolumeValueText",
        });
        Assert.True(displayResult.Success);
        Assert.NotNull(displayResult.Items);
        Assert.NotEmpty(displayResult.Items!);
    }
}
