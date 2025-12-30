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
    public async Task MinimizeWindow_MinimizesWindowAsync()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.MinimizeWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Minimize failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("minimized", result.Window.State);

        // Clean up - restore the window for other tests
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100); // Give window time to restore
    }

    [Fact]
    public async Task MaximizeWindow_MaximizesWindowAsync()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // Act
        var result = await _windowService.MaximizeWindowAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Maximize failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("maximized", result.Window.State);

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
        Assert.Equal("normal", result.Window.State);
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
        Assert.Equal("normal", result.Window.State);
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

    #region GetState Tests

    [Fact]
    public async Task GetWindowState_ReturnsCurrentState()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        // First ensure window is in normal state
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);

        // Act
        var result = await _windowService.GetWindowStateAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"GetState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("normal", result.Window.State);
    }

    [Fact]
    public async Task GetWindowState_MinimizedWindow_ReturnsMinimized()
    {
        // Arrange - Use the test harness window and minimize it
        nint handle = _fixture.TestWindowHandle;

        await _windowService.MinimizeWindowAsync(handle);
        await Task.Delay(100);

        // Act
        var result = await _windowService.GetWindowStateAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"GetState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("minimized", result.Window.State);

        // Clean up
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
    }

    [Fact]
    public async Task GetWindowState_MaximizedWindow_ReturnsMaximized()
    {
        // Arrange - Use the test harness window and maximize it
        nint handle = _fixture.TestWindowHandle;

        await _windowService.MaximizeWindowAsync(handle);
        await Task.Delay(100);

        // Act
        var result = await _windowService.GetWindowStateAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"GetState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("maximized", result.Window.State);

        // Clean up
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
    }

    [Fact]
    public async Task GetWindowState_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.GetWindowStateAsync(zeroHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetWindowState_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.GetWindowStateAsync(invalidHandle);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region WaitForState Tests

    [Fact]
    public async Task WaitForState_AlreadyInState_ReturnsImmediately()
    {
        // Arrange - Use the test harness window in normal state
        nint handle = _fixture.TestWindowHandle;

        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);

        // Act
        var result = await _windowService.WaitForStateAsync(handle, WindowState.Normal, 1000);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"WaitForState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("normal", result.Window.State);
    }

    [Fact]
    public async Task WaitForState_WaitsForMinimize()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);

        // Start waiting for minimized state
        var waitTask = _windowService.WaitForStateAsync(handle, WindowState.Minimized, 5000);

        // Minimize while waiting
        await Task.Delay(100);
        await _windowService.MinimizeWindowAsync(handle);

        // Act
        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"WaitForState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("minimized", result.Window.State);

        // Clean up
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
    }

    [Fact]
    public async Task WaitForState_WaitsForMaximize()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;

        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);

        // Start waiting for maximized state
        var waitTask = _windowService.WaitForStateAsync(handle, WindowState.Maximized, 5000);

        // Maximize while waiting
        await Task.Delay(100);
        await _windowService.MaximizeWindowAsync(handle);

        // Act
        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"WaitForState failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Equal("maximized", result.Window.State);

        // Clean up
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
    }

    [Fact]
    public async Task WaitForState_Timeout_ReturnsError()
    {
        // Arrange - Use the test harness window in normal state
        nint handle = _fixture.TestWindowHandle;

        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);

        // Act - Wait for minimized but don't minimize
        var result = await _windowService.WaitForStateAsync(handle, WindowState.Minimized, 500);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Timeout", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WaitForState_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.WaitForStateAsync(zeroHandle, WindowState.Normal, 1000);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task WaitForState_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.WaitForStateAsync(invalidHandle, WindowState.Normal, 1000);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    #endregion
}
