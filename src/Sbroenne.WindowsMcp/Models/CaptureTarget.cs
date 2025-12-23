namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Specifies what to capture.
/// </summary>
public enum CaptureTarget
{
    /// <summary>
    /// Capture the primary monitor (default). This is the main display with the taskbar.
    /// </summary>
    PrimaryScreen = 0,

    /// <summary>
    /// Capture the secondary monitor. Only works with exactly 2 monitors.
    /// For 3+ monitors, use Monitor target with monitorIndex.
    /// </summary>
    SecondaryScreen = 1,

    /// <summary>
    /// Capture a specific monitor by index.
    /// </summary>
    Monitor = 2,

    /// <summary>
    /// Capture a specific window by handle.
    /// </summary>
    Window = 3,

    /// <summary>
    /// Capture a rectangular screen region.
    /// </summary>
    Region = 4,

    /// <summary>
    /// Capture all connected monitors as a single composite image.
    /// Uses the virtual screen bounds to capture the entire multi-monitor desktop.
    /// </summary>
    AllMonitors = 5
}
