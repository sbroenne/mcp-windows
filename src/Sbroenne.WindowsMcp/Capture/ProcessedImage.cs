namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Result of image processing (scaling and encoding).
/// </summary>
/// <param name="Data">The encoded image data as byte array.</param>
/// <param name="Width">The output width after scaling.</param>
/// <param name="Height">The output height after scaling.</param>
/// <param name="OriginalWidth">The original width before scaling.</param>
/// <param name="OriginalHeight">The original height before scaling.</param>
/// <param name="Format">The output format (e.g., "jpeg", "png").</param>
public sealed record ProcessedImage(
    byte[] Data,
    int Width,
    int Height,
    int OriginalWidth,
    int OriginalHeight,
    string Format);
