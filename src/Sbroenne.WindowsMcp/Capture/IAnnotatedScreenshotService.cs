using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Service for creating annotated screenshots with numbered UI element labels.
/// </summary>
public interface IAnnotatedScreenshotService
{
    /// <summary>
    /// Captures an annotated screenshot with numbered labels on interactive UI elements.
    /// </summary>
    /// <param name="windowHandle">Optional window handle (HWND) as a decimal string. If null, captures foreground window.</param>
    /// <param name="controlTypeFilter">Optional control type to filter elements (e.g., "Button", "Edit").</param>
    /// <param name="maxElements">Maximum number of elements to annotate (default: 50).</param>
    /// <param name="searchDepth">Maximum depth to search for elements. Default: 15 (optimized for Electron/Chromium apps). Use 5-8 for WinForms, 8-10 for WPF.</param>
    /// <param name="format">Image format (jpeg or png). Default: jpeg.</param>
    /// <param name="quality">JPEG quality 1-100. Default: 85.</param>
    /// <param name="interactiveOnly">Filter to only interactive control types (Button, Edit, CheckBox, etc.). Default: true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing annotated image and element mapping.</returns>
    Task<AnnotatedScreenshotResult> CaptureAsync(
        string? windowHandle = null,
        string? controlTypeFilter = null,
        int maxElements = 50,
        int searchDepth = 15,
        ImageFormat format = ImageFormat.Jpeg,
        int quality = 85,
        bool interactiveOnly = true,
        CancellationToken cancellationToken = default);
}
