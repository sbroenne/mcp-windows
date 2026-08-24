using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Provides a dedicated STA thread for UI Automation operations.
/// UI Automation requires operations to run on an STA thread for COM interop.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIAutomationThread : IDisposable
{
    private const int DefaultQueueCapacity = 256;
    private readonly Thread _staThread;
    private readonly BlockingCollection<WorkItem> _workQueue;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly Lock _cleanupLock = new();
    private volatile bool _disposed;
    private bool _resourcesDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAutomationThread"/> class.
    /// </summary>
    public UIAutomationThread()
        : this(DefaultQueueCapacity)
    {
    }

    internal UIAutomationThread(int boundedCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(boundedCapacity, 1);

        _workQueue = new BlockingCollection<WorkItem>(
            new ConcurrentQueue<WorkItem>(),
            boundedCapacity);
        _shutdownCts = new CancellationTokenSource();

        _staThread = new Thread(ProcessWorkItems)
        {
            Name = "UIAutomation-STA",
            IsBackground = true
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
    }

    /// <summary>
    /// Executes a function on the STA thread.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function.</returns>
    public Task<T> ExecuteAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workItem = new WorkItem(
            () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = func();
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            },
            () => tcs.TrySetException(new ObjectDisposedException(nameof(UIAutomationThread))));

        try
        {
            if (!_workQueue.TryAdd(workItem))
            {
                tcs.TrySetException(new InvalidOperationException(
                    "The UI Automation queue is full. Retry after the current operation completes."));
            }
        }
        catch (InvalidOperationException)
        {
            tcs.TrySetException(new ObjectDisposedException(nameof(UIAutomationThread)));
        }

        return tcs.Task;
    }

    /// <summary>
    /// Executes an action on the STA thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public Task ExecuteAsync(Action action, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync<object?>(() =>
        {
            action();
            return null;
        }, cancellationToken);
    }

    private void ProcessWorkItems()
    {
        try
        {
            foreach (var workItem in _workQueue.GetConsumingEnumerable(_shutdownCts.Token))
            {
                if (_disposed)
                {
                    workItem.Cancel();
                    continue;
                }

                workItem.Execute();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested.
        }
        finally
        {
            CancelPendingWork();
            CleanupResources();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_cleanupLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _workQueue.CompleteAdding();
            _shutdownCts.Cancel();
            CancelPendingWork();
        }

        // Give the thread a chance to finish current work.
        _ = _staThread.Join(TimeSpan.FromSeconds(5));
    }

    private void CancelPendingWork()
    {
        while (_workQueue.TryTake(out var pending))
        {
            pending.Cancel();
        }
    }

    private void CleanupResources()
    {
        lock (_cleanupLock)
        {
            if (_resourcesDisposed)
            {
                return;
            }

            CancelPendingWork();
            _shutdownCts.Dispose();
            _workQueue.Dispose();
            _resourcesDisposed = true;
        }
    }

    private sealed class WorkItem(Action execute, Action cancel)
    {
        public void Execute() => execute();

        public void Cancel() => cancel();
    }
}
