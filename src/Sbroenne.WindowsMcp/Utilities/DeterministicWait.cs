using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sbroenne.WindowsMcp.Utilities;

/// <summary>
/// Bounded polling for observable conditions.
/// </summary>
public static partial class DeterministicWait
{
    private const uint CowaitDispatchCalls = 0x8;
    private const uint CowaitDispatchWindowMessages = 0x10;
    private const int RpcSCallPending = unchecked((int)0x80010115);

    /// <summary>
    /// Waits asynchronously until an observable condition is true or the timeout expires.
    /// </summary>
    public static async Task<bool> UntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan pollInterval,
        Func<Exception, bool>? transientException = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollInterval, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException &&
                transientException?.Invoke(exception) == true)
            {
                // The observed object is transitioning; retry until the bounded timeout.
            }

            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            await Task.Delay(
                remaining < pollInterval ? remaining : pollInterval,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits asynchronously until an asynchronous observable condition is true or the timeout expires.
    /// </summary>
    public static async Task<bool> UntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan pollInterval,
        Func<Exception, bool>? transientException = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollInterval, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await condition().ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException &&
                transientException?.Invoke(exception) == true)
            {
                // The observed object is transitioning; retry until the bounded timeout.
            }

            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            await Task.Delay(
                remaining < pollInterval ? remaining : pollInterval,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits synchronously until an observable condition is true or the timeout expires.
    /// </summary>
    public static bool Until(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan pollInterval,
        Func<Exception, bool>? transientException = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollInterval, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException &&
                transientException?.Invoke(exception) == true)
            {
                // The observed object is transitioning; retry until the bounded timeout.
            }

            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            var wait = remaining < pollInterval ? remaining : pollInterval;
            var handles = new[] { cancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle() };
            var result = CoWaitForMultipleHandles(
                CowaitDispatchCalls | CowaitDispatchWindowMessages,
                (uint)Math.Ceiling(wait.TotalMilliseconds),
                (uint)handles.Length,
                handles,
                out _);
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (result < 0 && result != RpcSCallPending)
            {
                Marshal.ThrowExceptionForHR(result);
            }
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoWaitForMultipleHandles(
        uint flags,
        uint timeout,
        uint handleCount,
        nint[] handles,
        out uint index);
}
