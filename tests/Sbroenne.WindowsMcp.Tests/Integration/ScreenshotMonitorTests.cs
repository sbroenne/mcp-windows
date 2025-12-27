using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for monitor-specific capture via <see cref="ScreenshotService"/>.
/// </summary>
public sealed class ScreenshotMonitorTests
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;

    public ScreenshotMonitorTests()
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
    public async Task CaptureMonitor_Index0_WithPngFormat_ReturnsSuccessWithValidDimensions()
    {
        // Arrange - explicitly request PNG
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 0,
            ImageFormat = ImageFormat.Png
        };
        var monitor0 = _monitorService.GetMonitor(0);
        Assert.NotNull(monitor0);

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Monitor capture failed: {result.Message}");
        Assert.Equal(monitor0.Width, result.Width);
        Assert.Equal(monitor0.Height, result.Height);
        Assert.Equal("png", result.Format);
        Assert.NotNull(result.ImageData);
    }

    [Fact]
    public async Task CaptureMonitor_Index0_WithPngFormat_ReturnsValidPng()
    {
        // Arrange - explicitly request PNG format
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 0,
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
    public async Task CaptureMonitor_NullIndex_DefaultsToIndex0()
    {
        // Arrange
        var requestWithNull = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = null
        };
        var monitor0 = _monitorService.GetMonitor(0);
        Assert.NotNull(monitor0);

        // Act
        var result = await _screenshotService.ExecuteAsync(requestWithNull);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(monitor0.Width, result.Width);
        Assert.Equal(monitor0.Height, result.Height);
    }

    [Fact]
    public async Task CaptureMonitor_InvalidIndex_ReturnsErrorWithAvailableMonitors()
    {
        // Arrange
        var invalidIndex = 999;
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = invalidIndex
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_monitor_index", result.ErrorCode);
        Assert.Contains(invalidIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Message);

        // Should include available monitors
        Assert.NotNull(result.AvailableMonitors);
        Assert.NotEmpty(result.AvailableMonitors);
        Assert.True(result.AvailableMonitors.Count >= 1, "Should have at least one monitor available");
    }

    [Fact]
    public async Task CaptureMonitor_NegativeIndex_ReturnsError()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = -1
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_monitor_index", result.ErrorCode);
        Assert.NotNull(result.AvailableMonitors);
    }

    [Fact]
    public async Task CaptureMonitor_AllMonitors_SucceedsWithValidDimensions()
    {
        // Arrange
        var monitorCount = _monitorService.MonitorCount;

        for (int i = 0; i < monitorCount; i++)
        {
            var monitor = _monitorService.GetMonitor(i);
            Assert.NotNull(monitor);

            var request = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.Monitor,
                MonitorIndex = i
            };

            // Act
            var result = await _screenshotService.ExecuteAsync(request);

            // Assert
            Assert.True(result.Success, $"Monitor {i} capture failed: {result.Message}");
            Assert.Equal(monitor.Width, result.Width);
            Assert.Equal(monitor.Height, result.Height);
        }
    }

    [Fact]
    public async Task CaptureMonitor_WithIncludeCursor_ReturnsSuccess()
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
        Assert.NotNull(result.ImageData);
    }

    [SkippableFact]
    public async Task CaptureMonitor_SecondaryMonitor_ReturnsCorrectDimensions()
    {
        // Skip if only one monitor
        Skip.If(_monitorService.MonitorCount < 2, "Test requires multiple monitors");

        // Arrange
        var monitor1 = _monitorService.GetMonitor(1);
        Assert.NotNull(monitor1);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 1
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Secondary monitor capture failed: {result.Message}");
        Assert.Equal(monitor1.Width, result.Width);
        Assert.Equal(monitor1.Height, result.Height);
    }
}
