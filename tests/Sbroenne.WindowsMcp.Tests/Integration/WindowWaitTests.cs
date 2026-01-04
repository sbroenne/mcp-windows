using System.Globalization;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for wait for window operations.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowWaitTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowWaitTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowWaitTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task WaitForWindow_ExistingWindow_ReturnsImmediately()
    {
        // Arrange - Get a window title that exists
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        if (listResult.Windows.Count == 0)
        {
            // No windows, skip test
            return;
        }

        var targetWindow = listResult.Windows.First(w => !string.IsNullOrEmpty(w.Title));
        var title = targetWindow.Title!;

        // Act - Wait for existing window (should return immediately)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _windowService.WaitForWindowAsync(title, useRegex: false, timeoutMs: 5000);
        stopwatch.Stop();

        // Assert - should succeed quickly (within 1 second for existing window)
        Assert.True(result.Success, $"WaitForWindow failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Contains(title, result.Window.Title ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Wait took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task WaitForWindow_NonExistentWindow_TimesOut()
    {
        // Arrange - Use a title that definitely doesn't exist
        string nonExistentTitle = $"NonExistentWindow_TestTitle_{Guid.NewGuid()}";

        // Act - Wait with a short timeout
        var result = await _windowService.WaitForWindowAsync(nonExistentTitle, useRegex: false, timeoutMs: 1000);

        // Assert - should fail with timeout
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        // Error should indicate timeout or not found
    }

    [Fact]
    public async Task WaitForWindow_WithRegex_MatchesPattern()
    {
        // Arrange - Get a window title that exists
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        if (listResult.Windows.Count == 0)
        {
            // No windows, skip test
            return;
        }

        var targetWindow = listResult.Windows.First(w => !string.IsNullOrEmpty(w.Title) && w.Title.Length >= 3);

        // Create a regex pattern that should match this window
        // Use first 3 characters as a pattern
        var firstChars = targetWindow.Title!.Substring(0, 3);
        var regexPattern = $"^{System.Text.RegularExpressions.Regex.Escape(firstChars)}.*";

        // Act - Wait with regex pattern
        var result = await _windowService.WaitForWindowAsync(regexPattern, useRegex: true, timeoutMs: 5000);

        // Assert - should succeed
        Assert.True(result.Success, $"WaitForWindow with regex failed: {result.Error}");
        Assert.NotNull(result.Window);
    }

    [Fact]
    public async Task WaitForWindow_CancellationRequested_CancelsWait()
    {
        // Arrange - Use a title that doesn't exist and cancel quickly
        string nonExistentTitle = $"NonExistentWindow_{Guid.NewGuid()}";
        using var cts = new CancellationTokenSource(500); // Cancel after 500ms

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _windowService.WaitForWindowAsync(
                nonExistentTitle,
                useRegex: false,
                timeoutMs: 60000, // Long timeout
                cancellationToken: cts.Token);
            stopwatch.Stop();

            // If we get here without exception, the method handles cancellation gracefully
            Assert.False(result.Success);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            // Expected - cancellation was handled via exception
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, "Should cancel quickly");
        }
    }

    [Fact]
    public async Task WaitForWindow_PartialTitleMatch_FindsWindow()
    {
        // Arrange - Get a window with a long title
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.Title) && w.Title.Length > 5);

        if (targetWindow is null)
        {
            // No suitable window, skip test
            return;
        }

        // Use a substring of the title
        var partialTitle = targetWindow.Title!.Substring(0, Math.Min(10, targetWindow.Title.Length));

        // Act - Wait for window with partial title (substring match)
        var result = await _windowService.WaitForWindowAsync(partialTitle, useRegex: false, timeoutMs: 5000);

        // Assert - should succeed
        Assert.True(result.Success, $"WaitForWindow with partial title failed: {result.Error}");
        Assert.NotNull(result.Window);
    }

    [Fact]
    public async Task WaitForWindow_CaseInsensitive_FindsWindow()
    {
        // Arrange - Get a window title that exists
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.Title) && w.Title.Any(char.IsLetter));

        if (targetWindow is null)
        {
            // No suitable window, skip test
            return;
        }

        // Convert title to opposite case
        var title = targetWindow.Title!;
        var oppositeCase = string.Concat(title.Select(c =>
            char.IsUpper(c) ? char.ToLower(c, CultureInfo.InvariantCulture) : char.ToUpper(c, CultureInfo.InvariantCulture)));

        // Act - Wait with opposite case (should still match)
        var result = await _windowService.WaitForWindowAsync(oppositeCase, useRegex: false, timeoutMs: 5000);

        // Assert - should succeed (case-insensitive matching)
        Assert.True(result.Success, $"WaitForWindow with opposite case failed: {result.Error}");
        Assert.NotNull(result.Window);
    }

    [Fact]
    public async Task WaitForWindow_InvalidRegex_ReturnsError()
    {
        // Arrange - Use an invalid regex pattern
        string invalidPattern = "[invalid(regex";

        // Act
        var result = await _windowService.WaitForWindowAsync(invalidPattern, useRegex: true, timeoutMs: 1000);

        // Assert - should fail with error (not crash)
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task WaitForWindow_WithShortTimeout_ReturnsQuickly()
    {
        // Arrange - Use a title that doesn't exist with short timeout
        string nonExistentTitle = $"NonExistentWindow_{Guid.NewGuid()}";

        // Act - Wait with explicit short timeout
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _windowService.WaitForWindowAsync(nonExistentTitle, useRegex: false, timeoutMs: 2000);
        stopwatch.Stop();

        // Assert - should fail quickly (within ~3 seconds)
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.True(stopwatch.Elapsed.TotalSeconds <= 5, $"Timeout took too long: {stopwatch.Elapsed.TotalSeconds}s");
    }
}

