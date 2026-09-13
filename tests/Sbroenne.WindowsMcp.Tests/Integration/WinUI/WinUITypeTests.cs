using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
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
[Trait("Category", "RequiresDesktop")]
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
    public async Task FindAndType_InUsernameTextBox_Succeeds()
    {
        // Navigate to Form Controls page
        await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act - Type text into the username field
        var testText = "TestUser123";
        var result = await _automationService.ObserveAndTypeAsync(
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
        await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        await Task.Delay(200);

        // Act - Type some text with multiple words
        var testText = "Hello world this is a test of the editor";
        var result = await _automationService.ObserveAndTypeAsync(
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
        await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Type initial text
        await _automationService.ObserveAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
            },
            text: "InitialText",
            clearFirst: true);
        await Task.Delay(100);

        // Type with clear option to replace
        var result = await _automationService.ObserveAndTypeAsync(
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

    [Theory]
    [InlineData("Project: Aurora")]
    [InlineData("Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan")]
    [InlineData("Project: Aurora\nStatus: Draft\nOwner: Taylor")]
    public async Task KeyboardTyping_PreservesEveryCharacterInModernEditor(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var navigate = await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        Assert.True(navigate.Success, navigate.ErrorMessage);
        var focus = await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EditorTextBox",
        });
        Assert.True(focus.Success, focus.ErrorMessage);

        using var keyboard = new KeyboardInputService();
        var handle = nint.Parse(_windowHandle, System.Globalization.CultureInfo.InvariantCulture);
        var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
        Assert.True(select.Success, select.Error);
        var typed = await keyboard.TypeTextAsync(text, handle);
        Assert.True(typed.Success, typed.Error);

        var found = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EditorTextBox",
        });
        Assert.True(found.Success, found.ErrorMessage);
        var editor = Assert.Single(found.Items!);
        var read = await _automationService.GetTextAsync(editor.Id, _windowHandle, false);
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(text.ReplaceLineEndings("\n"), read.Text?.ReplaceLineEndings("\n"));
    }
}
