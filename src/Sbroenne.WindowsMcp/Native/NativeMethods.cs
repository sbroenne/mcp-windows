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
}
