using System.Diagnostics;
using System.Globalization;
using System.Text;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.EventWaitSpike;

/// <summary>
/// Measurement helpers for the event-driven waiting spike (issue #189).
/// </summary>
/// <remarks>
/// The spike's exit criterion is a measurement-backed recommendation, so these helpers exist to
/// produce numbers rather than to assert behaviour. Results are written both to xUnit output and
/// to a report file, because xUnit output for a passing test is not surfaced by the default CI
/// logger and the whole point of the exercise is the numbers from a passing run.
/// </remarks>
internal static class EventWaitBenchmark
{
    /// <summary>
    /// How long into the wait the target element is made to appear. Chosen to land in the
    /// saturated part of the polling backoff (50, 100, 200, 400, then 500ms steps), where the
    /// penalty for polling is largest and most consistent. Appearing early would flatter polling
    /// by hiding it inside a short sleep.
    /// </summary>
    public const int AppearAfterMs = 2000;

    public static string ReportDirectory =>
        Path.Combine(Path.GetTempPath(), "mcp-windows-spike-189");

    /// <summary>
    /// Measures how long a wait takes to notice a condition that has already become true.
    /// </summary>
    /// <remarks>
    /// <paramref name="trigger"/> must not return until the change is committed, so the recorded
    /// timestamp is a lower bound on when the element became observable. Overstating that instant
    /// would understate the measured latency for both modes equally, but understating it would not.
    /// </remarks>
    public static async Task<WaitLatencySample> MeasureLatencyAsync(
        Func<ElementQuery, int, Task<UIAutomationResult>> wait,
        ElementQuery query,
        Func<Task> trigger,
        bool eventAssisted,
        int timeoutMs)
    {
        UIAutomationService.EventAssistedWaitEnabled = eventAssisted;

        var clock = Stopwatch.StartNew();
        long triggeredTicks = 0;

        var waitTask = wait(query, timeoutMs);

        var triggerTask = Task.Run(async () =>
        {
            await Task.Delay(AppearAfterMs);
            await trigger();
            Volatile.Write(ref triggeredTicks, clock.ElapsedTicks);
        });

        var result = await waitTask;
        var observedTicks = clock.ElapsedTicks;

        await triggerTask;

        var triggered = Volatile.Read(ref triggeredTicks);
        var latencyMs = (observedTicks - triggered) * 1000.0 / Stopwatch.Frequency;

        return new WaitLatencySample(
            result.Success,
            latencyMs,
            UIAutomationService.LastWaitEventCount);
    }

    /// <summary>
    /// Measures the cost of a wait that never succeeds. This is the event-storm probe: on a
    /// churning tree, an event subscription could make the timeout path more expensive than plain
    /// polling, which is the main documented risk of the whole idea.
    /// </summary>
    public static async Task<WaitTimeoutSample> MeasureTimeoutCostAsync(
        Func<ElementQuery, int, Task<UIAutomationResult>> wait,
        ElementQuery query,
        bool eventAssisted,
        int timeoutMs)
    {
        UIAutomationService.EventAssistedWaitEnabled = eventAssisted;

        var process = Process.GetCurrentProcess();
        var cpuBefore = process.TotalProcessorTime;
        var clock = Stopwatch.StartNew();

        var result = await wait(query, timeoutMs);

        clock.Stop();
        process.Refresh();
        var cpuMs = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;

        return new WaitTimeoutSample(
            result.Success,
            clock.Elapsed.TotalMilliseconds,
            cpuMs,
            UIAutomationService.LastWaitEventCount);
    }

    public static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(static value => value).ToArray();
        if (ordered.Length == 0)
        {
            return double.NaN;
        }

        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2.0;
    }

    /// <summary>
    /// Writes a report file so the numbers survive a passing test run in CI.
    /// </summary>
    public static void WriteReport(string harness, string report)
    {
        try
        {
            _ = Directory.CreateDirectory(ReportDirectory);
            var path = Path.Combine(ReportDirectory, $"{harness}.txt");
            File.WriteAllText(path, report, Encoding.UTF8);
        }
        catch (IOException)
        {
            // The report is a convenience; never fail a measurement because it could not be saved.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string FormatLatencyReport(
        string harness,
        IReadOnlyList<WaitLatencySample> polling,
        IReadOnlyList<WaitLatencySample> assisted)
    {
        var pollingMedian = Median(polling.Select(static sample => sample.LatencyMs));
        var assistedMedian = Median(assisted.Select(static sample => sample.LatencyMs));
        var improvement = pollingMedian > 0
            ? (1.0 - (assistedMedian / pollingMedian)) * 100.0
            : double.NaN;

        var pollingRaw = string.Join(", ", polling.Select(static s => s.LatencyMs.ToString("F1", CultureInfo.InvariantCulture)));
        var assistedRaw = string.Join(", ", assisted.Select(static s => s.LatencyMs.ToString("F1", CultureInfo.InvariantCulture)));
        var eventCounts = string.Join(", ", assisted.Select(static s => s.EventCount.ToString(CultureInfo.InvariantCulture)));

        var builder = new StringBuilder();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"=== #189 event-assisted wait: {harness} ===");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"samples per mode        : {polling.Count}");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"element appears after   : {AppearAfterMs}ms");
        _ = builder.AppendLine();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"polling  median latency : {pollingMedian:F1}ms  raw: [{pollingRaw}]");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"assisted median latency : {assistedMedian:F1}ms  raw: [{assistedRaw}]");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"improvement             : {improvement:F1}%");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"events per assisted wait: [{eventCounts}]");
        return builder.ToString();
    }

    public static string FormatTimeoutReport(
        string harness,
        WaitTimeoutSample polling,
        WaitTimeoutSample assisted,
        int timeoutMs)
    {
        var builder = new StringBuilder();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"=== #189 event-assisted wait, timeout path: {harness} ===");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"requested timeout       : {timeoutMs}ms");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"polling  elapsed/cpu    : {polling.ElapsedMs:F0}ms / {polling.CpuMs:F0}ms cpu");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"assisted elapsed/cpu    : {assisted.ElapsedMs:F0}ms / {assisted.CpuMs:F0}ms cpu");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"events during assisted  : {assisted.EventCount}");
        return builder.ToString();
    }
}

internal readonly record struct WaitLatencySample(bool Found, double LatencyMs, int EventCount);

internal readonly record struct WaitTimeoutSample(bool Found, double ElapsedMs, double CpuMs, int EventCount);
