using Microsoft.Extensions.Logging;

namespace Sbroenne.WindowsMcp.Logging;

/// <summary>
/// Provides structured logging for keyboard operations with correlation ID support.
/// </summary>
public sealed partial class KeyboardOperationLogger
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardOperationLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying logger.</param>
    public KeyboardOperationLogger(ILogger<KeyboardOperationLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the start of a keyboard operation.
    /// </summary>
    /// <param name="correlationId">The unique correlation ID for this operation.</param>
    /// <param name="action">The keyboard action being performed.</param>
    /// <param name="parameters">Additional parameters for the operation.</param>
    public void LogOperationStart(string correlationId, string action, object? parameters = null)
    {
        LogOperationStarted(_logger, correlationId, action, parameters?.ToString());
    }

    /// <summary>
    /// Logs the successful completion of a type operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="charactersTyped">Number of characters typed.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogTypeSuccess(string correlationId, int charactersTyped, long durationMs)
    {
        LogTypeSucceeded(_logger, correlationId, charactersTyped, durationMs);
    }

    /// <summary>
    /// Logs the successful completion of a key press operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="keyPressed">The key that was pressed.</param>
    /// <param name="modifiers">Any modifier keys that were held.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogPressSuccess(string correlationId, string keyPressed, string? modifiers, long durationMs)
    {
        LogPressSucceeded(_logger, correlationId, keyPressed, modifiers, durationMs);
    }

    /// <summary>
    /// Logs the successful completion of a sequence operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="sequenceLength">Number of keys in the sequence.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogSequenceSuccess(string correlationId, int sequenceLength, long durationMs)
    {
        LogSequenceSucceeded(_logger, correlationId, sequenceLength, durationMs);
    }

    /// <summary>
    /// Logs the successful completion of a keyboard layout query.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="languageTag">The keyboard layout language tag.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void LogLayoutQuerySuccess(string correlationId, string languageTag, long durationMs)
    {
        LogLayoutQuerySucceeded(_logger, correlationId, languageTag, durationMs);
    }

    /// <summary>
    /// Logs a failed keyboard operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="action">The keyboard action that was attempted.</param>
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
    /// Logs an unexpected exception during a keyboard operation.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this operation.</param>
    /// <param name="action">The keyboard action that was attempted.</param>
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Keyboard operation started. CorrelationId={CorrelationId}, Action={Action}, Parameters={Parameters}")]
    private static partial void LogOperationStarted(ILogger logger, string correlationId, string action, string? parameters);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Type operation succeeded. CorrelationId={CorrelationId}, CharactersTyped={CharactersTyped}, DurationMs={DurationMs}")]
    private static partial void LogTypeSucceeded(ILogger logger, string correlationId, int charactersTyped, long durationMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Press operation succeeded. CorrelationId={CorrelationId}, KeyPressed={KeyPressed}, Modifiers={Modifiers}, DurationMs={DurationMs}")]
    private static partial void LogPressSucceeded(ILogger logger, string correlationId, string keyPressed, string? modifiers, long durationMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sequence operation succeeded. CorrelationId={CorrelationId}, SequenceLength={SequenceLength}, DurationMs={DurationMs}")]
    private static partial void LogSequenceSucceeded(ILogger logger, string correlationId, int sequenceLength, long durationMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Layout query succeeded. CorrelationId={CorrelationId}, LanguageTag={LanguageTag}, DurationMs={DurationMs}")]
    private static partial void LogLayoutQuerySucceeded(ILogger logger, string correlationId, string languageTag, long durationMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keyboard operation failed. CorrelationId={CorrelationId}, Action={Action}, ErrorCode={ErrorCode}, Error={Error}, DurationMs={DurationMs}")]
    private static partial void LogOperationFailed(ILogger logger, string correlationId, string action, string errorCode, string error, long durationMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Keyboard operation threw exception. CorrelationId={CorrelationId}, Action={Action}")]
    private static partial void LogOperationError(ILogger logger, string correlationId, string action, Exception exception);
}
