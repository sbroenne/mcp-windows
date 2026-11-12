using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Window;

/// <summary>
/// Enumerates windows on the system and retrieves window information.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowEnumerator
{
    private readonly ElevationDetector _elevationDetector;

    /// <summary>
    /// Default timeout for querying window properties (hung detection) in milliseconds.
    /// </summary>
    private const int PropertyQueryTimeoutMs = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowEnumerator"/> class.
    /// </summary>
    /// <param name="elevationDetector">Elevation detector for checking window elevation status.</param>
    public WindowEnumerator(ElevationDetector elevationDetector)
    {
        ArgumentNullException.ThrowIfNull(elevationDetector);
        _elevationDetector = elevationDetector;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WindowInfo>> EnumerateWindowsAsync(
        string? filter = null,
        bool useRegex = false,
        bool includeAllDesktops = false,
        int? processId = null,
        CancellationToken cancellationToken = default)
    {
        // The single list filter is documented as matching the title *or* the process name,
        // so the same value is applied to both with OR semantics.
        return EnumerateWindowsCoreAsync(
            filter,
            filter,
            processId,
            useRegex,
            includeAllDesktops,
            cancellationToken,
            titleOrProcessName: true);
    }

    internal Task<IReadOnlyList<WindowInfo>> EnumerateLaunchWindowsAsync(CancellationToken cancellationToken) =>
        EnumerateWindowsCoreAsync(null, null, null, false, true, cancellationToken, includeUntitled: true);

    /// <summary>
    /// Core enumeration with separate title, process name, and process id filters.
    /// </summary>
    /// <param name="titleFilter">Substring or regex pattern matched against the window title.</param>
    /// <param name="processNameFilter">Substring or regex pattern matched against the owning process name.</param>
    /// <param name="processIdFilter">When set, only windows owned by this process id are returned.</param>
    /// <param name="useRegex">Treat the title and process name filters as regular expressions.</param>
    /// <param name="includeAllDesktops">Include windows hosted on other virtual desktops.</param>
    /// <param name="cancellationToken">Token used to cancel the enumeration.</param>
    /// <param name="includeUntitled">Include windows without a title.</param>
    /// <param name="titleOrProcessName">When true a window matches if either the title or the process name matches;
    /// when false every supplied filter must match.</param>
    private Task<IReadOnlyList<WindowInfo>> EnumerateWindowsCoreAsync(
        string? titleFilter,
        string? processNameFilter,
        int? processIdFilter,
        bool useRegex,
        bool includeAllDesktops,
        CancellationToken cancellationToken,
        bool includeUntitled = false,
        bool titleOrProcessName = false)
    {
        return Task.Run(() =>
        {
            var windows = new List<WindowInfo>();
            Regex? titleRegex = null;
            Regex? processRegex = null;

            if (!string.IsNullOrEmpty(titleFilter) && useRegex)
            {
                titleRegex = new Regex(titleFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            if (!string.IsNullOrEmpty(processNameFilter) && useRegex)
            {
                processRegex = new Regex(processNameFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            // EnumWindows callback
            bool EnumCallback(nint hwnd, nint lParam)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                // Resolve the owning process before the expensive per-window queries so a
                // process-id filter can reject non-matching windows early.
                _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);

                if (processIdFilter is not null && processId != unchecked((uint)processIdFilter.Value))
                {
                    return true; // Continue enumeration
                }

                var info = GetWindowInfoCore(hwnd, includeAllDesktops, includeUntitled, processId);

                if (info is null)
                {
                    return true; // Continue enumeration
                }

                // The combined list filter matches substrings, while a dedicated processName
                // filter keeps its documented exact, case-insensitive comparison.
                bool titleMatches = MatchesFilter(info.Title, titleFilter, titleRegex, exact: false);
                bool processMatches = MatchesFilter(info.ProcessName, processNameFilter, processRegex, exact: !titleOrProcessName);

                if (titleOrProcessName)
                {
                    var hasFilter = !string.IsNullOrEmpty(titleFilter) || !string.IsNullOrEmpty(processNameFilter);
                    if (hasFilter && !titleMatches && !processMatches)
                    {
                        return true; // Continue but don't add
                    }
                }
                else
                {
                    if ((!string.IsNullOrEmpty(titleFilter) && !titleMatches) ||
                        (!string.IsNullOrEmpty(processNameFilter) && !processMatches))
                    {
                        return true; // Continue but don't add
                    }
                }

                windows.Add(info);
                return true;
            }

            NativeMethods.EnumWindows(EnumCallback, IntPtr.Zero);
            cancellationToken.ThrowIfCancellationRequested();

            return (IReadOnlyList<WindowInfo>)windows;
        }, cancellationToken);
    }

    /// <summary>
    /// Applies a single text filter. An empty filter always matches. Process names compare
    /// exactly (case-insensitive) unless a regex was supplied, titles match as substrings.
    /// </summary>
    private static bool MatchesFilter(string? value, string? filter, Regex? regex, bool exact)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return true;
        }

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (regex is not null)
        {
            return regex.IsMatch(value);
        }

        return exact
            ? string.Equals(value, filter, StringComparison.OrdinalIgnoreCase)
            : value.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public Task<WindowInfo?> GetWindowInfoAsync(nint handle, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            _ = NativeMethods.GetWindowThreadProcessId(handle, out uint processId);
            return GetWindowInfoCore(handle, includeAllDesktops: true, includeUntitled: true, processId);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WindowInfo>> FindWindowsAsync(
        string? title,
        string? processName = null,
        bool useRegex = false,
        int? processId = null,
        CancellationToken cancellationToken = default)
    {
        return EnumerateWindowsCoreAsync(title, processName, processId, useRegex, includeAllDesktops: false, cancellationToken);
    }

    private WindowInfo? GetWindowInfoCore(nint hwnd, bool includeAllDesktops, bool includeUntitled, uint processId)
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
        if (!includeUntitled && string.IsNullOrEmpty(title))
        {
            return null;
        }

        // Get class name
        string className = GetClassName(hwnd);

        // Skip known system windows that shouldn't be enumerated
        if (ShouldSkipWindow(hwnd, className, title, includeUntitled))
        {
            return null;
        }

        // Get process information (the id is resolved by the caller so filters can run early)
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
        // Directly check the process elevation status using the process ID.
        // This is more reliable than using WindowFromPoint which could return
        // a different overlapping window (causing false positives in CI environments).
        return _elevationDetector.IsProcessElevated(processId);
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
            PropertyQueryTimeoutMs,
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

    private static bool ShouldSkipWindow(nint hwnd, string className, string title, bool includeUntitled)
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
        if (!includeUntitled && className.StartsWith("HwndWrapper[", StringComparison.OrdinalIgnoreCase))
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
