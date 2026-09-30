using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class FindDeadlineTests
{
    private static readonly int[] ExpectedDelays = [50, 75];

    [Fact]
    public async Task ProbeCrossingDeadline_DoesNotStartAnotherProbe()
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

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Equal(1, probes);
    }

    [Fact]
    public async Task DelayIsClamped_AndNoProbeStartsAtDeadline()
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
        Assert.Equal(new long[] { 0, 50 }, probeTimes);
        Assert.Equal(ExpectedDelays, delays);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AlreadyExpiredDeadline_DoesNotStartProviderCall(bool disappear)
    {
        long elapsed = 2000;
        var probes = 0;
        Task<UIAutomationResult> Probe()
        {
            probes++;
            elapsed += 500;
            return Task.FromResult(Missing());
        }
        Task Wait(int _, CancellationToken token) =>
            throw new InvalidOperationException("Must not delay after deadline.");
        var result = disappear
            ? await UIAutomationService.WaitForDisappearResultAsync(
                new ElementQuery(), 2000, Probe, Wait, () => elapsed, CancellationToken.None)
            : await UIAutomationService.WaitForFindResultAsync(
                new ElementQuery(), 2000, Probe, Wait, () => elapsed, CancellationToken.None);

        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Equal(0, probes);
        Assert.Contains("before it could start", result.ErrorMessage, StringComparison.Ordinal);
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
            () => 0,
            CancellationToken.None);

        Assert.Equal(errorType, result.ErrorType);
        Assert.Equal(1, probes);
    }

    private static UIAutomationResult Missing() =>
        UIAutomationResult.CreateFailure("find", UIAutomationErrorType.ElementNotFound, "Not present.");

    [Fact]
    public async Task Disappearance_ObservesChangeBeforeDeadlineWithoutEvent()
    {
        long elapsed = 0;
        var probes = 0;
        var result = await UIAutomationService.WaitForDisappearResultAsync(
            new ElementQuery(), 125,
            () =>
            {
                probes++;
                return Task.FromResult(elapsed < 50
                    ? new UIAutomationResult
                    {
                        Success = true,
                        Action = "find",
                        Items = [new() { Id = "observed", Type = "Button", Click = [0, 0, 0], Enabled = true }]
                    }
                    : Missing());
            },
            (delay, _) =>
            {
                elapsed += delay;
                return Task.CompletedTask;
            },
            () => elapsed,
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("wait_for_disappear", result.Action);
        Assert.Equal(50, elapsed);
        Assert.Equal(2, probes);
    }

    [Fact]
    public async Task Disappearance_IncompleteSearchDoesNotBecomeSuccessAtDeadline()
    {
        var result = await UIAutomationService.WaitForDisappearResultAsync(
            new ElementQuery(), 125,
            () => Task.FromResult(UIAutomationResult.CreateFailure(
                "find", UIAutomationErrorType.SearchIncomplete, "Unknown remaining elements.")),
            (_, _) => throw new InvalidOperationException("Incomplete search must not be retried."),
            () => 0,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.SearchIncomplete, result.ErrorType);
    }

    [Fact]
    public async Task Timeout_PreservesLastSearchDiagnostics()
    {
        long elapsed = 0;
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery(), 125,
            () => Task.FromResult(UIAutomationResult.CreateFailure(
                "find", UIAutomationErrorType.ElementNotFound, "Not present.",
                new UIAutomationDiagnostics { DurationMs = 10, ElementsScanned = 42, WindowTitle = "Installer" })),
            (delay, _) =>
            {
                elapsed += delay;
                return Task.CompletedTask;
            },
            () => elapsed,
            CancellationToken.None);

        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Equal(125, result.Diagnostics?.DurationMs);
        Assert.Equal(42, result.Diagnostics?.ElementsScanned);
        Assert.Equal("Installer", result.Diagnostics?.WindowTitle);
    }

    [Fact]
    public async Task Timeout_DistinguishesMissingDialogFromMissingElement()
    {
        long elapsed = 0;
        var result = await UIAutomationService.WaitForFindResultAsync(
            new ElementQuery { Scope = "active_dialog" }, 125,
            () => Task.FromResult(UIAutomationResult.CreateFailure(
                "find", UIAutomationErrorType.WindowNotFound,
                "No visible enabled dialog is currently owned by the requested window.")),
            (delay, _) =>
            {
                elapsed += delay;
                return Task.CompletedTask;
            },
            () => elapsed,
            CancellationToken.None);

        Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
        Assert.Contains("No visible enabled dialog", result.ErrorMessage, StringComparison.Ordinal);
    }
}
