using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Lightweight window information included in operation results to indicate
/// which window received the input or was the target of an action.
/// </summary>
public sealed record TargetWindowInfo
{
    /// <summary>
    /// Gets the window handle (HWND) as decimal string for JSON safety.
    /// </summary>
    [JsonPropertyName("handle")]
    public required string Handle { get; init; }

    /// <summary>
    /// Gets the window title text.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the process name (e.g., "notepad.exe", "chrome.exe").
    /// </summary>
    [JsonPropertyName("process_name")]
    public required string ProcessName { get; init; }

    /// <summary>
    /// Gets the process ID.
    /// </summary>
    [JsonPropertyName("process_id")]
    public required int ProcessId { get; init; }

    /// <summary>
    /// Creates a TargetWindowInfo from a WindowInfo.
    /// </summary>
    /// <param name="windowInfo">The full window info.</param>
    /// <returns>A lightweight TargetWindowInfo.</returns>
    public static TargetWindowInfo FromWindowInfo(WindowInfo windowInfo)
    {
        ArgumentNullException.ThrowIfNull(windowInfo);
        return new TargetWindowInfo
        {
            Handle = windowInfo.Handle,
            Title = windowInfo.Title,
            ProcessName = windowInfo.ProcessName,
            ProcessId = windowInfo.ProcessId
        };
    }

    /// <summary>
    /// Creates a TargetWindowInfo from raw window data.
    /// </summary>
    /// <param name="handle">The window handle.</param>
    /// <param name="title">The window title.</param>
    /// <param name="processName">The process name.</param>
    /// <param name="processId">The process ID.</param>
    /// <returns>A TargetWindowInfo instance.</returns>
    public static TargetWindowInfo Create(nint handle, string title, string processName, int processId)
    {
        return new TargetWindowInfo
        {
            Handle = handle.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Title = title,
            ProcessName = processName,
            ProcessId = processId
        };
    }
}
