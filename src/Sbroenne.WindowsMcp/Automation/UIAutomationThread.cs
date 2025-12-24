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
    private readonly Thread _staThread;
    private readonly BlockingCollection<WorkItem> _workQueue;
    private readonly CancellationTokenSource _shutdownCts;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAutomationThread"/> class.
    /// </summary>
    public UIAutomationThread()
    {
        _workQueue = new BlockingCollection<WorkItem>();
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
        var workItem = new WorkItem(() =>
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
        });

        if (!_workQueue.TryAdd(workItem))
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
                workItem.Execute();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _workQueue.CompleteAdding();
        _shutdownCts.Cancel();

        // Give the thread a chance to finish current work
        if (!_staThread.Join(TimeSpan.FromSeconds(5)))
        {
            // Thread didn't stop in time - it will be killed when the process exits
        }

        _shutdownCts.Dispose();
        _workQueue.Dispose();
    }

    private sealed class WorkItem(Action work)
    {
        public void Execute() => work();
    }
}
