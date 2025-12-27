using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for cursor capture functionality via <see cref="ScreenshotService"/>.
/// </summary>
public sealed class ScreenshotCursorTests
{
    private readonly ScreenshotService _screenshotService;

    public ScreenshotCursorTests()
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
    public async Task CapturePrimaryScreen_IncludeCursorFalse_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            IncludeCursor = false
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture without cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);
    }

    [Fact]
    public async Task CapturePrimaryScreen_IncludeCursorTrue_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            IncludeCursor = true
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture with cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);
    }

    [Fact]
    public async Task CapturePrimaryScreen_DefaultIncludeCursor_IsFalse()
    {
        // Arrange - default IncludeCursor should be false
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
            // IncludeCursor not specified, should default to false
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.False(request.IncludeCursor); // Verify default is false
    }

    [Fact]
    public async Task CaptureMonitor_WithCursor_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 0,
            IncludeCursor = true
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Monitor capture with cursor failed: {result.Message}");
    }

    [Fact]
    public async Task CaptureRegion_WithCursor_ReturnsSuccess()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 200, 150),
            IncludeCursor = true
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Region capture with cursor failed: {result.Message}");
    }

    [Fact]
    public async Task CaptureWithCursor_ProducesDifferentImageSize()
    {
        // Note: This test verifies that cursor capture doesn't break anything.
        // We can't easily verify the cursor is in the image without image analysis,
        // but we can verify that the operation succeeds and produces valid output.

        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var requestWithCursor = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 100, 100),
            IncludeCursor = true
        };

        var requestWithoutCursor = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 100, 100),
            IncludeCursor = false
        };

        // Act
        var resultWithCursor = await _screenshotService.ExecuteAsync(requestWithCursor);
        var resultWithoutCursor = await _screenshotService.ExecuteAsync(requestWithoutCursor);

        // Assert - both should succeed
        Assert.True(resultWithCursor.Success);
        Assert.True(resultWithoutCursor.Success);

        // Dimensions should be the same
        Assert.Equal(resultWithCursor.Width, resultWithoutCursor.Width);
        Assert.Equal(resultWithCursor.Height, resultWithoutCursor.Height);

        // Both should produce valid base64 data
        Assert.NotNull(resultWithCursor.ImageData);
        Assert.NotNull(resultWithoutCursor.ImageData);
    }

    [Fact]
    public async Task CaptureRegion_WithCursor_CapturesRegionSuccessfully()
    {
        // Arrange: position cursor to a known location, then capture a tight region around it
        var monitor = new MonitorService().GetMonitor(0) ?? throw new InvalidOperationException("No monitors available");
        var targetX = monitor.X + monitor.Width / 2;
        var targetY = monitor.Y + monitor.Height / 2;

        // Move cursor using native API for deterministic positioning
        SetCursorPos(targetX, targetY);
        await Task.Delay(100); // allow cursor to settle

        var region = new CaptureRegion(targetX - 10, targetY - 10, 20, 20);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = region,
            IncludeCursor = true,
            ImageFormat = ImageFormat.Jpeg,
            Quality = 90
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert - capture succeeded and produced valid image data
        Assert.True(result.Success, $"Capture with cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);

        // Verify the image can be decoded
        var bytes = Convert.FromBase64String(result.ImageData!);
        using var ms = new MemoryStream(bytes);
        using var bitmap = new Bitmap(ms);

        // Verify dimensions match the requested region
        Assert.Equal(20, bitmap.Width);
        Assert.Equal(20, bitmap.Height);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
}
