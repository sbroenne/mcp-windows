using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Subscribes to UIA <c>StructureChangedEvent</c> for a subtree and exposes the changes as a
/// signal that a waiter can block on.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately makes waiting event-<em>assisted</em> rather than event-<em>driven</em>.
/// The polling loop remains the correctness guarantee, and the signal is only used to cut a
/// sleep short. That ordering matters, because UIA providers are not required to raise
/// structure-changed events reliably: a purely event-driven wait would hang on providers that
/// stay silent. With this design the worst case is exactly the previous polling behaviour, and
/// the best case is that the waiter re-checks as soon as the tree actually changes instead of
/// after the remainder of its backoff.
/// </para>
/// <para>
/// The signal is a binary semaphore, so a burst of events coalesces into a single wake-up. That
/// is the debounce: a churning Chromium tree cannot queue unbounded re-checks, because at most
/// one wake-up can ever be pending.
/// </para>
/// <para>
/// UIA delivers event callbacks on its own threads. Registration and unregistration are both
/// marshalled to the automation STA thread, and unregistration is mandatory - a handler that is
/// never removed leaks a COM callback into the target process.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class StructureChangeSignal : UIA.IUIAutomationStructureChangedEventHandler, IAsyncDisposable
{
    private readonly UIAutomationThread _staThread;
    private readonly UIA.IUIAutomationElement _root;
    private readonly SemaphoreSlim _changed = new(0, 1);
    private int _eventCount;
    private bool _disposed;

    private StructureChangeSignal(UIAutomationThread staThread, UIA.IUIAutomationElement root)
    {
        _staThread = staThread;
        _root = root;
    }

    /// <summary>
    /// Gets the number of structure-changed events observed. Used to quantify event volume.
    /// </summary>
    public int EventCount => Volatile.Read(ref _eventCount);

    /// <summary>
    /// Subscribes to structure changes under <paramref name="root"/>, or returns <see langword="null"/>
    /// if the provider refuses the subscription. A null result is not an error: the caller simply
    /// falls back to unassisted polling.
    /// </summary>
    public static async Task<StructureChangeSignal?> CreateAsync(
        UIAutomationThread staThread,
        UIA.IUIAutomationElement root,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staThread);
        ArgumentNullException.ThrowIfNull(root);

        var signal = new StructureChangeSignal(staThread, root);

        var subscribed = await staThread.ExecuteAsync(
            () =>
            {
                try
                {
                    UIA3Automation.Instance.Automation.AddStructureChangedEventHandler(
                        root,
                        UIA.TreeScope.TreeScope_Subtree,
                        null,
                        signal);
                    return true;
                }
                catch (COMException)
                {
                    // Provider does not support the subscription; polling still covers us.
                    return false;
                }
            },
            cancellationToken).ConfigureAwait(false);

        return subscribed ? signal : null;
    }

    /// <summary>
    /// Waits for the next structure change, giving up after <paramref name="timeout"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a change was observed, <see langword="false"/> if the timeout
    /// expired. Callers re-check their condition either way, so the distinction only affects how
    /// long they waited.
    /// </returns>
    public async Task<bool> WaitForChangeAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            return await _changed.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Called by UIA on one of its own threads. Must stay cheap: any real work here blocks the
    /// provider, and doing UIA calls back into the tree from a handler risks re-entrancy.
    /// </summary>
    public void HandleStructureChangedEvent(UIA.IUIAutomationElement sender, UIA.StructureChangeType changeType, int[] runtimeId)
    {
        _ = Interlocked.Increment(ref _eventCount);

        try
        {
            _ = _changed.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake-up is already pending; this event coalesces into it.
        }
        catch (ObjectDisposedException)
        {
            // Raced with disposal; the waiter is gone.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _ = await _staThread.ExecuteAsync(
                () =>
                {
                    try
                    {
                        UIA3Automation.Instance.Automation.RemoveStructureChangedEventHandler(_root, this);
                    }
                    catch (COMException)
                    {
                        // The provider or its process is already gone, which unregisters us anyway.
                    }

                    return true;
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // The STA thread was torn down first; the handler dies with it.
        }
        catch (InvalidOperationException)
        {
            // The STA thread stopped accepting work during shutdown.
        }

        _changed.Dispose();
    }
}
