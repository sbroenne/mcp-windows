using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
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
    private readonly MouseInputService _mouseService;
    private readonly string _windowHandle;

    public UIClickToolIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();

        _windowHandle = _fixture.TestWindowHandleString;

        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        _windowEnumerator = new WindowEnumerator(elevationDetector);
        var windowActivator = new WindowActivator();
        _windowService = new WindowService(
            _windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector);

        _mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            _mouseService,
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
    public async Task FindAndClick_Button_PrefersSemanticInvoke()
    {
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
            ControlType = "Button",
        });

        Assert.True(clickResult.Success, $"FindAndClick failed: {clickResult.ErrorMessage}");
        var mouseInput = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitMouseInputLabel",
        });

        Assert.True(mouseInput.Success, $"Read failed: {mouseInput.ErrorMessage}");
        Assert.Equal("0", Assert.Single(mouseInput.Items!).Name);
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task FindAndClick_WhenSemanticPatternIsUnavailable_UsesPhysicalFallback()
    {
        await TestRetry.RunAsync(async _ =>
        {
            EnsureHarnessOnPrimaryMonitor();
            await SkipWhenPhysicalFallbackClickUnavailableAsync();
            var target = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "PhysicalFallbackTarget",
            });
            Assert.Single(target.Items!);

            var result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "PhysicalFallbackTarget",
            });

            Assert.True(result.Success, $"Physical fallback failed: {result.ErrorMessage}");
            var status = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "StatusLabel",
            });
            Assert.True(status.Success, $"Status read failed: {status.ErrorMessage}");
            Assert.Equal("Physical fallback clicked", Assert.Single(status.Items!).Name);
        });
    }

    [SkippableFact]
    [Trait("Category", "RequiresDesktop")]
    public async Task FindAndClick_WhenActionHasNoObservableEffect_ReturnsFailure()
    {
        EnsureHarnessOnPrimaryMonitor();
        await SkipWhenPhysicalClickUnavailableAsync();
        var target = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "InertTarget",
        });
        Assert.Single(target.Items!);

        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "InertTarget",
        });

        Assert.False(result.Success);
        Assert.Contains("observable", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SkipWhenPhysicalClickUnavailableAsync()
    {
        var probeTarget = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SubmitButton",
        });
        var clickPoint = Assert.Single(probeTarget.Items!).Click;
        var click = await _mouseService.ClickAsync(clickPoint[0], clickPoint[1]);
        var received = TestWait.Until(
            () => _fixture.Form is not null &&
                  (bool)_fixture.Form.Invoke(() =>
                      _fixture.Form.SubmitMouseInputCount > 0));

        _fixture.Reset();
        _fixture.BringToFront();
        Skip.If(
            !click.Success || !received,
            "The current desktop does not permit physical click injection.");
    }

    // Representative probe: verifies this desktop session can deliver AND verify a physical
    // click to the *actual* PhysicalFallbackTarget panel (near the form's bottom edge) before
    // the test asserts the product's physical-fallback behavior. A generic SubmitButton probe
    // is not representative: it can pass while a click to this specific low target is dropped or
    // lands off-target on a loaded, shared self-hosted desktop. Skipping (not failing) here keeps
    // a genuine fallback-logic regression detectable while eliminating environment-only failures.
    private async Task SkipWhenPhysicalFallbackClickUnavailableAsync()
    {
        var probeTarget = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "PhysicalFallbackTarget",
        });
        var clickPoint = Assert.Single(probeTarget.Items!).Click;
        var click = await _mouseService.ClickAsync(clickPoint[0], clickPoint[1]);
        var received = TestWait.Until(
            () => _fixture.Form is not null &&
                  (bool)_fixture.Form.Invoke(() =>
                      _fixture.Form.PhysicalFallbackClickCount > 0));

        _fixture.Reset();
        _fixture.BringToFront();
        Skip.If(
            !click.Success || !received,
            "The current desktop does not permit reliable physical clicks on the fallback target.");
    }

    private void EnsureHarnessOnPrimaryMonitor()
    {
        _fixture.Form?.Invoke(() => _fixture.Form.Location = new System.Drawing.Point(100, 100));
        _fixture.BringToFront();
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
        var selectedTabContent = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ItemsListView",
        });
        Assert.True(selectedTabContent.Success, $"Selected tab content was not found: {selectedTabContent.ErrorMessage}");
        Assert.Single(selectedTabContent.Items!);
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
        // Find and click a checkbox to toggle it
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NotificationsCheckbox",
            ControlType = "CheckBox",
        });

        Assert.True(clickResult.Success, $"Click on checkbox failed: {clickResult.ErrorMessage}");
        var status = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusLabel",
            Name = "Notifications: False",
        });
        Assert.True(status.Success, $"Toggle state was not observed: {status.ErrorMessage}");
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
