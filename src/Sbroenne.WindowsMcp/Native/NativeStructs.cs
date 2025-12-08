using System.Runtime.InteropServices;

namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// Contains native structure definitions for Windows API interop.
/// </summary>
internal static class NativeStructs
{
    // Marker class - all structures defined below
}

/// <summary>
/// Represents a point in screen coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    /// <summary>The x-coordinate of the point.</summary>
    public int X;

    /// <summary>The y-coordinate of the point.</summary>
    public int Y;

    /// <summary>
    /// Initializes a new instance of the <see cref="POINT"/> struct.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    public POINT(int x, int y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Used by SendInput to store information for synthesizing input events.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    /// <summary>The type of the input event (mouse, keyboard, or hardware).</summary>
    public uint Type;

    /// <summary>The input union containing mouse, keyboard, or hardware input data.</summary>
    public INPUTUNION Data;

    /// <summary>Input type constant for mouse input.</summary>
    public const uint INPUT_MOUSE = 0;

    /// <summary>Input type constant for keyboard input.</summary>
    public const uint INPUT_KEYBOARD = 1;

    /// <summary>Gets the size of the INPUT structure for use with SendInput.</summary>
    public static int Size => Marshal.SizeOf<INPUT>();
}

/// <summary>
/// Union for INPUT structure to hold different input types.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
    /// <summary>Mouse input data.</summary>
    [FieldOffset(0)]
    public MOUSEINPUT Mouse;

    /// <summary>Keyboard input data.</summary>
    [FieldOffset(0)]
    public KEYBDINPUT Keyboard;
}

/// <summary>
/// Contains information about a simulated mouse event.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    /// <summary>Absolute position or relative motion in x-direction.</summary>
    public int Dx;

    /// <summary>Absolute position or relative motion in y-direction.</summary>
    public int Dy;

    /// <summary>Wheel movement amount for scroll operations.</summary>
    public int MouseData;

    /// <summary>Mouse event flags specifying the type of mouse event.</summary>
    public uint DwFlags;

    /// <summary>Time stamp for the event (0 = system provides).</summary>
    public uint Time;

    /// <summary>Additional value associated with the event.</summary>
    public nuint DwExtraInfo;
}

/// <summary>
/// Contains information about a simulated keyboard event.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    /// <summary>Virtual-key code.</summary>
    public ushort WVk;

    /// <summary>Hardware scan code for the key.</summary>
    public ushort WScan;

    /// <summary>Keyboard event flags.</summary>
    public uint DwFlags;

    /// <summary>Time stamp for the event (0 = system provides).</summary>
    public uint Time;

    /// <summary>Additional value associated with the event.</summary>
    public nuint DwExtraInfo;
}

/// <summary>
/// Contains information about the elevation status of a process's token.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct TOKEN_ELEVATION
{
    /// <summary>Non-zero if the token is elevated.</summary>
    public int TokenIsElevated;
}

