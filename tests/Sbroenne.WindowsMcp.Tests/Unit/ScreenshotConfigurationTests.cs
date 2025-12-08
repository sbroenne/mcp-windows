using Sbroenne.WindowsMcp.Configuration;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ScreenshotConfiguration"/>.
/// </summary>
public sealed class ScreenshotConfigurationTests
{
    [Fact]
    public void FromEnvironment_NoVariablesSet_ReturnsDefaults()
    {
        // Arrange - ensure env vars are not set
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, null);
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.MaxPixelsEnvVar, null);

        // Act
        var config = ScreenshotConfiguration.FromEnvironment();

        // Assert
        Assert.Equal(ScreenshotConfiguration.DefaultTimeoutMs, config.TimeoutMs);
        Assert.Equal(ScreenshotConfiguration.DefaultMaxPixels, config.MaxPixels);
    }

    [Theory]
    [InlineData("1000", 1000)]
    [InlineData("5000", 5000)]
    [InlineData("10000", 10000)]
    public void FromEnvironment_ValidTimeoutValue_ParsesCorrectly(string envValue, int expected)
    {
        // Arrange
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, envValue);

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(expected, config.TimeoutMs);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, null);
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("   ")]
    public void FromEnvironment_InvalidTimeoutValue_ReturnsDefault(string envValue)
    {
        // Arrange
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, envValue);

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(ScreenshotConfiguration.DefaultTimeoutMs, config.TimeoutMs);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, null);
        }
    }

    [Fact]
    public void FromEnvironment_TimeoutTooLow_ClampsToMinimum()
    {
        // Arrange - value below minimum
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, "50");

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert - should clamp to 100ms minimum
            Assert.Equal(100, config.TimeoutMs);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, null);
        }
    }

    [Fact]
    public void FromEnvironment_TimeoutTooHigh_ClampsToMaximum()
    {
        // Arrange - value above maximum
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, "120000");

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert - should clamp to 60000ms maximum
            Assert.Equal(60000, config.TimeoutMs);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.TimeoutEnvVar, null);
        }
    }

    [Theory]
    [InlineData("1000000", 1000000)]
    [InlineData("33177600", 33177600)]
    public void FromEnvironment_ValidMaxPixelsValue_ParsesCorrectly(string envValue, int expected)
    {
        // Arrange
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.MaxPixelsEnvVar, envValue);

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(expected, config.MaxPixels);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.MaxPixelsEnvVar, null);
        }
    }

    [Fact]
    public void FromEnvironment_MaxPixelsTooLow_ReturnsDefault()
    {
        // Arrange - value is 0 or negative
        Environment.SetEnvironmentVariable(ScreenshotConfiguration.MaxPixelsEnvVar, "0");

        try
        {
            // Act
            var config = ScreenshotConfiguration.FromEnvironment();

            // Assert - should return default
            Assert.Equal(ScreenshotConfiguration.DefaultMaxPixels, config.MaxPixels);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ScreenshotConfiguration.MaxPixelsEnvVar, null);
        }
    }

    [Fact]
    public void DefaultMaxPixels_Equals8KResolution()
    {
        // 8K resolution: 7680 x 4320 = 33,177,600
        const int expected8KPixels = 7680 * 4320;
        Assert.Equal(expected8KPixels, ScreenshotConfiguration.DefaultMaxPixels);
    }

    [Fact]
    public void DefaultTimeoutMs_Equals5Seconds()
    {
        Assert.Equal(5000, ScreenshotConfiguration.DefaultTimeoutMs);
    }
}
