using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Service for capturing screenshots.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Executes a screenshot operation asynchronously.
    /// </summary>
    /// <param name="request">The screenshot request parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The screenshot result containing image data or error information.</returns>
    Task<ScreenshotControlResult> ExecuteAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken = default);
}
