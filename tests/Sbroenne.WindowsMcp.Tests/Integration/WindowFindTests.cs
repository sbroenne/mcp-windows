using System.Globalization;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window find operations.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowFindTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowTestFixture _fixture;
    private readonly WindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFindTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowFindTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task FindWindow_ByExactTitle_ReturnsWindow()
    {
        // Arrange - First list windows to get an actual title
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Get a window with a meaningful title
        var targetWindow = listResult.Windows.FirstOrDefault(w => !string.IsNullOrEmpty(w.Title));
        Assert.NotNull(targetWindow);

        // Act - Find by exact title
        var findResult = await _windowService.FindWindowAsync(targetWindow.Title);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.Contains(findResult.Windows, w => w.Handle == targetWindow.Handle);
    }

    [Fact]
    public async Task FindWindow_BySubstring_ReturnsMatchingWindows()
    {
        // Arrange - First list windows to find a common substring
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Find a window with a multi-word title
        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.Title) && w.Title.Contains(' '));

        if (targetWindow is null)
        {
            // Skip if no multi-word titles found
            return;
        }

        // Use first word as substring
        string substring = targetWindow.Title.Split(' ')[0];

        // Act
        var findResult = await _windowService.FindWindowAsync(substring);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.Contains(findResult.Windows, w => w.Title.Contains(substring, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FindWindow_IsCaseInsensitiveByDefault()
    {
        // Arrange - First list windows to get an actual title
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Get a window with a meaningful title containing letters
        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.Title) && w.Title.Any(char.IsLetter));
        Assert.NotNull(targetWindow);

        // Convert title to opposite case
        string searchTerm = targetWindow.Title.ToUpperInvariant().Contains(targetWindow.Title, StringComparison.Ordinal)
            ? targetWindow.Title.ToLowerInvariant()
            : targetWindow.Title.ToUpperInvariant();

        // Act
        var findResult = await _windowService.FindWindowAsync(searchTerm);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        // Should find the window despite case difference
        Assert.Contains(findResult.Windows, w =>
            w.Title.Equals(targetWindow.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FindWindow_WithRegex_UsesPatternMatching()
    {
        // Arrange - Use a simple pattern that should match some windows
        // The pattern ".*" should match everything
        const string pattern = ".*";

        // Act
        var findResult = await _windowService.FindWindowAsync(pattern, useRegex: true);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);
    }

    [Fact]
    public async Task FindWindow_WithNoMatch_ReturnsEmptyResult()
    {
        // Arrange - Use a title that definitely won't exist
        const string nonExistentTitle = "ThisWindowTitleDefinitelyDoesNotExist_12345678_XYZ";

        // Act
        var findResult = await _windowService.FindWindowAsync(nonExistentTitle);

        // Assert
        // Should succeed but with empty results (not an error)
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.Empty(findResult.Windows);
    }

    [Fact]
    public async Task FindWindow_WithInvalidRegex_ReturnsError()
    {
        // Arrange - Use an invalid regex pattern
        const string invalidPattern = "[invalid(regex";

        // Act
        var findResult = await _windowService.FindWindowAsync(invalidPattern, useRegex: true);

        // Assert
        Assert.False(findResult.Success);
        Assert.Equal(WindowManagementErrorCode.InvalidRegexPattern, findResult.ErrorCode);
        Assert.NotNull(findResult.Error);
        Assert.Contains("regex", findResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task FindWindow_MatchesByProcessName()
    {
        // Arrange - First list windows to get a process name
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Get a window with a process name
        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.ProcessName));
        Assert.NotNull(targetWindow);

        // Act - Find by process name
        var findResult = await _windowService.FindWindowAsync(title: null, processName: targetWindow.ProcessName);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.Contains(findResult.Windows, w =>
            w.ProcessName?.Equals(targetWindow.ProcessName, StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task FindWindow_ByProcessIdOnly_ReturnsWindowsOfThatProcess()
    {
        // Arrange - take a process id from the desktop so the test does not depend on a
        // specific application being running
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w => w.ProcessId > 0);
        Assert.NotNull(targetWindow);

        // Act - find with the process id alone
        var findResult = await _windowService.FindWindowAsync(
            title: null,
            processName: null,
            processId: targetWindow.ProcessId);

        // Assert
        Assert.True(findResult.Success, $"Find failed: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);
        Assert.All(findResult.Windows, w => Assert.Equal(targetWindow.ProcessId, w.ProcessId));
    }

    [Fact]
    public async Task FindWindow_ByProcessId_FindsTheTestHarnessWindow()
    {
        // Act - the fixture window belongs to this test process
        var findResult = await _windowService.FindWindowAsync(
            title: null,
            processName: null,
            processId: Environment.ProcessId);

        // Assert
        Assert.True(findResult.Success, $"Find failed: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.Contains(findResult.Windows, w => w.Handle == _fixture.TestWindowHandle.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task FindWindow_WithUnknownProcessId_ReturnsNoWindows()
    {
        // Act - int.MaxValue is never assignable to a running process
        var findResult = await _windowService.FindWindowAsync(
            title: null,
            processName: null,
            processId: int.MaxValue);

        // Assert
        Assert.True(findResult.Success, $"Find failed: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.Empty(findResult.Windows);
    }

    [Fact]
    public async Task FindWindow_WithRegexPattern_MatchesPartialTitle()
    {
        // Arrange - First list windows to get a title
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Get a window with letters in the title
        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            !string.IsNullOrEmpty(w.Title) && w.Title.Any(char.IsLetter));

        if (targetWindow is null)
        {
            // Skip if no windows with letters
            return;
        }

        // Create a regex that matches the first character
        char firstChar = targetWindow.Title.First(char.IsLetter);
        string pattern = $"^.*{firstChar}.*$";

        // Act
        var findResult = await _windowService.FindWindowAsync(pattern, useRegex: true);

        // Assert
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        // Should find at least our target window
        Assert.NotEmpty(findResult.Windows);
    }
}

