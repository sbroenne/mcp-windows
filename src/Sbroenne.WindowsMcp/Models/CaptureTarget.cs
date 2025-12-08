namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Specifies what to capture.
/// </summary>
public enum CaptureTarget
{
    /// <summary>
    /// Capture the primary monitor (default).
    /// </summary>
    PrimaryScreen = 0,

    /// <summary>
    /// Capture a specific monitor by index.
    /// </summary>
    Monitor = 1,

    /// <summary>
    /// Capture a specific window by handle.
    /// </summary>
    Window = 2,

    /// <summary>
    /// Capture a rectangular screen region.
    /// </summary>
    Region = 3
}
