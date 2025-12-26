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
    private readonly IMonitorService _monitorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateConverter"/> class.
    /// </summary>
    /// <param name="monitorService">The monitor service.</param>
    public CoordinateConverter(IMonitorService monitorService)
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
}
