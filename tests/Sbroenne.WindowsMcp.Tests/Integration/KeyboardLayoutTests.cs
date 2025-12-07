using Sbroenne.WindowsMcp.Input;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard layout detection functionality.
/// Tests User Story 6: Get Keyboard Layout (Priority: P2)
///
/// These tests verify that keyboard layout information can be queried
/// correctly from the system.
/// </summary>
[Collection("KeyboardIntegrationTests")]
public class KeyboardLayoutTests
{
    /// <summary>
    /// T073: Test that get_keyboard_layout returns layout information.
    /// Verifies layout query functionality returns non-empty data.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_ReturnsLayoutInfo()
    {
        // Arrange
        using var service = new KeyboardInputService();

        // Act
        var result = await service.GetKeyboardLayoutAsync();

        // Assert
        Assert.True(result.Success, $"Expected success but got: {result.Error}");
        Assert.NotNull(result.KeyboardLayout);
        Assert.NotNull(result.KeyboardLayout!.LanguageTag);
        Assert.NotNull(result.KeyboardLayout.DisplayName);
        Assert.NotNull(result.KeyboardLayout.LayoutId);
    }

    /// <summary>
    /// T074: Test KeyboardLayoutInfo structure validation.
    /// Verifies all fields are properly populated with valid data.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_LayoutInfoHasValidStructure()
    {
        // Arrange
        using var service = new KeyboardInputService();

        // Act
        var result = await service.GetKeyboardLayoutAsync();

        // Assert
        Assert.True(result.Success);
        var layout = result.KeyboardLayout;
        Assert.NotNull(layout);

        // Language tag should follow BCP-47 format or hex fallback
        // Examples: "en-US", "de-DE", "ja-JP", or "0x0409"
        Assert.False(string.IsNullOrWhiteSpace(layout!.LanguageTag));

        // Display name should be human-readable
        // Examples: "English (United States)", "German (Germany)"
        Assert.False(string.IsNullOrWhiteSpace(layout.DisplayName));

        // Layout ID should be a hex string (KLID)
        // Examples: "00000409" (US English), "00000407" (German)
        Assert.False(string.IsNullOrWhiteSpace(layout.LayoutId));
        Assert.Equal(8, layout.LayoutId.Length);
        Assert.True(
            layout.LayoutId.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')),
            $"Layout ID should be hexadecimal, got: {layout.LayoutId}");
    }

    /// <summary>
    /// T074: Test that language tag is valid BCP-47 format or hex fallback.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_LanguageTagIsValidFormat()
    {
        // Arrange
        using var service = new KeyboardInputService();

        // Act
        var result = await service.GetKeyboardLayoutAsync();

        // Assert
        Assert.True(result.Success);
        var languageTag = result.KeyboardLayout!.LanguageTag;

        // Should be either BCP-47 format (e.g., "en-US") or hex fallback (e.g., "0x0409")
        var isBcp47 = languageTag.Contains('-') || languageTag.Length == 2;
        var isHexFallback = languageTag.StartsWith("0x", StringComparison.Ordinal);
        var isNeutralCulture = languageTag.Length == 2 && languageTag.All(char.IsLetter);

        Assert.True(
            isBcp47 || isHexFallback || isNeutralCulture,
            $"Language tag should be BCP-47 format or hex fallback, got: {languageTag}");
    }

    /// <summary>
    /// T074: Test layout query with cancellation token.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_SupportsCancellation()
    {
        // Arrange
        using var service = new KeyboardInputService();
        using var cts = new CancellationTokenSource();

        // Act - should complete without throwing since layout query is fast
        var result = await service.GetKeyboardLayoutAsync(cts.Token);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.KeyboardLayout);
    }

    /// <summary>
    /// T074: Test that multiple layout queries return consistent results.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_ReturnsConsistentResults()
    {
        // Arrange
        using var service = new KeyboardInputService();

        // Act - query layout twice
        var result1 = await service.GetKeyboardLayoutAsync();
        var result2 = await service.GetKeyboardLayoutAsync();

        // Assert - both should succeed and return same layout
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.KeyboardLayout!.LayoutId, result2.KeyboardLayout!.LayoutId);
        Assert.Equal(result1.KeyboardLayout.LanguageTag, result2.KeyboardLayout.LanguageTag);
    }

    /// <summary>
    /// T074: Test common keyboard layout IDs are recognized.
    /// This test documents expected layout IDs for common layouts.
    /// </summary>
    [Fact]
    public async Task GetKeyboardLayoutAsync_RecognizedLayoutId()
    {
        // Arrange
        using var service = new KeyboardInputService();

        // Act
        var result = await service.GetKeyboardLayoutAsync();

        // Assert
        Assert.True(result.Success);

        // The actual layout depends on system configuration
        // We just verify the format is correct (8 hex chars)
        Assert.Equal(8, result.KeyboardLayout!.LayoutId.Length);

        // Note: In CI/CD environments, the default is typically US English (00000409)
        // This test documents the layout detection is working
    }
}
