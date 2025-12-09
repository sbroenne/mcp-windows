using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Complete information about a window.
/// </summary>
public sealed record WindowInfo
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
    /// Gets the window class name (e.g., "Notepad", "Chrome_WidgetWin_1").
    /// </summary>
    [JsonPropertyName("class_name")]
    public required string ClassName { get; init; }

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
    /// Gets the window bounds (position and size).
    /// </summary>
    [JsonPropertyName("bounds")]
    public required WindowBounds Bounds { get; init; }

    /// <summary>
    /// Gets the window state (normal, minimized, maximized, hidden).
    /// </summary>
    [JsonPropertyName("state")]
    public required WindowState State { get; init; }

    /// <summary>
    /// Gets the monitor index (0-based) the window is primarily on.
    /// </summary>
    [JsonPropertyName("monitor_index")]
    public required int MonitorIndex { get; init; }

    /// <summary>
    /// Gets the device name of the monitor the window is on (e.g., "\\\\.\\DISPLAY1").
    /// </summary>
    [JsonPropertyName("monitor_name")]
    public string? MonitorName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window is on the primary monitor.
    /// </summary>
    [JsonPropertyName("monitor_is_primary")]
    public bool MonitorIsPrimary { get; init; }

    /// <summary>
    /// Gets the bounds of the monitor the window is on.
    /// </summary>
    [JsonPropertyName("monitor_bounds")]
    public WindowBounds? MonitorBounds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window is on the current virtual desktop.
    /// </summary>
    [JsonPropertyName("on_current_desktop")]
    public bool OnCurrentDesktop { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the window's process is elevated (admin).
    /// </summary>
    [JsonPropertyName("is_elevated")]
    public bool IsElevated { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window is responding to messages.
    /// </summary>
    [JsonPropertyName("is_responding")]
    public bool IsResponding { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this is a UWP/Store app.
    /// </summary>
    [JsonPropertyName("is_uwp")]
    public bool IsUwp { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window has foreground focus.
    /// </summary>
    [JsonPropertyName("is_foreground")]
    public bool IsForeground { get; init; }
}
