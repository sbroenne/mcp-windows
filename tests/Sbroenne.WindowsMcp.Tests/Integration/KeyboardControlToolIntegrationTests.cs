using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard control focus behavior.
/// These tests verify that typing goes to the correct window
/// and help catch issues where focus is lost to other windows.
/// </summary>
[Collection("KeyboardIntegrationTests")]
public sealed class KeyboardControlToolIntegrationTests : IDisposable
{
    private readonly KeyboardTestFixture _fixture;

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, char[] lpString, int nMaxCount);

    public KeyboardControlToolIntegrationTests(KeyboardTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.EnsureTestWindowFocused();
        Thread.Sleep(200);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static string GetForegroundWindowTitle()
    {
        var hwnd = GetForegroundWindow();
        char[] buffer = new char[256];
        int length = GetWindowText(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, length);
    }

    /// <summary>
    /// Verifies that text actually ends up in the expected window.
    /// This is the critical test - we verify the harness received the text.
    /// The test ensures the harness is focused before typing and validates
    /// the text appears in the correct control.
    /// </summary>
    [Fact]
    public async Task TypeText_TextEndsUpInCorrectWindow()
    {
        // Arrange - ensure harness is focused with multiple retries
        _fixture.Reset();
        await _fixture.EnsureTestWindowFocusedAsync(maxRetries: 5, delayMs: 200);
        await Task.Delay(200);

        var testText = "CorrectWindow";

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(testText);

        // Assert - typing succeeded
        Assert.True(result.Success, $"TypeTextAsync failed: {result.Error}");

        // Assert - text ended up in our harness text box
        var textReceived = await _fixture.WaitForInputTextAsync(testText, TimeSpan.FromSeconds(3));
        Assert.True(textReceived,
            $"Text did not appear in test harness! Expected '{testText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Verifies that after typing, the input appears in the harness.
    /// This validates that keyboard input goes to the focused window.
    /// </summary>
    [Fact]
    public async Task TypeMultipleCharacters_AllAppearInHarness()
    {
        // Arrange - ensure harness is focused with multiple retries
        _fixture.Reset();
        await _fixture.EnsureTestWindowFocusedAsync(maxRetries: 5, delayMs: 200);
        await Task.Delay(200);

        var testText = "ABC123xyz";

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(testText);

        // Assert - typing succeeded
        Assert.True(result.Success, $"TypeTextAsync failed: {result.Error}");

        // Assert - all characters appeared
        var textReceived = await _fixture.WaitForInputTextAsync(testText, TimeSpan.FromSeconds(3));
        Assert.True(textReceived,
            $"Text did not appear in test harness! Expected '{testText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Verifies that special characters are typed correctly.
    /// </summary>
    [Fact]
    public async Task TypeSpecialCharacters_AppearCorrectly()
    {
        // Arrange - ensure harness is focused with multiple retries
        _fixture.Reset();
        await _fixture.EnsureTestWindowFocusedAsync(maxRetries: 5, delayMs: 200);
        await Task.Delay(200);

        var testText = "Hello, World!";

        // Act
        var result = await _fixture.KeyboardInputService.TypeTextAsync(testText);

        // Assert - typing succeeded
        Assert.True(result.Success, $"TypeTextAsync failed: {result.Error}");

        // Assert - text appeared correctly
        var textReceived = await _fixture.WaitForInputTextAsync(testText, TimeSpan.FromSeconds(3));
        Assert.True(textReceived,
            $"Text did not appear in test harness! Expected '{testText}', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Verifies that pressing a simple key adds it to the harness.
    /// </summary>
    [Fact]
    public async Task PressKey_AppearsInHarness()
    {
        // Arrange - ensure harness is focused with multiple retries
        _fixture.Reset();
        await _fixture.EnsureTestWindowFocusedAsync(maxRetries: 5, delayMs: 200);
        await Task.Delay(200);

        // Act - press the 'a' key
        var result = await _fixture.KeyboardInputService.PressKeyAsync("a", ModifierKey.None, 1);

        // Assert - key press succeeded
        Assert.True(result.Success, $"PressKeyAsync failed: {result.Error}");

        // Assert - character appeared in harness
        var textReceived = await _fixture.WaitForInputTextAsync("a", TimeSpan.FromSeconds(2));
        Assert.True(textReceived,
            $"Key press did not appear in test harness! Expected 'a', got '{_fixture.GetInputText()}'");
    }

    /// <summary>
    /// Verifies that consecutive typing operations go to the harness.
    /// </summary>
    [Fact]
    public async Task ConsecutiveTyping_AllTextAppearsInOrder()
    {
        // Arrange - ensure harness is focused with multiple retries
        _fixture.Reset();
        await _fixture.EnsureTestWindowFocusedAsync(maxRetries: 5, delayMs: 200);
        await Task.Delay(200);

        // Act - type in parts
        var result1 = await _fixture.KeyboardInputService.TypeTextAsync("First");
        Assert.True(result1.Success, $"First TypeTextAsync failed: {result1.Error}");

        var result2 = await _fixture.KeyboardInputService.TypeTextAsync("Second");
        Assert.True(result2.Success, $"Second TypeTextAsync failed: {result2.Error}");

        // Assert - both parts appeared
        var textReceived = await _fixture.WaitForInputTextAsync("FirstSecond", TimeSpan.FromSeconds(3));
        Assert.True(textReceived,
            $"Consecutive text did not appear! Expected 'FirstSecond', got '{_fixture.GetInputText()}'");
    }
}
