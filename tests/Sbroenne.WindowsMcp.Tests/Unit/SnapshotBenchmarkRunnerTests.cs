using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class SnapshotBenchmarkRunnerTests
{
    [Theory]
    [InlineData("diff", "diff")]
    [InlineData("full", "diff")]
    [InlineData("diff", "full")]
    public void AlignComparisonTokens_UsesSameCaptureTokensWithoutChangingResponseShape(
        string actualKind, string comparisonKind)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        var baseline = new UIAutomationResult
        {
            Success = true,
            Action = "snapshot",
            Kind = "full",
            SnapshotToken = new string('a', 32)
        };
        var comparisonBaseline = baseline with { SnapshotToken = new string('1', 32) };
        _ = SnapshotBenchmarkRunner.AlignComparisonTokens(comparisonBaseline, baseline, tokens);
        var actual = baseline with
        {
            Kind = actualKind,
            SnapshotToken = new string('b', 32),
            BaseSnapshotToken = actualKind == "diff" ? baseline.SnapshotToken : null
        };
        var comparison = comparisonBaseline with
        {
            Kind = comparisonKind,
            SnapshotToken = new string('2', 32),
            BaseSnapshotToken = comparisonKind == "diff" ? comparisonBaseline.SnapshotToken : null
        };

        var aligned = SnapshotBenchmarkRunner.AlignComparisonTokens(comparison, actual, tokens);

        var expected = actual with
        {
            Kind = comparisonKind,
            BaseSnapshotToken = comparisonKind == "diff" ? baseline.SnapshotToken : null
        };
        Assert.Equal(
            JsonSerializer.Serialize(expected, WindowsToolsBase.JsonOptions),
            JsonSerializer.Serialize(aligned, WindowsToolsBase.JsonOptions));
        Assert.Equal(new string('2', 32), comparison.SnapshotToken);
        Assert.Equal(comparisonKind, aligned.Kind);
    }

    [Fact]
    public void FormatReport_UsesMedianOfPairedSavings()
    {
        var result = new SnapshotBenchmarkResult(
            "Paired samples",
            "Test",
            [
                Run(1, SnapshotBenchmarkArm.ActionOnly, 0, 0),
                Run(1, SnapshotBenchmarkArm.Full, 100, 100),
                Run(1, SnapshotBenchmarkArm.Auto, 90, 100, 95),
                Run(2, SnapshotBenchmarkArm.Auto, 100, 1_000, 110),
                Run(3, SnapshotBenchmarkArm.Auto, 9_000, 10_000, 10_000)
            ]);

        var report = SnapshotBenchmarkRunner.FormatReport(result);

        Assert.Contains(
            "Median paired auto savings versus the same captures returned in full:** 10.0% bytes, 10.0% tokens.",
            report,
            StringComparison.Ordinal);
        Assert.Contains(
            "Median paired display-cleanup savings versus automatic semantic output:** 9.1% bytes, 9.1% tokens.",
            report,
            StringComparison.Ordinal);
    }

    private static SnapshotBenchmarkRun Run(
        int sample,
        SnapshotBenchmarkArm arm,
        int bytes,
        int comparableFullBytes,
        int? comparableSemanticBytes = null) =>
        new(
            sample,
            arm,
            ActionMs: 0,
            SnapshotMs: 0,
            Bytes: bytes,
            Tokens: bytes,
            ComparableFullBytes: comparableFullBytes,
            ComparableFullTokens: comparableFullBytes,
            ComparableSemanticBytes: comparableSemanticBytes ?? bytes,
            ComparableSemanticTokens: comparableSemanticBytes ?? bytes,
            FullResponses: arm == SnapshotBenchmarkArm.Full ? 1 : 0,
            DiffResponses: arm == SnapshotBenchmarkArm.Auto ? 1 : 0);
}
