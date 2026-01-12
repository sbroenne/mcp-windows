using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Handles coordinate normalization for multi-monitor virtual desktop.
/// </summary>
public static class CoordinateNormalizer
{
    /// <summary>
    /// Normalizes screen coordinates to the 0-65535 range required by SendInput.
    /// </summary>
    /// <param name="screenX">The screen x-coordinate.</param>
    /// <param name="screenY">The screen y-coordinate.</param>
    /// <returns>Normalized coordinates in the 0-65535 range.</returns>
    public static (int NormalizedX, int NormalizedY) Normalize(int screenX, int screenY)
    {
        var bounds = GetVirtualScreenBounds();
        return Normalize(screenX, screenY, bounds);
    }

    /// <summary>
    /// Normalizes screen coordinates using the provided bounds.
    /// </summary>
    /// <param name="screenX">The screen x-coordinate.</param>
    /// <param name="screenY">The screen y-coordinate.</param>
    /// <param name="bounds">The virtual screen bounds.</param>
    /// <returns>Normalized coordinates in the 0-65535 range.</returns>
    public static (int NormalizedX, int NormalizedY) Normalize(int screenX, int screenY, ScreenBounds bounds)
    {
        // Formula from research.md:
        // normalizedX = ((screenX - virtualLeft) * 65535.0 / virtualWidth) + 0.5
        // normalizedY = ((screenY - virtualTop) * 65535.0 / virtualHeight) + 0.5

        var normalizedX = (int)(((screenX - bounds.Left) * 65535.0 / bounds.Width) + 0.5);
        var normalizedY = (int)(((screenY - bounds.Top) * 65535.0 / bounds.Height) + 0.5);

        return (normalizedX, normalizedY);
    }

    /// <summary>
    /// Validates that coordinates are within the virtual screen bounds.
    /// </summary>
    /// <param name="x">The x-coordinate to validate.</param>
    /// <param name="y">The y-coordinate to validate.</param>
    /// <returns>Validation result with bounds if invalid.</returns>
    public static (bool IsValid, ScreenBounds Bounds) ValidateCoordinates(int x, int y)
    {
        var bounds = GetVirtualScreenBounds();
        var isValid = x >= bounds.Left && x < bounds.Right &&
                      y >= bounds.Top && y < bounds.Bottom;
        return (isValid, bounds);
    }

    /// <summary>
    /// Gets the current virtual screen bounds.
    /// </summary>
    /// <returns>The virtual screen bounds.</returns>
    public static ScreenBounds GetVirtualScreenBounds()
    {
        var left = NativeMethods.GetSystemMetrics(NativeConstants.SM_XVIRTUALSCREEN);
        var top = NativeMethods.GetSystemMetrics(NativeConstants.SM_YVIRTUALSCREEN);
        var width = NativeMethods.GetSystemMetrics(NativeConstants.SM_CXVIRTUALSCREEN);
        var height = NativeMethods.GetSystemMetrics(NativeConstants.SM_CYVIRTUALSCREEN);

        return new ScreenBounds(left, top, width, height);
    }
}