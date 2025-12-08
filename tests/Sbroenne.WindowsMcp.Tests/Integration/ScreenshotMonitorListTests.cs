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
}
