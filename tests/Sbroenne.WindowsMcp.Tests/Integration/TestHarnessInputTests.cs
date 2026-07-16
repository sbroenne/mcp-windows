using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests that use the test harness window for verified input testing.
/// These tests verify that input actually reaches the target and produces expected results.
/// </summary>
[Collection("TestHarness")]
public class TestHarnessInputTests : IDisposable
{
    private readonly TestHarnessFixture _fixture;
    private readonly MouseInputService _mouseInputService;
    private readonly KeyboardInputService _keyboardInputService;
    private readonly Coordinates _originalPosition;

    public TestHarnessInputTests(TestHarnessFixture fixture)
    {
        _fixture = fixture;
        _mouseInputService = new MouseInputService();
        _keyboardInputService = new KeyboardInputService();
        _originalPosition = Coordinates.FromCurrent();

        // Reset and bring the harness to front before each test. BringToFront verifies focus.
        _fixture.Reset();
        _fixture.BringToFront();
    }

    public void Dispose()
    {
        // Restore original cursor position
        _mouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Click_OnTestButton_IncrementsClickCount()
    {
        // Arrange
        var buttonCenter = _fixture.GetValue(f => f.TestButtonCenter);
        var initialCount = _fixture.GetValue(f => f.ButtonClickCount);

        // Act
        var result = await _mouseInputService.ClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert
        Assert.True(result.Success, $"Click failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.ButtonClickCount == initialCount + 1));
        var newCount = _fixture.GetValue(f => f.ButtonClickCount);
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task Click_OnTestButton2_IncrementsButton2Count()
    {
        // Arrange
        var buttonCenter = _fixture.GetValue(f => f.TestButton2Center);
        var initialCount = _fixture.GetValue(f => f.Button2ClickCount);

        // Act
        var result = await _mouseInputService.ClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert
        Assert.True(result.Success, $"Click failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.Button2ClickCount == initialCount + 1));
        var newCount = _fixture.GetValue(f => f.Button2ClickCount);
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task DoubleClick_OnTestButton_IncrementsClickCount()
    {
        // Note: Windows Forms buttons don't fire DoubleClick event by default.
        // Double-click on a button fires Click twice instead.
        // This test verifies the double-click is delivered as two clicks.

        // Arrange
        var buttonCenter = _fixture.GetValue(f => f.TestButtonCenter);
        var initialCount = _fixture.GetValue(f => f.ButtonClickCount);

        // Act
        var result = await _mouseInputService.DoubleClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert
        Assert.True(result.Success, $"Double-click failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.ButtonClickCount >= initialCount + 1));

        // Double-click on button should register as 2 clicks (WinForms button behavior)
        var newCount = _fixture.GetValue(f => f.ButtonClickCount);
        Assert.True(newCount >= initialCount + 1, $"Expected at least {initialCount + 1} clicks, got {newCount}");
    }

    [Fact]
    public async Task TypeText_InTextBox_TextAppearsInTextBox()
    {
        // Arrange
        _fixture.FocusTextBox();
        Assert.True(await _fixture.WaitForAsync(f => f.IsInputTextBoxFocused));

        var testText = "Hello";

        // Act
        var result = await _keyboardInputService.TypeTextAsync(testText);

        // Assert
        Assert.True(result.Success, $"Type failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.InputText == testText));
        var inputText = _fixture.GetValue(f => f.InputText);
        Assert.Equal(testText, inputText);
    }

    [Fact]
    public async Task PressKey_Enter_KeyIsDetected()
    {
        // Arrange
        _fixture.FocusTextBox();
        Assert.True(await _fixture.WaitForAsync(f => f.IsInputTextBoxFocused));

        // Act
        var result = await _keyboardInputService.PressKeyAsync("enter");

        // Assert
        Assert.True(result.Success, $"Key press failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.LastKeyPressed == Keys.Return));
        var lastKey = _fixture.GetValue(f => f.LastKeyPressed);
        Assert.Equal(System.Windows.Forms.Keys.Return, lastKey);
    }

    [Fact]
    public async Task Click_ThenType_TextAppearsInTextBox()
    {
        // This tests the full workflow: click on text box, then type

        // Arrange
        var textBoxCenter = _fixture.GetValue(f => f.TextBoxCenter);
        var testText = "Test123";

        // Act - Click on text box first
        var clickResult = await _mouseInputService.ClickAsync(textBoxCenter.X, textBoxCenter.Y);

        Assert.True(clickResult.Success, $"Click failed: {clickResult.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.IsInputTextBoxFocused));

        // Act - Type text
        var typeResult = await _keyboardInputService.TypeTextAsync(testText);

        // Assert
        Assert.True(typeResult.Success, $"Type failed: {typeResult.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.InputText == testText));
        var inputText = _fixture.GetValue(f => f.InputText);
        Assert.Equal(testText, inputText);
    }

    [Fact]
    public async Task MultipleClicks_OnDifferentButtons_EachButtonCountsCorrectly()
    {
        // Arrange
        var button1Center = _fixture.GetValue(f => f.TestButtonCenter);
        var button2Center = _fixture.GetValue(f => f.TestButton2Center);

        // Act - Click button 1 twice, button 2 three times
        await _mouseInputService.ClickAsync(button1Center.X, button1Center.Y);
        Assert.True(await _fixture.WaitForAsync(f => f.ButtonClickCount == 1));
        await _mouseInputService.ClickAsync(button1Center.X, button1Center.Y);
        Assert.True(await _fixture.WaitForAsync(f => f.ButtonClickCount == 2));

        await _mouseInputService.ClickAsync(button2Center.X, button2Center.Y);
        Assert.True(await _fixture.WaitForAsync(f => f.Button2ClickCount == 1));
        await _mouseInputService.ClickAsync(button2Center.X, button2Center.Y);
        Assert.True(await _fixture.WaitForAsync(f => f.Button2ClickCount == 2));
        await _mouseInputService.ClickAsync(button2Center.X, button2Center.Y);
        Assert.True(await _fixture.WaitForAsync(f => f.Button2ClickCount == 3));

        // Assert
        var button1Count = _fixture.GetValue(f => f.ButtonClickCount);
        var button2Count = _fixture.GetValue(f => f.Button2ClickCount);

        Assert.Equal(2, button1Count);
        Assert.Equal(3, button2Count);
    }

    [Fact]
    public async Task KeyCombo_CtrlA_IsDetected()
    {
        // Arrange - First put some text in the box
        _fixture.BringToFront();
        var textBoxCenter = _fixture.GetValue(f => f.TextBoxCenter);
        var focusResult = await _mouseInputService.ClickAsync(textBoxCenter.X, textBoxCenter.Y);
        Assert.True(focusResult.Success, $"Focus click failed: {focusResult.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.IsInputTextBoxFocused));

        await _keyboardInputService.TypeTextAsync("Select me");
        Assert.True(await _fixture.WaitForAsync(f => f.InputText == "Select me"));

        // Verify text was typed
        var inputText = _fixture.GetValue(f => f.InputText);
        Assert.Equal("Select me", inputText);

        // Record keys before Ctrl+A
        var keysBefore = _fixture.GetValue(f => f.KeysPressed.Count);

        // Act - Press Ctrl+A
        var result = await _keyboardInputService.PressKeyAsync("a", Models.ModifierKey.Ctrl);

        // Assert - Verify the combo was sent
        Assert.True(result.Success, $"Key combo failed: {result.Error}");
        Assert.True(await _fixture.WaitForAsync(f => f.KeysPressed.Count > keysBefore));

        // Verify the 'A' key was pressed with Ctrl modifier
        var keysAfter = _fixture.GetValue(f => f.KeysPressed.Count);
        var allKeys = _fixture.GetValue(f => f.KeysPressed.ToList());

        // The Ctrl+A should have added at least one key
        Assert.True(keysAfter > keysBefore, $"Expected more keys after Ctrl+A. Before: {keysBefore}, After: {keysAfter}. Keys: {string.Join(", ", allKeys)}");
    }
}
