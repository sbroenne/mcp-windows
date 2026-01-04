using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Detects whether a target window belongs to an elevated (admin) process.
/// </summary>
public class ElevationDetector
{
    /// <inheritdoc/>
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
