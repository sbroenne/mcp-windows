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
    /// <returns>True if the process is elevated, false otherwise.</returns>
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
            // Cannot open process - might be elevated or protected
            return true;
        }

        try
        {
            // Open the process token
            if (!NativeMethods.OpenProcessToken(hProcess, NativeConstants.TOKEN_QUERY, out var hToken))
            {
                // Cannot open token - likely elevated
                return true;
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
                    // Cannot query token - assume elevated
                    return true;
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
