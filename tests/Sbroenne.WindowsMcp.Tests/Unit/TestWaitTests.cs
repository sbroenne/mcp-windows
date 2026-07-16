using System.Diagnostics;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TestWaitTests
{
    [Fact]
    public async Task UntilAsync_WhenConditionBecomesTrue_ReturnsWithoutWaitingForTimeout()
    {
        var checks = 0;
        var stopwatch = Stopwatch.StartNew();

        var result = await TestWait.UntilAsync(
            () => Interlocked.Increment(ref checks) >= 3,
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.True(checks >= 3);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UntilAsync_WhenConditionNeverBecomesTrue_ReturnsFalseAtTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await TestWait.UntilAsync(
            () => false,
            timeout: TimeSpan.FromMilliseconds(80),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.False(result);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(60));
    }

    [Fact]
    public async Task RetryUntilAsync_RepeatsActionUntilConditionIsTrue()
    {
        var attempts = 0;

        var result = await TestWait.RetryUntilAsync(
            attempt: () => Interlocked.Increment(ref attempts),
            condition: () => Volatile.Read(ref attempts) >= 3,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RetryUntilAsync_WithAsyncAction_AwaitsEachAttempt()
    {
        var attempts = 0;

        var result = await TestWait.RetryUntilAsync(
            attempt: async () =>
            {
                await Task.Yield();
                Interlocked.Increment(ref attempts);
            },
            condition: () => Volatile.Read(ref attempts) >= 3,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RetryUntilAsync_WhenAttemptDoesNotComplete_ReturnsAtTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await TestWait.RetryUntilAsync(
            attempt: () => Task.Delay(150),
            condition: () => false,
            timeout: TimeSpan.FromMilliseconds(80),
            pollInterval: TimeSpan.FromMilliseconds(200));

        Assert.False(result);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(125));
    }

    [Fact]
    public async Task RetryUntilAsync_WhenAttemptThrowsTimeoutException_PropagatesException()
    {
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TestWait.RetryUntilAsync(
                attempt: () => Task.FromException(new TimeoutException("Attempt failed")),
                condition: () => false,
                timeout: TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Until_WhenConditionBecomesTrue_ReturnsTrue()
    {
        var checks = 0;

        var result = TestWait.Until(
            () => Interlocked.Increment(ref checks) >= 2,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(2, checks);
    }

    [Fact]
    public void RetryUntil_RepeatsActionUntilConditionIsTrue()
    {
        var attempts = 0;

        var result = TestWait.RetryUntil(
            attempt: () => Interlocked.Increment(ref attempts),
            condition: () => Volatile.Read(ref attempts) >= 2,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task UntilAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TestWait.UntilAsync(
                () => false,
                timeout: TimeSpan.FromSeconds(1),
                cancellationToken: cancellationSource.Token));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UntilAsync_WithNonPositiveTimeout_Throws(int timeoutMilliseconds)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TestWait.UntilAsync(
                () => true,
                timeout: TimeSpan.FromMilliseconds(timeoutMilliseconds)));
    }

    [Fact]
    public async Task UntilAsync_WithNonPositivePollInterval_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TestWait.UntilAsync(
                () => true,
                timeout: TimeSpan.FromSeconds(1),
                pollInterval: TimeSpan.Zero));
    }

    [Fact]
    public async Task UntilStableAsync_WhenValueStopsChanging_ReturnsTrue()
    {
        var value = 0;
        _ = Task.Run(async () =>
        {
            await Task.Delay(20);
            Interlocked.Increment(ref value);
            await Task.Delay(20);
            Interlocked.Increment(ref value);
        });

        var result = await TestWait.UntilStableAsync(
            () => Volatile.Read(ref value),
            stableFor: TimeSpan.FromMilliseconds(60),
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(result);
        Assert.Equal(2, value);
    }

    [Fact]
    public async Task UntilStableAsync_WhenValueKeepsChanging_ReturnsFalse()
    {
        var value = 0;
        using var timer = new System.Threading.Timer(
            _ => Interlocked.Increment(ref value), null, 0, 10);

        var result = await TestWait.UntilStableAsync(
            () => Volatile.Read(ref value),
            stableFor: TimeSpan.FromMilliseconds(50),
            timeout: TimeSpan.FromMilliseconds(120),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.False(result);
    }

    [Fact]
    public async Task UntilStableAsync_WhenPollIntervalExceedsTimeout_ReturnsAtTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await TestWait.UntilStableAsync(
            () => 1,
            stableFor: TimeSpan.FromSeconds(1),
            timeout: TimeSpan.FromMilliseconds(80),
            pollInterval: TimeSpan.FromMilliseconds(200));

        Assert.False(result);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(125));
    }
}
