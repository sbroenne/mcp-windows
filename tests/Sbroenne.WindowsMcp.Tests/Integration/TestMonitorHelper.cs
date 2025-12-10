namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Provides test coordinates on a preferred monitor to avoid DPI scaling issues.
/// Defaults to secondary monitor if available (usually has 100% scaling), falls back to primary.
/// </summary>
public static class TestMonitorHelper
{
    /// <summary>
    /// Gets the preferred monitor for testing. Prefers non-primary monitor if available
    /// to avoid DPI scaling issues common on primary monitors.
    /// </summary>
    /// <returns>The screen to use for testing.</returns>
    public static Screen GetPreferredTestMonitor()
    {
        var screens = Screen.AllScreens;

        // Prefer secondary monitor (usually has 100% DPI scaling)
        var secondary = screens.FirstOrDefault(s => !s.Primary);
        if (secondary != null)
        {
            return secondary;
        }

        // Fall back to primary if no secondary available
        return Screen.PrimaryScreen ?? screens[0];
    }

    /// <summary>
    /// Gets safe test coordinates on the preferred test monitor.
    /// </summary>
    /// <param name="offsetX">X offset from the top-left of the monitor (default: 100).</param>
    /// <param name="offsetY">Y offset from the top-left of the monitor (default: 100).</param>
    /// <returns>Absolute screen coordinates safe for testing.</returns>
    public static (int X, int Y) GetTestCoordinates(int offsetX = 100, int offsetY = 100)
    {
        var monitor = GetPreferredTestMonitor();
        return (monitor.Bounds.X + offsetX, monitor.Bounds.Y + offsetY);
    }

    /// <summary>
    /// Gets the bounds of the preferred test monitor.
    /// </summary>
    /// <returns>The bounds of the preferred test monitor.</returns>
    public static Rectangle GetTestMonitorBounds()
    {
        var monitor = GetPreferredTestMonitor();
        return monitor.Bounds;
    }

    /// <summary>
    /// Gets the center coordinates of the preferred test monitor.
    /// </summary>
    /// <returns>Center coordinates of the test monitor.</returns>
    public static (int X, int Y) GetTestMonitorCenter()
    {
        var bounds = GetTestMonitorBounds();
        return (bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    /// <summary>
    /// Checks if a secondary (non-primary) monitor is available.
    /// </summary>
    /// <returns>True if a secondary monitor is available.</returns>
    public static bool HasSecondaryMonitor()
    {
        return Screen.AllScreens.Any(s => !s.Primary);
    }

    /// <summary>
    /// Gets information about the preferred test monitor for debugging.
    /// </summary>
    /// <returns>A string describing the test monitor configuration.</returns>
    public static string GetTestMonitorInfo()
    {
        var monitor = GetPreferredTestMonitor();
        return $"Testing on monitor: {monitor.DeviceName} ({monitor.Bounds.Width}x{monitor.Bounds.Height} at {monitor.Bounds.X},{monitor.Bounds.Y}), Primary={monitor.Primary}";
    }
}
