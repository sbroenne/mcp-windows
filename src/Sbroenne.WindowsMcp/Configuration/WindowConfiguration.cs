namespace Sbroenne.WindowsMcp.Configuration;

/// <summary>
/// Configuration options for window management operations.
/// </summary>
public sealed record WindowConfiguration
{
    /// <summary>
    /// The environment variable name for configuring default operation timeout.
    /// </summary>
    public const string TimeoutEnvironmentVariable = "MCP_WINDOWS_WINDOW_TIMEOUT_MS";

    /// <summary>
    /// The environment variable name for configuring wait_for timeout.
    /// </summary>
    public const string WaitForTimeoutEnvironmentVariable = "MCP_WINDOWS_WINDOW_WAITFOR_TIMEOUT_MS";

    /// <summary>
    /// The environment variable name for configuring property query timeout.
    /// </summary>
    public const string PropertyQueryTimeoutEnvironmentVariable = "MCP_WINDOWS_WINDOW_PROPERTY_TIMEOUT_MS";

    /// <summary>
    /// The default operation timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// The default timeout for wait_for action in milliseconds.
    /// </summary>
    public const int DefaultWaitForTimeoutMs = 30000;

    /// <summary>
    /// The default timeout for querying window properties (hung detection) in milliseconds.
    /// </summary>
    public const int DefaultPropertyQueryTimeoutMs = 100;

    /// <summary>
    /// The default polling interval for wait_for action in milliseconds.
    /// </summary>
    public const int DefaultWaitForPollIntervalMs = 100;

    /// <summary>
    /// The default maximum number of activation retry attempts.
    /// </summary>
    public const int DefaultActivationMaxRetries = 3;

    /// <summary>
    /// The default delay between activation retries in milliseconds.
    /// </summary>
    public const int DefaultActivationRetryDelayMs = 100;

    /// <summary>
    /// Gets the configured operation timeout in milliseconds.
    /// </summary>
    public int OperationTimeoutMs { get; init; } = DefaultTimeoutMs;

    /// <summary>
    /// Gets the timeout for wait_for action in milliseconds.
    /// </summary>
    public int WaitForTimeoutMs { get; init; } = DefaultWaitForTimeoutMs;

    /// <summary>
    /// Gets the timeout for querying window properties (hung detection) in milliseconds.
    /// </summary>
    public int PropertyQueryTimeoutMs { get; init; } = DefaultPropertyQueryTimeoutMs;

    /// <summary>
    /// Gets the polling interval for wait_for action in milliseconds.
    /// </summary>
    public int WaitForPollIntervalMs { get; init; } = DefaultWaitForPollIntervalMs;

    /// <summary>
    /// Gets the maximum number of activation retry attempts.
    /// </summary>
    public int ActivationMaxRetries { get; init; } = DefaultActivationMaxRetries;

    /// <summary>
    /// Gets the delay between activation retries in milliseconds.
    /// </summary>
    public int ActivationRetryDelayMs { get; init; } = DefaultActivationRetryDelayMs;

    /// <summary>
    /// Creates a <see cref="WindowConfiguration"/> instance from environment variables.
    /// </summary>
    /// <returns>A configuration instance with values from environment variables or defaults.</returns>
    public static WindowConfiguration FromEnvironment()
    {
        var config = new WindowConfiguration();

        var timeoutEnv = Environment.GetEnvironmentVariable(TimeoutEnvironmentVariable);
        if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var parsedTimeout) && parsedTimeout > 0)
        {
            config = config with { OperationTimeoutMs = parsedTimeout };
        }

        var waitForTimeoutEnv = Environment.GetEnvironmentVariable(WaitForTimeoutEnvironmentVariable);
        if (!string.IsNullOrEmpty(waitForTimeoutEnv) && int.TryParse(waitForTimeoutEnv, out var parsedWaitForTimeout) && parsedWaitForTimeout > 0)
        {
            config = config with { WaitForTimeoutMs = parsedWaitForTimeout };
        }

        var propertyTimeoutEnv = Environment.GetEnvironmentVariable(PropertyQueryTimeoutEnvironmentVariable);
        if (!string.IsNullOrEmpty(propertyTimeoutEnv) && int.TryParse(propertyTimeoutEnv, out var parsedPropertyTimeout) && parsedPropertyTimeout > 0)
        {
            config = config with { PropertyQueryTimeoutMs = parsedPropertyTimeout };
        }

        return config;
    }
}
