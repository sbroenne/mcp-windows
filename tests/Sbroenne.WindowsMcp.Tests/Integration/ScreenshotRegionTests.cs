using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for region capture via <see cref="ScreenshotService"/>.
/// </summary>
public sealed class ScreenshotRegionTests
{
    private readonly ScreenshotService _screenshotService;

    public ScreenshotRegionTests()
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
    public async Task CaptureRegion_ValidRegion_WithPngFormat_ReturnsSuccess()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 400, 300),
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Region capture failed: {result.Message}");
        Assert.NotNull(result.ImageData);
        Assert.Equal("png", result.Format);
    }

    [Fact]
    public async Task CaptureRegion_ReturnsExactDimensions()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        const int expectedWidth = 400;
        const int expectedHeight = 300;
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, expectedWidth, expectedHeight)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedWidth, result.Width);
        Assert.Equal(expectedHeight, result.Height);
    }

    [Fact]
    public async Task CaptureRegion_WithPngFormat_ReturnsValidPng()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 200, 150),
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
    public async Task CaptureRegion_ZeroWidth_ReturnsInvalidRegionError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 0, 150) // Zero width
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_region", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureRegion_ZeroHeight_ReturnsInvalidRegionError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 400, 0) // Zero height
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_region", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureRegion_NegativeWidth_ReturnsInvalidRegionError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, -400, 300)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_region", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureRegion_NegativeHeight_ReturnsInvalidRegionError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 400, -300)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_region", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureRegion_NullRegion_ReturnsInvalidRegionError()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = null
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_region", result.ErrorCode);
    }

    [Fact]
    public async Task CaptureRegion_WithIncludeCursor_ReturnsSuccess()
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
        Assert.NotNull(result.ImageData);
    }

    [Fact]
    public async Task CaptureRegion_SmallRegion_ReturnsSuccess()
    {
        // Arrange - 1x1 pixel region on secondary monitor
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 1, 1)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }
}
