using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// A compact description of a single running process, optimized for LLM token efficiency.
/// </summary>
/// <param name="Pid">The process id.</param>
/// <param name="Name">The process name (without the .exe extension, as reported by the OS).</param>
/// <param name="MemoryMb">Working-set (physical) memory in megabytes, rounded to one decimal place.</param>
public sealed record ProcessSummary(
    [property: JsonPropertyName("pid")] int Pid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("memoryMb")] double MemoryMb);
