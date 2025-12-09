namespace Sbroenne.WindowsMcp.Configuration;

/// <summary>
/// Configuration options for mouse operations.
/// </summary>
public sealed class MouseConfiguration
{
    /// <summary>
    /// The environment variable name for configuring operation timeout.
    /// </summary>
    public const string TimeoutEnvironmentVariable = "MCP_WINDOWS_MOUSE_TIMEOUT_MS";

    /// <summary>
    /// The default operation timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Gets the operation timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseConfiguration"/> class
    /// with default values.
    /// </summary>
    public MouseConfiguration()
    {
        TimeoutMs = DefaultTimeoutMs;
    }

    /// <summary>
    /// Creates a <see cref="MouseConfiguration"/> instance from environment variables.
    /// </summary>
    /// <returns>A configuration instance with values from environment variables or defaults.</returns>
    public static MouseConfiguration FromEnvironment()
    {
        var timeoutMs = DefaultTimeoutMs;

        var timeoutEnv = Environment.GetEnvironmentVariable(TimeoutEnvironmentVariable);
        if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var parsedTimeout) && parsedTimeout > 0)
        {
            timeoutMs = parsedTimeout;
        }

        return new MouseConfiguration { TimeoutMs = timeoutMs };
    }
}
