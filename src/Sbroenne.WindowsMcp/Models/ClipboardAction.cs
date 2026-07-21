using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the available clipboard actions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ClipboardAction>))]
public enum ClipboardAction
{
    /// <summary>Read the current clipboard text.</summary>
    [JsonStringEnumMemberName("get")]
    Get,

    /// <summary>Replace the clipboard contents with the supplied text.</summary>
    [JsonStringEnumMemberName("set")]
    Set,

    /// <summary>Clear the clipboard.</summary>
    [JsonStringEnumMemberName("clear")]
    Clear
}
