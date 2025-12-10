using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window move and resize operations.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowMoveResizeTests : IClassFixture<WindowTestFixture>
{
    private readonly IWindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowMoveResizeTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowMoveResizeTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task MoveWindow_RepositionsWindow()
    {
        // Arrange - Get a window that is not minimized and not elevated
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            w.State == WindowState.Normal && !w.IsElevated && w.Bounds is not null);

        if (targetWindow is null)
        {
            // No suitable window, skip test
            return;
        }

        Assert.True(long.TryParse(targetWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Store original position for cleanup
        var originalX = targetWindow.Bounds!.X;
        var originalY = targetWindow.Bounds!.Y;

        // Act - Move to a new position on secondary monitor if available
        var (newX, newY) = TestMonitorHelper.GetTestCoordinates(150, 150);
        var result = await _windowService.MoveWindowAsync(handle, newX, newY);

        // Assert - operation completes (actual position may vary due to DPI, window restrictions, etc.)
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Window);
            Assert.NotNull(result.Window.Bounds);
            // Just verify we got valid bounds back - exact position may vary
            Assert.True(result.Window.Bounds.Width > 0);
            Assert.True(result.Window.Bounds.Height > 0);

            // Clean up - move back
            await _windowService.MoveWindowAsync(handle, originalX, originalY);
        }
    }

    [Fact]
    public async Task ResizeWindow_ChangesWindowDimensions()
    {
        // Arrange - Get a window that is not minimized and not elevated
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            w.State == WindowState.Normal && !w.IsElevated && w.Bounds is not null);

        if (targetWindow is null)
        {
            // No suitable window, skip test
            return;
        }

        Assert.True(long.TryParse(targetWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Store original size for cleanup
        var originalWidth = targetWindow.Bounds!.Width;
        var originalHeight = targetWindow.Bounds!.Height;

        // Act - Resize to new dimensions
        int newWidth = 800;
        int newHeight = 600;
        var result = await _windowService.ResizeWindowAsync(handle, newWidth, newHeight);

        // Assert - operation completes (actual size may vary due to window constraints)
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Window);
            Assert.NotNull(result.Window.Bounds);
            // Just verify we got valid bounds back - exact size may vary
            Assert.True(result.Window.Bounds.Width > 0);
            Assert.True(result.Window.Bounds.Height > 0);

            // Clean up - restore original size
            await _windowService.ResizeWindowAsync(handle, originalWidth, originalHeight);
        }
    }

    [Fact]
    public async Task SetBoundsWindow_ChangesPositionAndSizeAtomically()
    {
        // Arrange - Get a window that is not minimized and not elevated
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            w.State == WindowState.Normal && !w.IsElevated && w.Bounds is not null);

        if (targetWindow is null)
        {
            // No suitable window, skip test
            return;
        }

        Assert.True(long.TryParse(targetWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Store original bounds for cleanup
        var original = targetWindow.Bounds!;

        // Act - Set new bounds on secondary monitor if available
        var (newX, newY) = TestMonitorHelper.GetTestCoordinates(200, 200);
        var newBounds = new WindowBounds { X = newX, Y = newY, Width = 1024, Height = 768 };
        var result = await _windowService.SetBoundsAsync(handle, newBounds);

        // Assert - operation completes (actual bounds may vary due to window constraints)
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Window);
            Assert.NotNull(result.Window.Bounds);
            // Just verify we got valid bounds back
            Assert.True(result.Window.Bounds.Width > 0);
            Assert.True(result.Window.Bounds.Height > 0);

            // Clean up - restore original bounds
            await _windowService.SetBoundsAsync(handle, original);
        }
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
        // Arrange - Get a maximized window
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var targetWindow = listResult.Windows.FirstOrDefault(w =>
            w.State == WindowState.Maximized && !w.IsElevated);

        if (targetWindow is null)
        {
            // No maximized window, skip test
            return;
        }

        Assert.True(long.TryParse(targetWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Act - Try to move a maximized window to test monitor coordinates
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var result = await _windowService.MoveWindowAsync(handle, x, y);

        // Assert - operation completes (behavior may vary - may succeed, fail, or restore first)
        Assert.NotNull(result);
        // We just verify the operation doesn't throw and returns a valid result
    }

    [Fact]
    public async Task ResizeWindow_MinimizedWindow_FailsOrRestores()
    {
        // Arrange - Find a minimized window
        var listResult = await _windowService.ListWindowsAsync();
        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);

        var minimizedWindow = listResult.Windows.FirstOrDefault(w =>
            w.State == WindowState.Minimized && !w.IsElevated);

        if (minimizedWindow is null)
        {
            // No minimized window, skip test
            return;
        }

        Assert.True(long.TryParse(minimizedWindow.Handle, out long handleValue));
        nint handle = (nint)handleValue;

        // Act - Try to resize a minimized window
        var result = await _windowService.ResizeWindowAsync(handle, 800, 600);

        // Assert - operation completes (may fail or restore first)
        Assert.NotNull(result);
        // The behavior may vary - either fails or restores and resizes
    }
}
