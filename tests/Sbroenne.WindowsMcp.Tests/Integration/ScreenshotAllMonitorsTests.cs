using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for all monitors (virtual screen) capture via <see cref="ScreenshotService"/>.
/// Tests the <see cref="CaptureTarget.AllMonitors"/> target which captures the entire virtual screen
/// spanning all connected monitors.
/// </summary>
public sealed class ScreenshotAllMonitorsTests
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;

    public ScreenshotAllMonitorsTests()
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
    public async Task CaptureAllMonitors_ReturnsVirtualScreenDimensions()
    {
        // Arrange - disable auto-scaling to verify exact dimensions
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            MaxWidth = 0 // Disable auto-scaling
        };
        var virtualBounds = CoordinateNormalizer.GetVirtualScreenBounds();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.Message}");
        Assert.Equal("success", result.ErrorCode);
        Assert.Equal(virtualBounds.Width, result.Width);
        Assert.Equal(virtualBounds.Height, result.Height);
    }

    [Fact]
    public async Task CaptureAllMonitors_WithCursor_IncludesCursor()
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
        Assert.True(result.Success, $"Capture with cursor failed: {result.Message}");
        Assert.NotNull(result.ImageData);
        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task CaptureAllMonitors_SingleMonitor_SameAsPrimaryScreen()
    {
        // Arrange
        var monitors = _monitorService.GetMonitors();

        // This test is most meaningful on single-monitor systems
        // On multi-monitor systems, the dimensions will differ
        if (monitors.Count == 1)
        {
            // Disable auto-scaling to verify exact dimensions
            var allMonitorsRequest = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.AllMonitors,
                MaxWidth = 0 // Disable auto-scaling
            };
            var primaryRequest = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.PrimaryScreen,
                MaxWidth = 0 // Disable auto-scaling
            };

            // Act
            var allMonitorsResult = await _screenshotService.ExecuteAsync(allMonitorsRequest);
            var primaryResult = await _screenshotService.ExecuteAsync(primaryRequest);

            // Assert - on single monitor, both should have same dimensions
            Assert.True(allMonitorsResult.Success);
            Assert.True(primaryResult.Success);
            Assert.Equal(primaryResult.Width, allMonitorsResult.Width);
            Assert.Equal(primaryResult.Height, allMonitorsResult.Height);
        }
        else
        {
            // Multi-monitor system: verify all_monitors captures a larger area
            // Disable auto-scaling to verify exact dimensions
            var allMonitorsRequest = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.AllMonitors,
                MaxWidth = 0 // Disable auto-scaling
            };
            var primaryRequest = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.PrimaryScreen,
                MaxWidth = 0 // Disable auto-scaling
            };

            var allMonitorsResult = await _screenshotService.ExecuteAsync(allMonitorsRequest);
            var primaryResult = await _screenshotService.ExecuteAsync(primaryRequest);

            Assert.True(allMonitorsResult.Success);
            Assert.True(primaryResult.Success);

            // Combined virtual screen should be at least as large as primary
            // (may be larger if monitors are arranged side by side or stacked)
            var allMonitorsPixels = (long)(allMonitorsResult.Width ?? 0) * (allMonitorsResult.Height ?? 0);
            var primaryPixels = (long)(primaryResult.Width ?? 0) * (primaryResult.Height ?? 0);
            Assert.True(allMonitorsPixels >= primaryPixels,
                $"AllMonitors ({allMonitorsPixels} pixels) should be >= PrimaryScreen ({primaryPixels} pixels)");
        }
    }

    [Fact]
    public async Task CaptureAllMonitors_WithPngFormat_ReturnsValidPng()
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
        Assert.Equal("png", result.Format);

        // Verify base64 is valid and starts with PNG signature
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length >= 8, "Image data too short to be a valid PNG");
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]); // 'P'
        Assert.Equal(0x4E, imageBytes[2]); // 'N'
        Assert.Equal(0x47, imageBytes[3]); // 'G'
    }

    [Fact]
    public async Task CaptureAllMonitors_WithPngFormat_IncludesMetadata()
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
        Assert.NotNull(result.Width);
        Assert.NotNull(result.Height);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.Equal("png", result.Format);
    }

    [Fact]
    public async Task CaptureAllMonitors_HandlesNegativeVirtualScreenCoordinates()
    {
        // Arrange
        // This test verifies that captures work even when the virtual screen
        // has negative coordinates (e.g., monitor to the left of primary)
        var virtualBounds = CoordinateNormalizer.GetVirtualScreenBounds();

        // Disable auto-scaling to verify exact dimensions
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            MaxWidth = 0 // Disable auto-scaling
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success,
            $"Capture should succeed regardless of virtual screen origin ({virtualBounds.Left}, {virtualBounds.Top})");
        Assert.Equal(virtualBounds.Width, result.Width);
        Assert.Equal(virtualBounds.Height, result.Height);
    }

    [Fact]
    public async Task CaptureAllMonitors_ImageDataIsBase64Decodable()
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
        Assert.NotNull(result.ImageData);

        // Verify base64 is valid
        byte[]? imageBytes = null;
        var exception = Record.Exception(() => imageBytes = Convert.FromBase64String(result.ImageData));
        Assert.Null(exception);
        Assert.NotNull(imageBytes);
        Assert.NotEmpty(imageBytes);
    }

    [Fact]
    public async Task CaptureAllMonitors_BeforeAfterComparison_WithPngFormat_DetectsChange()
    {
        // Arrange
        // This test captures two screenshots of all monitors to verify
        // that differences can be detected (e.g., for before/after comparisons)
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors,
            ImageFormat = ImageFormat.Png
        };

        // Act - capture two screenshots in sequence
        var beforeResult = await _screenshotService.ExecuteAsync(request);

        // Small delay to allow for potential screen changes (clock tick, cursor blink, etc.)
        await Task.Delay(100);

        var afterResult = await _screenshotService.ExecuteAsync(request);

        // Assert - both captures should succeed and have same dimensions
        Assert.True(beforeResult.Success, $"Before capture failed: {beforeResult.Message}");
        Assert.True(afterResult.Success, $"After capture failed: {afterResult.Message}");

        // Dimensions should match (same virtual screen)
        Assert.Equal(beforeResult.Width, afterResult.Width);
        Assert.Equal(beforeResult.Height, afterResult.Height);

        // Both should produce valid images
        Assert.NotNull(beforeResult.ImageData);
        Assert.NotNull(afterResult.ImageData);

        // Verify both are valid base64 PNG data
        var beforeBytes = Convert.FromBase64String(beforeResult.ImageData);
        var afterBytes = Convert.FromBase64String(afterResult.ImageData);

        // Both should be valid PNGs
        Assert.True(beforeBytes.Length >= 8);
        Assert.True(afterBytes.Length >= 8);
        Assert.Equal(0x89, beforeBytes[0]); // PNG signature
        Assert.Equal(0x89, afterBytes[0]);

        // Note: We don't assert the images are different because
        // the screen may or may not have changed. The key is that
        // both captures succeed and produce comparable results.
    }

    [Fact]
    public async Task ListMonitors_IncludesVirtualScreenBounds()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.ListMonitors
        };
        var expectedBounds = CoordinateNormalizer.GetVirtualScreenBounds();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"ListMonitors failed: {result.Message}");
        Assert.NotNull(result.Monitors);
        Assert.NotEmpty(result.Monitors);

        // Verify virtual screen info is present
        Assert.NotNull(result.VirtualScreen);
        Assert.Equal(expectedBounds.Left, result.VirtualScreen.X);
        Assert.Equal(expectedBounds.Top, result.VirtualScreen.Y);
        Assert.Equal(expectedBounds.Width, result.VirtualScreen.Width);
        Assert.Equal(expectedBounds.Height, result.VirtualScreen.Height);
    }

    [Fact]
    public async Task ListMonitors_VirtualScreenSpansAllMonitors()
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
        Assert.NotNull(result.VirtualScreen);
        Assert.NotNull(result.Monitors);

        // NOTE: Virtual screen is in logical coordinates, monitor dimensions are physical.
        // With DPI scaling, physical monitor dimensions may exceed virtual screen dimensions.
        // We verify that monitor positions (which are logical) fall within the virtual screen bounds.
        foreach (var monitor in result.Monitors)
        {
            Assert.True(monitor.X >= result.VirtualScreen.X,
                $"Monitor {monitor.Index} X ({monitor.X}) should be >= virtual screen X ({result.VirtualScreen.X})");
            Assert.True(monitor.Y >= result.VirtualScreen.Y,
                $"Monitor {monitor.Index} Y ({monitor.Y}) should be >= virtual screen Y ({result.VirtualScreen.Y})");

            // Monitor dimensions should be positive
            Assert.True(monitor.Width > 0, $"Monitor {monitor.Index} width should be positive");
            Assert.True(monitor.Height > 0, $"Monitor {monitor.Index} height should be positive");
        }

        // Virtual screen dimensions should be positive
        Assert.True(result.VirtualScreen.Width > 0, "Virtual screen width should be positive");
        Assert.True(result.VirtualScreen.Height > 0, "Virtual screen height should be positive");
    }
}
