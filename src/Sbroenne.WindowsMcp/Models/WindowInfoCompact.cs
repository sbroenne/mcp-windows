using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Compact window information with minimal JSON property names for token efficiency.
/// </summary>
public sealed record WindowInfoCompact
{
    /// <summary>
    /// Window handle (HWND) as decimal string.
    /// </summary>
    [JsonPropertyName("handle")]
    public required string Handle { get; init; }

    /// <summary>
    /// Window title text.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Window class name (e.g., "Notepad", "Chrome_WidgetWin_1").
    /// </summary>
    [JsonPropertyName("className")]
    public required string ClassName { get; init; }

    /// <summary>
    /// Process name (e.g., "notepad.exe").
    /// </summary>
    [JsonPropertyName("processName")]
    public required string ProcessName { get; init; }

    /// <summary>
    /// Process ID.
    /// </summary>
    [JsonPropertyName("pid")]
    public required int ProcessId { get; init; }

    /// <summary>
    /// Bounds as [x, y, width, height].
    /// </summary>
    [JsonPropertyName("bounds")]
    public required int[] Bounds { get; init; }

    /// <summary>
    /// Window state: normal, minimized, maximized, hidden.
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }

    /// <summary>
    /// Monitor index (0-based).
    /// </summary>
    [JsonPropertyName("monitorIndex")]
    public required int MonitorIndex { get; init; }

    /// <summary>
    /// Is foreground window.
    /// </summary>
    [JsonPropertyName("isForeground")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsForeground { get; init; }

    /// <summary>
    /// Is elevated (admin) process.
    /// </summary>
    [JsonPropertyName("isElevated")]
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
