using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Configuration;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Input model for screenshot operations.
/// </summary>
public sealed record ScreenshotControlRequest
{
    /// <summary>
    /// Gets the operation to perform. Default is <see cref="ScreenshotAction.Capture"/>.
    /// </summary>
    [JsonPropertyName("action")]
    public ScreenshotAction Action { get; init; } = ScreenshotAction.Capture;

    /// <summary>
    /// Gets what to capture. Default is <see cref="CaptureTarget.PrimaryScreen"/>.
    /// </summary>
    [JsonPropertyName("target")]
    public CaptureTarget Target { get; init; } = CaptureTarget.PrimaryScreen;

    /// <summary>
    /// Gets the monitor index (0-based). Required when Target is <see cref="CaptureTarget.Monitor"/>.
    /// </summary>
    [JsonPropertyName("monitor_index")]
    public int? MonitorIndex { get; init; }

    /// <summary>
    /// Gets the window handle (HWND). Required when Target is <see cref="CaptureTarget.Window"/>.
    /// </summary>
    [JsonPropertyName("window_handle")]
    public long? WindowHandle { get; init; }

    /// <summary>
    /// Gets the region coordinates. Required when Target is <see cref="CaptureTarget.Region"/>.
    /// </summary>
    [JsonPropertyName("region")]
    public CaptureRegion? Region { get; init; }

    /// <summary>
    /// Gets whether to include the mouse cursor in the capture. Default is false.
    /// </summary>
    [JsonPropertyName("include_cursor")]
    public bool IncludeCursor { get; init; } = false;

    /// <summary>
    /// Gets the output image format. Default is JPEG (optimized for LLM consumption).
    /// </summary>
    [JsonPropertyName("image_format")]
    public ImageFormat ImageFormat { get; init; } = ScreenshotConfiguration.DefaultImageFormat;

    /// <summary>
    /// Gets the JPEG quality (1-100). Only applies when ImageFormat is Jpeg. Default is 85.
    /// </summary>
    [JsonPropertyName("quality")]
    public int Quality { get; init; } = ScreenshotConfiguration.DefaultQuality;

    /// <summary>
    /// Gets the maximum width in pixels. Image scaled down if wider (aspect ratio preserved).
    /// Default is 1568 (Claude's high-res native limit). Set to 0 to disable scaling.
    /// </summary>
    [JsonPropertyName("max_width")]
    public int MaxWidth { get; init; } = ScreenshotConfiguration.DefaultMaxWidth;

    /// <summary>
    /// Gets the maximum height in pixels. Image scaled down if taller (aspect ratio preserved).
    /// Default is 0 (no height constraint).
    /// </summary>
    [JsonPropertyName("max_height")]
    public int MaxHeight { get; init; } = ScreenshotConfiguration.DefaultMaxHeight;

    /// <summary>
    /// Gets the output mode. Default is inline (base64 in response).
    /// </summary>
    [JsonPropertyName("output_mode")]
    public OutputMode OutputMode { get; init; } = ScreenshotConfiguration.DefaultOutputMode;

    /// <summary>
    /// Gets the custom output file path. Only used when OutputMode is File.
    /// If null, generates temp file path automatically.
    /// </summary>
    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }
}
