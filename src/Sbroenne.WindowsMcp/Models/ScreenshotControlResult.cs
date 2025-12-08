using System.Text.Json.Serialization;

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
    /// Gets the base64-encoded PNG image. Present on capture success.
    /// </summary>
    [JsonPropertyName("image_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageData { get; init; }

    /// <summary>
    /// Gets the image width in pixels. Present on capture success.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; init; }

    /// <summary>
    /// Gets the image height in pixels. Present on capture success.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; init; }

    /// <summary>
    /// Gets the image format (always "png"). Present on capture success.
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }

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
    /// Creates a successful capture result.
    /// </summary>
    public static ScreenshotControlResult CaptureSuccess(string imageData, int width, int height, string message) =>
        new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            ImageData = imageData,
            Width = width,
            Height = height,
            Format = "png"
        };

    /// <summary>
    /// Creates a successful monitor list result.
    /// </summary>
    public static ScreenshotControlResult MonitorListSuccess(IReadOnlyList<MonitorInfo> monitors, string message) =>
        new()
        {
            Success = true,
            ErrorCode = ToSnakeCase(ScreenshotErrorCode.Success),
            Message = message,
            Monitors = monitors
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
