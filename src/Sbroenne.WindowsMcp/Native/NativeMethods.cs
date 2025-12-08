using System.Runtime.InteropServices;

namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// Contains P/Invoke declarations for Windows APIs used for mouse operations.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// Synthesizes keystrokes, mouse motions, and button clicks.
    /// </summary>
    /// <param name="nInputs">Number of structures in the pInputs array.</param>
    /// <param name="pInputs">Array of INPUT structures.</param>
    /// <param name="cbSize">Size of an INPUT structure in bytes.</param>
    /// <returns>Number of events successfully inserted into the input stream.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Retrieves the cursor's position in screen coordinates.
    /// </summary>
    /// <param name="lpPoint">Pointer to a POINT structure that receives the screen coordinates.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// Moves the cursor to the specified screen coordinates.
    /// </summary>
    /// <param name="X">The new x-coordinate of the cursor.</param>
    /// <param name="Y">The new y-coordinate of the cursor.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetCursorPos(int X, int Y);

    /// <summary>
    /// Retrieves the specified system metric or configuration setting.
    /// </summary>
    /// <param name="nIndex">The system metric to retrieve.</param>
    /// <returns>The requested system metric value.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int GetSystemMetrics(int nIndex);

    /// <summary>
    /// Retrieves the current double-click time for the mouse.
    /// </summary>
    /// <returns>The double-click time in milliseconds.</returns>
    [LibraryImport("user32.dll")]
    internal static partial uint GetDoubleClickTime();

    /// <summary>
    /// Retrieves a handle to the window that contains the specified point.
    /// </summary>
    /// <param name="Point">The point to be checked.</param>
    /// <returns>Handle to the window that contains the point, or NULL.</returns>
    [LibraryImport("user32.dll")]
    internal static partial nint WindowFromPoint(POINT Point);

    /// <summary>
    /// Retrieves the thread and process IDs for the specified window.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpdwProcessId">Pointer to receive the process ID.</param>
    /// <returns>The thread identifier that created the window.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Copies the text of the specified window's title bar into a buffer.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpString">The buffer to receive the text.</param>
    /// <param name="nMaxCount">Maximum number of characters to copy.</param>
    /// <returns>Length of the copied string, or zero if no title.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int GetWindowText(nint hWnd, [Out] char[] lpString, int nMaxCount);

    /// <summary>
    /// Retrieves the handle to the desktop assigned to the specified thread.
    /// </summary>
    /// <param name="dwFlags">Reserved; must be zero.</param>
    /// <param name="fInherit">If TRUE, child processes inherit the handle.</param>
    /// <param name="dwDesiredAccess">The access to the desktop object.</param>
    /// <returns>Handle to the input desktop, or NULL if a secure desktop is active.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, uint dwDesiredAccess);

    /// <summary>
    /// Closes an open handle to a desktop object.
    /// </summary>
    /// <param name="hDesktop">Handle to the desktop to close.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseDesktop(nint hDesktop);

    /// <summary>
    /// Determines the state of a key at the time the function is called.
    /// </summary>
    /// <param name="vKey">The virtual-key code of the key.</param>
    /// <returns>A value indicating key state (high bit set = pressed).</returns>
    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int vKey);

    /// <summary>
    /// Opens an existing local process object.
    /// </summary>
    /// <param name="dwDesiredAccess">The access rights requested.</param>
    /// <param name="bInheritHandle">If TRUE, child processes inherit the handle.</param>
    /// <param name="dwProcessId">The process identifier.</param>
    /// <returns>Handle to the process, or NULL on failure.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    /// <summary>
    /// Opens the access token associated with a process.
    /// </summary>
    /// <param name="ProcessHandle">Handle to the process.</param>
    /// <param name="DesiredAccess">Access rights requested for the token.</param>
    /// <param name="TokenHandle">Pointer to receive the token handle.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(nint ProcessHandle, uint DesiredAccess, out nint TokenHandle);

    /// <summary>
    /// Retrieves a specified type of information about an access token.
    /// </summary>
    /// <param name="TokenHandle">Handle to an access token.</param>
    /// <param name="TokenInformationClass">Type of information to retrieve.</param>
    /// <param name="TokenInformation">Pointer to receive the information.</param>
    /// <param name="TokenInformationLength">Size of the TokenInformation buffer.</param>
    /// <param name="ReturnLength">Receives the required buffer size.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetTokenInformation(
        nint TokenHandle,
        int TokenInformationClass,
        out TOKEN_ELEVATION TokenInformation,
        int TokenInformationLength,
        out int ReturnLength);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">Handle to an open object.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint hObject);

    #region Keyboard Layout APIs

    /// <summary>
    /// Retrieves the active input locale identifier (keyboard layout) for the specified thread.
    /// </summary>
    /// <param name="idThread">The thread identifier. Use 0 for the current thread.</param>
    /// <returns>The input locale identifier for the thread, or the default input locale for the system if the thread does not have an active keyboard layout.</returns>
    [LibraryImport("user32.dll")]
    internal static partial nint GetKeyboardLayout(uint idThread);

    /// <summary>
    /// Retrieves the name of the active keyboard layout for the calling thread.
    /// </summary>
    /// <param name="pwszKLID">Pointer to the buffer that receives the keyboard layout identifier string. The buffer must be at least KL_NAMELENGTH characters (9 characters).</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetKeyboardLayoutNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetKeyboardLayoutName([Out] char[] pwszKLID);

    /// <summary>
    /// Retrieves the handle to the foreground window.
    /// </summary>
    /// <returns>Handle to the foreground window, or NULL if there is no foreground window.</returns>
    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    #endregion

    #region Window Management APIs

    /// <summary>
    /// Delegate for EnumWindows callback.
    /// </summary>
    /// <param name="hWnd">Handle to a top-level window.</param>
    /// <param name="lParam">Application-defined value.</param>
    /// <returns>True to continue enumeration, false to stop.</returns>
    internal delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    /// <summary>
    /// Enumerates all top-level windows on the screen.
    /// </summary>
    /// <param name="lpEnumFunc">Callback function for each window.</param>
    /// <param name="lParam">Application-defined value to pass to callback.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    /// <summary>
    /// Retrieves the name of the class to which the specified window belongs.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpClassName">Buffer to receive the class name.</param>
    /// <param name="nMaxCount">Maximum number of characters to copy.</param>
    /// <returns>Number of characters copied, or zero on failure.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int GetClassName(nint hWnd, [Out] char[] lpClassName, int nMaxCount);

    /// <summary>
    /// Determines the visibility state of the specified window.
    /// </summary>
    /// <param name="hWnd">Handle to the window to test.</param>
    /// <returns>True if the window is visible, false otherwise.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint hWnd);

    /// <summary>
    /// Retrieves the dimensions of the bounding rectangle of the specified window.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpRect">Pointer to a RECT structure that receives the coordinates.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    /// <summary>
    /// Retrieves the show state and positions of a window.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpwndpl">Pointer to WINDOWPLACEMENT structure.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    /// <summary>
    /// Determines whether the specified window is minimized (iconic).
    /// </summary>
    /// <param name="hWnd">Handle to the window to test.</param>
    /// <returns>True if the window is minimized, false otherwise.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hWnd);

    /// <summary>
    /// Determines whether a window is maximized.
    /// </summary>
    /// <param name="hWnd">Handle to the window to test.</param>
    /// <returns>True if the window is maximized, false otherwise.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsZoomed(nint hWnd);

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window.
    /// </summary>
    /// <param name="hWnd">Handle to the window to activate.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);

    /// <summary>
    /// Brings the specified window to the top of the Z order.
    /// </summary>
    /// <param name="hWnd">Handle to the window to bring to the top.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BringWindowToTop(nint hWnd);

    /// <summary>
    /// Enables the specified process to set the foreground window.
    /// </summary>
    /// <param name="dwProcessId">The process identifier to allow, or ASFW_ANY (-1) to allow any process.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AllowSetForegroundWindow(int dwProcessId);

    /// <summary>
    /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
    /// </summary>
    /// <param name="idAttach">Thread to attach.</param>
    /// <param name="idAttachTo">Thread to attach to.</param>
    /// <param name="fAttach">True to attach, false to detach.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    /// <summary>
    /// Sets the specified window's show state.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="nCmdShow">Show command (SW_* constants).</param>
    /// <returns>True if the window was previously visible, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

    /// <summary>
    /// Changes the size, position, and Z order of a window.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="hWndInsertAfter">Z-order position handle or special value.</param>
    /// <param name="X">New x-coordinate of the window.</param>
    /// <param name="Y">New y-coordinate of the window.</param>
    /// <param name="cx">New width of the window.</param>
    /// <param name="cy">New height of the window.</param>
    /// <param name="uFlags">Window sizing and positioning flags (SWP_* constants).</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Posts a message to the message queue of the specified thread.
    /// </summary>
    /// <param name="hWnd">Handle to the window whose window procedure is to receive the message.</param>
    /// <param name="Msg">The message to be posted.</param>
    /// <param name="wParam">Additional message-specific information.</param>
    /// <param name="lParam">Additional message-specific information.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    /// <summary>
    /// Sends a message to the specified window with timeout.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="Msg">The message to send.</param>
    /// <param name="wParam">Additional message-specific information.</param>
    /// <param name="lParam">Buffer for return value (for WM_GETTEXT, etc.).</param>
    /// <param name="fuFlags">Send message flags (SMTO_* constants).</param>
    /// <param name="uTimeout">Timeout in milliseconds.</param>
    /// <param name="lpdwResult">Receives the result of the message processing.</param>
    /// <returns>Non-zero if successful, zero on failure or timeout.</returns>
    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint SendMessageTimeout(
        nint hWnd,
        uint Msg,
        nint wParam,
        [Out] char[] lParam,
        uint fuFlags,
        uint uTimeout,
        out nint lpdwResult);

    /// <summary>
    /// Retrieves a handle to the display monitor that contains the largest part of a window.
    /// </summary>
    /// <param name="hwnd">Handle to the window.</param>
    /// <param name="dwFlags">Flags for determining monitor selection when window doesn't intersect any monitor.</param>
    /// <returns>Handle to the monitor, or NULL.</returns>
    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromWindow(nint hwnd, uint dwFlags);

    /// <summary>
    /// Retrieves information about a display monitor.
    /// </summary>
    /// <param name="hMonitor">Handle to the display monitor.</param>
    /// <param name="lpmi">Pointer to MONITORINFO structure.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// Enumerates display monitors.
    /// </summary>
    /// <param name="hdc">Handle to a display device context (can be NULL).</param>
    /// <param name="lprcClip">Pointer to a clipping rectangle (can be NULL for all monitors).</param>
    /// <param name="lpfnEnum">Callback function for each monitor.</param>
    /// <param name="dwData">Application-defined data to pass to callback.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    /// <summary>
    /// Delegate for EnumDisplayMonitors callback.
    /// </summary>
    /// <param name="hMonitor">Handle to display monitor.</param>
    /// <param name="hdcMonitor">Handle to monitor DC (can be NULL).</param>
    /// <param name="lprcMonitor">Pointer to monitor intersection rectangle.</param>
    /// <param name="dwData">Application-defined data.</param>
    /// <returns>True to continue enumeration, false to stop.</returns>
    internal delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    /// <summary>
    /// Retrieves the identifier of the thread that created the specified window.
    /// </summary>
    /// <returns>Thread identifier of the calling thread.</returns>
    [LibraryImport("kernel32.dll")]
    internal static partial uint GetCurrentThreadId();

    #endregion

    #region DWM APIs

    /// <summary>
    /// Retrieves the current value of a specified Desktop Window Manager (DWM) attribute.
    /// </summary>
    /// <param name="hwnd">The handle to the window.</param>
    /// <param name="dwAttribute">The attribute to retrieve (DWMWA_* constant).</param>
    /// <param name="pvAttribute">Pointer to receive the attribute value.</param>
    /// <param name="cbAttribute">Size of the pvAttribute buffer in bytes.</param>
    /// <returns>S_OK if successful, or an error code.</returns>
    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    /// <summary>
    /// Retrieves the current cloaked state of a window.
    /// </summary>
    /// <param name="hwnd">The handle to the window.</param>
    /// <param name="dwAttribute">The attribute to retrieve (DWMWA_CLOAKED).</param>
    /// <param name="pvAttribute">Pointer to receive the cloaked state value.</param>
    /// <param name="cbAttribute">Size of the pvAttribute buffer in bytes.</param>
    /// <returns>S_OK if successful, or an error code.</returns>
    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    internal static partial int DwmGetWindowAttributeInt(nint hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    #endregion

    #region Screenshot Capture APIs

    /// <summary>
    /// Copies the visual representation of a window into the specified device context.
    /// </summary>
    /// <param name="hwnd">Handle to the window to copy.</param>
    /// <param name="hdcBlt">Handle to the device context to copy into.</param>
    /// <param name="nFlags">Drawing options (PW_CLIENTONLY or PW_RENDERFULLCONTENT).</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <remarks>
    /// Use PW_RENDERFULLCONTENT (0x00000002) for Windows 8.1+ to capture the full window content.
    /// </remarks>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PrintWindow(nint hwnd, nint hdcBlt, uint nFlags);

    /// <summary>
    /// Retrieves information about the global cursor.
    /// </summary>
    /// <param name="pci">Pointer to a CURSORINFO structure that receives the information.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorInfo(ref CURSORINFO pci);

    /// <summary>
    /// Draws an icon or cursor into the specified device context.
    /// </summary>
    /// <param name="hDC">Handle to the device context.</param>
    /// <param name="X">X-coordinate of the upper-left corner of the icon.</param>
    /// <param name="Y">Y-coordinate of the upper-left corner of the icon.</param>
    /// <param name="hIcon">Handle to the icon to be drawn.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DrawIcon(nint hDC, int X, int Y, nint hIcon);

    /// <summary>
    /// Determines whether the specified window handle identifies an existing window.
    /// </summary>
    /// <param name="hWnd">Handle to the window to test.</param>
    /// <returns>True if the window handle identifies an existing window, false otherwise.</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(nint hWnd);

    #endregion
}
