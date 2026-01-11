using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Integration tests for UI Automation wait operations against WinUI 3 modern app harness.
/// Tests verify that waiting for UI element state changes works correctly with modern WinUI 3 controls.
/// </summary>
[Collection("ModernTestHarness")]
public sealed class WinUIWaitTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUIWaitTests(ModernTestHarnessFixture fixture)
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
    public async Task WaitFor_ExistingElement_ReturnsImmediately()
    {
        // Act - Wait for the navigation that already exists
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "MainNavView",
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
                AutomationId = "ThisElementDoesNotExist12345",
            },
            timeoutMs: 1000);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLowerInvariant() ?? string.Empty);
    }

    [Fact]
    public async Task WaitFor_MultipleButtons_ReturnsFirst()
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
        // Navigate to Form Controls page where text boxes exist
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(300);

        // Find a text box - use UsernameInput which is more reliable
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.True(findResult.Items!.Length > 0, "Expected to find the UsernameInput control");
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
