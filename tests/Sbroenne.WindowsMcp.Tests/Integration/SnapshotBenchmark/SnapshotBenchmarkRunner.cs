using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SharpToken;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

internal enum SnapshotBenchmarkArm
{
    ActionOnly,
    Full,
    Auto
}

internal sealed record SnapshotBenchmarkScenario(
    string Name,
    string WindowHandle,
    UIAutomationService AutomationService,
    IReadOnlyList<Func<CancellationToken, Task>> Actions,
    string Environment,
    Func<ValueTask>? CleanupAsync = null,
    int MaxDepth = 5,
    string? ControlTypeFilter = null,
    Func<string>? CurrentWindowHandle = null) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => CleanupAsync?.Invoke() ?? ValueTask.CompletedTask;
}

internal sealed record SnapshotBenchmarkRun(
    int Sample,
    SnapshotBenchmarkArm Arm,
    double ActionMs,
    double SnapshotMs,
    int Bytes,
    int Tokens,
    int ComparableFullBytes,
    int ComparableFullTokens,
    int FullResponses,
    int DiffResponses);

internal sealed record SnapshotBenchmarkResult(
    string Scenario,
    string Environment,
    IReadOnlyList<SnapshotBenchmarkRun> Runs)
{
    public IReadOnlyList<SnapshotBenchmarkRun> For(SnapshotBenchmarkArm arm) =>
        Runs.Where(run => run.Arm == arm).ToArray();
}

internal static class SnapshotBenchmarkRunner
{
    public const int DefaultSamples = 5;

    private static readonly GptEncoding TokenEncoding = GptEncoding.GetEncoding("cl100k_base");

    public static string ReportDirectory =>
        Environment.GetEnvironmentVariable("MCP_SNAPSHOT_BENCHMARK_OUTPUT")
        ?? Path.Combine(Path.GetTempPath(), "mcp-windows-snapshot-benchmark");

    public static async Task<SnapshotBenchmarkResult> RunAsync(
        string scenarioName,
        Func<SnapshotBenchmarkArm, int, Task<SnapshotBenchmarkScenario>> createScenario,
        int samples = DefaultSamples,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
        ArgumentNullException.ThrowIfNull(createScenario);
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 1);

        var runs = new List<SnapshotBenchmarkRun>(samples * 3);
        string? environment = null;
        var arms = Enum.GetValues<SnapshotBenchmarkArm>();

