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

        // Act
        var config = ScreenshotConfiguration.FromEnvironment();

        // Assert
        Assert.Equal(ScreenshotConfiguration.DefaultTimeoutMs, config.TimeoutMs);
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

    [Fact]
    public void DefaultTimeoutMs_Equals5Seconds()
    {
        Assert.Equal(5000, ScreenshotConfiguration.DefaultTimeoutMs);
    }

    [Fact]
    public void DefaultQuality_Is40()
    {
        Assert.Equal(40, ScreenshotConfiguration.DefaultQuality);
    }
}
