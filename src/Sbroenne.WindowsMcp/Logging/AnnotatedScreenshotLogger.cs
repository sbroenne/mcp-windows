using Microsoft.Extensions.Logging;

namespace Sbroenne.WindowsMcp.Logging;

/// <summary>
/// Provides structured logging for annotated screenshot operations.
/// Note: Image data is never logged per Constitution Principle XI.
/// </summary>
public sealed partial class AnnotatedScreenshotLogger
{
    private readonly ILogger<AnnotatedScreenshotLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnotatedScreenshotLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AnnotatedScreenshotLogger(ILogger<AnnotatedScreenshotLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the start of an annotated screenshot capture.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Capturing annotated screenshot for window handle: {WindowHandle}")]
    public partial void LogCaptureStarted(nint? windowHandle);

    /// <summary>
    /// Logs successful annotated screenshot creation.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created annotated screenshot with {ElementCount} elements")]
    public partial void LogCaptureSuccess(int elementCount);

    /// <summary>
    /// Logs an annotated screenshot capture error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to capture annotated screenshot: {ErrorMessage}")]
    public partial void LogCaptureError(Exception? exception, string errorMessage);
}
