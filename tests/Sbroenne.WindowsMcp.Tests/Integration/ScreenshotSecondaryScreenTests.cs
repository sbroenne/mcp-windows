using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for secondary screen capture functionality.
/// </summary>
public sealed class ScreenshotSecondaryScreenTests
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;

    public ScreenshotSecondaryScreenTests()
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

    [SkippableFact]
    public async Task CaptureSecondaryScreen_TwoMonitors_ReturnsSuccess()
    {
        // Skip if not exactly two monitors
        Skip.If(_monitorService.MonitorCount != 2, "Test requires exactly 2 monitors");

        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.SecondaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Secondary screen capture failed: {result.Message}");
        Assert.NotNull(result.ImageData);
    }

    [SkippableFact]
    public async Task CaptureSecondaryScreen_TwoMonitors_ReturnsNonPrimaryMonitorDimensions()
    {
        // Skip if not exactly two monitors
        Skip.If(_monitorService.MonitorCount != 2, "Test requires exactly 2 monitors");

        // Arrange
        var secondary = _monitorService.GetSecondaryMonitor();
        Assert.NotNull(secondary);

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.SecondaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Secondary screen capture failed: {result.Message}");
        Assert.Equal(secondary.Width, result.Width);
        Assert.Equal(secondary.Height, result.Height);
    }

    [SkippableFact]
    public void CaptureSecondaryScreen_TwoMonitors_SecondaryIsNotPrimary()
    {
        // Skip if not exactly two monitors
        Skip.If(_monitorService.MonitorCount != 2, "Test requires exactly 2 monitors");

        // Arrange
        var primary = _monitorService.GetPrimaryMonitor();
        var secondary = _monitorService.GetSecondaryMonitor();
        Assert.NotNull(secondary);

        // The secondary should be a different monitor
        Assert.NotEqual(primary.Index, secondary.Index);
    }

    [Fact]
    public async Task CaptureSecondaryScreen_SingleMonitor_ReturnsHelpfulError()
    {
        // Skip if multiple monitors
        if (_monitorService.MonitorCount > 1)
        {
            return; // Can't test single-monitor behavior on multi-monitor setup
        }

        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.SecondaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("no_secondary_screen", result.ErrorCode);
        Assert.Contains("primary_screen", result.Message);
    }

    [SkippableFact]
    public async Task CaptureSecondaryScreen_ThreeOrMoreMonitors_ReturnsHelpfulError()
    {
        // Skip if not enough monitors
        Skip.If(_monitorService.MonitorCount < 3, "Test requires 3+ monitors");

        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.SecondaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("no_secondary_screen", result.ErrorCode);
        Assert.Contains("list_monitors", result.Message);
        Assert.NotNull(result.AvailableMonitors);
        Assert.True(result.AvailableMonitors.Count >= 3);
    }

    [Fact]
    public void CaptureTarget_SecondaryScreen_EnumValueExists()
    {
        // Assert that the enum value exists and has the expected value
        Assert.Equal(1, (int)CaptureTarget.SecondaryScreen);
    }
}
