using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Lightweight window information included in operation results to indicate
/// which window received the input or was the target of an action.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - h: Handle (HWND as decimal string)
/// - t: Title
/// - pn: Process Name
/// - pid: Process ID
/// </remarks>
public sealed record TargetWindowInfo
{
    /// <summary>
    /// Gets the window handle (HWND) as decimal string for JSON safety.
    /// </summary>
    [JsonPropertyName("h")]
    public required string Handle { get; init; }

    /// <summary>
    /// Gets the window title text.
    /// </summary>
    [JsonPropertyName("t")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the process name (e.g., "notepad.exe", "chrome.exe").
    /// </summary>
    [JsonPropertyName("pn")]
    public required string ProcessName { get; init; }

    /// <summary>
    /// Gets the process ID.
    /// </summary>
    [JsonPropertyName("pid")]
    public required int ProcessId { get; init; }

    /// <summary>
    /// Creates a TargetWindowInfo from a WindowInfoCompact.
    /// </summary>
    /// <param name="windowInfo">The compact window info.</param>
    /// <returns>A lightweight TargetWindowInfo.</returns>
    public static TargetWindowInfo FromWindowInfo(WindowInfoCompact windowInfo)
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
    /// Creates a TargetWindowInfo from a full WindowInfo.
    /// </summary>
    /// <param name="windowInfo">The full window info.</param>
    /// <returns>A lightweight TargetWindowInfo.</returns>
    public static TargetWindowInfo FromFullWindowInfo(WindowInfo windowInfo)
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
