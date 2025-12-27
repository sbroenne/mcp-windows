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
    /// <param name="windowHandle">Optional window handle to capture. If null, captures foreground window.</param>
    /// <param name="controlTypeFilter">Optional control type to filter elements (e.g., "Button", "Edit").</param>
    /// <param name="maxElements">Maximum number of elements to annotate (default: 50).</param>
    /// <param name="format">Image format (jpeg or png). Default: jpeg.</param>
    /// <param name="quality">JPEG quality 1-100. Default: 85.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing annotated image and element mapping.</returns>
    Task<AnnotatedScreenshotResult> CaptureAsync(
        nint? windowHandle = null,
        string? controlTypeFilter = null,
        int maxElements = 50,
        ImageFormat format = ImageFormat.Jpeg,
        int quality = 85,
        CancellationToken cancellationToken = default);
}
