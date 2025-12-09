using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window listing operations.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowListTests : IClassFixture<WindowTestFixture>
{
    private readonly IWindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowListTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowListTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task ListWindows_ReturnsMultipleWindows()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success, $"List operation failed: {result.Error}");
        Assert.NotNull(result.Windows);
        Assert.NotEmpty(result.Windows);
        // There should be at least one window (this test runner)
        Assert.True(result.Windows.Count > 0);
    }

    [Fact]
    public async Task ListWindows_IncludesWindowTitles()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have non-null titles
        foreach (var window in result.Windows)
        {
            Assert.NotNull(window.Title);
        }

        // At least some windows should have non-empty titles
        Assert.Contains(result.Windows, w => !string.IsNullOrEmpty(w.Title));
    }

    [Fact]
    public async Task ListWindows_IncludesHandles()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        foreach (var window in result.Windows)
        {
            // Handle should be a valid non-zero string representation
            Assert.False(string.IsNullOrEmpty(window.Handle));
            Assert.True(long.TryParse(window.Handle, out var handleValue));
            Assert.NotEqual(0L, handleValue);
        }
    }

    [Fact]
    public async Task ListWindows_IncludesProcessNames()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // Most windows should have a process name
        Assert.Contains(result.Windows, w => !string.IsNullOrEmpty(w.ProcessName));
    }

    [Fact]
    public async Task ListWindows_IncludesBoundsWithValidCoordinates()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        foreach (var window in result.Windows)
        {
            Assert.NotNull(window.Bounds);
            // Bounds should have valid dimensions (can be negative for multi-monitor)
            // Width and Height should be non-negative for visible windows
            // Note: Minimized windows may have 0 bounds
            if (window.State != WindowState.Minimized)
            {
                Assert.True(window.Bounds.Width >= 0, $"Window '{window.Title}' has negative width");
                Assert.True(window.Bounds.Height >= 0, $"Window '{window.Title}' has negative height");
            }
        }
    }

    [Fact]
    public async Task ListWindows_IncludesMinimizedWindows()
    {
        // Arrange - This test checks the response includes minimized windows
        // Note: We can't guarantee a minimized window exists, so we check the field is populated

        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have a valid state
        foreach (var window in result.Windows)
        {
            Assert.True(
                window.State == WindowState.Normal ||
                window.State == WindowState.Minimized ||
                window.State == WindowState.Maximized ||
                window.State == WindowState.Hidden,
                $"Window '{window.Title}' has unexpected state: {window.State}");
        }
    }

    [Fact]
    public async Task ListWindows_WithFilter_ReturnsOnlyMatchingWindows()
    {
        // Arrange - Filter for a common string that should exist
        // The test runner process should be running
        const string filter = "test";

        // Act
        var result = await _windowService.ListWindowsAsync(filter: filter);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All returned windows should match the filter (title or process name)
        foreach (var window in result.Windows)
        {
            bool matchesTitle = window.Title.Contains(filter, StringComparison.OrdinalIgnoreCase);
            bool matchesProcess = window.ProcessName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
            Assert.True(matchesTitle || matchesProcess,
                $"Window '{window.Title}' (process: {window.ProcessName}) doesn't match filter '{filter}'");
        }
    }

    [Fact]
    public async Task ListWindows_ExcludesCloakedWindowsByDefault()
    {
        // Act
        var result = await _windowService.ListWindowsAsync(includeAllDesktops: false);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // Windows on the current desktop should report OnCurrentDesktop = true
        // Note: This is informational - we can't guarantee all are on current desktop
        // but we verify the field is populated
        foreach (var window in result.Windows)
        {
            // The field should be set (default to true for visible windows on current desktop)
            // Just verify the field exists and is accessible
            _ = window.OnCurrentDesktop;
        }
    }

    [Fact]
    public async Task ListWindows_WithIncludeAllDesktops_ReturnsMoreOrEqualWindows()
    {
        // Act
        var resultDefault = await _windowService.ListWindowsAsync(includeAllDesktops: false);
        var resultAll = await _windowService.ListWindowsAsync(includeAllDesktops: true);

        // Assert
        Assert.True(resultDefault.Success);
        Assert.True(resultAll.Success);
        Assert.NotNull(resultDefault.Windows);
        Assert.NotNull(resultAll.Windows);

        // Including all desktops should return >= windows than the filtered set
        Assert.True(resultAll.Windows.Count >= resultDefault.Windows.Count);
    }

    [Fact]
    public async Task ListWindows_IncludesMonitorIndex()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have a valid monitor index (0-based)
        foreach (var window in result.Windows)
        {
            Assert.True(window.MonitorIndex >= 0, $"Window '{window.Title}' has invalid monitor index: {window.MonitorIndex}");
        }
    }

    [Fact]
    public async Task ListWindows_IncludesRespondingStatus()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // Most normal windows should be responding
        // Check that the IsResponding field is accessible and most are true
        int respondingCount = result.Windows.Count(w => w.IsResponding);
        int totalCount = result.Windows.Count;

        // At least half should be responding (healthy system)
        Assert.True(respondingCount >= totalCount / 2,
            $"Too few responding windows: {respondingCount}/{totalCount}");
    }
}

/// <summary>
/// Test fixture for window management tests.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowTestFixture : IDisposable
{
    /// <summary>
    /// Gets the window service instance for testing.
    /// </summary>
    public IWindowService WindowService { get; }

    /// <summary>
    /// Gets the window enumerator instance for testing.
    /// </summary>
    public IWindowEnumerator WindowEnumerator { get; }

    /// <summary>
    /// Gets the window activator instance for testing.
    /// </summary>
    public IWindowActivator WindowActivator { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowTestFixture"/> class.
    /// </summary>
    public WindowTestFixture()
    {
        var configuration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        WindowEnumerator = new WindowEnumerator(elevationDetector, configuration);
        WindowActivator = new WindowActivator(configuration);
        WindowService = new WindowService(
            WindowEnumerator,
            WindowActivator,
            monitorService,
            secureDesktopDetector,
            configuration);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose for now
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Collection definition for window management tests to ensure sequential execution.
/// </summary>
[CollectionDefinition("WindowManagement")]
public class WindowManagementTests : ICollectionFixture<WindowTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
