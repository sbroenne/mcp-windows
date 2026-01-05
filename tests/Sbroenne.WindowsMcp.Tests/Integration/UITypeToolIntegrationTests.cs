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
/// Integration tests for UITypeTool - typing text into UI elements.
/// Tests real UI Automation text input operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UITypeToolIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UITypeToolIntegrationTests(UITestHarnessFixture fixture)
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
    public async Task FindAndType_InTextBox_EntersText()
    {
        // Arrange
        var testText = "Hello from UI Automation";

        // Act - Use automationId to target the specific UsernameInput textbox
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);

        // Assert
        Assert.True(typeResult.Success, $"FindAndType failed: {typeResult.ErrorMessage}");
        await Task.Delay(100); // Allow UI to update

        var actualText = _fixture.Form?.UsernameText ?? string.Empty;
        Assert.Equal(testText, actualText);
    }

    [Fact]
    public async Task FindAndType_WithNoSelector_TypesIntoFirstTextControl()
    {
        // Arrange
        var testText = "Fallback typing works";

        // Act - Rely on default Document/Edit fallback when no selector is provided
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
            },
            text: testText,
            clearFirst: true);

        // Assert
        Assert.True(typeResult.Success, $"FindAndType failed: {typeResult.ErrorMessage}");
        Assert.NotNull(typeResult.Items);
        var typedElementId = typeResult.Items![0].Id;

        var getTextResult = await _automationService.GetTextAsync(
            elementId: typedElementId,
            windowHandle: _windowHandle,
            includeChildren: false);

        Assert.True(getTextResult.Success, $"GetText failed: {getTextResult.ErrorMessage}");
        Assert.Equal(testText, getTextResult.Text);
    }

    [Fact]
    public async Task FindAndType_ClearFirst_ReplacesExistingText()
    {
        // Arrange - type initial text
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Initial text",
            clearFirst: true);
        await Task.Delay(50);

        // Act - type new text with clearFirst=true
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Replaced text",
            clearFirst: true);

        // Assert
        Assert.True(typeResult.Success, $"FindAndType failed: {typeResult.ErrorMessage}");
        await Task.Delay(50);
        var actualText = _fixture.Form?.UsernameText ?? string.Empty;
        Assert.Equal("Replaced text", actualText);
    }

    [Fact]
    public async Task FindAndType_AppendText_TypesText()
    {
        // Arrange - type initial text
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Initial",
            clearFirst: true);
        await Task.Delay(50);

        // Act - type more text with clearFirst=false
        // Note: clearFirst=false sends text to focused element without clearing
        // The exact behavior depends on the element's cursor position
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Appended",
            clearFirst: false);

        // Assert - verify typing succeeded
        Assert.True(typeResult.Success, $"FindAndType failed: {typeResult.ErrorMessage}");
        await Task.Delay(50);
        var actualText = _fixture.Form?.UsernameText ?? string.Empty;
        // With clearFirst=false, text should contain "Appended"
        // It may or may not contain "Initial" depending on element behavior
        Assert.Contains("Appended", actualText);
    }
}
