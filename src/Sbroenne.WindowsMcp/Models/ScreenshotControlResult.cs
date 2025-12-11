using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Capture;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output model for screenshot operations.
/// </summary>
public sealed record ScreenshotControlResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error classification.
    /// </summary>
    [JsonPropertyName("error_code")]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets a human-readable description.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the base64-encoded image data. Null when OutputMode is File.
    /// </summary>
    [JsonPropertyName("image_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageData { get; init; }

    /// <summary>
    /// Gets the output image width in pixels (after scaling if applied). Present on capture success.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; init; }

    /// <summary>
    /// Gets the output image height in pixels (after scaling if applied). Present on capture success.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; init; }

    /// <summary>
    /// Gets the original capture width before scaling. Present on capture success.
    /// </summary>
    [JsonPropertyName("original_width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OriginalWidth { get; init; }

    /// <summary>
    /// Gets the original capture height before scaling. Present on capture success.
    /// </summary>
    [JsonPropertyName("original_height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OriginalHeight { get; init; }

    /// <summary>
    /// Gets the image format ("jpeg" or "png"). Present on capture success.
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }

    /// <summary>
    /// Gets the file size in bytes. Present on capture success.
    /// </summary>
    [JsonPropertyName("file_size_bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the file path when OutputMode is File. Null for inline mode.
    /// </summary>
    [JsonPropertyName("file_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the list of available monitors. Present on ListMonitors action.
    /// </summary>
    [JsonPropertyName("monitors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MonitorInfo>? Monitors { get; init; }

    /// <summary>
    /// Gets the list of available monitors as a hint. Present on InvalidMonitorIndex error.
    /// </summary>
    [JsonPropertyName("available_monitors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MonitorInfo>? AvailableMonitors { get; init; }

    /// <summary>
    /// Gets the virtual screen bounds spanning all monitors. Present on ListMonitors action.
    /// </summary>
    [JsonPropertyName("virtual_screen")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VirtualScreenInfo? VirtualScreen { get; init; }

    /// <summary>
    /// Gets the composite screenshot metadata. Present on all-monitors capture success.
    /// Contains monitor region information for each display in the composite image.
    /// </summary>
    [JsonPropertyName("composite_metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompositeScreenshotMetadata? CompositeMetadata { get; init; }

    /// <summary>
    /// Creates a successful capture result with LLM optimization metadata.
    /// </summary>
    /// <param name="processed">The processed image data and metadata.</param>
    /// <param name="message">Human-readable success message.</param>
    /// <param name="filePath">File path if output mode is file, null for inline.</param>
    /// <returns>A successful capture result.</returns>
    public static ScreenshotControlResult CaptureSuccess(
        ProcessedImage processed,
        string message,
        string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(processed);
        var wasScaled = processed.OriginalWidth != processed.Width;
        return new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            ImageData = filePath is null ? Convert.ToBase64String(processed.Data) : null,
            Width = processed.Width,
            Height = processed.Height,
            OriginalWidth = wasScaled ? processed.OriginalWidth : null,
            OriginalHeight = wasScaled ? processed.OriginalHeight : null,
            Format = processed.Format,
            FileSizeBytes = processed.Data.Length,
            FilePath = filePath
        };
    }

    /// <summary>
    /// Creates a successful capture result (legacy method for backward compatibility).
    /// </summary>
    public static ScreenshotControlResult CaptureSuccess(string imageData, int width, int height, string format) =>
        new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = $"Captured {width}x{height} {format}",
            ImageData = imageData,
            Width = width,
            Height = height,
            Format = format
        };

    /// <summary>
    /// Creates a successful composite (all-monitors) capture result.
    /// </summary>
    /// <param name="processed">The processed image data and metadata.</param>
    /// <param name="metadata">Composite screenshot metadata with monitor regions.</param>
    /// <param name="message">Human-readable success message.</param>
    /// <param name="filePath">File path if output mode is file, null for inline.</param>
    /// <returns>A successful composite capture result.</returns>
    public static ScreenshotControlResult CompositeSuccess(
        ProcessedImage processed,
        CompositeScreenshotMetadata metadata,
        string message,
        string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(processed);
        ArgumentNullException.ThrowIfNull(metadata);
        var wasScaled = processed.OriginalWidth != processed.Width;
        return new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            ImageData = filePath is null ? Convert.ToBase64String(processed.Data) : null,
            Width = processed.Width,
            Height = processed.Height,
            OriginalWidth = wasScaled ? processed.OriginalWidth : null,
            OriginalHeight = wasScaled ? processed.OriginalHeight : null,
            Format = processed.Format,
            FileSizeBytes = processed.Data.Length,
            FilePath = filePath,
            CompositeMetadata = metadata
        };
    }

    /// <summary>
    /// Creates a successful composite (all-monitors) capture result (legacy method).
    /// </summary>
    public static ScreenshotControlResult CompositeSuccess(
        string imageData,
        CompositeScreenshotMetadata metadata,
        string message)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            ImageData = imageData,
            Width = metadata.ImageWidth,
            Height = metadata.ImageHeight,
            Format = "png",
            CompositeMetadata = metadata
        };
    }

    /// <summary>
    /// Creates a successful monitor list result.
    /// </summary>
    public static ScreenshotControlResult MonitorListSuccess(
        IReadOnlyList<MonitorInfo> monitors,
        VirtualScreenInfo virtualScreen,
        string message) =>
        new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            Monitors = monitors,
            VirtualScreen = virtualScreen
        };

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static ScreenshotControlResult Error(ScreenshotErrorCode errorCode, string message) =>
        new()
        {
            Success = false,
            ErrorCode = ToSnakeCase(errorCode),
            Message = message
        };

    /// <summary>
    /// Creates an error result with available monitors hint.
    /// </summary>
    public static ScreenshotControlResult ErrorWithMonitors(
        ScreenshotErrorCode errorCode,
        string message,
        IReadOnlyList<MonitorInfo> availableMonitors) =>
        new()
        {
            Success = false,
            ErrorCode = ToSnakeCase(errorCode),
            Message = message,
            AvailableMonitors = availableMonitors
        };

    /// <summary>
    /// Converts an error code enum value to snake_case string.
    /// </summary>
    private static string ToSnakeCase(ScreenshotErrorCode errorCode)
    {
        // Convert PascalCase to snake_case
        var name = errorCode.ToString();
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
