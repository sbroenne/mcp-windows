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
public sealed partial record MonitorInfo(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("displayNumber")] int DisplayNumber,
    [property: JsonPropertyName("deviceName")] string DeviceName,
    [property: JsonIgnore] int PhysicalWidth,
    [property: JsonIgnore] int PhysicalHeight,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary)
{
    /// <summary>Effective DPI of the monitor (96 = 100% scaling). Defaults to 96 when unavailable.</summary>
    [JsonPropertyName("effectiveDpi")]
    public int EffectiveDpi { get; init; } = 96;

    /// <summary>Display scale factor (EffectiveDpi / 96, e.g. 1.5 = 150%). Defaults to 1.0.</summary>
    [JsonPropertyName("scale")]
    public double Scale { get; init; } = 1.0;

    /// <summary>Orientation derived from the logical dimensions: "landscape" or "portrait".</summary>
    [JsonPropertyName("orientation")]
    public string Orientation { get; init; } = "landscape";

    /// <summary>
    /// The usable work area (the monitor rectangle minus the taskbar and docked app bars).
    /// Useful for placing windows where the taskbar will not cover them. Omitted when unknown.
    /// </summary>
    [JsonPropertyName("workArea")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkAreaInfo? WorkArea { get; init; }

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
        var match = MyRegex().Match(deviceName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
        {
            return number;
        }

        return 0;
    }

    [GeneratedRegex(@"DISPLAY(\d+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex();
}

/// <summary>The usable desktop rectangle of a monitor, excluding the taskbar and docked app bars.</summary>
/// <param name="X">Left edge X coordinate (virtual screen).</param>
/// <param name="Y">Top edge Y coordinate (virtual screen).</param>
/// <param name="Width">Work-area width in pixels.</param>
/// <param name="Height">Work-area height in pixels.</param>
public sealed record WorkAreaInfo(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);
