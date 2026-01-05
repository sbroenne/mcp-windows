using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Detects whether a secure desktop (UAC, lock screen) is currently active.
/// </summary>
/// <remarks>
/// Implementation based on FlaUI and pywinauto patterns:
/// - FlaUI: Uses fallback checks when primary detection fails
/// - pywinauto: Tries to access windows before declaring failure
/// 
/// This implementation adds a secondary check using GetForegroundWindow()
/// and UI Automation to avoid false positives when modal dialogs (like Save As)
/// are open but the desktop is still accessible.
/// </remarks>
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

            // ERROR_ACCESS_DENIED (5) may indicate secure desktop OR false positive
            // (e.g., Save As dialog is open, or process lacks permissions)
            if (error == NativeConstants.ERROR_ACCESS_DENIED)
            {
                // Secondary check: Can we access the foreground window via UI Automation?
                // If yes, this is a false positive - the desktop is accessible
                // Pattern inspired by FlaUI's retry logic and pywinauto's window access checks
                if (IsForegroundWindowAccessible())
                {
                    // Foreground window is accessible despite ACCESS_DENIED from OpenInputDesktop
                    // This is a false positive (modal dialog or permission issue, not secure desktop)
                    return false;
                }

                // Foreground window is not accessible - likely a true secure desktop
                return true;
            }

            // Other errors - not a secure desktop, just some other issue
            return false;
        }

        // We got a handle - close it and return false (no secure desktop)
        _ = NativeMethods.CloseDesktop(hDesktop);
        return false;
    }

    /// <summary>
    /// Checks if the foreground window is accessible via UI Automation.
    /// </summary>
    /// <remarks>
    /// Pattern from FlaUI/pywinauto: Before declaring secure desktop,
    /// verify that we actually cannot interact with any windows.
    /// If GetForegroundWindow returns a valid handle and we can get
    /// the UI Automation element, the desktop is accessible.
    /// </remarks>
    private bool IsForegroundWindowAccessible()
    {
        try
        {
            var foregroundHwnd = NativeMethods.GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
            {
                return false;
            }

            // Try to get the UI Automation element for the foreground window
            // If this succeeds, the window is accessible and we're not on a secure desktop
            var uia = UIA3Automation.Instance;
            var element = uia.ElementFromHandle(foregroundHwnd);

            // Check if we can read basic properties
            if (element != null)
            {
                // Try to read the Name property - if this works, the window is accessible
                _ = element.CurrentName;
                return true;
            }

            return false;
        }
        catch (COMException)
        {
            // COM exception typically means we can't access the window
            return false;
        }
        catch (Exception)
        {
            // Any other exception - assume not accessible for safety
            return false;
        }
    }
}
