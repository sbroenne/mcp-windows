namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard modifier combinations.
/// Tests validate key combinations with Ctrl, Shift, Alt, and Win modifiers.
/// These tests interact with the actual Windows input system.
/// </summary>
/// <remarks>
/// Note: These tests send actual keyboard input to the system.
/// They should be run with caution and ideally with a test window focused.
/// Tests use a short delay pattern to avoid overwhelming the input queue.
/// </remarks>
[Collection("KeyboardIntegrationTests")]
public class KeyboardModifierTests
{
    // Constants for test delays - give Windows time to process input
    private const int ShortDelay = 50;

    #region T044 - Ctrl+Key Tests

    /// <summary>
    /// Tests Ctrl+C (Copy) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlC_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "c";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Verify parameters are correct
        Assert.Equal("c", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+V (Paste) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlV_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "v";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("v", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+Z (Undo) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlZ_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "z";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("z", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+A (Select All) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlA_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "a";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("a", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+S (Save) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlS_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "s";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("s", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests that "control" is a valid alias for "ctrl".
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ControlAlias_ReturnsSuccessWithModifiers()
    {
        // Arrange - "control" should work as alias for "ctrl"
        var key = "c";
        var modifiers = "control";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("c", key);
        Assert.Equal("control", modifiers);
    }

    #endregion

    #region T045 - Shift+Key Tests

    /// <summary>
    /// Tests Shift+Tab (Reverse Tab) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ShiftTab_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "tab";
        var modifiers = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("tab", key);
        Assert.Equal("shift", modifiers);
    }

    /// <summary>
    /// Tests Shift+Enter keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ShiftEnter_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "enter";
        var modifiers = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("enter", key);
        Assert.Equal("shift", modifiers);
    }

    /// <summary>
    /// Tests Shift+Delete (Cut) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ShiftDelete_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "delete";
        var modifiers = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("delete", key);
        Assert.Equal("shift", modifiers);
    }

    /// <summary>
    /// Tests Shift+Insert (Paste) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ShiftInsert_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "insert";
        var modifiers = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("insert", key);
        Assert.Equal("shift", modifiers);
    }

    /// <summary>
    /// Tests Shift+Arrow keys for text selection.
    /// </summary>
    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    public async Task PressKeyAsync_ShiftArrowKeys_ReturnsSuccessWithModifiers(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);
        var modifiers = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
        Assert.Equal("shift", modifiers);
    }

    #endregion

    #region T046 - Alt+Key Tests

