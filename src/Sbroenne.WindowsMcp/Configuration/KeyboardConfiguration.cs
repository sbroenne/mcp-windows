namespace Sbroenne.WindowsMcp.Configuration;

/// <summary>
/// Configuration options for keyboard operations.
/// </summary>
public sealed class KeyboardConfiguration
{
    /// <summary>
    /// The environment variable name for configuring operation timeout.
    /// </summary>
    public const string TimeoutEnvironmentVariable = "MCP_WINDOWS_KEYBOARD_TIMEOUT_MS";

    /// <summary>
    /// The environment variable name for configuring inter-key delay.
    /// </summary>
    public const string InterKeyDelayEnvironmentVariable = "MCP_WINDOWS_KEYBOARD_KEY_DELAY_MS";

    /// <summary>
    /// The environment variable name for configuring chunk delay for long text.
    /// </summary>
    public const string ChunkDelayEnvironmentVariable = "MCP_WINDOWS_KEYBOARD_CHUNK_DELAY_MS";

    /// <summary>
    /// The default operation timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 30000;

    /// <summary>
    /// The default inter-key delay in milliseconds for sequence operations.
    /// </summary>
    public const int DefaultInterKeyDelayMs = 10;

    /// <summary>
    /// The default delay between text chunks in milliseconds.
    /// </summary>
    public const int DefaultChunkDelayMs = 50;

    /// <summary>
    /// The maximum number of characters to type in a single chunk.
    /// </summary>
    public const int TextChunkSize = 1000;

    /// <summary>
    /// Gets the operation timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; init; }

    /// <summary>
    /// Gets the inter-key delay in milliseconds for sequence operations.
    /// </summary>
    public int InterKeyDelayMs { get; init; }

    /// <summary>
    /// Gets the delay between text chunks in milliseconds for long text typing.
    /// </summary>
    public int ChunkDelayMs { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardConfiguration"/> class
    /// with default values.
    /// </summary>
    public KeyboardConfiguration()
    {
        TimeoutMs = DefaultTimeoutMs;
        InterKeyDelayMs = DefaultInterKeyDelayMs;
        ChunkDelayMs = DefaultChunkDelayMs;
    }

    /// <summary>
    /// Creates a <see cref="KeyboardConfiguration"/> instance from environment variables.
    /// </summary>
    /// <returns>A configuration instance with values from environment variables or defaults.</returns>
    public static KeyboardConfiguration FromEnvironment()
    {
        var timeoutMs = DefaultTimeoutMs;
        var interKeyDelayMs = DefaultInterKeyDelayMs;
        var chunkDelayMs = DefaultChunkDelayMs;

        var timeoutEnv = Environment.GetEnvironmentVariable(TimeoutEnvironmentVariable);
        if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var parsedTimeout) && parsedTimeout > 0)
        {
            timeoutMs = parsedTimeout;
        }

        var interKeyDelayEnv = Environment.GetEnvironmentVariable(InterKeyDelayEnvironmentVariable);
        if (!string.IsNullOrEmpty(interKeyDelayEnv) && int.TryParse(interKeyDelayEnv, out var parsedDelay) && parsedDelay >= 0)
        {
            interKeyDelayMs = parsedDelay;
        }

        var chunkDelayEnv = Environment.GetEnvironmentVariable(ChunkDelayEnvironmentVariable);
        if (!string.IsNullOrEmpty(chunkDelayEnv) && int.TryParse(chunkDelayEnv, out var parsedChunkDelay) && parsedChunkDelay >= 0)
        {
            chunkDelayMs = parsedChunkDelay;
        }

        return new KeyboardConfiguration
        {
            TimeoutMs = timeoutMs,
            InterKeyDelayMs = interKeyDelayMs,
            ChunkDelayMs = chunkDelayMs
        };
    }
}
