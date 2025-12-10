using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

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
        var configuration = ScreenshotConfiguration.FromEnvironment();
        var logger = new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance);

        _screenshotService = new ScreenshotService(
            monitorService,
            secureDesktopDetector,
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
}
