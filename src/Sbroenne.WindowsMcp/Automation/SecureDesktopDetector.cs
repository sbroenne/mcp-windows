using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Detects whether a secure desktop (UAC, lock screen) is currently active.
/// </summary>
public class SecureDesktopDetector
{
    /// <inheritdoc/>
    public bool IsSecureDesktopActive()
    {
        // Try to open the input desktop with read-only access first.
        // This is less restrictive than DESKTOP_SWITCHDESKTOP and works in most contexts.
        // If we're on a secure desktop (UAC, lock screen, Ctrl+Alt+Del),
        // this will return NULL with ERROR_ACCESS_DENIED.
        var hDesktop = NativeMethods.OpenInputDesktop(
            0,
            false,
            NativeConstants.DESKTOP_READOBJECTS);

        if (hDesktop == IntPtr.Zero)
        {
            // Check the error code to distinguish between secure desktop and other failures
            var error = Marshal.GetLastWin32Error();

            // ERROR_ACCESS_DENIED (5) indicates we're on a secure desktop
            // Other errors (like ERROR_INVALID_HANDLE) may indicate other issues
            // but we'll be conservative and assume secure desktop for safety
            return error == NativeConstants.ERROR_ACCESS_DENIED;
        }

        // We got a handle - close it and return false (no secure desktop)
        _ = NativeMethods.CloseDesktop(hDesktop);
        return false;
    }
}
