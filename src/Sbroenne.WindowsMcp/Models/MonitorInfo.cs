using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Describes a display device.
/// </summary>
/// <param name="Index">Zero-based API enumeration index (internal use - may not match Windows display number).</param>
/// <param name="DisplayNumber">Windows display number as shown in Settings (1, 2, 3...). Use this to target specific monitors.</param>
/// <param name="DeviceName">Windows device name (e.g., \\.\DISPLAY1).</param>
/// <param name="PhysicalWidth">Physical horizontal resolution in pixels (internal - do not use for coordinates).</param>
/// <param name="PhysicalHeight">Physical vertical resolution in pixels (internal - do not use for coordinates).</param>
/// <param name="Width">Monitor width in pixels. Screenshot dimensions match this. Use for mouse coordinates.</param>
/// <param name="Height">Monitor height in pixels. Screenshot dimensions match this. Use for mouse coordinates.</param>
/// <param name="X">Left edge X coordinate (virtual screen).</param>
/// <param name="Y">Top edge Y coordinate (virtual screen).</param>
/// <param name="IsPrimary">True if this is the primary monitor (main display with taskbar).</param>
public sealed record MonitorInfo(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("display_number")] int DisplayNumber,
    [property: JsonPropertyName("device_name")] string DeviceName,
    [property: JsonIgnore] int PhysicalWidth,
    [property: JsonIgnore] int PhysicalHeight,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("is_primary")] bool IsPrimary)
{
    /// <summary>
    /// Extracts the display number from a Windows device name.
    /// </summary>
    /// <param name="deviceName">The device name (e.g., \\.\DISPLAY1).</param>
    /// <returns>The display number, or 0 if extraction fails.</returns>
    public static int ExtractDisplayNumber(string? deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return 0;
        }

        // Match DISPLAY followed by digits (e.g., \\.\DISPLAY1 -> 1)
        var match = Regex.Match(deviceName, @"DISPLAY(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
        {
            return number;
        }

        return 0;
    }
}
