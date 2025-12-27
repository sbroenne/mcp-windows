namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard type action.
/// Tests validate text input via KEYEVENTF_UNICODE for layout-independent typing.
/// These tests use a dedicated test harness window to verify input is actually received.
/// </summary>
[Collection("KeyboardIntegrationTests")]
public class KeyboardTypeTests : IDisposable
{
    private readonly KeyboardTestFixture _fixture;

    public KeyboardTypeTests(KeyboardTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.EnsureTestWindowFocused();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests that basic ASCII text can be typed correctly and is received by the harness.
    /// </summary>
    [Theory]
    [InlineData("Hello")]
    [InlineData("Test123")]
    [InlineData("abc")]
    public async Task TypeAsync_BasicAsciiText_VerifiedByHarness(string text)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(text);
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50); // Let focus settle

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.Equal(text.Length, result.CharactersTyped);

        // Assert - harness actually received the text
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that empty text returns success with zero characters.
    /// </summary>
    [Fact]
    public async Task TypeAsync_EmptyText_ReturnsSuccessWithZeroCharacters()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync("");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.CharactersTyped);

        // Assert - harness text box should remain empty
        Assert.Equal("", _fixture.GetInputText());
    }

    /// <summary>
    /// Tests that special characters commonly used in code are typed correctly.
    /// </summary>
    [Theory]
    [InlineData("!@#")]
    [InlineData("$%^")]
    [InlineData("&*()")]
    [InlineData("+-=")]
    public async Task TypeAsync_SpecialCharacters_VerifiedByHarness(string text)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(text);
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.Equal(text.Length, result.CharactersTyped);

        // Assert - harness received the special characters
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that spaces are typed correctly.
    /// </summary>
    [Fact]
    public async Task TypeAsync_WithSpaces_VerifiedByHarness()
    {
        // Arrange
        var text = "Hello World";
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received text with space
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that uppercase letters are typed correctly.
    /// </summary>
    [Theory]
    [InlineData("ABC")]
    [InlineData("XYZ")]
    [InlineData("HELLO")]
    public async Task TypeAsync_UppercaseLetters_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received uppercase text
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that mixed case text is typed correctly.
    /// </summary>
    [Theory]
    [InlineData("HelloWorld")]
    [InlineData("CamelCase")]
    [InlineData("mixedCASE")]
    public async Task TypeAsync_MixedCase_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received mixed case text
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that numbers are typed correctly.
    /// </summary>
    [Theory]
    [InlineData("12345")]
    [InlineData("67890")]
    [InlineData("0")]
    public async Task TypeAsync_Numbers_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received numbers
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that typing multiple times appends to existing text.
    /// </summary>
    [Fact]
    public async Task TypeAsync_MultipleTimes_AppendsText()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act - type "Hello" then " World"
        var result1 = await _fixture.KeyboardInputService.TypeTextAsync("Hello");
        Assert.True(result1.Success);

        var result2 = await _fixture.KeyboardInputService.TypeTextAsync(" World");
        Assert.True(result2.Success);

        // Assert - harness should have "Hello World"
        var textReceived = await _fixture.WaitForInputTextAsync("Hello World");
        Assert.True(textReceived, $"Expected 'Hello World', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that Unicode characters (accented) are typed correctly.
    /// </summary>
    [Theory]
    [InlineData("café")]
    [InlineData("naïve")]
    [InlineData("résumé")]
    public async Task TypeAsync_AccentedCharacters_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received accented text
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that punctuation is typed correctly.
    /// </summary>
    [Theory]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData(";")]
    [InlineData(":")]
    [InlineData("?")]
    public async Task TypeAsync_Punctuation_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received punctuation
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that brackets and quotes are typed correctly.
    /// </summary>
    [Theory]
    [InlineData("()")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"")]
    [InlineData("'")]
    public async Task TypeAsync_BracketsAndQuotes_VerifiedByHarness(string text)
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received brackets/quotes
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Tests that a complete sentence is typed correctly.
    /// </summary>
    [Fact]
    public async Task TypeAsync_CompleteSentence_VerifiedByHarness()
    {
        // Arrange
        var text = "Hello, World! 123";
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(50);

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(text);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness received complete sentence
        var textReceived = await _fixture.WaitForInputTextAsync(text);
        Assert.True(textReceived, $"Test harness did not receive expected text '{text}', got '{_fixture.GetInputText()}'");
    }

    #region WaitForIdle Tests

    /// <summary>
    /// Tests that WaitForIdle returns success when called (basic functionality).
    /// </summary>
    [Fact]
    public async Task WaitForIdle_ReturnsSuccessOrAppropriateError()
    {
        // Arrange
        _fixture.EnsureTestWindowFocused();
        await Task.Delay(100);

        // Act
        var result = await _fixture.KeyboardInputService.WaitForIdleAsync();

        // Assert - should either succeed or fail with a clear reason
        // On some test systems, there may be no foreground window
        if (!result.Success)
        {
            Assert.NotNull(result.Error);
            Assert.True(result.Error.Contains("foreground", StringComparison.OrdinalIgnoreCase) ||
                       result.Error.Contains("window", StringComparison.OrdinalIgnoreCase) ||
                       result.Error.Contains("process", StringComparison.OrdinalIgnoreCase),
                       $"Unexpected error: {result.Error}");
        }
        else
        {
            Assert.True(result.Success);
            // When successful, should include a message about the process being idle
            Assert.NotNull(result.Message);
        }
    }

    /// <summary>
    /// Tests that WaitForIdle can be called with a cancellation token.
    /// Note: Current implementation may not honor cancellation immediately since
    /// it uses WaitForInputIdle which is a blocking call.
    /// </summary>
    [Fact]
    public async Task WaitForIdle_WithCancellationToken_CompletesNormally()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - call with a cancellation token (not cancelled)
        var result = await _fixture.KeyboardInputService.WaitForIdleAsync(cts.Token);

        // Assert - should complete (either success or error, but not throw)
        Assert.NotNull(result);
    }

    #endregion
}


