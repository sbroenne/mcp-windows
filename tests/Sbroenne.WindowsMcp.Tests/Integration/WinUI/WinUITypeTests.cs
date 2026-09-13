using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _output;

    public WinUITypeTests(ModernTestHarnessFixture fixture, ITestOutputHelper output)
    {
        _output = output;
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
        var handle = await FocusEditorAsync();
        using var keyboard = new KeyboardInputService();
        var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
        Assert.True(select.Success, select.Error);
        var typed = await keyboard.TypeTextAsync(text, handle);
        Assert.True(typed.Success, typed.Error);

        await AssertEditorTextAsync(text);
    }

    [Fact]
    public async Task KeyboardTyping_IndividuallySentCharactersArePreserved()
    {
        var handle = await FocusEditorAsync();
        using var keyboard = new KeyboardInputService();
        var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
        Assert.True(select.Success, select.Error);
        const string text = "Project: Aurora";
        foreach (var character in text)
        {
            var typed = await keyboard.TypeTextAsync(character.ToString(), handle);
            Assert.True(typed.Success, typed.Error);
        }

        await AssertEditorTextAsync(text);
    }

    [Fact]
    public async Task KeyboardPress_VirtualKeysAreDeliveredToEditor()
    {
        var handle = await FocusEditorAsync();
        using var keyboard = new KeyboardInputService();
        var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
        Assert.True(select.Success, select.Error);
        foreach (var key in new[] { "a", "b", "c" })
        {
            var pressed = await keyboard.PressKeyAsync(key, ModifierKey.None, 1, handle);
            Assert.True(pressed.Success, pressed.Error);
        }

        await AssertEditorTextAsync("abc");
    }

    private async Task<nint> FocusEditorAsync()
    {
        var navigate = await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        Assert.True(navigate.Success, navigate.ErrorMessage);
        var target = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EditorTextBox",
        });
        Assert.True(target.Success, target.ErrorMessage);
        var targetEditor = Assert.Single(target.Items!);
        var focus = await _automationService.FocusElementAsync(targetEditor.Id);
        Assert.True(focus.Success, focus.ErrorMessage);
        UIAutomationResult? focused = null;
        var editorHasFocus = await TestWait.RetryUntilAsync(
            attempt: async () => focused = await _automationService.GetFocusedElementAsync(),
            condition: () => focused is { Success: true, Items.Length: 1 }
                && focused.Items[0].Id == targetEditor.Id);
        Assert.True(editorHasFocus, $"Editor did not receive keyboard focus: {focused?.ErrorMessage}");

        return nint.Parse(_windowHandle, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task EditorTextReadback_MatchesSemanticTextEntry()
    {
        var navigate = await _automationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        Assert.True(navigate.Success, navigate.ErrorMessage);
        const string text = "Read-back probe 123";
        var typed = await _automationService.ObserveAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "EditorTextBox",
            },
            text,
            clearFirst: true);
        Assert.True(typed.Success, typed.ErrorMessage);
        await AssertEditorTextAsync(text);
    }

    private async Task AssertEditorTextAsync(string text)
    {
        var found = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "EditorTextBox",
        });
        Assert.True(found.Success, found.ErrorMessage);
        var editor = Assert.Single(found.Items!);
        UIAutomationResult? read = null;
        var matched = await TestWait.RetryUntilAsync(
            attempt: async () =>
            {
                read = await _automationService.GetTextAsync(editor.Id, _windowHandle, false);
                _output.WriteLine($"Editor read: {System.Text.Json.JsonSerializer.Serialize(read)}");
            },
            condition: () => read is { Success: true }
                && read.Text?.ReplaceLineEndings("\n") == text.ReplaceLineEndings("\n"));
        if (!matched)
        {
            var counts = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "CharacterCountText",
            });
            var focused = await _automationService.GetFocusedElementAsync();
            _output.WriteLine($"Character count: {System.Text.Json.JsonSerializer.Serialize(counts)}");
            _output.WriteLine($"Focus after typing: {System.Text.Json.JsonSerializer.Serialize(focused)}");
        }

        Assert.NotNull(read);
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(text.ReplaceLineEndings("\n"), read.Text?.ReplaceLineEndings("\n"));
    }
}
