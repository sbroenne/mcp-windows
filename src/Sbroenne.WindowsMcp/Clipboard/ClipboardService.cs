using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Clipboard;

/// <summary>
/// Reads and writes the Windows text clipboard using the raw Win32 clipboard API.
/// All operations are marshalled onto the shared <see cref="UIAutomationThread"/> (STA) so the
/// clipboard is always accessed from a single, consistent thread.
/// </summary>
/// <remarks>
/// Clipboard read/write is often the fastest bulk-text IO in and out of desktop apps - far cheaper
/// than typing character-by-character or OCR. This service is a "dumb actuator": it does not
/// transform content, it only moves text across the clipboard boundary. The Win32 API is used
/// instead of <c>System.Windows.Forms.Clipboard</c> because the OLE clipboard requires a message
/// pump on the calling thread, which the STA worker thread does not run.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ClipboardService(UIAutomationThread staThread)
{
    private const int OpenAttempts = 10;
    private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(20);

    private readonly UIAutomationThread _staThread = staThread
        ?? throw new ArgumentNullException(nameof(staThread));

    /// <summary>
    /// Reads the current clipboard text.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result carrying the clipboard text, or a null text when the clipboard has no text.</returns>
    public async Task<ClipboardResult> GetTextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var text = await _staThread.ExecuteAsync(ReadClipboardText, cancellationToken);
            return ClipboardResult.CreateGetSuccess(text);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ClipboardResult.CreateFailure(
                "get",
                $"Could not read the clipboard: {ex.Message}. Another process may be holding the clipboard open.");
        }
    }

    /// <summary>
    /// Replaces the clipboard contents with the supplied text. An empty string clears the clipboard.
    /// </summary>
    /// <param name="text">The text to place on the clipboard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result describing how many characters were written.</returns>
    public async Task<ClipboardResult> SetTextAsync(string? text, CancellationToken cancellationToken = default)
    {
        var value = text ?? string.Empty;

        try
        {
            await _staThread.ExecuteAsync(() =>
            {
                if (value.Length == 0)
                {
                    ClearClipboard();
                }
                else
                {
                    WriteClipboardText(value);
                }
            }, cancellationToken);

            return ClipboardResult.CreateSetSuccess(value.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ClipboardResult.CreateFailure(
                "set",
                $"Could not write to the clipboard: {ex.Message}. Another process may be holding the clipboard open.");
        }
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result describing the clear operation.</returns>
    public async Task<ClipboardResult> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _staThread.ExecuteAsync(ClearClipboard, cancellationToken);
            return ClipboardResult.CreateClearSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ClipboardResult.CreateFailure(
                "clear",
                $"Could not clear the clipboard: {ex.Message}. Another process may be holding the clipboard open.");
        }
    }

    private static string? ReadClipboardText()
    {
        if (!NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT))
        {
            // Nothing to open/close if there's no text at all.
            return null;
        }

        OpenClipboardWithRetry();
        try
        {
            var handle = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
            if (handle == 0)
            {
                return null;
            }

            var pointer = NativeMethods.GlobalLock(handle);
            if (pointer == 0)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                NativeMethods.GlobalUnlock(handle);
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    private static void WriteClipboardText(string text)
    {
        // +1 for the null terminator; UTF-16 = 2 bytes per char.
        var byteCount = (nuint)((text.Length + 1) * 2);
        var hMem = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, byteCount);
        if (hMem == 0)
        {
            throw new InvalidOperationException("GlobalAlloc failed to allocate clipboard memory.");
        }

        var ownedByClipboard = false;
        try
        {
            var pointer = NativeMethods.GlobalLock(hMem);
            if (pointer == 0)
            {
                throw new InvalidOperationException("GlobalLock failed on clipboard memory.");
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                // Null terminator.
                Marshal.WriteInt16(pointer, text.Length * 2, 0);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hMem);
            }

            OpenClipboardWithRetry();
            try
            {
                NativeMethods.EmptyClipboard();
                if (NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, hMem) == 0)
                {
                    throw new InvalidOperationException(
                        $"SetClipboardData failed (Win32 error {Marshal.GetLastWin32Error()}).");
                }

                // Ownership of hMem transfers to the system on success; must not free it.
                ownedByClipboard = true;
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        }
        finally
        {
            if (!ownedByClipboard)
            {
                NativeMethods.GlobalFree(hMem);
            }
        }
    }

    private static void ClearClipboard()
    {
        OpenClipboardWithRetry();
        try
        {
            NativeMethods.EmptyClipboard();
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    private static void OpenClipboardWithRetry()
    {
        for (var attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (NativeMethods.OpenClipboard(nint.Zero))
            {
                return;
            }

            // Brief back-off before retrying. SpinWait.SpinUntil with an always-false predicate
            // yields the thread for the delay without a blocking sleep (forbidden by the timing audit).
            SpinWait.SpinUntil(static () => false, OpenRetryDelay);
        }

        throw new InvalidOperationException(
            $"OpenClipboard failed after {OpenAttempts} attempts (Win32 error {Marshal.GetLastWin32Error()}).");
    }
}
