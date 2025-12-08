using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Provides monitor enumeration and information services.
/// </summary>
public sealed class MonitorService : IMonitorService
{
    /// <inheritdoc />
    public int MonitorCount => Screen.AllScreens.Length;

    /// <inheritdoc />
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var screens = Screen.AllScreens;
        var monitors = new MonitorInfo[screens.Length];

        for (var i = 0; i < screens.Length; i++)
        {
            monitors[i] = CreateMonitorInfo(screens[i], i);
        }

        return monitors;
    }

    /// <inheritdoc />
    public MonitorInfo? GetMonitor(int index)
    {
        var screens = Screen.AllScreens;

        if (index < 0 || index >= screens.Length)
        {
            return null;
        }

        return CreateMonitorInfo(screens[index], index);
    }

    /// <inheritdoc />
    public MonitorInfo GetPrimaryMonitor()
    {
        var screens = Screen.AllScreens;

        for (var i = 0; i < screens.Length; i++)
        {
            if (screens[i].Primary)
            {
                return CreateMonitorInfo(screens[i], i);
            }
        }

        // Fallback to first monitor if no primary found (should never happen)
        return CreateMonitorInfo(screens[0], 0);
    }

    /// <summary>
    /// Creates a <see cref="MonitorInfo"/> from a <see cref="Screen"/> instance.
    /// </summary>
    /// <param name="screen">The screen to convert.</param>
    /// <param name="index">The index of the screen.</param>
    /// <returns>A new <see cref="MonitorInfo"/> instance.</returns>
    private static MonitorInfo CreateMonitorInfo(Screen screen, int index)
    {
        return new MonitorInfo(
            Index: index,
            DeviceName: screen.DeviceName,
            Width: screen.Bounds.Width,
            Height: screen.Bounds.Height,
            X: screen.Bounds.X,
            Y: screen.Bounds.Y,
            IsPrimary: screen.Primary);
    }
}
