namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the type of window management operation to perform.
/// </summary>
public enum WindowAction
{
    /// <summary>List all visible top-level windows.</summary>
    List,

    /// <summary>Find windows by title (substring or regex).</summary>
    Find,

    /// <summary>Bring window to foreground and give focus.</summary>
    Activate,

    /// <summary>Get current foreground window info.</summary>
    GetForeground,

    /// <summary>Minimize window to taskbar.</summary>
    Minimize,

    /// <summary>Maximize window to fill screen.</summary>
    Maximize,

    /// <summary>Restore window to normal state.</summary>
    Restore,

    /// <summary>Send WM_CLOSE to window.</summary>
    Close,

    /// <summary>Move window to new position.</summary>
    Move,

    /// <summary>Resize window to new dimensions.</summary>
    Resize,

    /// <summary>Move and resize atomically.</summary>
    SetBounds,

    /// <summary>Wait for window to appear.</summary>
    WaitFor,

    /// <summary>Move window to a specific monitor by index.</summary>
    MoveToMonitor,

    /// <summary>Get window state (minimized, maximized, normal).</summary>
    GetState,

    /// <summary>Wait for window to reach a specific state.</summary>
    WaitForState
}
