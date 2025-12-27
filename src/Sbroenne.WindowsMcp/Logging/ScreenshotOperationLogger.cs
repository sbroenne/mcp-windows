using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Logging;

/// <summary>
/// Provides structured logging for screenshot operations.
/// Note: Image data is never logged per Constitution Principle XI.
/// </summary>
public sealed partial class ScreenshotOperationLogger
{
    private readonly ILogger<ScreenshotOperationLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotOperationLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ScreenshotOperationLogger(ILogger<ScreenshotOperationLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the start of a screenshot operation.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Screenshot operation started: Action={Action}, Target={Target}")]
    public partial void LogOperationStarted(ScreenshotAction action, CaptureTarget target);

    /// <summary>
    /// Logs a successful capture with dimensions (but not image data).
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Screenshot captured successfully: {Width}x{Height} pixels")]
    public partial void LogCaptureSuccess(int width, int height);

    /// <summary>
    /// Logs a monitor list operation.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {MonitorCount} monitors")]
    public partial void LogMonitorListSuccess(int monitorCount);

    /// <summary>
    /// Logs an operation error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Screenshot operation failed: {ErrorCode} - {ErrorMessage}")]
    public partial void LogOperationError(string errorCode, string errorMessage);

    /// <summary>
    /// Logs secure desktop detection.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Screenshot blocked: Secure desktop is active")]
    public partial void LogSecureDesktopBlocked();

    /// <summary>
    /// Logs an invalid window handle.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid window handle: {WindowHandle}")]
    public partial void LogInvalidWindowHandle(long windowHandle);

    /// <summary>
    /// Logs a minimized window error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot capture minimized window: {WindowHandle}")]
    public partial void LogWindowMinimized(long windowHandle);

    /// <summary>
    /// Logs an invalid monitor index.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid monitor index: {MonitorIndex} (available: 0-{MaxIndex})")]
    public partial void LogInvalidMonitorIndex(int monitorIndex, int maxIndex);

    /// <summary>
    /// Logs an image size limit exceeded error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Image too large: {Width}x{Height} = {TotalPixels} pixels (max: {MaxPixels})")]
    public partial void LogImageTooLarge(int width, int height, long totalPixels, int maxPixels);

    /// <summary>
    /// Logs a capture operation duration.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Screenshot capture completed in {DurationMs}ms")]
    public partial void LogCaptureDuration(long durationMs);

    /// <summary>
    /// Logs a PrintWindow failure with fallback.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "PrintWindow failed for handle {WindowHandle}, falling back to screen capture")]
    public partial void LogPrintWindowFailed(long windowHandle);
}
