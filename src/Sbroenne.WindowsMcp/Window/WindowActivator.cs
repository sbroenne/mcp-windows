using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Window;

/// <summary>
/// Activates windows and brings them to the foreground using multiple strategies.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowActivator
{
    private readonly WindowConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowActivator"/> class.
    /// </summary>
    /// <param name="configuration">Window configuration.</param>
    public WindowActivator(WindowConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateWindowAsync(
        nint handle,
        bool useFallbackStrategies = true,
        CancellationToken cancellationToken = default)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        // Check if window is valid
        if (!NativeMethods.IsWindowVisible(handle))
        {
            return false;
        }

        // Save current window bounds before any operations that might change position
        // This is critical for multi-monitor setups where SW_RESTORE can move windows
        RECT savedBounds = default;
        bool hasSavedBounds = NativeMethods.GetWindowRect(handle, out savedBounds);

        // If window is minimized, restore it first
        if (NativeMethods.IsIconic(handle))
        {
            NativeMethods.ShowWindow(handle, NativeConstants.SW_RESTORE);
            await Task.Delay(50, cancellationToken);

            // Restore saved bounds since SW_RESTORE may have moved the window
            if (hasSavedBounds)
            {
                RestoreWindowBounds(handle, savedBounds);
            }
        }

        // Strategy 1: Simple SetForegroundWindow
        if (TrySetForegroundWindow(handle))
        {
            return true;
        }

        if (!useFallbackStrategies)
        {
            return IsForegroundWindow(handle);
        }

        // Strategy 2: AllowSetForegroundWindow + SetForegroundWindow
        if (TryWithAllowSetForegroundWindow(handle))
        {
            return true;
        }

        // Strategy 3: Simulate Alt key press to bypass foreground lock
        if (await TryWithAltKeyAsync(handle, cancellationToken))
        {
            return true;
        }

        // Strategy 4: AttachThreadInput (use with caution)
        if (TryWithAttachThreadInput(handle))
        {
            return true;
        }

        // Strategy 5: Minimize and restore
        if (await TryWithMinimizeRestoreAsync(handle, savedBounds, hasSavedBounds, cancellationToken))
        {
            return true;
        }

        return IsForegroundWindow(handle);
    }

    /// <inheritdoc/>
    public nint GetForegroundWindow()
    {
        return NativeMethods.GetForegroundWindow();
    }

    /// <inheritdoc/>
    public bool IsForegroundWindow(nint handle)
    {
        return handle != IntPtr.Zero && NativeMethods.GetForegroundWindow() == handle;
    }

    /// <summary>
    /// Strategy 1: Simple SetForegroundWindow.
    /// </summary>
    private bool TrySetForegroundWindow(nint handle)
    {
        return NativeMethods.SetForegroundWindow(handle) && IsForegroundWindow(handle);
    }

    /// <summary>
    /// Strategy 2: AllowSetForegroundWindow before SetForegroundWindow.
    /// </summary>
    private bool TryWithAllowSetForegroundWindow(nint handle)
    {
        // Get the process that owns the target window
        _ = NativeMethods.GetWindowThreadProcessId(handle, out uint targetProcessId);

        // Allow that process to set foreground
        NativeMethods.AllowSetForegroundWindow(NativeConstants.ASFW_ANY);

        return NativeMethods.SetForegroundWindow(handle) && IsForegroundWindow(handle);
    }

    /// <summary>
    /// Strategy 3: Simulate Alt key press to bypass foreground lock.
    /// </summary>
    private async Task<bool> TryWithAltKeyAsync(nint handle, CancellationToken cancellationToken)
    {
        // Windows prevents SetForegroundWindow from working if the calling process
        // isn't the foreground. Pressing Alt can help bypass this restriction.

        // Simulate Alt key press
        var input = new INPUT
        {
            Type = INPUT.INPUT_KEYBOARD,
            Data = new INPUTUNION
            {
                Keyboard = new KEYBDINPUT
                {
                    WVk = NativeConstants.VK_MENU, // Alt key
                    DwFlags = 0 // Key down
                }
            }
        };

        _ = NativeMethods.SendInput(1, [input], INPUT.Size);

        // Brief delay
        await Task.Delay(10, cancellationToken);

        // Release Alt key
        input.Data.Keyboard.DwFlags = NativeConstants.KEYEVENTF_KEYUP;
        _ = NativeMethods.SendInput(1, [input], INPUT.Size);

        // Now try to set foreground
        return NativeMethods.SetForegroundWindow(handle) && IsForegroundWindow(handle);
    }

    /// <summary>
    /// Strategy 4: AttachThreadInput to share input state.
    /// </summary>
    private bool TryWithAttachThreadInput(nint handle)
    {
        // Get thread IDs
        uint targetThreadId = NativeMethods.GetWindowThreadProcessId(handle, out _);
        uint currentThreadId = NativeMethods.GetCurrentThreadId();

        if (targetThreadId == 0 || targetThreadId == currentThreadId)
        {
            return false;
        }

        bool attached = false;

        try
        {
            // Attach our thread to the target window's thread
            attached = NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);

            if (!attached)
            {
                return false;
            }

            // Try to bring window to front
            NativeMethods.BringWindowToTop(handle);
            NativeMethods.SetForegroundWindow(handle);

            return IsForegroundWindow(handle);
        }
        finally
        {
            // Always detach threads
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }

    /// <summary>
    /// Strategy 5: Minimize then restore the window.
    /// </summary>
    private async Task<bool> TryWithMinimizeRestoreAsync(
        nint handle,
        RECT savedBounds,
        bool hasSavedBounds,
        CancellationToken cancellationToken)
    {
        // This is a last-resort strategy that briefly minimizes the window
        NativeMethods.ShowWindow(handle, NativeConstants.SW_MINIMIZE);
        await Task.Delay(50, cancellationToken);
        NativeMethods.ShowWindow(handle, NativeConstants.SW_RESTORE);
        await Task.Delay(50, cancellationToken);

        // Restore the original bounds if we saved them
        // This is critical for multi-monitor setups
        if (hasSavedBounds)
        {
            RestoreWindowBounds(handle, savedBounds);
        }

        return NativeMethods.SetForegroundWindow(handle) && IsForegroundWindow(handle);
    }

    /// <summary>
    /// Restores window to saved bounds without changing Z-order.
    /// </summary>
    private static void RestoreWindowBounds(nint handle, RECT bounds)
    {
        int width = bounds.Right - bounds.Left;
        int height = bounds.Bottom - bounds.Top;
        NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            bounds.Left,
            bounds.Top,
            width,
            height,
            NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE);
    }
}
