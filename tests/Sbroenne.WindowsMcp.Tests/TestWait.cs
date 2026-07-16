using System.Diagnostics;

namespace Sbroenne.WindowsMcp.Tests;

/// <summary>
/// Bounded polling helpers for integration tests. Tests wait for observable state rather than
/// sleeping for an assumed amount of time.
/// </summary>
internal static class TestWait
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(25);

    public static async Task<bool> UntilAsync(
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var effectivePollInterval = pollInterval ?? DefaultPollInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectivePollInterval, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (condition())
            {
                return true;
            }

            var remaining = effectiveTimeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            await Task.Delay(
                remaining < effectivePollInterval ? remaining : effectivePollInterval,
                cancellationToken);
        }
    }

    public static bool Until(
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return UntilAsync(condition, timeout, pollInterval, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public static Task<bool> RetryUntilAsync(
        Action attempt,
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(condition);

        return UntilAsync(
            () =>
            {
                attempt();
                return condition();
            },
            timeout,
            pollInterval,
            cancellationToken);
    }

    public static async Task<bool> RetryUntilAsync(
        Func<Task> attempt,
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(condition);

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var effectivePollInterval = pollInterval ?? DefaultPollInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectivePollInterval, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = effectiveTimeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return condition();
            }

            var attemptTask = attempt();
            try
            {
                await attemptTask.WaitAsync(remaining, cancellationToken);
            }
            catch (TimeoutException) when (!attemptTask.IsCompleted)
            {
                return condition();
            }

            if (condition())
            {
                return true;
            }

            remaining = effectiveTimeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return condition();
            }

            await Task.Delay(
                remaining < effectivePollInterval ? remaining : effectivePollInterval,
                cancellationToken);
        }
    }

    public static bool RetryUntil(
        Action attempt,
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return RetryUntilAsync(attempt, condition, timeout, pollInterval, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public static async Task<bool> UntilStableAsync<T>(
        Func<T> valueProvider,
        TimeSpan stableFor,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(stableFor, TimeSpan.Zero);

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var effectivePollInterval = pollInterval ?? DefaultPollInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectivePollInterval, TimeSpan.Zero);

        var timeoutWatch = Stopwatch.StartNew();
        var stableWatch = Stopwatch.StartNew();
        var previous = valueProvider();

        while (timeoutWatch.Elapsed < effectiveTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = effectiveTimeout - timeoutWatch.Elapsed;
            await Task.Delay(
                remaining < effectivePollInterval ? remaining : effectivePollInterval,
                cancellationToken);

            var current = valueProvider();
            if (!EqualityComparer<T>.Default.Equals(previous, current))
            {
                previous = current;
                stableWatch.Restart();
                continue;
            }

            if (stableWatch.Elapsed >= stableFor)
            {
                return true;
            }
        }

        return false;
    }

    public static bool UntilStable<T>(
        Func<T> valueProvider,
        TimeSpan stableFor,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return UntilStableAsync(
                valueProvider,
                stableFor,
                timeout,
                pollInterval,
                cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
}
