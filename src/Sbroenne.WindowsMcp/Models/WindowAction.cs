using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the type of window management operation to perform.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WindowAction>))]
public enum WindowAction
{
    /// <summary>List all visible top-level windows.</summary>
    [JsonStringEnumMemberName("list")]
    List,

    /// <summary>Find windows by title (substring or regex).</summary>
    [JsonStringEnumMemberName("find")]
    Find,

    /// <summary>Bring window to foreground and give focus.</summary>
    [JsonStringEnumMemberName("activate")]
    Activate,

    /// <summary>Get current foreground window info.</summary>
    [JsonStringEnumMemberName("get_foreground")]
    GetForeground,

    /// <summary>Minimize window to taskbar.</summary>
    [JsonStringEnumMemberName("minimize")]
    Minimize,

    /// <summary>Maximize window to fill screen.</summary>
    [JsonStringEnumMemberName("maximize")]
    Maximize,

    /// <summary>Restore window to normal state.</summary>
    [JsonStringEnumMemberName("restore")]
    Restore,

    /// <summary>Send WM_CLOSE to window.</summary>
    [JsonStringEnumMemberName("close")]
    Close,

    /// <summary>Move window to new position.</summary>
    [JsonStringEnumMemberName("move")]
    Move,

    /// <summary>Resize window to new dimensions.</summary>
    [JsonStringEnumMemberName("resize")]
    Resize,

    /// <summary>Move and resize atomically.</summary>
    [JsonStringEnumMemberName("set_bounds")]
    SetBounds,

    /// <summary>Wait for window to appear.</summary>
    [JsonStringEnumMemberName("wait_for")]
    WaitFor,

    /// <summary>Move window to a specific monitor by index.</summary>
    [JsonStringEnumMemberName("move_to_monitor")]
    MoveToMonitor,

    /// <summary>Get window state (minimized, maximized, normal).</summary>
    [JsonStringEnumMemberName("get_state")]
    GetState,

    /// <summary>Wait for window to reach a specific state.</summary>
    [JsonStringEnumMemberName("wait_for_state")]
    WaitForState,

    /// <summary>Move window to position and activate it in one step.</summary>
    [JsonStringEnumMemberName("move_and_activate")]
    MoveAndActivate,

    /// <summary>Ensure window is visible (restore if minimized, activate).</summary>
    [JsonStringEnumMemberName("ensure_visible")]
    EnsureVisible
}
