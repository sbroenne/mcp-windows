using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the screenshot operation to perform.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScreenshotAction>))]
public enum ScreenshotAction
{
    /// <summary>
    /// Capture screen, monitor, window, or region (default).
    /// </summary>
    [JsonStringEnumMemberName("capture")]
    Capture = 0,

    /// <summary>
    /// List available monitors with metadata.
    /// </summary>
    [JsonStringEnumMemberName("list_monitors")]
    ListMonitors = 1
}
