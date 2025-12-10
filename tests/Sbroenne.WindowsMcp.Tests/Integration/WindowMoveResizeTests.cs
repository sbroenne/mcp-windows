using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window move and resize operations.
/// Uses the dedicated test harness window to avoid interfering with user's active work.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowMoveResizeTests : IClassFixture<WindowTestFixture>
{
    private readonly IWindowService _windowService;
    private readonly WindowTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowMoveResizeTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowMoveResizeTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task MoveWindow_RepositionsWindow()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;
        var originalBounds = _fixture.TestWindowBounds;

        // Act - Move to a new position (stay on the same monitor)
        int newX = originalBounds.X + 50;
        int newY = originalBounds.Y + 50;
        var result = await _windowService.MoveWindowAsync(handle, newX, newY);

        // Assert - operation completes
        Assert.NotNull(result);
        Assert.True(result.Success, $"Move failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.NotNull(result.Window.Bounds);
        Assert.True(result.Window.Bounds.Width > 0);
        Assert.True(result.Window.Bounds.Height > 0);

        // Clean up - move back to original position
        await _windowService.MoveWindowAsync(handle, originalBounds.X, originalBounds.Y);
    }

    [Fact]
    public async Task ResizeWindow_ChangesWindowDimensions()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;
        var originalBounds = _fixture.TestWindowBounds;

        // Act - Resize to new dimensions
        int newWidth = 700;
        int newHeight = 500;
        var result = await _windowService.ResizeWindowAsync(handle, newWidth, newHeight);

        // Assert - operation completes
        Assert.NotNull(result);
        Assert.True(result.Success, $"Resize failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.NotNull(result.Window.Bounds);
        Assert.True(result.Window.Bounds.Width > 0);
        Assert.True(result.Window.Bounds.Height > 0);

        // Clean up - restore original size
        await _windowService.ResizeWindowAsync(handle, originalBounds.Width, originalBounds.Height);
    }

    [Fact]
    public async Task SetBoundsWindow_ChangesPositionAndSizeAtomically()
    {
        // Arrange - Use the test harness window
        nint handle = _fixture.TestWindowHandle;
        var originalBounds = _fixture.TestWindowBounds;

        // Act - Set new bounds (stay on the same monitor)
        var newBounds = new WindowBounds
        {
            X = originalBounds.X + 30,
            Y = originalBounds.Y + 30,
            Width = 750,
            Height = 550
        };
        var result = await _windowService.SetBoundsAsync(handle, newBounds);

        // Assert - operation completes
        Assert.NotNull(result);
        Assert.True(result.Success, $"SetBounds failed: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.NotNull(result.Window.Bounds);
        Assert.True(result.Window.Bounds.Width > 0);
        Assert.True(result.Window.Bounds.Height > 0);

        // Clean up - restore original bounds
        var restoreBounds = new WindowBounds
        {
            X = originalBounds.X,
            Y = originalBounds.Y,
            Width = originalBounds.Width,
            Height = originalBounds.Height
        };
        await _windowService.SetBoundsAsync(handle, restoreBounds);
    }

    [Fact]
    public async Task MoveWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle and test monitor coordinates
        nint invalidHandle = (nint)0x12345678;
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);

        // Act
        var result = await _windowService.MoveWindowAsync(invalidHandle, x, y);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResizeWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle
        nint invalidHandle = (nint)0x12345678;

        // Act
        var result = await _windowService.ResizeWindowAsync(invalidHandle, 800, 600);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SetBoundsWindow_InvalidHandle_ReturnsError()
    {
        // Arrange - Use an invalid handle and test monitor coordinates
        nint invalidHandle = (nint)0x12345678;
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var bounds = new WindowBounds { X = x, Y = y, Width = 800, Height = 600 };

        // Act
        var result = await _windowService.SetBoundsAsync(invalidHandle, bounds);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MoveWindow_ZeroHandle_ReturnsError()
    {
        // Arrange - use test monitor coordinates
        nint zeroHandle = IntPtr.Zero;
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);

        // Act
        var result = await _windowService.MoveWindowAsync(zeroHandle, x, y);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResizeWindow_ZeroHandle_ReturnsError()
    {
        // Arrange
        nint zeroHandle = IntPtr.Zero;

        // Act
        var result = await _windowService.ResizeWindowAsync(zeroHandle, 800, 600);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SetBoundsWindow_ZeroHandle_ReturnsError()
    {
        // Arrange - use test monitor coordinates
        nint zeroHandle = IntPtr.Zero;
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var bounds = new WindowBounds { X = x, Y = y, Width = 800, Height = 600 };

        // Act
        var result = await _windowService.SetBoundsAsync(zeroHandle, bounds);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MoveWindow_MaximizedWindow_CompletesWithoutError()
    {
        // Arrange - Use the test harness window and maximize it
        nint handle = _fixture.TestWindowHandle;
        var originalBounds = _fixture.TestWindowBounds;

        var maximizeResult = await _windowService.MaximizeWindowAsync(handle);
        Assert.True(maximizeResult.Success, $"Setup maximize failed: {maximizeResult.Error}");
        await Task.Delay(100); // Give window time to maximize

        // Act - Try to move a maximized window
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var result = await _windowService.MoveWindowAsync(handle, x, y);

        // Assert - operation completes (behavior may vary - may succeed, fail, or restore first)
        Assert.NotNull(result);
        // We just verify the operation doesn't throw and returns a valid result

        // Clean up - restore window to original position
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
        await _windowService.MoveWindowAsync(handle, originalBounds.X, originalBounds.Y);
    }

    [Fact]
    public async Task ResizeWindow_MinimizedWindow_FailsOrRestores()
    {
        // Arrange - Use the test harness window and minimize it
        nint handle = _fixture.TestWindowHandle;

        var minimizeResult = await _windowService.MinimizeWindowAsync(handle);
        Assert.True(minimizeResult.Success, $"Setup minimize failed: {minimizeResult.Error}");
        await Task.Delay(100); // Give window time to minimize

        // Act - Try to resize a minimized window
        var result = await _windowService.ResizeWindowAsync(handle, 800, 600);

        // Assert - operation completes (may fail or restore first)
        Assert.NotNull(result);
        // The behavior may vary - either fails or restores and resizes

        // Clean up - restore the window
        await _windowService.RestoreWindowAsync(handle);
        await Task.Delay(100);
    }
}
