namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Error classification for screenshot capture operations.
/// </summary>
public enum ScreenshotErrorCode
{
    /// <summary>
    /// Operation completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// UAC prompt, lock screen, or Ctrl+Alt+Del active.
    /// </summary>
    SecureDesktopActive = 1,

    /// <summary>
    /// Specified window handle is not valid.
    /// </summary>
    InvalidWindowHandle = 2,

    /// <summary>
    /// Cannot capture minimized window.
    /// </summary>
    WindowMinimized = 3,

    /// <summary>
    /// Window is not visible (hidden or cloaked).
    /// </summary>
    WindowNotVisible = 4,

    /// <summary>
    /// PrintWindow failed (window didn't respond).
    /// </summary>
    WindowCaptureFailed = 5,

    /// <summary>
    /// Monitor index out of range.
    /// </summary>
    InvalidMonitorIndex = 6,

    /// <summary>
    /// Region coordinates are invalid (negative dimensions, zero area).
    /// </summary>
    InvalidRegion = 7,

    /// <summary>
    /// Capture dimensions exceed configured limits.
    /// </summary>
    ImageTooLarge = 8,

    /// <summary>
    /// Operation exceeded timeout threshold.
    /// </summary>
    Timeout = 9,

    /// <summary>
    /// General capture failure.
    /// </summary>
    CaptureError = 10,

    /// <summary>
    /// Request validation failed.
    /// </summary>
    InvalidRequest = 11,

    /// <summary>
    /// Secondary screen target requires exactly 2 monitors.
    /// </summary>
    NoSecondaryScreen = 12
}
