using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for full screen capture via <see cref="ScreenshotService"/>.
/// </summary>
public sealed class ScreenshotFullScreenTests
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;

    public ScreenshotFullScreenTests()
    {
        _monitorService = new MonitorService();
        var secureDesktopDetector = new SecureDesktopDetector();
        var imageProcessor = new ImageProcessor();
        var configuration = ScreenshotConfiguration.FromEnvironment();
        var logger = new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance);

        _screenshotService = new ScreenshotService(
            _monitorService,
            secureDesktopDetector,
            imageProcessor,
            configuration,
            logger);
    }

    [Fact]
    public async Task CapturePrimaryScreen_NoParameters_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.Message}");
        Assert.Equal("success", result.ErrorCode);
        Assert.NotNull(result.ImageData);
        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task CapturePrimaryScreen_WithPngFormat_ReturnsValidDimensions()
    {
        // Arrange - explicitly request PNG and no scaling for backward compatibility
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png,
            MaxWidth = 0 // Disable auto-scaling
        };
        var primaryMonitor = _monitorService.GetPrimaryMonitor();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(primaryMonitor.Width, result.Width);
        Assert.Equal(primaryMonitor.Height, result.Height);
    }

    [Fact]
    public async Task CapturePrimaryScreen_WithPngFormat_IncludesMetadata()
    {
        // Arrange - explicitly request PNG format
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Width);
        Assert.NotNull(result.Height);
        Assert.Equal("png", result.Format);
    }

    [Fact]
    public async Task CapturePrimaryScreen_ImageDataIsBase64Decodable()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ImageData);

        // Verify base64 is valid
        byte[]? imageBytes = null;
        var exception = Record.Exception(() => imageBytes = Convert.FromBase64String(result.ImageData));
        Assert.Null(exception);
        Assert.NotNull(imageBytes);
        Assert.NotEmpty(imageBytes);
    }

    [Fact]
    public async Task CapturePrimaryScreen_WithPngFormat_ImageDataStartsWithPngSignature()
    {
        // Arrange - explicitly request PNG format
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ImageData);

        var imageBytes = Convert.FromBase64String(result.ImageData);

        // PNG signature: 137 80 78 71 13 10 26 10 (or 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A)
        Assert.True(imageBytes.Length >= 8, "Image data too short to be a valid PNG");
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]); // 'P'
        Assert.Equal(0x4E, imageBytes[2]); // 'N'
        Assert.Equal(0x47, imageBytes[3]); // 'G'
    }

    [Fact]
    public async Task CapturePrimaryScreen_WithIncludeCursor_ReturnsSuccess()
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
    public async Task CaptureMonitorIndex0_ReturnsSameAsPrimaryScreen()
    {
        // Arrange
        var primaryRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };
        var monitorRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 0
        };

        // Act
        var primaryResult = await _screenshotService.ExecuteAsync(primaryRequest);
        var monitorResult = await _screenshotService.ExecuteAsync(monitorRequest);

        // Assert
        Assert.True(primaryResult.Success);
        Assert.True(monitorResult.Success);

        // Both should have same dimensions (primary is index 0)
        var primaryMonitor = _monitorService.GetPrimaryMonitor();
        var monitor0 = _monitorService.GetMonitor(0);

        if (primaryMonitor.Index == 0)
        {
            Assert.Equal(primaryResult.Width, monitorResult.Width);
            Assert.Equal(primaryResult.Height, monitorResult.Height);
        }
    }

    [Fact]
    public async Task CaptureInvalidMonitorIndex_ReturnsErrorWithAvailableMonitors()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 999
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_monitor_index", result.ErrorCode);
        Assert.NotNull(result.AvailableMonitors);
        Assert.NotEmpty(result.AvailableMonitors);
    }

    [Fact]
    public async Task ListMonitors_ReturnsMonitorList()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.ListMonitors
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Monitors);
        Assert.NotEmpty(result.Monitors);
        Assert.All(result.Monitors, m =>
        {
            Assert.True(m.Width > 0);
            Assert.True(m.Height > 0);
        });
    }

    [Fact]
    public async Task CaptureRegion_ValidRegion_ReturnsSuccess()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(100, 100);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 200, 150)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Region capture failed: {result.Message}");
        Assert.Equal(200, result.Width);
        Assert.Equal(150, result.Height);
    }

    [Fact]
    public async Task CaptureRegion_InvalidDimensions_ReturnsError()
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
    public async Task CaptureRegion_MissingRegion_ReturnsError()
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
}
