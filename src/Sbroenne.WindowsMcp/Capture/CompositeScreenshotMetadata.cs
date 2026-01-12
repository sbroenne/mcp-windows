using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Metadata describing a composite screenshot that captures all connected monitors.
/// </summary>
public sealed record CompositeScreenshotMetadata
{
    /// <summary>
    /// Gets the ISO 8601 timestamp when the screenshot was captured.
    /// </summary>
    [JsonPropertyName("capture_time")]
    public required DateTimeOffset CaptureTime { get; init; }

    /// <summary>
    /// Gets the bounding rectangle of all monitors in virtual screen coordinates.
    /// </summary>
    [JsonPropertyName("virtual_screen")]
    public required VirtualScreenBounds VirtualScreen { get; init; }

    /// <summary>
    /// Gets the position and size of each monitor within the composite image.
    /// </summary>
    [JsonPropertyName("monitors")]
    public required IReadOnlyList<MonitorRegion> Monitors { get; init; }

    /// <summary>
    /// Gets the width of the composite image in pixels.
    /// </summary>
    [JsonPropertyName("image_width")]
    public required int ImageWidth { get; init; }

    /// <summary>
    /// Gets the height of the composite image in pixels.
    /// </summary>
    [JsonPropertyName("image_height")]
    public required int ImageHeight { get; init; }

    /// <summary>
    /// Gets whether the mouse cursor was included in the capture.
    /// </summary>
    [JsonPropertyName("included_cursor")]
    public bool IncludedCursor { get; init; }

    /// <summary>
    /// Gets the cursor position within the composite image (if IncludedCursor is true).
    /// </summary>
    [JsonPropertyName("cursor_position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Point? CursorPosition { get; init; }
}

/// <summary>
/// Represents the bounding rectangle of all monitors in virtual screen coordinates.
/// </summary>
public sealed record VirtualScreenBounds
{
    /// <summary>
    /// Gets the left edge in virtual screen coordinates (can be negative).
    /// </summary>
    [JsonPropertyName("x")]
    public required int X { get; init; }

    /// <summary>
    /// Gets the top edge in virtual screen coordinates (can be negative).
    /// </summary>
    [JsonPropertyName("y")]
    public required int Y { get; init; }

    /// <summary>
    /// Gets the total width spanning all monitors.
    /// </summary>
    [JsonPropertyName("width")]
    public required int Width { get; init; }

    /// <summary>
    /// Gets the total height spanning all monitors.
    /// </summary>
    [JsonPropertyName("height")]
    public required int Height { get; init; }
}

/// <summary>
/// Represents a monitor's region within a composite screenshot.
/// </summary>
public sealed record MonitorRegion
{
    /// <summary>
    /// Gets the monitor index (0-based).
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>
    /// Gets the X position of this monitor within the composite image.
    /// </summary>
    [JsonPropertyName("x")]
    public required int X { get; init; }

    /// <summary>
    /// Gets the Y position of this monitor within the composite image.
    /// </summary>
    [JsonPropertyName("y")]
    public required int Y { get; init; }

    /// <summary>
    /// Gets the width of this monitor in pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public required int Width { get; init; }

    /// <summary>
    /// Gets the height of this monitor in pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public required int Height { get; init; }

    /// <summary>
    /// Gets whether this is the primary monitor.
    /// </summary>
    [JsonPropertyName("is_primary")]
    public required bool IsPrimary { get; init; }

    /// <summary>
    /// Gets the Windows device name (e.g., '\\.\DISPLAY1').
    /// </summary>
    [JsonPropertyName("device_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceName { get; init; }

    /// <summary>
    /// Creates a MonitorRegion from a MonitorInfo, adjusting coordinates relative to the virtual screen origin.
    /// </summary>
    /// <param name="monitor">The monitor information.</param>
    /// <param name="virtualScreenX">The X coordinate of the virtual screen origin.</param>
    /// <param name="virtualScreenY">The Y coordinate of the virtual screen origin.</param>
    /// <returns>A MonitorRegion with coordinates relative to the composite image.</returns>
    /// <exception cref="ArgumentNullException">Thrown when monitor is null.</exception>
    public static MonitorRegion FromMonitorInfo(MonitorInfo monitor, int virtualScreenX, int virtualScreenY)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        return
        new()
        {
            Index = monitor.Index,
            X = monitor.X - virtualScreenX,
            Y = monitor.Y - virtualScreenY,
            Width = monitor.Width,
            Height = monitor.Height,
            IsPrimary = monitor.IsPrimary,
            DeviceName = monitor.DeviceName
        };
    }
}

/// <summary>
/// Represents a point coordinate.
/// </summary>
public sealed record Point
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    [JsonPropertyName("x")]
    public required int X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    [JsonPropertyName("y")]
    public required int Y { get; init; }
}