        for (var sample = 1; sample <= samples; sample++)
        {
            for (var armIndex = 0; armIndex < arms.Length; armIndex++)
            {
                var arm = arms[(sample - 1 + armIndex) % arms.Length];
                await using var scenario = await createScenario(arm, sample).ConfigureAwait(false);
                environment ??= scenario.Environment;

                var state = new SnapshotStateService();
                var baselineHandle = GetCurrentWindowHandle(scenario);
                var key = SnapshotRequestKey.Create(
                baselineHandle,
                parentElementId: null,
                scenario.MaxDepth,
                scenario.ControlTypeFilter);

                if (arm == SnapshotBenchmarkArm.Auto)
                {
                    var baseline = await state.CaptureAsync(
                        key,
                        SnapshotMode.Reset,
                        token => scenario.AutomationService.GetTreeAsync(
                            baselineHandle, null, scenario.MaxDepth, scenario.ControlTypeFilter, token),
                        cancellationToken).ConfigureAwait(false);
                    Assert.True(baseline.Success, $"Could not establish auto baseline: {baseline.ErrorMessage}");
                    Assert.Equal("full", baseline.Kind);
                }

                var actionMs = 0.0;
                var snapshotMs = 0.0;
                var bytes = 0;
                var tokens = 0;
                var comparableFullBytes = 0;
                var comparableFullTokens = 0;
                var fullResponses = 0;
                var diffResponses = 0;

                for (var actionIndex = 0; actionIndex < scenario.Actions.Count; actionIndex++)
                {
                    var action = scenario.Actions[actionIndex];
                    var actionClock = Stopwatch.StartNew();
                    await action(cancellationToken).ConfigureAwait(false);
                    actionClock.Stop();
                    actionMs += actionClock.Elapsed.TotalMilliseconds;

                    if (arm == SnapshotBenchmarkArm.ActionOnly)
                    {
                        continue;
                    }

                    var currentHandle = GetCurrentWindowHandle(scenario);
                    key = SnapshotRequestKey.Create(
                        currentHandle,
                        parentElementId: null,
                        scenario.MaxDepth,
                        scenario.ControlTypeFilter);
                    var snapshotClock = Stopwatch.StartNew();
                    UIAutomationResult? capturedFull = null;
                    var snapshot = await state.CaptureAsync(
                        key,
                        arm == SnapshotBenchmarkArm.Full ? SnapshotMode.Full : SnapshotMode.Auto,
                        async token =>
                        {
                            capturedFull = await scenario.AutomationService.GetTreeAsync(
                                currentHandle,
                                null,
                                scenario.MaxDepth,
                                scenario.ControlTypeFilter,
                                token).ConfigureAwait(false);
                            return capturedFull;
                        },
                        cancellationToken).ConfigureAwait(false);
                    snapshotClock.Stop();

                    Assert.True(
                        snapshot.Success,
                        $"{scenario.Name} {arm} sample {sample} action {actionIndex + 1} snapshot failed: " +
                        snapshot.ErrorMessage);
                    Assert.DoesNotContain(
                        snapshot.Diagnostics?.Warnings ?? [],
                        warning => warning.Contains("truncated", StringComparison.OrdinalIgnoreCase));
                    snapshotMs += snapshotClock.Elapsed.TotalMilliseconds;

                    var json = JsonSerializer.Serialize(snapshot, WindowsToolsBase.JsonOptions);
                    bytes += System.Text.Encoding.UTF8.GetByteCount(json);
                    tokens += TokenEncoding.Encode(json).Count;
                    var comparableJson = JsonSerializer.Serialize(capturedFull, WindowsToolsBase.JsonOptions);
                    comparableFullBytes += System.Text.Encoding.UTF8.GetByteCount(comparableJson);
                    comparableFullTokens += TokenEncoding.Encode(comparableJson).Count;
                    fullResponses += string.Equals(snapshot.Kind, "full", StringComparison.Ordinal) ? 1 : 0;
                    diffResponses += string.Equals(snapshot.Kind, "diff", StringComparison.Ordinal) ? 1 : 0;
                }

                runs.Add(new SnapshotBenchmarkRun(
                    sample,
                    arm,
                    actionMs,
                    snapshotMs,
                    bytes,
                    tokens,
                    comparableFullBytes,
                    comparableFullTokens,
                    fullResponses,
                    diffResponses));
            }
        }

