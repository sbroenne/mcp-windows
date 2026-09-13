using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class FindDeadlineTests
{
    private static readonly int[] ExpectedDelays = [50, 75];

    [Fact]
    public async Task ProbeCrossingDeadline_ReceivesOneFreshFinalProbe()
    {
        long elapsed = 0;
        var probes = 0;
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery(), 2000,
            () =>
            {
                probes++;
                elapsed = 2001;
                return Task.FromResult(probes == 1
                    ? Missing()
                    : UIAutomationResult.CreateSuccess("find"));
            },
            (_, _) => throw new InvalidOperationException("An expired deadline must not sleep."),
            () => elapsed,
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, probes);
    }

    [Fact]
    public async Task DelayIsClamped_AndFinalProbeStartsAtDeadline()
    {
        long elapsed = 0;
        var probeTimes = new List<long>();
        var delays = new List<int>();
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery(), 125,
            () =>
            {
                probeTimes.Add(elapsed);
                return Task.FromResult(Missing());
            },
            (delay, _) =>
            {
                delays.Add(delay);
                elapsed += delay;
                return Task.CompletedTask;
            },
            () => elapsed,
            CancellationToken.None);

        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Equal(new long[] { 0, 50, 125 }, probeTimes);
        Assert.Equal(ExpectedDelays, delays);
    }

    [Fact]
    public async Task SlowFinalProbe_DoesNotStartAdditionalProbes()
    {
        long elapsed = 2000;
        var probes = 0;
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery(), 2000,
            () =>
            {
                probes++;
                elapsed += 500;
                return Task.FromResult(Missing());
            },
            (_, _) => throw new InvalidOperationException("Must not delay after final probe."),
            () => elapsed,
            CancellationToken.None);

        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Equal(1, probes);
    }

    [Fact]
    public async Task CancellationAfterProbe_PreventsFinalProviderCall()
    {
        using var cancellation = new CancellationTokenSource();
        long elapsed = 0;
        var probes = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            UIAutomationService.WaitForFindResultAsync(
                new ElementQuery(), 2000,
                () =>
                {
                    probes++;
                    elapsed = 2001;
                    cancellation.Cancel();
                    return Task.FromResult(Missing());
                },
                (_, _) => throw new InvalidOperationException("Cancellation must not wait."),
                () => elapsed,
                cancellation.Token));

        Assert.Equal(1, probes);
    }

    [Theory]
    [InlineData(UIAutomationErrorType.SearchIncomplete)]
    [InlineData(UIAutomationErrorType.MultipleMatches)]
    public async Task NonRetryableResult_IsNotHiddenByFinalProbe(string errorType)
    {
        var probes = 0;
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery(), 2000,
            () =>
            {
                probes++;
                return Task.FromResult(UIAutomationResult.CreateFailure("find", errorType, "Stop."));
            },
            (_, _) => throw new InvalidOperationException("Must not retry this result."),
            () => 2000,
            CancellationToken.None);

        Assert.Equal(errorType, result.ErrorType);
        Assert.Equal(1, probes);
    }

    private static UIAutomationResult Missing() =>
        UIAutomationResult.CreateFailure("find", UIAutomationErrorType.ElementNotFound, "Not present.");
}
