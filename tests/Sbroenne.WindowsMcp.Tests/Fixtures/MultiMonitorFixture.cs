using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Fixtures;

/// <summary>
/// Provides shared setup for multi-monitor integration tests.
/// Detects available monitors and provides utility methods for coordinate generation.
/// </summary>
public sealed class MultiMonitorFixture : IAsyncLifetime
{
    private readonly MonitorService _monitorService;
    private IReadOnlyList<MonitorInfo> _availableMonitors = Array.Empty<MonitorInfo>();

    public MultiMonitorFixture()
    {
        _monitorService = new MonitorService();
    }

    /// <summary>
    /// Gets the list of available monitor indices.
    /// </summary>
    public IReadOnlyList<int> AvailableMonitorIndices =>
        _availableMonitors.Select(m => m.Index).ToList();

    /// <summary>
    /// Gets the count of available monitors.
    /// </summary>
    public int MonitorCount => _availableMonitors.Count;

    /// <summary>
    /// Gets whether multi-monitor setup is available (2+ monitors).
    /// </summary>
    public bool IsMultiMonitorSetup => _availableMonitors.Count >= 2;

    /// <summary>
    /// Gets the primary monitor.
    /// </summary>
    public MonitorInfo PrimaryMonitor =>
        _availableMonitors.First(m => m.IsPrimary);

    /// <summary>
    /// Gets a secondary monitor (if available and exactly 2 monitors).
    /// Matches the behavior of MonitorService.GetSecondaryMonitor().
    /// </summary>
    /// <returns>The non-primary monitor if exactly 2 monitors exist, otherwise null.</returns>
    public MonitorInfo? GetSecondaryMonitor()
    {
        // Match MonitorService logic: only return secondary for exactly 2 monitors
        if (_availableMonitors.Count != 2)
        {
            return null;
        }

        // Return the non-primary monitor
        return _availableMonitors.FirstOrDefault(m => !m.IsPrimary);
    }

    /// <summary>
    /// Gets a monitor by index.
    /// </summary>
    /// <param name="index">The monitor index.</param>
    /// <returns>The monitor info, or null if index invalid.</returns>
    public MonitorInfo? GetMonitor(int index)
    {
        return _monitorService.GetMonitor(index);
    }

    /// <summary>
    /// Gets center coordinates for a specific monitor.
    /// </summary>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <returns>Center point (x, y) relative to the monitor's origin.</returns>
    public (int X, int Y) GetMonitorCenter(int monitorIndex)
    {
        var monitor = _monitorService.GetMonitor(monitorIndex);
        if (monitor == null)
        {
            throw new ArgumentException($"Monitor {monitorIndex} not found", nameof(monitorIndex));
        }

        return (monitor.Width / 2, monitor.Height / 2);
    }

    /// <summary>
    /// Gets safe coordinates within a monitor's bounds (10% padding from edges).
    /// </summary>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <returns>Safe point (x, y) relative to the monitor's origin.</returns>
    public (int X, int Y) GetSafeCoordinates(int monitorIndex)
    {
        var monitor = _monitorService.GetMonitor(monitorIndex);
        if (monitor == null)
        {
            throw new ArgumentException($"Monitor {monitorIndex} not found", nameof(monitorIndex));
        }

        // Use 10% padding from edges
        var xOffset = (int)(monitor.Width * 0.1);
        var yOffset = (int)(monitor.Height * 0.1);

        return (xOffset, yOffset);
    }

    /// <summary>
    /// Checks if coordinates are within a monitor's bounds.
    /// </summary>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <param name="x">X-coordinate (monitor-relative).</param>
    /// <param name="y">Y-coordinate (monitor-relative).</param>
    /// <returns>True if within bounds, false otherwise.</returns>
    public bool AreCoordinatesInBounds(int monitorIndex, int x, int y)
    {
        var monitor = _monitorService.GetMonitor(monitorIndex);
        if (monitor == null)
        {
            return false;
        }

        return x >= 0 && x < monitor.Width && y >= 0 && y < monitor.Height;
    }

    /// <summary>
    /// Gets coordinates that are out of bounds for a monitor.
    /// </summary>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <returns>Out-of-bounds point (x, y).</returns>
    public (int X, int Y) GetOutOfBoundsCoordinates(int monitorIndex)
    {
        var monitor = _monitorService.GetMonitor(monitorIndex);
        if (monitor == null)
        {
            throw new ArgumentException($"Monitor {monitorIndex} not found", nameof(monitorIndex));
        }

        // Return coordinates well beyond monitor bounds
        return (monitor.Width + 1000, monitor.Height + 1000);
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        // Detect available monitors
        _availableMonitors = _monitorService.GetMonitors();

        if (_availableMonitors.Count == 0)
        {
            throw new InvalidOperationException(
                "No monitors detected. Integration tests require at least one monitor.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        // No cleanup needed - we don't move the cursor or modify state
        return Task.CompletedTask;
    }
}