        var result = new SnapshotBenchmarkResult(
            scenarioName,
            environment ?? "Unknown",
            runs);
        Validate(result);
        WriteReport(result);
        return result;
    }

    public static string FormatReport(SnapshotBenchmarkResult result)
    {
        var builder = new StringBuilder();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"# Incremental snapshot benchmark: {result.Scenario}");
        _ = builder.AppendLine();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"- Environment: {result.Environment}");
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"- Samples per arm: {result.Runs.Max(run => run.Sample)}");
        _ = builder.AppendLine("- Tokenizer: SharpToken `cl100k_base` approximation");
        _ = builder.AppendLine();
        _ = builder.AppendLine("| Arm | Median action ms | Median snapshot ms | Median bytes | Median tokens | Full/diff responses |");
        _ = builder.AppendLine("|---|---:|---:|---:|---:|---:|");

        foreach (var arm in Enum.GetValues<SnapshotBenchmarkArm>())
        {
            var runs = result.For(arm);
            _ = builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {ArmName(arm)} | {Median(runs.Select(run => run.ActionMs)):F1} | " +
                $"{Median(runs.Select(run => run.SnapshotMs)):F1} | " +
                $"{Median(runs.Select(run => (double)run.Bytes)):F0} | " +
                $"{Median(runs.Select(run => (double)run.Tokens)):F0} | " +
                $"{runs.Sum(run => run.FullResponses)}/{runs.Sum(run => run.DiffResponses)} |");
        }

        var autoRuns = result.For(SnapshotBenchmarkArm.Auto);
        var byteSavings = Median(autoRuns.Select(run =>
            Reduction(run.ComparableFullBytes, run.Bytes)));
        var tokenSavings = Median(autoRuns.Select(run =>
            Reduction(run.ComparableFullTokens, run.Tokens)));

        _ = builder.AppendLine();
        _ = builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**Median paired auto savings versus the same captures returned in full:** " +
            $"{byteSavings:F1}% bytes, {tokenSavings:F1}% tokens.");
        _ = builder.AppendLine();
        _ = builder.AppendLine("## Raw samples");
        _ = builder.AppendLine();
        _ = builder.AppendLine("| Sample | Arm | Action ms | Snapshot ms | Bytes | Tokens | Same-capture full bytes | Same-capture full tokens | Full | Diff |");
        _ = builder.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var run in result.Runs)
        {
            _ = builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {run.Sample} | {ArmName(run.Arm)} | {run.ActionMs:F1} | {run.SnapshotMs:F1} | " +
                $"{run.Bytes} | {run.Tokens} | {run.ComparableFullBytes} | {run.ComparableFullTokens} | " +
                $"{run.FullResponses} | {run.DiffResponses} |");
        }

        return builder.ToString();
    }

    private static void Validate(SnapshotBenchmarkResult result)
    {
        var actionOnly = result.For(SnapshotBenchmarkArm.ActionOnly);
        var full = result.For(SnapshotBenchmarkArm.Full);
        var auto = result.For(SnapshotBenchmarkArm.Auto);

        Assert.Equal(DefaultSamples, actionOnly.Count);
        Assert.Equal(DefaultSamples, full.Count);
        Assert.Equal(DefaultSamples, auto.Count);
        Assert.All(actionOnly, run =>
        {
            Assert.Equal(0, run.Bytes);
            Assert.Equal(0, run.Tokens);
            Assert.Equal(0, run.SnapshotMs);
            Assert.Equal(0, run.ComparableFullBytes);
            Assert.Equal(0, run.ComparableFullTokens);
        });
        Assert.All(full, run =>
        {
            Assert.True(run.Bytes > 0);
            Assert.True(run.Tokens > 0);
            Assert.True(run.ComparableFullBytes >= run.Bytes);
            Assert.True(run.ComparableFullTokens >= run.Tokens);
            Assert.True(run.FullResponses > 0);
        });
        Assert.All(auto, run =>
        {
            Assert.True(run.Bytes > 0);
            Assert.True(run.Tokens > 0);
            Assert.True(run.ComparableFullBytes >= run.Bytes);
            Assert.True(run.ComparableFullTokens >= run.Tokens);
            Assert.True(run.FullResponses + run.DiffResponses > 0);
        });
    }

    private static void WriteReport(SnapshotBenchmarkResult result)
    {
        Directory.CreateDirectory(ReportDirectory);
        var safeName = string.Concat(result.Scenario.Select(
            character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-'))
            .Trim('-');
        File.WriteAllText(
            Path.Combine(ReportDirectory, $"{safeName}.md"),
            FormatReport(result),
            System.Text.Encoding.UTF8);
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }

    private static double Reduction(double baseline, double current) =>
        baseline <= 0 ? 0 : (1.0 - (current / baseline)) * 100.0;

    private static string GetCurrentWindowHandle(SnapshotBenchmarkScenario scenario) =>
        scenario.CurrentWindowHandle?.Invoke() ?? scenario.WindowHandle;

    private static string ArmName(SnapshotBenchmarkArm arm) => arm switch
    {
        SnapshotBenchmarkArm.ActionOnly => "action-only",
        SnapshotBenchmarkArm.Full => "full",
        SnapshotBenchmarkArm.Auto => "auto",
        _ => throw new ArgumentOutOfRangeException(nameof(arm), arm, null)
    };
}
