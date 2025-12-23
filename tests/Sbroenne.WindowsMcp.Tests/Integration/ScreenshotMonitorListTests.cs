using Sbroenne.WindowsMcp.Capture;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for monitor enumeration via <see cref="MonitorService"/>.
/// </summary>
public sealed class ScreenshotMonitorListTests
{
    private readonly MonitorService _monitorService;

    public ScreenshotMonitorListTests()
    {
        _monitorService = new MonitorService();
    }

    [Fact]
    public void GetMonitors_ReturnsAtLeastOneMonitor()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        Assert.NotEmpty(monitors);
    }

    [Fact]
    public void GetMonitors_AllMonitorsHavePositiveResolution()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        foreach (var monitor in monitors)
        {
            Assert.True(monitor.Width > 0, $"Monitor {monitor.Index} has invalid width: {monitor.Width}");
            Assert.True(monitor.Height > 0, $"Monitor {monitor.Index} has invalid height: {monitor.Height}");
        }
    }

    [Fact]
    public void GetMonitors_IndicesAreSequential()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        for (var i = 0; i < monitors.Count; i++)
        {
            Assert.Equal(i, monitors[i].Index);
        }
    }

    [Fact]
    public void GetMonitors_ExactlyOnePrimaryMonitor()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        var primaryCount = monitors.Count(m => m.IsPrimary);
        Assert.Equal(1, primaryCount);
    }

    [Fact]
    public void GetMonitors_AllHaveDeviceName()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        foreach (var monitor in monitors)
        {
            Assert.False(string.IsNullOrEmpty(monitor.DeviceName), $"Monitor {monitor.Index} has no device name");
        }
    }

    [Fact]
    public void GetPrimaryMonitor_ReturnsPrimaryMonitor()
    {
        // Act
        var primary = _monitorService.GetPrimaryMonitor();

        // Assert
        Assert.True(primary.IsPrimary);
    }

    [Fact]
    public void GetPrimaryMonitor_HasValidDimensions()
    {
        // Act
        var primary = _monitorService.GetPrimaryMonitor();

        // Assert
        Assert.True(primary.Width > 0);
        Assert.True(primary.Height > 0);
    }

    [Fact]
    public void GetMonitor_ValidIndex_ReturnsMonitor()
    {
        // Act
        var monitor = _monitorService.GetMonitor(0);

        // Assert
        Assert.NotNull(monitor);
        Assert.Equal(0, monitor.Index);
    }

    [Fact]
    public void GetMonitor_InvalidNegativeIndex_ReturnsNull()
    {
        // Act
        var monitor = _monitorService.GetMonitor(-1);

        // Assert
        Assert.Null(monitor);
    }

    [Fact]
    public void GetMonitor_InvalidHighIndex_ReturnsNull()
    {
        // Arrange
        var count = _monitorService.MonitorCount;

        // Act
        var monitor = _monitorService.GetMonitor(count);

        // Assert
        Assert.Null(monitor);
    }

    [Fact]
    public void MonitorCount_MatchesGetMonitorsCount()
    {
        // Act
        var count = _monitorService.MonitorCount;
        var monitors = _monitorService.GetMonitors();

        // Assert
        Assert.Equal(count, monitors.Count);
    }

    [Fact]
    public void MonitorInfo_CanSerializeToJson()
    {
        // Arrange
        var monitor = _monitorService.GetPrimaryMonitor();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(monitor);

        // Assert
        Assert.Contains("\"index\"", json);
        Assert.Contains("\"device_name\"", json);
        Assert.Contains("\"width\"", json);
        Assert.Contains("\"height\"", json);
        Assert.Contains("\"is_primary\"", json);
    }

    [Fact]
    public void GetMonitor_SameIndexTwice_ReturnsSameData()
    {
        // Act
        var monitor1 = _monitorService.GetMonitor(0);
        var monitor2 = _monitorService.GetMonitor(0);

        // Assert
        Assert.NotNull(monitor1);
        Assert.NotNull(monitor2);
        Assert.Equal(monitor1, monitor2);
    }

    [Fact]
    public void GetMonitors_AllHaveValidDisplayNumber()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert
        foreach (var monitor in monitors)
        {
            Assert.True(
                monitor.DisplayNumber > 0,
                $"Monitor {monitor.Index} should have positive DisplayNumber, got: {monitor.DisplayNumber}");
        }
    }

    [Fact]
    public void GetMonitors_DisplayNumberMatchesDeviceName()
    {
        // Act
        var monitors = _monitorService.GetMonitors();

        // Assert - display number should be extracted from device name
        foreach (var monitor in monitors)
        {
            var expectedNumber = Sbroenne.WindowsMcp.Models.MonitorInfo.ExtractDisplayNumber(monitor.DeviceName);
            Assert.Equal(expectedNumber, monitor.DisplayNumber);
        }
    }

    [Fact]
    public void GetSecondaryMonitor_SingleMonitor_ReturnsNull()
    {
        // This test verifies behavior - with only one monitor, no secondary exists
        // Skip if multiple monitors are present
        if (_monitorService.MonitorCount > 1)
        {
            return; // Can't test single-monitor behavior on multi-monitor setup
        }

        // Act
        var secondary = _monitorService.GetSecondaryMonitor();

        // Assert
        Assert.Null(secondary);
    }

    [SkippableFact]
    public void GetSecondaryMonitor_MultipleMonitors_ReturnsNonPrimary()
    {
        // Skip if only one monitor
        Skip.If(_monitorService.MonitorCount < 2, "Test requires multiple monitors");

        // Act
        var secondary = _monitorService.GetSecondaryMonitor();

        // Assert
        Assert.NotNull(secondary);
        Assert.False(secondary.IsPrimary, "Secondary monitor should not be primary");
    }

    [SkippableFact]
    public void GetSecondaryMonitor_ExactlyTwoMonitors_ReturnsTheOtherOne()
    {
        // Skip if not exactly two monitors
        Skip.If(_monitorService.MonitorCount != 2, "Test requires exactly 2 monitors");

        // Arrange
        var primary = _monitorService.GetPrimaryMonitor();

        // Act
        var secondary = _monitorService.GetSecondaryMonitor();

        // Assert
        Assert.NotNull(secondary);
        Assert.NotEqual(primary.Index, secondary.Index);
    }

    [SkippableFact]
    public void GetSecondaryMonitor_ThreeOrMoreMonitors_ReturnsNull()
    {
        // Skip if not enough monitors
        Skip.If(_monitorService.MonitorCount < 3, "Test requires 3+ monitors");

        // Act - with 3+ monitors, secondary_screen is ambiguous
        var secondary = _monitorService.GetSecondaryMonitor();

        // Assert
        Assert.Null(secondary);
    }

    [Fact]
    public void MonitorInfo_CanSerializeToJson_IncludesDisplayNumber()
    {
        // Arrange
        var monitor = _monitorService.GetPrimaryMonitor();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(monitor);

        // Assert
        Assert.Contains("\"display_number\"", json);
    }
}