/// <summary>
/// Defines a rectangle by the coordinates of its upper-left and lower-right corners.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    /// <summary>The x-coordinate of the upper-left corner of the rectangle.</summary>
    public int Left;

    /// <summary>The y-coordinate of the upper-left corner of the rectangle.</summary>
    public int Top;

    /// <summary>The x-coordinate of the lower-right corner of the rectangle.</summary>
    public int Right;

    /// <summary>The y-coordinate of the lower-right corner of the rectangle.</summary>
    public int Bottom;

    /// <summary>Gets the width of the rectangle.</summary>
    public readonly int Width => Right - Left;

    /// <summary>Gets the height of the rectangle.</summary>
    public readonly int Height => Bottom - Top;

    /// <summary>
    /// Initializes a new instance of the <see cref="RECT"/> struct.
    /// </summary>
    /// <param name="left">The x-coordinate of the upper-left corner.</param>
    /// <param name="top">The y-coordinate of the upper-left corner.</param>
    /// <param name="right">The x-coordinate of the lower-right corner.</param>
    /// <param name="bottom">The y-coordinate of the lower-right corner.</param>
    public RECT(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

/// <summary>
/// Contains information about the placement of a window on the screen.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WINDOWPLACEMENT
{
    /// <summary>The length of the structure, in bytes.</summary>
    public uint Length;

    /// <summary>Specifies flags that control the position of the minimized window and the method by which the window is restored.</summary>
    public uint Flags;

    /// <summary>The current show state of the window (SW_* values).</summary>
    public uint ShowCmd;

    /// <summary>The coordinates of the window's upper-left corner when the window is minimized.</summary>
    public POINT PtMinPosition;

    /// <summary>The coordinates of the window's upper-left corner when the window is maximized.</summary>
    public POINT PtMaxPosition;

    /// <summary>The window's coordinates when the window is in the restored position.</summary>
    public RECT RcNormalPosition;

    /// <summary>Gets the size of the structure.</summary>
    public static uint Size => (uint)Marshal.SizeOf<WINDOWPLACEMENT>();

    /// <summary>
    /// Creates a new WINDOWPLACEMENT structure with the length field initialized.
    /// </summary>
    /// <returns>An initialized WINDOWPLACEMENT structure.</returns>
    public static WINDOWPLACEMENT Create()
    {
        return new WINDOWPLACEMENT { Length = Size };
    }
}

/// <summary>
/// Contains information about a display monitor.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct MONITORINFO
{
    /// <summary>The size of the structure, in bytes.</summary>
    public uint CbSize;

    /// <summary>A RECT structure that specifies the display monitor rectangle.</summary>
    public RECT RcMonitor;

    /// <summary>A RECT structure that specifies the work area rectangle of the display monitor.</summary>
    public RECT RcWork;

    /// <summary>A set of flags that represent attributes of the display monitor.</summary>
    public uint DwFlags;

    /// <summary>This is the primary display monitor.</summary>
    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    /// <summary>Gets the size of the structure.</summary>
    public static uint Size => (uint)Marshal.SizeOf<MONITORINFO>();

    /// <summary>
    /// Creates a new MONITORINFO structure with the cbSize field initialized.
    /// </summary>
    /// <returns>An initialized MONITORINFO structure.</returns>
    public static MONITORINFO Create()
    {
        return new MONITORINFO { CbSize = Size };
    }

    /// <summary>
    /// Gets a value indicating whether this is the primary monitor.
    /// </summary>
    public readonly bool IsPrimary => (DwFlags & MONITORINFOF_PRIMARY) != 0;
}

/// <summary>
/// Contains global cursor information.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CURSORINFO
{
    /// <summary>The size of the structure, in bytes.</summary>
    public int CbSize;

    /// <summary>The cursor state (0 = hidden, CURSOR_SHOWING = visible).</summary>
    public int Flags;

    /// <summary>Handle to the cursor.</summary>
    public nint HCursor;

    /// <summary>Screen coordinates of the cursor.</summary>
    public POINT PtScreenPos;

    /// <summary>Cursor is visible.</summary>
    public const int CURSOR_SHOWING = 0x00000001;

    /// <summary>Cursor is suppressed (touch/pen mode).</summary>
    public const int CURSOR_SUPPRESSED = 0x00000002;

    /// <summary>Gets the size of the structure.</summary>
    public static int Size => Marshal.SizeOf<CURSORINFO>();

    /// <summary>
    /// Creates a new CURSORINFO structure with the cbSize field initialized.
    /// </summary>
    /// <returns>An initialized CURSORINFO structure.</returns>
    public static CURSORINFO Create()
    {
        return new CURSORINFO { CbSize = Size };
    }

    /// <summary>
    /// Gets a value indicating whether the cursor is visible.
    /// </summary>
    public readonly bool IsVisible => (Flags & CURSOR_SHOWING) != 0;
}
