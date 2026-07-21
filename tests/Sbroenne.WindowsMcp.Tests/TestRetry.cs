using Xunit.Sdk;

namespace Sbroenne.WindowsMcp.Tests;

/// <summary>
/// Retries a flaky interactive-desktop test body before failing.
///
/// Physical input (mouse/keyboard) and Save / Save-As tests depend on the target
/// window holding foreground focus. On a shared or busy desktop — such as the
/// self-hosted CI runner — that focus can be lost intermittently (another window
/// steals activation, or the 1&#160;s foreground race in the harness is not won in
/// time), producing a spurious failure even though the automation itself is correct.
/// Retrying the whole body — which re-activates the window on each attempt — mirrors
/// the FlaUI <c>Retry.While*</c> and pywinauto <c>wait('ready')</c> resilience
/// patterns and turns these non-deterministic focus races into deterministic passes.
///
/// A <see cref="Xunit.SkipException"/> raised by Xunit.SkippableFact is never retried: it is
/// rethrown immediately so a genuine "this environment cannot run the test" decision is
/// honored on the first attempt instead of being masked as a failure.
/// </summary>
internal static class TestRetry
{
    /// <summary>Number of attempts made before the body is considered failed.</summary>
    public const int DefaultMaxAttempts = 3;

    /// <summary>Pause between attempts, giving transient focus/UI state time to settle.</summary>
    public static readonly TimeSpan DefaultDelayBetweenAttempts = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Runs <paramref name="body"/>, retrying it up to <paramref name="maxAttempts"/> times if it
    /// throws anything other than a <see cref="Xunit.SkipException"/>. The body receives the 1-based
    /// attempt number so it can, for example, log or vary diagnostics per attempt.
    /// </summary>
    /// <param name="body">The test body to run. Must re-establish any required window focus itself
    /// (typically by calling the harness fixture's BringToFront) so each attempt starts clean.</param>
    /// <param name="maxAttempts">Total attempts before failing. Defaults to <see cref="DefaultMaxAttempts"/>.</param>
    /// <param name="delayBetweenAttempts">Pause between attempts. Defaults to
    /// <see cref="DefaultDelayBetweenAttempts"/>.</param>
    public static async Task RunAsync(
        Func<int, Task> body,
        int? maxAttempts = null,
        TimeSpan? delayBetweenAttempts = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        var attempts = maxAttempts ?? DefaultMaxAttempts;
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        var delay = delayBetweenAttempts ?? DefaultDelayBetweenAttempts;

        var failures = new List<Exception>(attempts);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await body(attempt);
                return;
            }
            catch (Xunit.SkipException)
            {
                // Honor SkippableFact skips immediately; never retry or swallow them.
                throw;
            }
#pragma warning disable CA1031 // Deliberately broad: any test failure is retried once budget remains.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                failures.Add(ex);
                if (attempt >= attempts)
                {
                    break;
                }

                await Task.Delay(delay);
            }
        }

        var lastFailure = failures[^1];
        throw new XunitException(
            $"Flaky interactive-desktop test failed all {attempts} attempt(s). Last failure: {lastFailure.Message}",
            lastFailure);
    }
}
