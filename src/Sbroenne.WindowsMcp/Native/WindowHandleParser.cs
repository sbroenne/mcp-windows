using System.Globalization;

namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// Parses and formats Win32 window handles (HWND) as JSON-safe decimal strings.
/// </summary>
public static class WindowHandleParser
{
    /// <summary>
    /// Attempts to parse a decimal-string window handle into a native pointer.
    /// </summary>
    /// <param name="handleString">Decimal string (digits only).</param>
    /// <param name="handle">Parsed handle.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? handleString, out nint handle)
    {
        handle = IntPtr.Zero;

        if (string.IsNullOrWhiteSpace(handleString))
        {
            return false;
        }

        // Decimal string only: digits 0-9, no sign, no 0x prefix, no whitespace.
        foreach (var ch in handleString)
        {
            if (ch is < '0' or > '9')
            {
                return false;
            }
        }

        if (!ulong.TryParse(handleString, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        // Ensure value fits into a signed pointer-sized integer.
        var max = IntPtr.Size == 8 ? (ulong)long.MaxValue : int.MaxValue;
        if (value > max)
        {
            return false;
        }

        handle = (nint)(long)value;
        return true;
    }

    /// <summary>
    /// Formats a native window handle (HWND) as a decimal string.
    /// </summary>
    /// <param name="handle">Native handle.</param>
    /// <returns>Decimal string representation.</returns>
    public static string Format(nint handle)
    {
        return handle.ToInt64().ToString(CultureInfo.InvariantCulture);
    }
}
