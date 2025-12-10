using Microsoft.Extensions.Logging;

namespace Sbroenne.WindowsMcp.Logging;

/// <summary>
/// High-performance logging methods for MCP client logging.
/// Uses LoggerMessage source generators for optimal performance.
/// </summary>
internal static partial class McpClientLoggerMessages
{
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Mouse operation started: {Action}")]
    public static partial void LogMouseOperationStarted(this ILogger logger, string action);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "Keyboard operation started: {Action}")]
    public static partial void LogKeyboardOperationStarted(this ILogger logger, string action);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Information,
        Message = "Window operation started: {Action}")]
    public static partial void LogWindowOperationStarted(this ILogger logger, string action);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Information,
        Message = "Screenshot operation started: Action={Action}, Target={Target}")]
    public static partial void LogScreenshotOperationStarted(this ILogger logger, string action, string target);
}
