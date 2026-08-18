using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

[SupportedOSPlatform("windows")]
public sealed class UIAutomationThreadTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

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
