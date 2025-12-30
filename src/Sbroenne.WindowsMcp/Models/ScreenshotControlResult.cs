using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Capture;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output model for screenshot operations.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - ok: Success
/// - ec: Error code
/// - msg: Message
/// - img: Image data (base64)
/// - w: Width
/// - h: Height
/// - ow: Original width
/// - oh: Original height
/// - fmt: Format
/// - sz: File size bytes
/// - fp: File path
/// - mon: Monitors list
/// - avail: Available monitors
/// - vs: Virtual screen
/// - cmp: Composite metadata
/// - ae: Annotated elements
/// - n: Element count
/// - hint: Usage hint
/// </remarks>
public sealed record ScreenshotControlResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("ok")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error classification.
    /// </summary>
    [JsonPropertyName("ec")]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets a human-readable description.
    /// </summary>
    [JsonPropertyName("msg")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the base64-encoded image data. Null when OutputMode is File.
    /// </summary>
    [JsonPropertyName("img")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageData { get; init; }

    /// <summary>
    /// Gets the output image width in pixels (after scaling if applied). Present on capture success.
    /// </summary>
    [JsonPropertyName("w")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; init; }

    /// <summary>
    /// Gets the output image height in pixels (after scaling if applied). Present on capture success.
    /// </summary>
    [JsonPropertyName("h")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; init; }

    /// <summary>
    /// Gets the original capture width before scaling. Present on capture success.
    /// </summary>
    [JsonPropertyName("ow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OriginalWidth { get; init; }

    /// <summary>
    /// Gets the original capture height before scaling. Present on capture success.
    /// </summary>
    [JsonPropertyName("oh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OriginalHeight { get; init; }

    /// <summary>
    /// Gets the image format ("jpeg" or "png"). Present on capture success.
    /// </summary>
    [JsonPropertyName("fmt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }

    /// <summary>
    /// Gets the file size in bytes. Present on capture success.
    /// </summary>
    [JsonPropertyName("sz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the file path when OutputMode is File. Null for inline mode.
    /// </summary>
    [JsonPropertyName("fp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the list of available monitors. Present on ListMonitors action.
    /// </summary>
    [JsonPropertyName("mon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MonitorInfo>? Monitors { get; init; }

    /// <summary>
    /// Gets the list of available monitors as a hint. Present on InvalidMonitorIndex error.
    /// </summary>
    [JsonPropertyName("avail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MonitorInfo>? AvailableMonitors { get; init; }

    /// <summary>
    /// Gets the virtual screen bounds spanning all monitors. Present on ListMonitors action.
    /// </summary>
    [JsonPropertyName("vs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VirtualScreenInfo? VirtualScreen { get; init; }

    /// <summary>
    /// Gets the composite screenshot metadata. Present on all-monitors capture success.
    /// Contains monitor region information for each display in the composite image.
    /// </summary>
    [JsonPropertyName("cmp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompositeScreenshotMetadata? CompositeMetadata { get; init; }

    /// <summary>
    /// Gets the list of annotated UI elements when annotate=true.
    /// Each element has an index matching the numbered label on the screenshot.
    /// </summary>
    [JsonPropertyName("ae")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<AnnotatedElement>? AnnotatedElements { get; init; }

    /// <summary>
    /// Gets the count of annotated elements. Present when annotate=true.
    /// </summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ElementCount { get; init; }

    /// <summary>
    /// Gets a usage hint for the LLM when annotations are present.
    /// </summary>
    [JsonPropertyName("hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UsageHint { get; init; }

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
    /// Creates a successful annotated screenshot result with element discovery.
    /// </summary>
    /// <param name="imageData">Base64-encoded image data (null if file output).</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="format">Image format (jpeg/png).</param>
    /// <param name="elements">List of annotated elements with indices.</param>
    /// <param name="filePath">File path if output mode is file.</param>
    /// <returns>A successful annotated capture result.</returns>
    public static ScreenshotControlResult AnnotatedSuccess(
        string? imageData,
        int width,
        int height,
        string format,
        IReadOnlyList<AnnotatedElement> elements,
        string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var elementCount = elements.Count;
        var usageHint = $"Screenshot with {elementCount} numbered elements. Reference elements by index (1-{elementCount}). " +
                        "Each element has an elementId for ui_automation operations (click, type, toggle).";
        if (filePath != null)
        {
            usageHint = $"Image saved to '{filePath}'. " + usageHint;
        }

        return new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = $"Captured {width}x{height} {format} with {elementCount} annotated elements",
            ImageData = imageData,
            Width = width,
            Height = height,
            Format = format,
            FilePath = filePath,
            AnnotatedElements = elements,
            ElementCount = elementCount,
            UsageHint = usageHint
        };
    }

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
