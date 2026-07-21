using Xunit.Sdk;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TestRetryTests
{
    private static readonly TimeSpan NoDelay = TimeSpan.Zero;
    private static readonly int[] FirstTwoAttempts = [1, 2];

    [Fact]
    public async Task RunAsync_WhenBodySucceedsFirstTry_RunsExactlyOnce()
    {
        var attempts = 0;

        await TestRetry.RunAsync(
            _ =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            delayBetweenAttempts: NoDelay);

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RunAsync_WhenBodyFailsThenSucceeds_RetriesUntilItPasses()
    {
        var attempts = 0;

        await TestRetry.RunAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient focus loss");
                }

                return Task.CompletedTask;
            },
            delayBetweenAttempts: NoDelay);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RunAsync_PassesOneBasedAttemptNumberToBody()
    {
        var seen = new List<int>();

        await TestRetry.RunAsync(
            attempt =>
            {
                seen.Add(attempt);
                if (seen.Count < 2)
                {
                    throw new InvalidOperationException("retry me");
                }

                return Task.CompletedTask;
            },
            delayBetweenAttempts: NoDelay);

        Assert.Equal(FirstTwoAttempts, seen);
    }

    [Fact]
    public async Task RunAsync_WhenBodyAlwaysFails_ThrowsAfterExhaustingAttempts()
    {
        var attempts = 0;

        var ex = await Assert.ThrowsAsync<XunitException>(() =>
            TestRetry.RunAsync(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("persistent failure");
                },
                maxAttempts: 3,
                delayBetweenAttempts: NoDelay));

        Assert.Equal(3, attempts);
        Assert.Contains("all 3 attempt", ex.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("persistent failure", ex.InnerException!.Message);
    }

    [Fact]
    public async Task RunAsync_WhenBodySkips_PropagatesSkipImmediatelyWithoutRetrying()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<Xunit.SkipException>(() =>
            TestRetry.RunAsync(
                _ =>
                {
                    attempts++;
                    Skip.If(true, "environment cannot run this test");
                    return Task.CompletedTask;
                },
                delayBetweenAttempts: NoDelay));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RunAsync_WhenMaxAttemptsIsLessThanOne_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TestRetry.RunAsync(_ => Task.CompletedTask, maxAttempts: 0));
    }

    [Fact]
    public async Task RunAsync_WhenBodyIsNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TestRetry.RunAsync(null!));
    }
}
