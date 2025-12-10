using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for all-monitors composite screenshot capture via <see cref="ScreenshotService"/>.
/// Tests the AllMonitors capture target which captures all monitors into a single composite image.
/// </summary>
public sealed class AllMonitorsCaptureTests
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;

    public AllMonitorsCaptureTests()
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
    public async Task CaptureAllMonitors_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"All monitors capture failed: {result.Message}");
        Assert.Equal("success", result.ErrorCode);
        Assert.NotNull(result.ImageData);
        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task CaptureAllMonitors_WithPngFormat_ReturnsValidPngData()
    {
        // Arrange - explicitly request PNG format
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
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
    public async Task CaptureAllMonitors_ReturnsDimensionsMatchingVirtualScreen()
    {
        // Arrange - disable auto-scaling to verify exact dimensions
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            MaxWidth = 0 // Disable auto-scaling
        };

        // Get virtual screen bounds (bounding rectangle of all monitors)
        var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(virtualScreen.Width, result.Width);
        Assert.Equal(virtualScreen.Height, result.Height);
    }

    [Fact]
    public async Task CaptureAllMonitors_ReturnsCompositeMetadata()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata);
    }

    [Fact]
    public async Task CaptureAllMonitors_MetadataContainsVirtualScreenBounds()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata);
        Assert.NotNull(result.CompositeMetadata.VirtualScreen);

        Assert.Equal(virtualScreen.X, result.CompositeMetadata.VirtualScreen.X);
        Assert.Equal(virtualScreen.Y, result.CompositeMetadata.VirtualScreen.Y);
        Assert.Equal(virtualScreen.Width, result.CompositeMetadata.VirtualScreen.Width);
        Assert.Equal(virtualScreen.Height, result.CompositeMetadata.VirtualScreen.Height);
    }

    [Fact]
    public async Task CaptureAllMonitors_MetadataContainsMonitorRegions()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        var allMonitors = _monitorService.GetMonitors();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata);
        Assert.NotNull(result.CompositeMetadata.Monitors);
        Assert.Equal(allMonitors.Count, result.CompositeMetadata.Monitors.Count);
    }

    [Fact]
    public async Task CaptureAllMonitors_MonitorRegionsHaveCorrectIndices()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        var allMonitors = _monitorService.GetMonitors();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata?.Monitors);

        for (int i = 0; i < allMonitors.Count; i++)
        {
            var region = result.CompositeMetadata.Monitors[i];
            Assert.Equal(allMonitors[i].Index, region.Index);
        }
    }

    [Fact]
    public async Task CaptureAllMonitors_MonitorRegionsHaveCorrectDimensions()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        var allMonitors = _monitorService.GetMonitors();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata?.Monitors);

        foreach (var monitor in allMonitors)
        {
            var region = result.CompositeMetadata.Monitors
                .FirstOrDefault(r => r.Index == monitor.Index);
            Assert.NotNull(region);
            Assert.Equal(monitor.Width, region.Width);
            Assert.Equal(monitor.Height, region.Height);
        }
    }

    [Fact]
    public async Task CaptureAllMonitors_MonitorRegionsHaveValidImageCoordinates()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata?.Monitors);

        foreach (var region in result.CompositeMetadata.Monitors)
        {
            // Image coordinates should be non-negative (offset from virtual screen origin)
            Assert.True(region.X >= 0, $"X should be >= 0, was {region.X}");
            Assert.True(region.Y >= 0, $"Y should be >= 0, was {region.Y}");

            // Image coordinates + dimensions should be within virtual screen bounds
            Assert.True(region.X + region.Width <= virtualScreen.Width,
                $"Region extends beyond virtual screen width");
            Assert.True(region.Y + region.Height <= virtualScreen.Height,
                $"Region extends beyond virtual screen height");
        }
    }

    [Fact]
    public async Task CaptureAllMonitors_PrimaryMonitorIsMarked()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompositeMetadata?.Monitors);

        var primaryRegions = result.CompositeMetadata.Monitors.Where(r => r.IsPrimary).ToList();
        Assert.Single(primaryRegions); // Exactly one primary monitor
    }

    [Fact]
    public async Task CaptureAllMonitors_WithIncludeCursor_ReturnsSuccess()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            IncludeCursor = true
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"All monitors capture with cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);
        Assert.NotNull(result.CompositeMetadata);
    }

    [Fact]
    public async Task CaptureAllMonitors_WithPngFormat_FormatIsPng()
    {
        // Arrange - explicitly request PNG format
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("png", result.Format);
    }
}
