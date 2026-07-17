using System.Diagnostics;
using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class DeterministicWaitTests
{
    [Fact]
    public async Task UntilAsync_StopsWhenConditionBecomesTrue()
    {
        var checks = 0;

        var result = await DeterministicWait.UntilAsync(
            () => Interlocked.Increment(ref checks) >= 3,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(3, checks);
    }

    [Fact]
    public async Task UntilAsync_DoesNotDelayPastTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await DeterministicWait.UntilAsync(
            () => false,
            TimeSpan.FromMilliseconds(75),
            TimeSpan.FromMilliseconds(200));

        Assert.False(result);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 50, 150);
    }

    [Fact]
    public async Task UntilAsync_AwaitsAsynchronousCondition()
    {
        var checks = 0;

        var result = await DeterministicWait.UntilAsync(
            async () =>
            {
                await Task.Yield();
                return Interlocked.Increment(ref checks) >= 2;
            },
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(2, checks);
    }

    [Fact]
    public async Task UntilAsync_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DeterministicWait.UntilAsync(
                () => false,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(10),
                cancellationToken: cancellationSource.Token));
    }

    [Fact]
    public void Until_SupportsObservableStaleTransitions()
    {
        var checks = 0;

        var result = DeterministicWait.Until(
            () =>
            {
                checks++;
                if (checks < 3)
                {
                    throw new InvalidOperationException("Element is still alive");
                }

                return true;
            },
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10),
            transientException: static exception => exception is InvalidOperationException);

        Assert.True(result);
        Assert.Equal(3, checks);
    }

    [Fact]
    public void Until_EvaluatesConditionOnCallingThread()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var observedThreads = new List<int>();

        var result = DeterministicWait.Until(
            () =>
            {
                observedThreads.Add(Environment.CurrentManagedThreadId);
                return observedThreads.Count >= 2;
            },
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.All(observedThreads, thread => Assert.Equal(callingThread, thread));
    }
}
