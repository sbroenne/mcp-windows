using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact window information with minimal JSON property names for token efficiency.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - h: Handle (HWND as decimal string)
/// - t: Title
/// - cn: Class Name
/// - pn: Process Name
/// - pid: Process ID
/// - b: Bounds [x, y, w, h]
/// - s: State (normal/minimized/maximized/hidden)
/// - mi: Monitor Index
/// - fg: Is Foreground
/// - el: Is Elevated
/// </remarks>
public sealed record WindowInfoCompact
{
    /// <summary>
    /// Window handle (HWND) as decimal string.
    /// </summary>
    [JsonPropertyName("h")]
    public required string Handle { get; init; }

    /// <summary>
    /// Window title text.
    /// </summary>
    [JsonPropertyName("t")]
    public required string Title { get; init; }

    /// <summary>
    /// Window class name (e.g., "Notepad", "Chrome_WidgetWin_1").
    /// </summary>
    [JsonPropertyName("cn")]
    public required string ClassName { get; init; }

    /// <summary>
    /// Process name (e.g., "notepad.exe").
    /// </summary>
    [JsonPropertyName("pn")]
    public required string ProcessName { get; init; }

    /// <summary>
    /// Process ID.
    /// </summary>
    [JsonPropertyName("pid")]
    public required int ProcessId { get; init; }

    /// <summary>
    /// Bounds as [x, y, width, height].
    /// </summary>
    [JsonPropertyName("b")]
    public required int[] Bounds { get; init; }

    /// <summary>
    /// Window state: normal, minimized, maximized, hidden.
    /// </summary>
    [JsonPropertyName("s")]
    public required string State { get; init; }

    /// <summary>
    /// Monitor index (0-based).
    /// </summary>
    [JsonPropertyName("mi")]
    public required int MonitorIndex { get; init; }

    /// <summary>
    /// Is foreground window.
    /// </summary>
    [JsonPropertyName("fg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsForeground { get; init; }

    /// <summary>
    /// Is elevated (admin) process.
    /// </summary>
    [JsonPropertyName("el")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsElevated { get; init; }

    /// <summary>
    /// Creates a compact WindowInfo from a full WindowInfo.
    /// </summary>
    public static WindowInfoCompact FromFull(WindowInfo full)
    {
        ArgumentNullException.ThrowIfNull(full);
        return new WindowInfoCompact
        {
            Handle = full.Handle,
            Title = full.Title,
            ClassName = full.ClassName,
            ProcessName = full.ProcessName,
            ProcessId = full.ProcessId,
            Bounds = [full.Bounds.X, full.Bounds.Y, full.Bounds.Width, full.Bounds.Height],
            State = full.State.ToString().ToLowerInvariant(),
            MonitorIndex = full.MonitorIndex,
            IsForeground = full.IsForeground,
            IsElevated = full.IsElevated
        };
    }
}
