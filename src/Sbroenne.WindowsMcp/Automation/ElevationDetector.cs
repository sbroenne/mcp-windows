using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Detects whether a target window belongs to an elevated (admin) process.
/// </summary>
public class ElevationDetector
{
    /// <summary>
    /// Checks if the window at the specified screen coordinates belongs to an elevated process.
    /// </summary>
    /// <param name="x">X screen coordinate.</param>
    /// <param name="y">Y screen coordinate.</param>
    /// <returns>True if the window at the point is elevated.</returns>
    /// <remarks>
    /// WARNING: This method uses WindowFromPoint which returns whatever window is at that location.
    /// If another window overlaps the target, the wrong window will be checked.
    /// Prefer <see cref="IsProcessElevated(uint)"/> when you have the process ID.
    /// </remarks>
    public bool IsTargetElevated(int x, int y)
    {
        var point = new POINT(x, y);
        var hwnd = NativeMethods.WindowFromPoint(point);

        if (hwnd == IntPtr.Zero)
        {
            // No window at the specified point - not elevated
            return false;
        }

        // Get the process ID for the window
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);

        return IsProcessElevated(processId);
    }

    /// <summary>
    /// Checks if the specified process is elevated (running as administrator).
    /// </summary>
    /// <param name="processId">The process ID to check.</param>
    /// <returns>True if the process is definitely elevated, false if not elevated or unknown.</returns>
    /// <remarks>
    /// This method returns false when elevation status cannot be determined (e.g., due to
    /// security policies, process protection, or access restrictions). This is intentional:
    /// it's better to attempt interaction with a window and handle failure than to
    /// incorrectly refuse interaction with a non-elevated window.
    /// </remarks>
    public bool IsProcessElevated(uint processId)
    {
        if (processId == 0)
        {
            return false;
        }

        // Open the process with limited information access
        var hProcess = NativeMethods.OpenProcess(
            NativeConstants.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);

        if (hProcess == IntPtr.Zero)
        {
            // Cannot open process - may be due to security policies, anti-virus, etc.
            // Default to false (not elevated) - better to try and fail than refuse.
            return false;
        }

        try
        {
            // Open the process token
            if (!NativeMethods.OpenProcessToken(hProcess, NativeConstants.TOKEN_QUERY, out var hToken))
            {
                // Cannot open token - may be due to security policies.
                // Default to false - better to try and fail than refuse.
                return false;
            }

            try
            {
                // Query the token elevation status
                var tokenInfo = new TOKEN_ELEVATION();
                var returnLength = 0;

                if (!NativeMethods.GetTokenInformation(
                    hToken,
                    NativeConstants.TokenElevation,
                    out tokenInfo,
                    Marshal.SizeOf<TOKEN_ELEVATION>(),
                    out returnLength))
                {
                    // Cannot query token - default to false.
                    return false;
                }

                return tokenInfo.TokenIsElevated != 0;
            }
            finally
            {
                _ = NativeMethods.CloseHandle(hToken);
            }
        }
        finally
        {
            _ = NativeMethods.CloseHandle(hProcess);
        }
    }
}
