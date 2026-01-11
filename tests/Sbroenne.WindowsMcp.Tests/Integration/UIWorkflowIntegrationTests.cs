using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end workflows against the WinForms test harness.
/// Tests verify complete user scenarios work correctly with WinForms controls.
/// These tests use our own MCP tools for verification.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIWorkflowIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public UIWorkflowIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
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

    /// <summary>
    /// Tests navigation workflow: Navigate through all tabs and verify each loads correctly.
    /// </summary>
    [Fact]
    public async Task Workflow_NavigateAllTabs_AllTabsLoad()
    {
        var tabNames = new[]
        {
            ("FormControlsTab", "SubmitButton"),
            ("ListViewTab", "ItemsListView"),
            ("TreeViewTab", "FolderTreeView"),
            ("DataGridTab", "ProductsDataGrid"),
            ("DialogsTab", "SaveAsButton"),
        };

        foreach (var (tabName, expectedControl) in tabNames)
        {
            // Click on the tab
            var tabResult = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = tabName.Replace("Tab", " ").Trim(),
            });
            // Note: tabs may be accessed differently, try by automation ID if name fails
            if (!tabResult.Success)
            {
                tabResult = await _automationService.FindAndClickAsync(new ElementQuery
                {
                    WindowHandle = _windowHandle,
                    AutomationId = tabName,
                });
            }

            await Task.Delay(100);

            // Verify expected control is visible
            var findResult = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = expectedControl,
            });

            Assert.True(findResult.Success, $"Failed to find {expectedControl} on tab {tabName}");
            Assert.NotNull(findResult.Items);
            Assert.NotEmpty(findResult.Items!);
        }
    }

    /// <summary>
    /// Tests form filling workflow: Fill out login form and submit.
    /// </summary>
    [Fact]
    public async Task Workflow_FillLoginForm_AllValuesSet()
    {
        // 1. Type username
        var usernameResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "testuser@example.com",
            clearFirst: true);
        Assert.True(usernameResult.Success, $"Username type failed: {usernameResult.ErrorMessage}");
        await Task.Delay(50);

        // 2. Type password
        var passwordResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "PasswordInput",
            },
            text: "SecureP@ss123",
            clearFirst: true);
        Assert.True(passwordResult.Success, $"Password type failed: {passwordResult.ErrorMessage}");
        await Task.Delay(50);

        // 3. Toggle a checkbox
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "AutosaveCheckbox",
        });
        await Task.Delay(50);

        // 4. Click submit
        var submitResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });
        Assert.True(submitResult.Success, $"Submit failed: {submitResult.ErrorMessage}");
        await Task.Delay(100);

        // Verify form values via fixture
        Assert.Equal("testuser@example.com", _fixture.Form!.UsernameText);
        Assert.Equal("SecureP@ss123", _fixture.Form!.PasswordText);
        Assert.Equal(1, _fixture.Form!.SubmitClickCount);
    }

    /// <summary>
    /// Tests radio button selection workflow.
    /// </summary>
    [Fact]
    public async Task Workflow_SelectRadioButtons_CorrectSelectionMade()
    {
        // Click "Large" radio button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "LargeRadio",
        });
        Assert.True(clickResult.Success, $"Radio click failed: {clickResult.ErrorMessage}");
        await Task.Delay(50);

        // Verify selection via fixture
        Assert.Equal("Large", _fixture.Form!.SelectedSize);

        // Now click "Small"
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SmallRadio",
        });
        await Task.Delay(50);

        Assert.Equal("Small", _fixture.Form!.SelectedSize);
    }

    /// <summary>
    /// Tests slider manipulation workflow.
    /// </summary>
    [Fact]
    public async Task Workflow_SliderExists_CanBeFound()
    {
        // Find the volume slider
        var sliderResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "VolumeSlider",
        });
        Assert.True(sliderResult.Success);
        Assert.NotNull(sliderResult.Items);
        Assert.NotEmpty(sliderResult.Items!);

        // Verify slider has click coordinates
        var slider = sliderResult.Items![0];
        Assert.NotNull(slider.Click);
        Assert.True(slider.Click.Length >= 2);
    }

    /// <summary>
    /// Tests combined workflow: Fill form, change options, submit, and verify all changes.
    /// </summary>
    [Fact]
    public async Task Workflow_CompleteFormInteraction_AllStateCorrect()
    {
        // Step 1: Fill username
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "john.doe",
            clearFirst: true);
        await Task.Delay(50);

        // Step 2: Toggle dark mode checkbox
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "DarkModeCheckbox",
        });
        await Task.Delay(50);

        // Step 3: Select "Large" size
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "LargeRadio",
        });
        await Task.Delay(50);

        // Step 4: Click submit
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });
        await Task.Delay(100);

        // Verify all state via fixture
        Assert.Equal("john.doe", _fixture.Form!.UsernameText);
        Assert.True(_fixture.Form!.CheckboxStates.Option3); // Dark Mode
        Assert.Equal("Large", _fixture.Form!.SelectedSize);
        Assert.Equal(1, _fixture.Form!.SubmitClickCount);
    }
}