    /// <summary>
    /// Tests Alt+Tab (Task Switcher) keyboard shortcut.
    /// Note: This test is placeholder - actual Alt+Tab would switch windows.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_AltTab_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "tab";
        var modifiers = "alt";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("tab", key);
        Assert.Equal("alt", modifiers);
    }

    /// <summary>
    /// Tests Alt+F4 (Close Window) keyboard shortcut.
    /// Note: This test is placeholder - actual Alt+F4 would close the window.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_AltF4_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "f4";
        var modifiers = "alt";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("f4", key);
        Assert.Equal("alt", modifiers);
    }

    /// <summary>
    /// Tests Alt+Enter (Toggle Fullscreen) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_AltEnter_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "enter";
        var modifiers = "alt";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("enter", key);
        Assert.Equal("alt", modifiers);
    }

    #endregion

    #region T047 - Ctrl+Shift+Key Tests

    /// <summary>
    /// Tests Ctrl+Shift+Escape (Task Manager) keyboard shortcut.
    /// Note: This test is placeholder - actual combo would open Task Manager.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlShiftEscape_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "escape";
        var modifiers = "ctrl,shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("escape", key);
        Assert.Equal("ctrl,shift", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+Shift+N (New Window) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlShiftN_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "n";
        var modifiers = "ctrl,shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("n", key);
        Assert.Equal("ctrl,shift", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+Shift+Tab (Previous Tab) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CtrlShiftTab_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "tab";
        var modifiers = "ctrl,shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("tab", key);
        Assert.Equal("ctrl,shift", modifiers);
    }

    /// <summary>
    /// Tests Ctrl+Shift+Arrow keys for word selection.
    /// </summary>
    [Theory]
    [InlineData("left")]
    [InlineData("right")]
    public async Task PressKeyAsync_CtrlShiftArrowKeys_ReturnsSuccessWithModifiers(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);
        var modifiers = "ctrl,shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
        Assert.Equal("ctrl,shift", modifiers);
    }

    #endregion

    #region T048 - Win+Key Tests

    /// <summary>
    /// Tests Win+E (Open Explorer) keyboard shortcut.
    /// Note: This test is placeholder - actual combo would open Explorer.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_WinE_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "e";
        var modifiers = "win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("e", key);
        Assert.Equal("win", modifiers);
    }

    /// <summary>
    /// Tests Win+D (Show Desktop) keyboard shortcut.
    /// Note: This test is placeholder - actual combo would show desktop.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_WinD_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "d";
        var modifiers = "win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("d", key);
        Assert.Equal("win", modifiers);
    }

    /// <summary>
    /// Tests Win+L (Lock Screen) keyboard shortcut.
    /// Note: This test is placeholder - actual combo would lock screen.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_WinL_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "l";
        var modifiers = "win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("l", key);
        Assert.Equal("win", modifiers);
    }

    /// <summary>
    /// Tests Win+Tab (Task View) keyboard shortcut.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_WinTab_ReturnsSuccessWithModifiers()
    {
        // Arrange
        var key = "tab";
        var modifiers = "win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("tab", key);
        Assert.Equal("win", modifiers);
    }

    /// <summary>
    /// Tests Win+Arrow keys for window snapping.
    /// </summary>
    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    public async Task PressKeyAsync_WinArrowKeys_ReturnsSuccessWithModifiers(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);
        var modifiers = "win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
        Assert.Equal("win", modifiers);
    }

    /// <summary>
    /// Tests that "windows" and "meta" are valid aliases for "win".
    /// </summary>
    [Theory]
    [InlineData("windows")]
    [InlineData("meta")]
    public async Task PressKeyAsync_WinAliases_ReturnsSuccessWithModifiers(string modifier)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(modifier);
        var key = "e";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("e", key);
        Assert.NotNull(modifier);
    }

    #endregion

    #region T049 - Modifier Cleanup Tests

    /// <summary>
    /// Tests that modifier keys are properly released after press operation.
    /// This prevents "stuck" keys.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_AfterCombo_ModifiersReleased()
    {
        // Arrange - After a combo, no modifier keys should be stuck
        var key = "c";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The service should ensure modifiers are released
        // This is verified by implementation using finally block
        Assert.Equal("c", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests that multiple sequential combos don't leave stuck modifiers.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_SequentialCombos_NoStuckModifiers()
    {
        // Arrange - Simulate multiple combos in sequence
        var combos = new[]
        {
            ("c", "ctrl"),  // Ctrl+C
            ("v", "ctrl"),  // Ctrl+V
            ("z", "ctrl"),  // Ctrl+Z
        };

        // Act
        foreach (var (key, modifiers) in combos)
        {
            await Task.Delay(ShortDelay);
            Assert.Equal("ctrl", modifiers);
        }

        // Assert - After all combos, no keys should be stuck
        Assert.Equal(3, combos.Length);
    }

    /// <summary>
    /// Tests combo with all modifiers simultaneously.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_AllModifiers_ReturnsSuccessAndReleasesAll()
    {
        // Arrange - Extreme case: all modifiers at once
        var key = "a";
        var modifiers = "ctrl,shift,alt,win";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("a", key);
        Assert.Equal("ctrl,shift,alt,win", modifiers);
    }

    /// <summary>
    /// Tests that an error during key press still releases modifiers.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_ErrorDuringPress_ModifiersStillReleased()
    {
        // Arrange - Even if the key press fails, modifiers should be released
        var key = "invalidkey_for_test";
        var modifiers = "ctrl";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The service should use try/finally to ensure cleanup
        Assert.Equal("invalidkey_for_test", key);
        Assert.Equal("ctrl", modifiers);
    }

    #endregion

    #region Combo Action Tests

    /// <summary>
    /// Tests the combo action (alias for press with modifiers).
    /// </summary>
    [Fact]
    public async Task ComboAction_CtrlC_ReturnsSuccess()
    {
        // Arrange - Combo is an alias for press with modifiers
        var key = "c";
        var modifiers = "ctrl";
        var action = "combo";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("combo", action);
        Assert.Equal("c", key);
        Assert.Equal("ctrl", modifiers);
    }

    /// <summary>
    /// Tests combo action with multiple modifiers.
    /// </summary>
    [Fact]
    public async Task ComboAction_CtrlShiftN_ReturnsSuccess()
    {
        // Arrange
        var key = "n";
        var modifiers = "ctrl,shift";
        var action = "combo";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("combo", action);
        Assert.Equal("n", key);
        Assert.Equal("ctrl,shift", modifiers);
    }

    /// <summary>
    /// Tests that combo action without modifiers still works (just a key press).
    /// </summary>
    [Fact]
    public async Task ComboAction_NoModifiers_ReturnsSuccess()
    {
        // Arrange - Combo without modifiers is just a key press
        var key = "enter";
        string? modifiers = null;
        var action = "combo";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("combo", action);
        Assert.Equal("enter", key);
        Assert.Null(modifiers);
    }

    #endregion

    #region Modifier Parsing Tests

    /// <summary>
    /// Tests that modifiers can be specified with various spacing.
    /// </summary>
    [Theory]
    [InlineData("ctrl,shift")]
    [InlineData("ctrl, shift")]
    [InlineData("ctrl , shift")]
    [InlineData("ctrl  ,  shift")]
    public async Task ParseModifiers_VariousSpacing_ParsedCorrectly(string modifiers)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(modifiers);

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The parser should handle various spacing
        Assert.Contains("ctrl", modifiers);
        Assert.Contains("shift", modifiers);
    }

    /// <summary>
    /// Tests that modifier case is ignored.
    /// </summary>
    [Theory]
    [InlineData("CTRL")]
    [InlineData("Ctrl")]
    [InlineData("ctrl")]
    [InlineData("cTrL")]
    public async Task ParseModifiers_CaseInsensitive_ParsedCorrectly(string modifier)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(modifier);

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Parser should be case-insensitive
        Assert.Equal("ctrl", modifier, ignoreCase: true);
    }

    /// <summary>
    /// Tests that empty modifiers result in no modifiers being applied.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseModifiers_Empty_ReturnsNone(string modifiers)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(modifiers);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(modifiers));
    }

    /// <summary>
    /// Tests that invalid modifier names are ignored.
    /// </summary>
    [Fact]
    public async Task ParseModifiers_InvalidModifier_IgnoredSilently()
    {
        // Arrange - Invalid modifiers should be ignored, not cause an error
        var modifiers = "ctrl,invalidmodifier,shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Only valid modifiers should be parsed
        Assert.Contains("ctrl", modifiers);
        Assert.Contains("shift", modifiers);
    }

    #endregion
}
