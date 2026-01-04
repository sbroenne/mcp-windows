using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// COM interface for interacting with virtual desktops in Windows 10/11.
/// </summary>
/// <remarks>
/// This interface allows checking which virtual desktop a window is on.
/// The implementation is provided by the system shell.
/// </remarks>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
internal interface IVirtualDesktopManager
{
    /// <summary>
    /// Indicates whether the provided window is on the currently active virtual desktop.
    /// </summary>
    /// <param name="topLevelWindow">The window of interest.</param>
    /// <param name="onCurrentDesktop">True if the window is on the current virtual desktop; false otherwise.</param>
    /// <returns>An HRESULT indicating success or failure.</returns>
    [PreserveSig]
    int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrentDesktop);

    /// <summary>
    /// Gets the virtual desktop assigned to a specified window.
    /// </summary>
    /// <param name="topLevelWindow">The window of interest.</param>
    /// <param name="desktopId">The ID of the virtual desktop the window is on.</param>
    /// <returns>An HRESULT indicating success or failure.</returns>
    [PreserveSig]
    int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

    /// <summary>
    /// Moves a window to a specified virtual desktop.
    /// </summary>
    /// <param name="topLevelWindow">The window to move.</param>
    /// <param name="desktopId">The ID of the target virtual desktop.</param>
    /// <returns>An HRESULT indicating success or failure.</returns>
    [PreserveSig]
    int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}

/// <summary>
/// Factory class for creating IVirtualDesktopManager instances.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class VirtualDesktopManagerFactory
{
    /// <summary>CLSID for the VirtualDesktopManager class.</summary>
    private static readonly Guid s_clsidVirtualDesktopManager = new("aa509086-5ca9-4c25-8f95-589d3c07b48a");

    /// <summary>
    /// Creates a new instance of IVirtualDesktopManager.
    /// </summary>
    /// <returns>An IVirtualDesktopManager instance, or null if creation fails.</returns>
    public static IVirtualDesktopManager? Create()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(s_clsidVirtualDesktopManager);
            if (type == null)
            {
                return null;
            }

            return Activator.CreateInstance(type) as IVirtualDesktopManager;
        }
        catch (COMException)
        {
            // Virtual desktop manager not available (e.g., Windows 7)
            return null;
        }
        catch (InvalidCastException)
        {
            // Interface not supported
            return null;
        }
    }

    /// <summary>
    /// Checks if a window is on the current virtual desktop.
    /// </summary>
    /// <param name="hwnd">The window handle to check.</param>
    /// <returns>
    /// True if the window is on the current virtual desktop or if virtual desktops are not supported.
    /// False if the window is on a different virtual desktop.
    /// </returns>
    public static bool IsWindowOnCurrentDesktop(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var manager = Create();
        if (manager == null)
        {
            // Virtual desktops not supported, assume window is on current desktop
            return true;
        }

        try
        {
            int hr = manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool onCurrentDesktop);
            if (hr >= 0)
            {
                return onCurrentDesktop;
            }

            // If the call fails, assume window is on current desktop
            return true;
        }
        finally
        {
            // Release COM object
            if (manager is not null)
            {
                Marshal.ReleaseComObject(manager);
            }
        }
    }
}
