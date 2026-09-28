using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

[SupportedOSPlatform("windows")]
public sealed class UIAutomationThreadTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ExecuteAsync_UsesDedicatedMtaForAutomationAndEventSubscriptions()
    {
        using var executor = new UIAutomationThread();
        var first = await executor.ExecuteAsync(() =>
            (Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState()));
        var second = await executor.ExecuteAsync(() =>
            (Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState()));
        Assert.Equal(ApartmentState.MTA, first.Item2);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Dispose_RunsRegisteredCleanupOnTheOwningThreadOnce()
    {
        var executor = new UIAutomationThread();
        var cleanupCount = 0;
        var cleanupThread = 0;
        var removedCleanupCount = 0;
        Action removed = () => removedCleanupCount++;
        var owner = await executor.ExecuteAsync(() =>
        {
            executor.RegisterShutdownCleanup(() =>
            {
                cleanupCount++;
                cleanupThread = Environment.CurrentManagedThreadId;
            });
            executor.RegisterShutdownCleanup(removed);
            executor.UnregisterShutdownCleanup(removed);
            return Environment.CurrentManagedThreadId;
        });
        executor.Dispose();
        executor.Dispose();
        Assert.Equal(1, cleanupCount);
        Assert.Equal(owner, cleanupThread);
        Assert.Equal(0, removedCleanupCount);
    }

    [Fact]
    public async Task Dispose_RunsCleanupAfterWorkFailsOrIsCancelled()
    {
        using var executor = new UIAutomationThread();
        var cleanupCount = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(() =>
        {
            executor.RegisterShutdownCleanup(() => cleanupCount++);
            throw new InvalidOperationException("Deliberate work failure.");
        }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(() =>
        {
            executor.RegisterShutdownCleanup(() => cleanupCount++);
            throw new OperationCanceledException();
        }));
        executor.Dispose();
        Assert.Equal(2, cleanupCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenQueueIsFull_FailsWithoutBlockingCaller()
    {
        using var executor = new UIAutomationThread(boundedCapacity: 1);
        using var releaseRunningWork = new ManualResetEventSlim();
        var runningWorkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var running = executor.ExecuteAsync(() =>
        {
            runningWorkStarted.SetResult();
            if (!releaseRunningWork.Wait(TestTimeout))
            {
                throw new TimeoutException("Timed out waiting to release the running work item.");
            }
        });
        await runningWorkStarted.Task.WaitAsync(TestTimeout);

        var queued = executor.ExecuteAsync(() => { });
        var rejected = executor.ExecuteAsync(() => { });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => rejected);
        Assert.Contains("queue is full", exception.Message, StringComparison.OrdinalIgnoreCase);

        releaseRunningWork.Set();
        await Task.WhenAll(running, queued);
    }

    [Fact]
    public async Task Dispose_CancelsQueuedWork()
    {
        var executor = new UIAutomationThread(boundedCapacity: 1);
        using var releaseRunningWork = new ManualResetEventSlim();
        var runningWorkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var running = executor.ExecuteAsync(() =>
        {
            runningWorkStarted.SetResult();
            if (!releaseRunningWork.Wait(TestTimeout))
            {
                throw new TimeoutException("Timed out waiting to release the running work item.");
            }
        });
        await runningWorkStarted.Task.WaitAsync(TestTimeout);

        var queued = executor.ExecuteAsync(() => { });
        var disposing = Task.Run(executor.Dispose);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => queued);
        releaseRunningWork.Set();
        await Task.WhenAll(running, disposing);
    }
}
