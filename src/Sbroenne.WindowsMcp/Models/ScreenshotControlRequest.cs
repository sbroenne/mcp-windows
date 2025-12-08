using System.Text.Json.Serialization;

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
}
