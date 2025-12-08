using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Describes a display device.
/// </summary>
/// <param name="Index">Zero-based monitor index.</param>
/// <param name="DeviceName">Windows device name (e.g., \\.\DISPLAY1).</param>
/// <param name="Width">Horizontal resolution in pixels.</param>
/// <param name="Height">Vertical resolution in pixels.</param>
/// <param name="X">Left edge X coordinate (virtual screen).</param>
/// <param name="Y">Top edge Y coordinate (virtual screen).</param>
/// <param name="IsPrimary">True if this is the primary monitor.</param>
public sealed record MonitorInfo(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("is_primary")] bool IsPrimary);
