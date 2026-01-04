using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Window;

/// <summary>
/// Enumerates windows on the system and retrieves window information.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowEnumerator
{
    private readonly ElevationDetector _elevationDetector;
    private readonly WindowConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowEnumerator"/> class.
    /// </summary>
    /// <param name="elevationDetector">Elevation detector for checking window elevation status.</param>
    /// <param name="configuration">Window configuration.</param>
    public WindowEnumerator(ElevationDetector elevationDetector, WindowConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(elevationDetector);
        ArgumentNullException.ThrowIfNull(configuration);

        _elevationDetector = elevationDetector;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WindowInfo>> EnumerateWindowsAsync(
        string? filter = null,
        bool useRegex = false,
        bool includeAllDesktops = false,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var windows = new List<WindowInfo>();
            Regex? filterRegex = null;

            if (!string.IsNullOrEmpty(filter) && useRegex)
            {
                filterRegex = new Regex(filter, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            // EnumWindows callback
            bool EnumCallback(nint hwnd, nint lParam)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var info = GetWindowInfoCore(hwnd, includeAllDesktops);

                if (info is null)
                {
                    return true; // Continue enumeration
                }

                // Apply filter if specified
                if (!string.IsNullOrEmpty(filter))
                {
                    bool matches = filterRegex is not null
                        ? filterRegex.IsMatch(info.Title) || filterRegex.IsMatch(info.ProcessName ?? "")
                        : (info.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           (info.ProcessName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));

                    if (!matches)
                    {
                        return true; // Continue but don't add
                    }
                }

                windows.Add(info);
                return true;
            }

            NativeMethods.EnumWindows(EnumCallback, IntPtr.Zero);

            return (IReadOnlyList<WindowInfo>)windows;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<WindowInfo?> GetWindowInfoAsync(nint handle, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetWindowInfoCore(handle, includeAllDesktops: true), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WindowInfo>> FindWindowsAsync(
        string title,
        bool useRegex = false,
        CancellationToken cancellationToken = default)
    {
        return EnumerateWindowsAsync(title, useRegex, includeAllDesktops: false, cancellationToken);
    }

    private WindowInfo? GetWindowInfoCore(nint hwnd, bool includeAllDesktops)
    {
        // Check if window is visible
        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            return null;
        }

        // Check cloaked state (virtual desktop, etc.)
        bool isCloaked = IsCloaked(hwnd);
        if (isCloaked && !includeAllDesktops)
        {
            return null;
        }

        // Get window title
        string title = GetWindowTitle(hwnd);

        // Skip windows with empty titles (typically system/framework windows)
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        // Get class name
        string className = GetClassName(hwnd);

        // Skip known system windows that shouldn't be enumerated
        if (ShouldSkipWindow(hwnd, className, title))
        {
            return null;
        }

        // Get process information
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        string? processName = GetProcessName(processId);

        // Get window bounds (prefer DWM extended bounds for accuracy)
        WindowBounds bounds = GetWindowBounds(hwnd);

        // Get window state
        WindowState state = GetWindowState(hwnd);

        // Get monitor info
        var (monitorIndex, monitorName, monitorIsPrimary, monitorBounds) = GetMonitorInfo(hwnd);

        // Check if elevated
        bool isElevated = IsWindowElevated(hwnd, processId);

        // Check if responding
        bool isResponding = IsWindowResponding(hwnd);

        // Check if UWP app
        bool isUwp = IsUwpWindow(processName);

        // Check if foreground
        bool isForeground = hwnd == NativeMethods.GetForegroundWindow();

        return new WindowInfo
        {
            Handle = hwnd.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Title = title,
            ClassName = className,
            ProcessName = processName ?? string.Empty,
            ProcessId = (int)processId,
            Bounds = bounds,
            State = state,
            MonitorIndex = monitorIndex,
            MonitorName = monitorName,
            MonitorIsPrimary = monitorIsPrimary,
            MonitorBounds = monitorBounds,
            IsElevated = isElevated,
            IsResponding = isResponding,
            IsUwp = isUwp,
            IsForeground = isForeground,
            OnCurrentDesktop = !isCloaked || VirtualDesktopManagerFactory.IsWindowOnCurrentDesktop(hwnd)
        };
    }

    private static string GetWindowTitle(nint hwnd)
    {
        const int MaxLength = 512;
        var buffer = new char[MaxLength];
        int length = NativeMethods.GetWindowText(hwnd, buffer, MaxLength);
        return new string(buffer, 0, length);
    }

    private static string GetClassName(nint hwnd)
    {
        const int MaxLength = 256;
        var buffer = new char[MaxLength];
        int length = NativeMethods.GetClassName(hwnd, buffer, MaxLength);
        return new string(buffer, 0, length);
    }

    private static string? GetProcessName(uint processId)
    {
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            // Process no longer exists
            return null;
        }
        catch (InvalidOperationException)
        {
            // Process has exited
            return null;
        }
    }

    private static WindowBounds GetWindowBounds(nint hwnd)
    {
        // Try to get DWM extended frame bounds first (more accurate with window shadows)
        if (NativeMethods.DwmGetWindowAttribute(
            hwnd,
            NativeConstants.DWMWA_EXTENDED_FRAME_BOUNDS,
            out RECT dwmRect,
            Marshal.SizeOf<RECT>()) == 0)
        {
            return WindowBounds.FromRect(dwmRect.Left, dwmRect.Top, dwmRect.Right, dwmRect.Bottom);
        }

        // Fall back to GetWindowRect
        if (NativeMethods.GetWindowRect(hwnd, out RECT rect))
        {
            return WindowBounds.FromRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        return new WindowBounds { X = 0, Y = 0, Width = 0, Height = 0 };
    }

    private static WindowState GetWindowState(nint hwnd)
    {
        if (NativeMethods.IsIconic(hwnd))
        {
            return WindowState.Minimized;
        }

        if (NativeMethods.IsZoomed(hwnd))
        {
            return WindowState.Maximized;
        }

        // Check if window is visible
        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            return WindowState.Hidden;
        }

        return WindowState.Normal;
    }

    private static (int Index, string? Name, bool IsPrimary, WindowBounds? Bounds) GetMonitorInfo(nint hwnd)
    {
        var hMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero)
        {
            return (0, null, false, null);
        }

        // Get monitor info for name, primary status, and bounds
        var monitorInfo = MONITORINFO.Create();
        string? monitorName = null;
        bool isPrimary = false;
        WindowBounds? monitorBounds = null;

        if (NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            isPrimary = monitorInfo.IsPrimary;
            monitorBounds = new WindowBounds
            {
                X = monitorInfo.RcMonitor.Left,
                Y = monitorInfo.RcMonitor.Top,
                Width = monitorInfo.RcMonitor.Right - monitorInfo.RcMonitor.Left,
                Height = monitorInfo.RcMonitor.Bottom - monitorInfo.RcMonitor.Top
            };
        }

        // Enumerate monitors to find index and device name
        int index = 0;
        int resultIndex = 0;

        // Use Screen.AllScreens to get device names (more reliable than MONITORINFOEX)
        var screens = Screen.AllScreens;

        bool EnumMonitorCallback(nint hMon, nint hdcMonitor, ref RECT lprcMonitor, nint dwData)
        {
            if (hMon == hMonitor)
            {
                resultIndex = index;
                // Match with Screen by position
                foreach (var screen in screens)
                {
                    if (screen.Bounds.X == lprcMonitor.Left && screen.Bounds.Y == lprcMonitor.Top)
                    {
                        monitorName = screen.DeviceName;
                        break;
                    }
                }
            }
            index++;
            return true;
        }

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorCallback, IntPtr.Zero);

        return (resultIndex, monitorName, isPrimary, monitorBounds);
    }

    private bool IsWindowElevated(nint hwnd, uint processId)
    {
        if (processId == 0)
        {
            return false;
        }

        // Get window center for elevation check
        if (NativeMethods.GetWindowRect(hwnd, out RECT rect))
        {
            int centerX = (rect.Left + rect.Right) / 2;
            int centerY = (rect.Top + rect.Bottom) / 2;
            return _elevationDetector.IsTargetElevated(centerX, centerY);
        }

        return false;
    }

    private bool IsWindowResponding(nint hwnd)
    {
        // Use SendMessageTimeout to check if window is responding
        // A timeout suggests the window is hung
        nint result;
        var returnValue = NativeMethods.SendMessageTimeout(
            hwnd,
            NativeConstants.WM_NULL,
            IntPtr.Zero,
            [],
            NativeConstants.SMTO_ABORTIFHUNG,
            (uint)_configuration.PropertyQueryTimeoutMs,
            out result);

        return returnValue != IntPtr.Zero;
    }

    private static bool IsUwpWindow(string? processName)
    {
        // UWP apps run under ApplicationFrameHost
        return string.Equals(processName, "ApplicationFrameHost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCloaked(nint hwnd)
    {
        int cloaked;
        int result = NativeMethods.DwmGetWindowAttributeInt(
            hwnd,
            NativeConstants.DWMWA_CLOAKED,
            out cloaked,
            sizeof(int));

        return result == 0 && cloaked != 0;
    }

    private static bool ShouldSkipWindow(nint hwnd, string className, string title)
    {
        // Skip known system window classes
        string[] skipClasses =
        [
            "Windows.UI.Core.CoreWindow",      // UWP core windows (use ApplicationFrameWindow instead)
            "Progman",                         // Program Manager (desktop)
            "WorkerW",                         // Desktop worker windows
            "Shell_TrayWnd",                   // Taskbar
            "Shell_SecondaryTrayWnd",          // Secondary taskbar
            "NotifyIconOverflowWindow",        // System tray overflow
            "Windows.Internal.Shell.TabProxyWindow", // Tab proxy windows
        ];

        if (skipClasses.Contains(className, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Skip windows that are clearly not user windows
        if (className.StartsWith("HwndWrapper[", StringComparison.OrdinalIgnoreCase))
        {
            // WPF wrapper windows - only skip if no meaningful title
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }
        }

        return false;
    }
}
