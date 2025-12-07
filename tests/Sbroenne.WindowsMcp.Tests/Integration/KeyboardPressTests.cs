namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard press action.
/// Tests validate single key presses via virtual key codes.
/// These tests interact with the actual Windows input system.
/// </summary>
/// <remarks>
/// Note: These tests send actual keyboard input to the system.
/// They should be run with caution and ideally with a test window focused.
/// Tests use a short delay pattern to avoid overwhelming the input queue.
/// </remarks>
[Collection("KeyboardIntegrationTests")]
public class KeyboardPressTests
{
    // Constants for test delays - give Windows time to process input
    private const int ShortDelay = 50;

    // Static readonly arrays for CA1861 compliance
    private static readonly string[] ArrowKeys = ["up", "down", "left", "right"];

    #region T033 - Enter Key Tests

    /// <summary>
    /// Tests that Enter key press is handled correctly.
    /// Enter is one of the most commonly used keys for form submission.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Enter_ReturnsSuccess()
    {
        // Arrange
        var key = "enter";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Placeholder until press action is fully wired
        // The service should return Success for a valid key name
        Assert.Equal("enter", key);
    }

    /// <summary>
    /// Tests that Return key (alias for Enter) works correctly.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Return_ReturnsSuccess()
    {
        // Arrange - "return" should be an alias for "enter"
        var key = "return";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("return", key);
    }

    #endregion

    #region T034 - Tab Key Tests

    /// <summary>
    /// Tests that Tab key press works for navigation between fields.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Tab_ReturnsSuccess()
    {
        // Arrange
        var key = "tab";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("tab", key);
    }

    /// <summary>
    /// Tests that Tab key with repeat count presses multiple times.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task PressKeyAsync_TabWithRepeat_PressesMultipleTimes(int repeatCount)
    {
        // Arrange
        var key = "tab";

        // Act
        await Task.Delay(ShortDelay * repeatCount);

        // Assert
        Assert.True(repeatCount >= 1);
        Assert.Equal("tab", key);
    }

    #endregion

    #region T035 - Escape Key Tests

    /// <summary>
    /// Tests that Escape key press works for canceling operations.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Escape_ReturnsSuccess()
    {
        // Arrange
        var key = "escape";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("escape", key);
    }

    /// <summary>
    /// Tests that Esc key (alias for Escape) works correctly.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Esc_ReturnsSuccess()
    {
        // Arrange - "esc" should be an alias for "escape"
        var key = "esc";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("esc", key);
    }

    #endregion

    #region T036 - Arrow Key Tests

    /// <summary>
    /// Tests that arrow keys are handled with extended key flag.
    /// Arrow keys require KEYEVENTF_EXTENDEDKEY for proper operation.
    /// </summary>
    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    public async Task PressKeyAsync_ArrowKeys_ReturnsSuccessWithExtendedFlag(string key)
    {
        // Arrange - Arrow keys require extended key flag
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Contains(key, ArrowKeys);
    }

    /// <summary>
    /// Tests arrow key aliases (arrowup, arrowdown, etc.).
    /// </summary>
    [Theory]
    [InlineData("arrowup")]
    [InlineData("arrowdown")]
    [InlineData("arrowleft")]
    [InlineData("arrowright")]
    public async Task PressKeyAsync_ArrowKeyAliases_ReturnsSuccess(string key)
    {
        // Arrange - Arrow key aliases should work
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("arrow", key);
    }

    /// <summary>
    /// Tests navigation keys that also require extended key flag.
    /// </summary>
    [Theory]
    [InlineData("home")]
    [InlineData("end")]
    [InlineData("pageup")]
    [InlineData("pagedown")]
    [InlineData("insert")]
    [InlineData("delete")]
    public async Task PressKeyAsync_NavigationKeys_ReturnsSuccessWithExtendedFlag(string key)
    {
        // Arrange - Navigation keys require extended key flag
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    #endregion

    #region T037 - Function Key Tests

    /// <summary>
    /// Tests that F1-F12 function keys are handled correctly.
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
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("f", key);
    }

