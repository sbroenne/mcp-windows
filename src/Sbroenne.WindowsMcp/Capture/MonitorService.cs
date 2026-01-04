using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Provides monitor enumeration and information services.
/// Uses native Win32 APIs to get physical pixel coordinates for DPI-aware screenshot capture.
/// </summary>
public sealed class MonitorService
{
    private const int ENUM_CURRENT_SETTINGS = -1;

    /// <inheritdoc />
    public int MonitorCount => Screen.AllScreens.Length;

    /// <inheritdoc />
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        bool EnumCallback(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData)
        {
            var monitorInfo = MONITORINFO.Create();
            if (NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                // Get logical position and dimensions (used by mouse control and virtual screen)
                int logicalX = monitorInfo.RcMonitor.Left;
                int logicalY = monitorInfo.RcMonitor.Top;
                int logicalWidth = monitorInfo.RcMonitor.Right - monitorInfo.RcMonitor.Left;
                int logicalHeight = monitorInfo.RcMonitor.Bottom - monitorInfo.RcMonitor.Top;

                // Get device name for this monitor
                string? deviceName = GetDeviceNameForMonitor(monitorInfo.RcMonitor);

                // Use EnumDisplaySettingsW to get true physical resolution (for high-DPI screenshot capture)
                int physicalWidth = logicalWidth;
                int physicalHeight = logicalHeight;

                if (deviceName != null)
                {
                    var devMode = DEVMODE.Create();
                    if (NativeMethods.EnumDisplaySettingsW(deviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        // dmPelsWidth and dmPelsHeight are the true physical resolution
                        physicalWidth = (int)devMode.dmPelsWidth;
                        physicalHeight = (int)devMode.dmPelsHeight;
                    }
                }

                // Extract display number from device name (e.g., \\.\DISPLAY1 -> 1)
                int displayNumber = MonitorInfo.ExtractDisplayNumber(deviceName);

                monitors.Add(new MonitorInfo(
                    Index: index,
                    DisplayNumber: displayNumber,
                    DeviceName: deviceName ?? $"Monitor {index}",
                    PhysicalWidth: physicalWidth,
                    PhysicalHeight: physicalHeight,
                    Width: logicalWidth,
                    Height: logicalHeight,
                    X: logicalX,
                    Y: logicalY,
                    IsPrimary: monitorInfo.IsPrimary));
            }
            index++;
            return true;
        }

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumCallback, IntPtr.Zero);

        return monitors;
    }

    /// <inheritdoc />
    public MonitorInfo? GetMonitor(int index)
    {
        var monitors = GetMonitors();

        if (index < 0 || index >= monitors.Count)
        {
            return null;
        }

        return monitors[index];
    }

    /// <inheritdoc />
    public MonitorInfo GetPrimaryMonitor()
    {
        var monitors = GetMonitors();

        foreach (var monitor in monitors)
        {
            if (monitor.IsPrimary)
            {
                return monitor;
            }
        }

        // Fallback to first monitor if no primary found (should never happen)
        return monitors.Count > 0 ? monitors[0] : new MonitorInfo(0, 0, "Primary", 1920, 1080, 1920, 1080, 0, 0, true);
    }

    /// <inheritdoc />
    public MonitorInfo? GetSecondaryMonitor()
    {
        var monitors = GetMonitors();

        // Only valid for exactly 2 monitors
        if (monitors.Count != 2)
        {
            return null;
        }

        // Return the non-primary monitor
        foreach (var monitor in monitors)
        {
            if (!monitor.IsPrimary)
            {
                return monitor;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the device name from Screen.AllScreens that matches the given bounds.
    /// </summary>
    /// <param name="bounds">The bounds from native API.</param>
    /// <returns>The device name if found, otherwise null.</returns>
    private static string? GetDeviceNameForMonitor(RECT bounds)
    {
        var screens = Screen.AllScreens;

        // Try to match by comparing positions
        foreach (var screen in screens)
        {
            // If positions match exactly
            if (screen.Bounds.X == bounds.Left && screen.Bounds.Y == bounds.Top)
            {
                return screen.DeviceName;
            }
        }

        // If no exact match, try to match by primary status for the primary monitor
        foreach (var screen in screens)
        {
            if (screen.Primary && bounds.Left == 0 && bounds.Top == 0)
            {
                return screen.DeviceName;
            }
        }

        return null;
    }
}
