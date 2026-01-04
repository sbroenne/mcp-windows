using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Converts screen coordinates to monitor-relative coordinates.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CoordinateConverter
{
    private readonly MonitorService _monitorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateConverter"/> class.
    /// </summary>
    /// <param name="monitorService">The monitor service.</param>
    public CoordinateConverter(MonitorService monitorService)
    {
        ArgumentNullException.ThrowIfNull(monitorService);
        _monitorService = monitorService;
    }

    /// <summary>
    /// Converts screen coordinates to monitor-relative coordinates.
    /// </summary>
    /// <param name="screenRect">The bounding rectangle in screen coordinates.</param>
    /// <returns>A tuple of the monitor-relative rect and the monitor index.</returns>
    public (MonitorRelativeRect Rect, int MonitorIndex) ToMonitorRelative(BoundingRect screenRect)
    {
        ArgumentNullException.ThrowIfNull(screenRect);

        var monitors = _monitorService.GetMonitors();

        // Find which monitor contains the center of the element
        var centerX = screenRect.CenterX;
        var centerY = screenRect.CenterY;

        for (int i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            if (centerX >= monitor.X && centerX < monitor.X + monitor.Width &&
                centerY >= monitor.Y && centerY < monitor.Y + monitor.Height)
            {
                var relativeRect = new MonitorRelativeRect
                {
                    X = screenRect.X - monitor.X,
                    Y = screenRect.Y - monitor.Y,
                    Width = screenRect.Width,
                    Height = screenRect.Height
                };

                return (relativeRect, i);
            }
        }

        // Fallback to primary monitor if no match (element might be off-screen)
        var primary = _monitorService.GetPrimaryMonitor();
        var primaryIndex = 0;

        for (int i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].IsPrimary)
            {
                primaryIndex = i;
                break;
            }
        }

        var fallbackRect = new MonitorRelativeRect
        {
            X = screenRect.X - primary.X,
            Y = screenRect.Y - primary.Y,
            Width = screenRect.Width,
            Height = screenRect.Height
        };

        return (fallbackRect, primaryIndex);
    }

    /// <summary>
    /// Gets the origin (top-left corner) of a monitor in screen coordinates.
    /// Used to convert monitor-relative coordinates back to screen coordinates.
    /// </summary>
    /// <param name="monitorIndex">The 0-based monitor index.</param>
    /// <returns>The monitor origin as a Point.</returns>
    public Capture.Point GetMonitorOrigin(int monitorIndex)
    {
        var monitors = _monitorService.GetMonitors();

        if (monitorIndex >= 0 && monitorIndex < monitors.Count)
        {
            var monitor = monitors[monitorIndex];
            return new Capture.Point { X = monitor.X, Y = monitor.Y };
        }

        // Fallback to primary monitor
        var primary = _monitorService.GetPrimaryMonitor();
        return new Capture.Point { X = primary.X, Y = primary.Y };
    }
}
