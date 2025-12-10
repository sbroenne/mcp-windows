using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window state control operations (minimize, maximize, restore, close).
/// Uses the dedicated test harness window to avoid interfering with user's active work.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowStateTests : IClassFixture<WindowTestFixture>
{
    private readonly IWindowService _windowService;
    private readonly WindowTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowStateTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowStateTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task MinimizeWindow_MinimizesWindow()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.MinimizeWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Minimize failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal(WindowState.Minimized, result.Window.State);

        // Clean up - restore the window for other tests
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100); // Give window time to restore
    }

    [Fact]
    public async Task MaximizeWindow_MaximizesWindow()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.MaximizeWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Maximize failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal(WindowState.Maximized, result.Window.State);

        // Clean up - restore the window for other tests
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100); // Give window time to restore
    }

    [Fact]
    public async Task RestoreWindow_RestoresMinimizedWindow()
    {
        // Arrange - Use the test harness window and minimize it first
        nint handle = _fixture.TestWindowHandle;

        var minimizeResult = await _windowService.MinimizeWindowAsync(handle);
        Assert.True(minimizeResult.Success, $"Setup minimize failed: {minimizeResult.Error}");
        await Task.Delay(100); // Give window time to minimize

        // Act
        var result = await _windowService.RestoreWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Restore failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal(WindowState.Normal, result.Window.State);
    }

    [Fact]
    public async Task RestoreWindow_RestoresMaximizedWindow()
    {
        // Arrange - Use the test harness window and maximize it first
        nint handle = _fixture.TestWindowHandle;

        var maximizeResult = await _windowService.MaximizeWindowAsync(handle);
        Assert.True(maximizeResult.Success, $"Setup maximize failed: {maximizeResult.Error}");
        await Task.Delay(100); // Give window time to maximize

        // Act
        var result = await _windowService.RestoreWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Restore failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal(WindowState.Normal, result.Window.State);
    }

    [Fact]
    public async Task MinimizeWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.MinimizeWindowAsync(invalidHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MaximizeWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.MaximizeWindowAsync(invalidHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RestoreWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.RestoreWindowAsync(invalidHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MinimizeWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.MinimizeWindowAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MaximizeWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.MaximizeWindowAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RestoreWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.RestoreWindowAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CloseWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.CloseWindowAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CloseWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.CloseWindowAsync(invalidHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
