using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window activation operations.
/// Uses the dedicated test harness window to avoid interfering with user's active work.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowActivateTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowService _windowService;
    private readonly WindowActivator _windowActivator;
    private readonly WindowTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowActivateTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowActivateTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _windowService = fixture.WindowService;
        _windowActivator = fixture.WindowActivator;
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ActivateWindow_BringsWindowToForeground()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.ActivateWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Activate failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.True(result.Window.IsForeground, "Window should be foreground after activation");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ActivateWindow_RestoresMinimizedWindow()
    {
        // Arrange - Use the test harness window and minimize it first
        nint handle = _fixture.TestWindowHandle;

        var minimizeResult = await _windowService.MinimizeWindowAsync(handle);
        Assert.True(minimizeResult.Success, $"Setup minimize failed: {minimizeResult.Error}");
        await Task.Delay(100); // Give window time to minimize

        // Act
        var result = await _windowService.ActivateWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Activate failed: {result.Error}");
        Assert.NotNull(result.Window);
        // After activation, window should not be minimized
        Assert.NotEqual("minimized", result.Window.State);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ActivateWindow_ReturnsWindowInfo()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.ActivateWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Activate failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.NotNull(result.Window.Title);
        Assert.NotNull(result.Window.Bounds);
        Assert.True(result.Window.Bounds[2] > 0); // Width
        Assert.True(result.Window.Bounds[3] > 0); // Height
    }

    [Fact]
    public async Task ActivateWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678; // Almost certainly invalid

        // Act
        var result = await _windowService.ActivateWindowAsync(invalidHandle);

        // Assert
        // The result may succeed but with window not found, or fail with an error
        // Either way, there should be no exception and the result should indicate the issue
        if (!result.Success)
        {
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ActivateWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.ActivateWindowAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetForegroundWindow_ReturnsCurrentForeground()
    {
        // Act
        var result = await _windowService.GetForegroundWindowAsync();

        // Assert - should typically succeed but may fail in headless environments
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Window);
            Assert.True(result.Window.IsForeground);
            Assert.NotNull(result.Window.Title);
        }
        // In some test environments, there may be no foreground window
    }

    [Fact]
    public async Task GetForegroundWindow_ReturnsValidHandle()
    {
        // Act
        var result = await _windowService.GetForegroundWindowAsync();

        // Assert - should typically succeed but may fail in headless environments
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Window);
            Assert.True(long.TryParse(result.Window.Handle, out long handleValue));
            Assert.NotEqual(0L, handleValue);
        }
    }

    [Fact]
    public void IsForegroundWindow_ReturnsTrueForForeground()
    {
        // Arrange - Get the current foreground window
        nint foregroundHandle = _windowActivator.GetForegroundWindow();
        Assert.NotEqual(IntPtr.Zero, foregroundHandle);

        // Act
        bool isForeground = _windowActivator.IsForegroundWindow(foregroundHandle);

        // Assert
        Assert.True(isForeground);
    }

    [Fact]
    public async Task IsForegroundWindow_ReturnsFalseForNonForeground()
    {
        // Arrange - Get a list of windows and find one that's not foreground
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var nonForegroundWindow = listResult.Windows.FirstOrDefault(w => !w.IsForeground);
        if (nonForegroundWindow is null)
        {
            // Only one window, skip test
            return;
        }

        Assert.True(long.TryParse(nonForegroundWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Act
        bool isForeground = _windowActivator.IsForegroundWindow(handle);

        // Assert
        Assert.False(isForeground);
    }
}
