using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines a rectangular capture area.
/// </summary>
/// <param name="X">Left edge (screen coordinates). Can be negative for multi-monitor setups.</param>
/// <param name="Y">Top edge (screen coordinates). Can be negative for multi-monitor setups.</param>
/// <param name="Width">Width in pixels. Must be greater than 0.</param>
/// <param name="Height">Height in pixels. Must be greater than 0.</param>
public sealed record CaptureRegion(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height)
{
    /// <summary>
    /// Validates that the region has positive dimensions.
    /// </summary>
    /// <returns>True if the region is valid, false otherwise.</returns>
    public bool IsValid() => Width > 0 && Height > 0;

    /// <summary>
    /// Gets the total number of pixels in the region.
    /// </summary>
    public long TotalPixels => (long)Width * Height;
}
