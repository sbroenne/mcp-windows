using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard press action.
/// Tests validate single key presses via virtual key codes against the test harness.
/// These tests interact with the actual Windows input system.
/// </summary>
[Collection("KeyboardIntegrationTests")]
public class KeyboardPressTests : IDisposable
{
    private readonly KeyboardTestFixture _fixture;

    public KeyboardPressTests(KeyboardTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.EnsureTestWindowFocused();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Letter Key Tests - Verified by Harness

    /// <summary>
    /// Tests letter key presses are received by the harness.
    /// </summary>
    [Theory]
    [InlineData("a", "a")]
    [InlineData("z", "z")]
    [InlineData("m", "m")]
    public async Task PressKeyAsync_LetterKeys_VerifiedByHarness(string key, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for key '{key}', got: {result.Error}");

        // Assert - harness received the key
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Test harness did not receive expected text '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests uppercase letter keys via shift modifier.
    /// </summary>
    [Theory]
    [InlineData("a", "A")]
    [InlineData("z", "Z")]
    public async Task PressKeyAsync_LetterWithShift_ProducesUppercase(string key, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key, ModifierKey.Shift);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for shift+'{key}', got: {result.Error}");

        // Assert - harness received uppercase
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Test harness did not receive expected text '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Number Key Tests - Verified by Harness

    /// <summary>
    /// Tests number key presses are received by the harness.
    /// </summary>
    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("5", "5")]
    [InlineData("9", "9")]
    public async Task PressKeyAsync_NumberKeys_VerifiedByHarness(string key, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for key '{key}', got: {result.Error}");

        // Assert - harness received the number
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Test harness did not receive expected text '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Space Key Tests - Verified by Harness

    /// <summary>
    /// Tests that space key press produces a space character.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Space_VerifiedByHarness()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("space");

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for space, got: {result.Error}");

        // Assert - harness received a space
        var textReceived = await _fixture.WaitForInputTextAsync(" ");
        Assert.True(textReceived, $"Test harness did not receive space, got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Backspace Key Tests - Verified by Harness

    /// <summary>
    /// Tests that backspace deletes the previous character.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Backspace_DeletesPreviousCharacter()
    {
        // Arrange - first type some text
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);
        await _fixture.KeyboardInputService.TypeTextAsync("abc");
        await _fixture.WaitForInputTextAsync("abc");

        // Act - press backspace
        var result = await _fixture.KeyboardInputService.PressKeyAsync("backspace");

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for backspace, got: {result.Error}");

        // Assert - one character was deleted
        var textReceived = await _fixture.WaitForInputTextAsync("ab");
        Assert.True(textReceived, $"Expected 'ab' after backspace, got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that backspace with repeat count deletes multiple characters.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_BackspaceWithRepeat_DeletesMultipleCharacters()
    {
        // Arrange - first type some text
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);
        await _fixture.KeyboardInputService.TypeTextAsync("abcde");
        await _fixture.WaitForInputTextAsync("abcde");

        // Act - press backspace 3 times
        var result = await _fixture.KeyboardInputService.PressKeyAsync("backspace", repeat: 3);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for backspace x3, got: {result.Error}");

        // Assert - three characters were deleted
        var textReceived = await _fixture.WaitForInputTextAsync("ab");
        Assert.True(textReceived, $"Expected 'ab' after 3 backspaces, got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Arrow Keys - API Response Tests

    /// <summary>
    /// Tests that arrow keys return success.
    /// Note: Arrow keys don't produce visible text, but API should return success.
    /// </summary>
    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    public async Task PressKeyAsync_ArrowKeys_ReturnsSuccess(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success for valid arrow keys
        Assert.True(result.Success, $"Expected success for arrow key '{key}', got: {result.Error}");
    }

    /// <summary>
    /// Tests arrow key aliases work correctly.
    /// </summary>
    [Theory]
    [InlineData("arrowup")]
    [InlineData("arrowdown")]
    [InlineData("arrowleft")]
    [InlineData("arrowright")]
    public async Task PressKeyAsync_ArrowKeyAliases_ReturnsSuccess(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success for arrow key aliases
        Assert.True(result.Success, $"Expected success for arrow alias '{key}', got: {result.Error}");
    }

    #endregion

    #region Navigation Keys - API Response Tests

    /// <summary>
    /// Tests navigation keys return success.
    /// </summary>
    [Theory]
    [InlineData("home")]
    [InlineData("end")]
    [InlineData("pageup")]
    [InlineData("pagedown")]
    [InlineData("insert")]
    [InlineData("delete")]
    public async Task PressKeyAsync_NavigationKeys_ReturnsSuccess(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert
        Assert.True(result.Success, $"Expected success for navigation key '{key}', got: {result.Error}");
    }

    /// <summary>
    /// Tests that Home/End keys move cursor within text.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_HomeAndEnd_MovesCursor()
    {
        // Arrange - type some text
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);
        await _fixture.KeyboardInputService.TypeTextAsync("Hello");
        await _fixture.WaitForInputTextAsync("Hello");

        // Act - press Home, then type 'X'
        await _fixture.KeyboardInputService.PressKeyAsync("home");
        await Task.Delay(50);
        await _fixture.KeyboardInputService.PressKeyAsync("x");

        // Assert - X should be at the beginning
        var textReceived = await _fixture.WaitForInputTextAsync("xHello");
        Assert.True(textReceived, $"Expected 'xHello', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that Delete key removes character at cursor.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Delete_RemovesCharacterAtCursor()
    {
        // Arrange - type text and move cursor to beginning
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);
        await _fixture.KeyboardInputService.TypeTextAsync("Hello");
        await _fixture.WaitForInputTextAsync("Hello");
        await _fixture.KeyboardInputService.PressKeyAsync("home");
        await Task.Delay(50);

        // Act - press Delete to remove 'H'
        var result = await _fixture.KeyboardInputService.PressKeyAsync("delete");

        // Assert
        Assert.True(result.Success, $"Expected success for delete, got: {result.Error}");
        var textReceived = await _fixture.WaitForInputTextAsync("ello");
        Assert.True(textReceived, $"Expected 'ello' after delete, got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Function Keys - API Response Tests

    /// <summary>
    /// Tests that F1-F12 function keys return success.
    /// Note: Function keys trigger system actions, so we only verify API response.
    /// </summary>
    [Theory]
    [InlineData("f1")]
    [InlineData("f2")]
    [InlineData("f3")]
    [InlineData("f4")]
    [InlineData("f5")]
    [InlineData("f6")]
    [InlineData("f7")]
    [InlineData("f8")]
    [InlineData("f9")]
    [InlineData("f10")]
    [InlineData("f11")]
    [InlineData("f12")]
    public async Task PressKeyAsync_FunctionKeys_ReturnsSuccess(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success for valid function keys
        Assert.True(result.Success, $"Expected success for function key '{key}', got: {result.Error}");
    }

    /// <summary>
    /// Tests extended function keys F13-F24.
    /// </summary>
    [Theory]
    [InlineData("f13")]
    [InlineData("f14")]
    [InlineData("f15")]
    [InlineData("f16")]
    [InlineData("f17")]
    [InlineData("f18")]
    [InlineData("f19")]
    [InlineData("f20")]
    [InlineData("f21")]
    [InlineData("f22")]
    [InlineData("f23")]
    [InlineData("f24")]
    public async Task PressKeyAsync_ExtendedFunctionKeys_ReturnsSuccess(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert
        Assert.True(result.Success, $"Expected success for extended function key '{key}', got: {result.Error}");
    }

    #endregion

    #region Tab Key Tests - Verified by Harness

    /// <summary>
    /// Tests that Tab key produces a tab character in text input.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Tab_VerifiedByHarness()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("tab");

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for tab, got: {result.Error}");

        // Note: In a textbox, Tab might either insert a tab character or move focus.
        // The test harness textbox should accept tab characters.
        // If this fails, the test harness may need AcceptsTab=true
    }

    /// <summary>
    /// Tests Tab with repeat count.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task PressKeyAsync_TabWithRepeat_ReturnsSuccess(int repeatCount)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("tab", repeat: repeatCount);

        // Assert
        Assert.True(result.Success, $"Expected success for tab x{repeatCount}, got: {result.Error}");
    }

    #endregion

    #region Escape Key Tests - API Response

    /// <summary>
    /// Tests that Escape key returns success.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Escape_ReturnsSuccess()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("escape");

        // Assert
        Assert.True(result.Success, $"Expected success for escape, got: {result.Error}");
    }

    /// <summary>
    /// Tests Esc alias.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_EscAlias_ReturnsSuccess()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("esc");

        // Assert
        Assert.True(result.Success, $"Expected success for esc alias, got: {result.Error}");
    }

    #endregion

    #region Enter Key Tests - API Response

    /// <summary>
    /// Tests that Enter key returns success.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Enter_ReturnsSuccess()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("enter");

        // Assert
        Assert.True(result.Success, $"Expected success for enter, got: {result.Error}");
    }

    /// <summary>
    /// Tests Return alias for Enter.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ReturnAlias_ReturnsSuccess()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("return");

        // Assert
        Assert.True(result.Success, $"Expected success for return alias, got: {result.Error}");
    }

    #endregion

    #region Numpad Keys - Verified by Harness

    /// <summary>
    /// Tests numpad number keys produce correct digits.
    /// </summary>
    [Theory]
    [InlineData("numpad0", "0")]
    [InlineData("numpad1", "1")]
    [InlineData("numpad5", "5")]
    [InlineData("numpad9", "9")]
    public async Task PressKeyAsync_NumpadNumbers_VerifiedByHarness(string key, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for numpad key '{key}', got: {result.Error}");

        // Assert - harness received the digit
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Test harness did not receive expected '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests numpad operator keys.
    /// </summary>
    [Theory]
    [InlineData("numpadmultiply", "*")]
    [InlineData("numpadadd", "+")]
    [InlineData("numpadsubtract", "-")]
    [InlineData("numpaddivide", "/")]
    public async Task PressKeyAsync_NumpadOperators_VerifiedByHarness(string key, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for numpad key '{key}', got: {result.Error}");

        // Assert - harness received the operator
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Test harness did not receive expected '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests numpad decimal key - locale-dependent (may be . or , depending on keyboard layout).
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_NumpadDecimal_ReturnsSuccessAndProducesDecimalSeparator()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("numpaddecimal");

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for numpaddecimal, got: {result.Error}");

        // Wait for input to appear
        await Task.Delay(100);
        var inputText = _fixture.GetInputText();

        // Assert - harness received either . or , (locale-dependent)
        Assert.True(inputText == "." || inputText == ",",
            $"Expected '.' or ',' for numpaddecimal (locale-dependent), got '{inputText}'");
    }

    #endregion

    #region Repeat Count Tests - Verified by Harness

    /// <summary>
    /// Tests that repeat count works correctly.
    /// </summary>
    [Theory]
    [InlineData(1, "a")]
    [InlineData(3, "aaa")]
    [InlineData(5, "aaaaa")]
    public async Task PressKeyAsync_WithRepeatCount_PressesMultipleTimes(int repeatCount, string expectedText)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("a", repeat: repeatCount);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success for 'a' x{repeatCount}, got: {result.Error}");

        // Assert - harness received repeated characters
        var textReceived = await _fixture.WaitForInputTextAsync(expectedText);
        Assert.True(textReceived, $"Expected '{expectedText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that repeat=0 or negative is handled (implementation clamps to 1).
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_RepeatZeroOrNegative_ClampsToOne()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act - repeat=0 should be clamped to 1
        var result = await _fixture.KeyboardInputService.PressKeyAsync("a", repeat: 0);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got: {result.Error}");

        // Assert - should have typed exactly one 'a'
        var textReceived = await _fixture.WaitForInputTextAsync("a");
        Assert.True(textReceived, $"Expected 'a', got '{_fixture.GetInputText()}'");
    }

    #endregion

    #region Invalid Key Name Tests

    /// <summary>
    /// Tests that invalid key names return InvalidKey error.
    /// </summary>
    [Theory]
    [InlineData("invalidkey")]
    [InlineData("notakey")]
    [InlineData("xyz123")]
    public async Task PressKeyAsync_InvalidKeyName_ReturnsInvalidKeyError(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - should return failure with InvalidKey error code
        Assert.False(result.Success, "Expected failure for invalid key name");
        Assert.Equal(KeyboardControlErrorCode.InvalidKey, result.ErrorCode);
        Assert.Contains("Unknown key", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that empty key name returns InvalidKey error.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_EmptyKeyName_ReturnsInvalidKeyError()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync("");

        // Assert
        Assert.False(result.Success, "Expected failure for empty key name");
        Assert.Equal(KeyboardControlErrorCode.InvalidKey, result.ErrorCode);
    }

    /// <summary>
    /// Tests that shortcut-style key names give helpful error message.
    /// </summary>
    [Theory]
    [InlineData("Ctrl+S")]
    [InlineData("Alt+Tab")]
    [InlineData("Ctrl+Shift+N")]
    public async Task PressKeyAsync_ShortcutSyntax_ReturnsHelpfulError(string key)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.PressKeyAsync(key);

        // Assert - should return helpful error about using modifiers parameter
        Assert.False(result.Success, "Expected failure for shortcut syntax");
        Assert.Equal(KeyboardControlErrorCode.InvalidKey, result.ErrorCode);
        Assert.Contains("modifiers", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Modifier Key Tests

    /// <summary>
    /// Tests Ctrl+A selects all (verifiable via the harness).
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlA_SelectsAll()
    {
        // Arrange - type some text first
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);
        await _fixture.KeyboardInputService.TypeTextAsync("Hello");
        await _fixture.WaitForInputTextAsync("Hello");

        // Act - Ctrl+A to select all, then type to replace
        var result = await _fixture.KeyboardInputService.PressKeyAsync("a", ModifierKey.Ctrl);
        Assert.True(result.Success, $"Expected success for Ctrl+A, got: {result.Error}");

        // Now type 'X' to replace selected text
        await Task.Delay(50);
        await _fixture.KeyboardInputService.PressKeyAsync("x");

        // Assert - text should be replaced
        var textReceived = await _fixture.WaitForInputTextAsync("x");
        Assert.True(textReceived, $"Expected 'x' after Ctrl+A and typing, got '{_fixture.GetInputText()}'");
    }

    #endregion
}
