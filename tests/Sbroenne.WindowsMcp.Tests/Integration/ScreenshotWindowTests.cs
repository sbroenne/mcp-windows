using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window capture via <see cref="ScreenshotService"/>.
/// </summary>
public sealed class ScreenshotWindowTests
{
    private readonly ScreenshotService _screenshotService;

    // P/Invoke for window management in tests
    [DllImport("user32.dll")]
    private static extern nint GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public ScreenshotWindowTests()
    {
        var monitorService = new MonitorService();
        var secureDesktopDetector = new SecureDesktopDetector();
        var imageProcessor = new ImageProcessor();
        var configuration = ScreenshotConfiguration.FromEnvironment();
        var logger = new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance);

        _screenshotService = new ScreenshotService(
            monitorService,
            secureDesktopDetector,
            imageProcessor,
            configuration,
            logger);
    }

    [Fact]
    public async Task CaptureWindow_DesktopWindow_WithPngFormat_ReturnsSuccess()
    {
        // Arrange - use the desktop window which always exists
        var windowHandle = GetDesktopWindow();
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = windowHandle.ToInt64(),
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Window capture failed: {result.Message}");
        Assert.NotNull(result.ImageData);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.Equal("png", result.Format);
    }

    [Fact]
    public async Task CaptureWindow_DesktopWindow_WithPngFormat_ReturnsValidPng()
    {
        // Arrange - explicitly request PNG format
        var windowHandle = GetDesktopWindow();
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = windowHandle.ToInt64(),
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ImageData);

        var imageBytes = Convert.FromBase64String(result.ImageData);

        // PNG signature check
        Assert.True(imageBytes.Length >= 8);
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]); // 'P'
        Assert.Equal(0x4E, imageBytes[2]); // 'N'
        Assert.Equal(0x47, imageBytes[3]); // 'G'
    }

    [Fact]
    public async Task CaptureWindow_InvalidHandle_ReturnsInvalidWindowHandleError()
    {
        // Arrange
        var invalidHandle = 0x12345678L; // Invalid handle
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = invalidHandle
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_window_handle", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureWindow_ZeroHandle_ReturnsInvalidWindowHandleError()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = 0
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_window_handle", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureWindow_NullHandle_ReturnsInvalidWindowHandleError()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = null
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_window_handle", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureWindow_WithIncludeCursor_ReturnsSuccess()
    {
        // Arrange
        var windowHandle = GetDesktopWindow();
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = windowHandle.ToInt64(),
            IncludeCursor = true
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Window capture with cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);
    }
}
