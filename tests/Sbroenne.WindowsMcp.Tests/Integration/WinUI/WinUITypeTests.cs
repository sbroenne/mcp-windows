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
/// Integration tests for UI Automation type operations against WinUI 3 modern app harness.
/// Tests verify that text entry via MCP tools works correctly with modern WinUI 3 controls.
/// </summary>
[Collection("ModernTestHarness")]
public sealed class WinUITypeTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUITypeTests(ModernTestHarnessFixture fixture)
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

    [Fact]
    public async Task FindAndType_InUsernameTextBox_Succeeds()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act - Type text into the username field
        var testText = "TestUser123";
        var result = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: testText,
            clearFirst: true);

        // Assert
        Assert.True(result.Success, $"Type failed: {result.ErrorMessage}");
    }

    [Fact]
    public async Task FindAndType_InEditorTextBox_Succeeds()
    {
        // Navigate to Editor page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        await Task.Delay(200);

        // Act - Type some text with multiple words
        var testText = "Hello world this is a test of the editor";
        var result = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "EditorTextBox",
            },
            text: testText,
            clearFirst: true);

        // Assert
        Assert.True(result.Success, $"Type failed: {result.ErrorMessage}");
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

    [Fact]
    public async Task FindAndType_ClearAndReplace_WorksCorrectly()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Type initial text
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "InitialText",
            clearFirst: true);
        await Task.Delay(100);

        // Type with clear option to replace
        var result = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "ReplacedText",
            clearFirst: true);

        // Assert
        Assert.True(result.Success, $"Type with clear failed: {result.ErrorMessage}");
    }
}
