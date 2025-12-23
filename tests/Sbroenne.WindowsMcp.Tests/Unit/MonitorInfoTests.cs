using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="MonitorInfo"/> and related functionality.
/// </summary>
public sealed class MonitorInfoTests
{
    [Theory]
    [InlineData(@"\\.\DISPLAY1", 1)]
    [InlineData(@"\\.\DISPLAY2", 2)]
    [InlineData(@"\\.\DISPLAY10", 10)]
    [InlineData(@"\\.\DISPLAY99", 99)]
    [InlineData("DISPLAY1", 1)]
    [InlineData("display2", 2)]
    [InlineData("Display3", 3)]
    public void ExtractDisplayNumber_ValidDeviceName_ReturnsNumber(string deviceName, int expectedNumber)
    {
        // Act
        var result = MonitorInfo.ExtractDisplayNumber(deviceName);

        // Assert
        Assert.Equal(expectedNumber, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractDisplayNumber_NullOrEmpty_ReturnsZero(string? deviceName)
    {
        // Act
        var result = MonitorInfo.ExtractDisplayNumber(deviceName);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("MONITOR1")]
    [InlineData("SCREEN2")]
    [InlineData(@"\\.\MONITOR1")]
    [InlineData("unknown")]
    [InlineData("DISPLAY")]
    [InlineData("DISPLAYone")]
    public void ExtractDisplayNumber_InvalidDeviceName_ReturnsZero(string deviceName)
    {
        // Act
        var result = MonitorInfo.ExtractDisplayNumber(deviceName);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void MonitorInfo_SerializesToJson_WithSnakeCaseProperties()
    {
        // Arrange
        var monitor = new MonitorInfo(
            Index: 0,
            DisplayNumber: 1,
            DeviceName: @"\\.\DISPLAY1",
            PhysicalWidth: 1920,
            PhysicalHeight: 1080,
            Width: 1920,
            Height: 1080,
            X: 0,
            Y: 0,
            IsPrimary: true);

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(monitor);

        // Assert - physical dimensions are hidden (JsonIgnore), only logical dimensions exposed
        Assert.Contains("\"index\":0", json);
        Assert.Contains("\"display_number\":1", json);
        Assert.Contains("\"device_name\":", json);
        Assert.Contains("\"width\":1920", json);
        Assert.Contains("\"height\":1080", json);
        Assert.Contains("\"x\":0", json);
        Assert.Contains("\"y\":0", json);
        Assert.Contains("\"is_primary\":true", json);
        // Physical dimensions should NOT be in JSON (hidden from LLMs)
        Assert.DoesNotContain("physical_width", json);
        Assert.DoesNotContain("physical_height", json);
    }

    [Fact]
    public void MonitorInfo_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var monitor1 = new MonitorInfo(0, 1, @"\\.\.DISPLAY1", 1920, 1080, 1920, 1080, 0, 0, true);
        var monitor2 = new MonitorInfo(0, 1, @"\\.\.DISPLAY1", 1920, 1080, 1920, 1080, 0, 0, true);
        var monitor3 = new MonitorInfo(1, 2, @"\\.\.DISPLAY2", 1920, 1080, 1920, 1080, 1920, 0, false);

        // Assert
        Assert.Equal(monitor1, monitor2);
        Assert.NotEqual(monitor1, monitor3);
    }

    [Fact]
    public void MonitorInfo_ToString_ContainsRelevantInfo()
    {
        // Arrange
        var monitor = new MonitorInfo(0, 1, @"\\.\.DISPLAY1", 1920, 1080, 1920, 1080, 0, 0, true);

        // Act
        var result = monitor.ToString();

        // Assert - record ToString() includes all properties
        Assert.Contains("Index", result);
        Assert.Contains("DisplayNumber", result);
        Assert.Contains("1920", result);
        Assert.Contains("1080", result);
        Assert.Contains("IsPrimary", result);
    }
}
