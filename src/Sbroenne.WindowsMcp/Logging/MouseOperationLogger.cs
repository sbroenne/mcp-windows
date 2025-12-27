using Microsoft.Extensions.Logging;

namespace Sbroenne.WindowsMcp.Logging;

/// <summary>
/// Provides structured logging for mouse operations with correlation ID support.
/// </summary>
public sealed partial class MouseOperationLogger
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseOperationLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying logger.</param>
    public MouseOperationLogger(ILogger<MouseOperationLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the start of a mouse operation.
    /// </summary>
    /// <param name="correlationId">The unique correlation ID for this operation.</param>
    /// <param name="action">The mouse action being performed.</param>
    /// <param name="parameters">Additional parameters for the operation.</param>
    public void LogOperationStart(string correlationId, string action, object? parameters = null)
    {
        LogOperationStarted(_logger, correlationId, action, parameters?.ToString());
    }

    /// <summary>
    /// Logs the successful completion of a mouse operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="action">The mouse action that was performed.</param>
    /// <param name="finalX">The final cursor X coordinate.</param>
    /// <param name="finalY">The final cursor Y coordinate.</param>
    /// <param name="windowTitle">Optional window title under the cursor.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogOperationSuccess(
        string correlationId,
        string action,
        int finalX,
        int finalY,
        string? windowTitle,
        long durationMs)
    {
        LogOperationSucceeded(_logger, correlationId, action, finalX, finalY, windowTitle, durationMs);
    }

    /// <summary>
    /// Logs a failed mouse operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="action">The mouse action that was attempted.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogOperationFailure(
        string correlationId,
        string action,
        string errorCode,
        string errorMessage,
        long durationMs)
    {
        LogOperationFailed(_logger, correlationId, action, errorCode, errorMessage, durationMs);
    }

    /// <summary>
    /// Logs an unexpected exception during a mouse operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="action">The mouse action that was attempted.</param>
    /// <param name="exception">The exception that occurred.</param>
    public void LogOperationException(string correlationId, string action, Exception exception)
    {
        LogOperationError(_logger, correlationId, action, exception);
    }

    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    /// <returns>A new unique correlation ID.</returns>
    public static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mouse operation started. CorrelationId={CorrelationId}, Action={Action}, Parameters={Parameters}")]
    private static partial void LogOperationStarted(ILogger logger, string correlationId, string action, string? parameters);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mouse operation succeeded. CorrelationId={CorrelationId}, Action={Action}, FinalPosition=({FinalX}, {FinalY}), WindowTitle={WindowTitle}, DurationMs={DurationMs}")]
    private static partial void LogOperationSucceeded(ILogger logger, string correlationId, string action, int finalX, int finalY, string? windowTitle, long durationMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mouse operation failed. CorrelationId={CorrelationId}, Action={Action}, ErrorCode={ErrorCode}, Error={Error}, DurationMs={DurationMs}")]
    private static partial void LogOperationFailed(ILogger logger, string correlationId, string action, string errorCode, string error, long durationMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Mouse operation threw exception. CorrelationId={CorrelationId}, Action={Action}")]
    private static partial void LogOperationError(ILogger logger, string correlationId, string action, Exception exception);
}
