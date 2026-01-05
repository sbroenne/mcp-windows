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
/// Integration tests for UIWaitTool - waiting for UI element state changes.
/// Tests real UI Automation wait operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIWaitToolIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIWaitToolIntegrationTests(UITestHarnessFixture fixture)
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
    public async Task WaitFor_ExistingElement_ReturnsImmediately()
    {
        // Act - Wait for the Submit button that already exists
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            },
            timeoutMs: 5000);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"WaitFor failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "WaitFor should return quickly for existing elements");
    }

    [Fact]
    public async Task WaitFor_NonExistentElement_TimesOut()
    {
        // Act - Wait for an element that doesn't exist with short timeout
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "This Button Does Not Exist",
                ControlType = "Button",
            },
            timeoutMs: 1000);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLowerInvariant() ?? string.Empty);
    }

    [Fact]
    public async Task WaitFor_MultipleElements_ReturnsFirst()
    {
        // Act - Wait for any button (multiple exist)
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "Button",
            },
            timeoutMs: 5000);

        // Assert
        Assert.True(result.Success, $"WaitFor failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [SkippableFact]
    public async Task Focus_TextBox_SetsFocus()
    {
        // Find the text box first
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.True(findResult.Items!.Length > 0, "Expected to find at least one Edit control");
        var textBoxId = findResult.Items![0].Id;
        Assert.NotNull(textBoxId);

        // Element IDs are now short numeric identifiers
        Assert.True(int.TryParse(textBoxId, out _), $"Element ID format unexpected: {textBoxId}");

        // Act - try to focus using the element ID
        var focusResult = await _automationService.FocusElementAsync(textBoxId);

        // Skip if elevation prevents focus (common in CI environments)
        Skip.If(focusResult.ErrorMessage?.Contains("elevated", StringComparison.OrdinalIgnoreCase) == true,
            "Focus requires same elevation level - skipping in CI environment");

        // Assert
        Assert.True(focusResult.Success, $"Focus failed: {focusResult.ErrorMessage}. ElementId was: {textBoxId}");
    }
}