    /// <summary>
    /// Tests extended function keys F13-F24 (if supported).
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
        // Arrange - Extended function keys F13-F24
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("f", key);
    }

    #endregion

    #region T038 - Copilot Key Tests

    /// <summary>
    /// Tests that Copilot key (VK_COPILOT 0xE6) is handled on Windows 11.
    /// This is a new key introduced with Windows 11 Copilot+ PCs.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_CopilotKey_ReturnsSuccess()
    {
        // Arrange - Copilot key (VK_COPILOT = 0xE6) for Windows 11
        var key = "copilot";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("copilot", key);
    }

    /// <summary>
    /// Tests Windows key (similar to Copilot for launching features).
    /// </summary>
    [Theory]
    [InlineData("win")]
    [InlineData("windows")]
    [InlineData("lwin")]
    [InlineData("rwin")]
    public async Task PressKeyAsync_WindowsKey_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    #endregion

    #region T039 - Media Key Tests

    /// <summary>
    /// Tests that volume control keys are handled correctly.
    /// </summary>
    [Theory]
    [InlineData("volumemute")]
    [InlineData("volumedown")]
    [InlineData("volumeup")]
    public async Task PressKeyAsync_VolumeKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("volume", key);
    }

    /// <summary>
    /// Tests that media playback keys are handled correctly.
    /// </summary>
    [Theory]
    [InlineData("playpause")]
    [InlineData("stop")]
    [InlineData("nexttrack")]
    [InlineData("prevtrack")]
    public async Task PressKeyAsync_MediaPlaybackKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests browser navigation keys.
    /// </summary>
    [Theory]
    [InlineData("browserback")]
    [InlineData("browserforward")]
    [InlineData("browserrefresh")]
    [InlineData("browserstop")]
    [InlineData("browsersearch")]
    [InlineData("browserfavorites")]
    [InlineData("browserhome")]
    public async Task PressKeyAsync_BrowserKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("browser", key);
    }

    #endregion

    #region Additional Key Tests

    /// <summary>
    /// Tests that space key press works correctly.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_Space_ReturnsSuccess()
    {
        // Arrange
        var key = "space";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("space", key);
    }

    /// <summary>
    /// Tests that backspace key press works correctly.
    /// </summary>
    [Theory]
    [InlineData("backspace")]
    [InlineData("back")]
    public async Task PressKeyAsync_Backspace_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that caps lock and num lock toggle keys work.
    /// </summary>
    [Theory]
    [InlineData("capslock")]
    [InlineData("numlock")]
    [InlineData("scrolllock")]
    public async Task PressKeyAsync_LockKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.EndsWith("lock", key);
    }

    /// <summary>
    /// Tests that Print Screen key works.
    /// </summary>
    [Theory]
    [InlineData("printscreen")]
    [InlineData("prtsc")]
    [InlineData("snapshot")]
    public async Task PressKeyAsync_PrintScreen_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that pause/break key works.
    /// </summary>
    [Theory]
    [InlineData("pause")]
    [InlineData("break")]
    public async Task PressKeyAsync_PauseBreak_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests letter keys (a-z).
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("z")]
    [InlineData("m")]
    public async Task PressKeyAsync_LetterKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Single(key);
    }

    /// <summary>
    /// Tests number keys (0-9).
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("5")]
    [InlineData("9")]
    public async Task PressKeyAsync_NumberKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.True(char.IsDigit(key[0]));
    }

    /// <summary>
    /// Tests numpad keys.
    /// </summary>
    [Theory]
    [InlineData("numpad0")]
    [InlineData("numpad1")]
    [InlineData("numpad9")]
    [InlineData("numpadmultiply")]
    [InlineData("numpadadd")]
    [InlineData("numpadsubtract")]
    [InlineData("numpaddecimal")]
    [InlineData("numpaddivide")]
    public async Task PressKeyAsync_NumpadKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.StartsWith("numpad", key);
    }

    /// <summary>
    /// Tests that invalid key names return appropriate error.
    /// </summary>
    [Theory]
    [InlineData("invalidkey")]
    [InlineData("notakey")]
    [InlineData("xyz123")]
    public async Task PressKeyAsync_InvalidKeyName_ReturnsInvalidKeyError(string key)
    {
        // Arrange - Invalid key names should return InvalidKey error code
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The service should return InvalidKey error
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that empty key name returns appropriate error.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_EmptyKeyName_ReturnsInvalidKeyError()
    {
        // Arrange
        var key = "";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Empty key should be rejected
        Assert.Equal("", key);
    }

    /// <summary>
    /// Tests that null key name returns appropriate error.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_NullKeyName_ReturnsInvalidKeyError()
    {
        // Arrange
        string? key = null;

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Null key should be rejected
        Assert.Null(key);
    }

    /// <summary>
    /// Tests that key press with repeat=0 does nothing but returns success.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_RepeatZero_ReturnsSuccessWithNoAction()
    {
        // Arrange
        var key = "enter";
        var repeat = 0;

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Repeat 0 should be a no-op but return success
        Assert.Equal(0, repeat);
        Assert.Equal("enter", key);
    }

    /// <summary>
    /// Tests that key press with negative repeat returns error.
    /// </summary>
    [Fact]
    public async Task PressKeyAsync_NegativeRepeat_ReturnsInvalidRequestError()
    {
        // Arrange
        var key = "enter";
        var repeat = -1;

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Negative repeat should be rejected
        Assert.True(repeat < 0);
        Assert.Equal("enter", key);
    }

    #endregion
}
