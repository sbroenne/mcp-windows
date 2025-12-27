namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result from an annotated screenshot capture operation.
/// </summary>
public sealed record AnnotatedScreenshotResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Base64-encoded image data of the annotated screenshot.
    /// </summary>
    public string? ImageData { get; init; }

    /// <summary>
    /// Image format (jpeg or png).
    /// </summary>
    public string? ImageFormat { get; init; }

    /// <summary>
    /// Width of the captured image in pixels.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the captured image in pixels.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Array of annotated elements with their indices matching the numbered labels on the image.
    /// </summary>
    public AnnotatedElement[]? Elements { get; init; }

    /// <summary>
    /// Number of elements annotated.
    /// </summary>
    public int ElementCount { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static AnnotatedScreenshotResult CreateSuccess(
        string imageData,
        string imageFormat,
        int width,
        int height,
        AnnotatedElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        return new()
        {
            Success = true,
            ImageData = imageData,
            ImageFormat = imageFormat,
            Width = width,
            Height = height,
            Elements = elements,
            ElementCount = elements.Length
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static AnnotatedScreenshotResult CreateFailure(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ElementCount = 0
    };
}
