using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the available actions for the <c>process</c> tool.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProcessAction>))]
public enum ProcessAction
{
    /// <summary>List running processes.</summary>
    [JsonStringEnumMemberName("list")]
    List,

    /// <summary>Terminate a running process by PID or name.</summary>
    [JsonStringEnumMemberName("kill")]
    Kill
}
