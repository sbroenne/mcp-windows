using Sbroenne.WindowsMcp.Configuration;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the KeyboardConfiguration class.
/// </summary>
public class KeyboardConfigurationTests
{
    [Fact]
    public void DefaultConstructor_UsesDefaultValues()
    {
        // Act
        var config = new KeyboardConfiguration();

        // Assert
        Assert.Equal(KeyboardConfiguration.DefaultTimeoutMs, config.TimeoutMs);
        Assert.Equal(KeyboardConfiguration.DefaultInterKeyDelayMs, config.InterKeyDelayMs);
        Assert.Equal(KeyboardConfiguration.DefaultChunkDelayMs, config.ChunkDelayMs);
    }

    [Fact]
    public void DefaultTimeoutMs_Is30000()
    {
        // Assert
        Assert.Equal(30000, KeyboardConfiguration.DefaultTimeoutMs);
    }

    [Fact]
    public void DefaultInterKeyDelayMs_Is10()
    {
        // Assert
        Assert.Equal(10, KeyboardConfiguration.DefaultInterKeyDelayMs);
    }

    [Fact]
    public void DefaultChunkDelayMs_Is50()
    {
        // Assert
        Assert.Equal(50, KeyboardConfiguration.DefaultChunkDelayMs);
    }

    [Fact]
    public void TextChunkSize_Is1000()
    {
        // Assert
        Assert.Equal(1000, KeyboardConfiguration.TextChunkSize);
    }

    [Fact]
    public void FromEnvironment_NoVariablesSet_UsesDefaults()
    {
        // Arrange - clear any existing env vars
        Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(KeyboardConfiguration.ChunkDelayEnvironmentVariable, null);

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(KeyboardConfiguration.DefaultTimeoutMs, config.TimeoutMs);
            Assert.Equal(KeyboardConfiguration.DefaultInterKeyDelayMs, config.InterKeyDelayMs);
            Assert.Equal(KeyboardConfiguration.DefaultChunkDelayMs, config.ChunkDelayMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(KeyboardConfiguration.ChunkDelayEnvironmentVariable, null);
        }
    }

    [Fact]
    public void FromEnvironment_ValidTimeoutSet_UsesEnvironmentValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, "60000");

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(60000, config.TimeoutMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, null);
        }
    }

    [Fact]
    public void FromEnvironment_ValidInterKeyDelaySet_UsesEnvironmentValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, "20");

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(20, config.InterKeyDelayMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, null);
        }
    }

    [Fact]
    public void FromEnvironment_ValidChunkDelaySet_UsesEnvironmentValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(KeyboardConfiguration.ChunkDelayEnvironmentVariable, "100");

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(100, config.ChunkDelayMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.ChunkDelayEnvironmentVariable, null);
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("-100")]
    [InlineData("0")]
    public void FromEnvironment_InvalidTimeout_UsesDefault(string invalidValue)
    {
        // Arrange
        Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, invalidValue);

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(KeyboardConfiguration.DefaultTimeoutMs, config.TimeoutMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.TimeoutEnvironmentVariable, null);
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("-100")]
    public void FromEnvironment_InvalidInterKeyDelay_UsesDefault(string invalidValue)
    {
        // Arrange
        Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, invalidValue);

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(KeyboardConfiguration.DefaultInterKeyDelayMs, config.InterKeyDelayMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, null);
        }
    }

    [Fact]
    public void FromEnvironment_ZeroInterKeyDelay_IsAllowed()
    {
        // Arrange - zero is allowed for inter-key delay (means no delay)
        Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, "0");

        try
        {
            // Act
            var config = KeyboardConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(0, config.InterKeyDelayMs);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(KeyboardConfiguration.InterKeyDelayEnvironmentVariable, null);
        }
    }

    [Fact]
    public void EnvironmentVariableNames_AreCorrect()
    {
        // Assert
        Assert.Equal("MCP_WINDOWS_KEYBOARD_TIMEOUT_MS", KeyboardConfiguration.TimeoutEnvironmentVariable);
        Assert.Equal("MCP_WINDOWS_KEYBOARD_KEY_DELAY_MS", KeyboardConfiguration.InterKeyDelayEnvironmentVariable);
        Assert.Equal("MCP_WINDOWS_KEYBOARD_CHUNK_DELAY_MS", KeyboardConfiguration.ChunkDelayEnvironmentVariable);
    }
}
