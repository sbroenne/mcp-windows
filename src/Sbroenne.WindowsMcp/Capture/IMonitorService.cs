using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Service for enumerating display monitors.
/// </summary>
public interface IMonitorService
{
    /// <summary>
    /// Gets all available monitors.
    /// </summary>
    /// <returns>A read-only list of monitor information.</returns>
    IReadOnlyList<MonitorInfo> GetMonitors();

    /// <summary>
    /// Gets a specific monitor by index.
    /// </summary>
    /// <param name="index">The zero-based monitor index.</param>
    /// <returns>The monitor information, or null if index is out of range.</returns>
    MonitorInfo? GetMonitor(int index);

    /// <summary>
    /// Gets the primary monitor.
    /// </summary>
    /// <returns>The primary monitor information.</returns>
    MonitorInfo GetPrimaryMonitor();

    /// <summary>
    /// Gets the secondary monitor (non-primary). Only valid for 2-monitor setups.
    /// </summary>
    /// <returns>The secondary monitor, or null if there are 0, 1, or 3+ monitors.</returns>
    MonitorInfo? GetSecondaryMonitor();

    /// <summary>
    /// Gets the total number of monitors.
    /// </summary>
    int MonitorCount { get; }
}
