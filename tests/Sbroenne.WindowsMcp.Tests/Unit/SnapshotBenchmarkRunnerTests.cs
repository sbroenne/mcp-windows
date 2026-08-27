using Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class SnapshotBenchmarkRunnerTests
{
    [Fact]
    public void FormatReport_UsesMedianOfPairedSavings()
    {
        var result = new SnapshotBenchmarkResult(
            "Paired samples",
            "Test",
            [
                Run(1, SnapshotBenchmarkArm.ActionOnly, 0, 0),
                Run(1, SnapshotBenchmarkArm.Full, 100, 100),
                Run(1, SnapshotBenchmarkArm.Auto, 90, 100),
                Run(2, SnapshotBenchmarkArm.Auto, 100, 1_000),
                Run(3, SnapshotBenchmarkArm.Auto, 9_000, 10_000)
            ]);

        var report = SnapshotBenchmarkRunner.FormatReport(result);

        Assert.Contains(
            "Median paired auto savings versus the same captures returned in full:** 10.0% bytes, 10.0% tokens.",
            report,
            StringComparison.Ordinal);
    }

    private static SnapshotBenchmarkRun Run(
        int sample,
        SnapshotBenchmarkArm arm,
        int bytes,
        int comparableFullBytes) =>
        new(
            sample,
            arm,
            ActionMs: 0,
            SnapshotMs: 0,
            Bytes: bytes,
            Tokens: bytes,
            ComparableFullBytes: comparableFullBytes,
            ComparableFullTokens: comparableFullBytes,
            FullResponses: arm == SnapshotBenchmarkArm.Full ? 1 : 0,
            DiffResponses: arm == SnapshotBenchmarkArm.Auto ? 1 : 0);
}
