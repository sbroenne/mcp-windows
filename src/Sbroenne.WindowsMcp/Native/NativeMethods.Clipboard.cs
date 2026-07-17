using System.Runtime.InteropServices;

namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// P/Invoke declarations for the Win32 clipboard API. Used instead of
/// <see cref="System.Windows.Forms.Clipboard"/> because the OLE-based WinForms clipboard requires
/// a message pump on the calling thread, which the dedicated STA worker thread does not run.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>CF_UNICODETEXT clipboard format.</summary>
    internal const uint CF_UNICODETEXT = 13;

    /// <summary>GMEM_MOVEABLE flag for <see cref="GlobalAlloc"/>.</summary>
    internal const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>Opens the clipboard for examination and prevents other apps from modifying it.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenClipboard(nint hWndNewOwner);

    /// <summary>Closes the clipboard.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseClipboard();

    /// <summary>Empties the clipboard and frees handles to data in it.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EmptyClipboard();

    /// <summary>Determines whether the clipboard contains data in the specified format.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsClipboardFormatAvailable(uint format);

    /// <summary>Retrieves data from the clipboard in the specified format.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint GetClipboardData(uint uFormat);

    /// <summary>Places data on the clipboard in the specified format.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetClipboardData(uint uFormat, nint hMem);

    /// <summary>Allocates the specified number of bytes from the global heap.</summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    /// <summary>Frees a global memory block.</summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint GlobalFree(nint hMem);

    /// <summary>Locks a global memory object and returns a pointer to its first byte.</summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint GlobalLock(nint hMem);

    /// <summary>Unlocks a global memory object.</summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalUnlock(nint hMem);

    /// <summary>Retrieves the current size, in bytes, of a global memory object.</summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint GlobalSize(nint hMem);
}
