using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines how a process listing is ordered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProcessSortBy>))]
public enum ProcessSortBy
{
    /// <summary>Order by working-set memory, largest first (default).</summary>
    [JsonStringEnumMemberName("memory")]
    Memory,

    /// <summary>Order by process name, ascending (case-insensitive).</summary>
    [JsonStringEnumMemberName("name")]
    Name,

    /// <summary>Order by process id, ascending.</summary>
    [JsonStringEnumMemberName("pid")]
    Pid
}
