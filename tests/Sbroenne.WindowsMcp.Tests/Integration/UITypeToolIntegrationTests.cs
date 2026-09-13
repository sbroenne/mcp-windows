using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
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
        var typeResult = await _automationService.ObserveAndTypeAsync(
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
    public async Task Type_WithoutObservedId_DoesNotChooseFirstTextControl()
    {
        var before = _fixture.Form?.UsernameText;
        var result = await Sbroenne.WindowsMcp.Automation.Tools.UITypeTool.ExecuteAsync(
            _windowHandle, "must not be typed", "", true, false, "full", false, "auto", CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Equal(before, _fixture.Form?.UsernameText);
    }

    [Fact]
    public async Task FindAndType_RequireUnique_DoesNotFallThroughAfterAmbiguousEditSearch()
    {
        var typeResult = await _automationService.ObserveAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                RequireUnique = true,
            },
            text: "must-not-be-typed",
            clearFirst: true,
            inputMode: "value");

        Assert.False(typeResult.Success);
        Assert.Equal(UIAutomationErrorType.MultipleMatches, typeResult.ErrorType);
        Assert.Equal(string.Empty, _fixture.Form?.UsernameText);
        Assert.Equal(string.Empty, _fixture.Form?.PasswordText);
    }

    [Fact]
    public async Task FindAndType_ClearFirst_ReplacesExistingText()
    {
        // Arrange - type initial text
        await _automationService.ObserveAndTypeAsync(
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
        var typeResult = await _automationService.ObserveAndTypeAsync(
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
        await _automationService.ObserveAndTypeAsync(
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
        var typeResult = await _automationService.ObserveAndTypeAsync(
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
