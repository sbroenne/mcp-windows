using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Configuration;

/// <summary>
/// Configuration for screenshot capture operations.
/// </summary>
public sealed class ScreenshotConfiguration
{
    /// <summary>
    /// Environment variable name for timeout configuration.
    /// </summary>
    public const string TimeoutEnvVar = "MCP_WINDOWS_SCREENSHOT_TIMEOUT_MS";

    /// <summary>
    /// Environment variable name for maximum pixels configuration.
    /// </summary>
    public const string MaxPixelsEnvVar = "MCP_WINDOWS_SCREENSHOT_MAX_PIXELS";

    /// <summary>
    /// Default timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Default maximum pixels (8K resolution: 7680 × 4320).
    /// </summary>
    public const int DefaultMaxPixels = 33_177_600;

    /// <summary>
    /// Default image format for LLM-optimized captures.
    /// </summary>
    public const ImageFormat DefaultImageFormat = ImageFormat.Jpeg;

    /// <summary>
    /// Default JPEG quality (1-100). 85 provides optimal balance between size and quality.
    /// </summary>
    public const int DefaultQuality = 85;

    /// <summary>
    /// Default maximum width for auto-scaling. 1568 is Claude's high-res native limit.
    /// Set to 0 to disable scaling.
    /// </summary>
    public const int DefaultMaxWidth = 1568;

    /// <summary>
    /// Default maximum height for auto-scaling. 0 means no height constraint.
    /// </summary>
    public const int DefaultMaxHeight = 0;

    /// <summary>
    /// Default output mode (inline base64).
    /// </summary>
    public const OutputMode DefaultOutputMode = OutputMode.Inline;

    /// <summary>
    /// Gets the operation timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; init; } = DefaultTimeoutMs;

    /// <summary>
    /// Gets the maximum number of pixels allowed in a capture.
    /// </summary>
    public int MaxPixels { get; init; } = DefaultMaxPixels;

    /// <summary>
    /// Creates a configuration instance from environment variables.
    /// </summary>
    /// <returns>A new <see cref="ScreenshotConfiguration"/> instance.</returns>
    public static ScreenshotConfiguration FromEnvironment()
    {
        var timeoutMs = GetEnvInt(TimeoutEnvVar, DefaultTimeoutMs);
        var maxPixels = GetEnvInt(MaxPixelsEnvVar, DefaultMaxPixels);

        // Ensure reasonable bounds
        if (timeoutMs < 100)
        {
            timeoutMs = 100;
        }

        if (timeoutMs > 60000)
        {
            timeoutMs = 60000;
        }

        if (maxPixels < 1)
        {
            maxPixels = DefaultMaxPixels;
        }

        return new ScreenshotConfiguration
        {
            TimeoutMs = timeoutMs,
            MaxPixels = maxPixels
        };
    }

    /// <summary>
    /// Gets an integer value from an environment variable with a default fallback.
    /// </summary>
    private static int GetEnvInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
