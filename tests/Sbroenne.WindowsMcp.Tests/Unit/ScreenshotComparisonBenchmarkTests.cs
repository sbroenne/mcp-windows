using Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class ScreenshotComparisonBenchmarkTests
{
    [Theory]
    [InlineData(1024, 1024, 765)]
    [InlineData(2048, 4096, 1105)]
    [InlineData(4096, 512, 765)]
    [InlineData(512, 512, 255)]
    [InlineData(513, 512, 425)]
    [InlineData(1, 1, 255)]
    public void ImageTokens_ApplyDocumentedGpt41TileRules(int width, int height, int expected)
    {
        Assert.Equal(expected, ScreenshotComparisonBenchmark.ImageTokens(width, height, highDetail: true));
        Assert.Equal(85, ScreenshotComparisonBenchmark.ImageTokens(width, height, highDetail: false));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    public void ImageTokens_RejectInvalidDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScreenshotComparisonBenchmark.ImageTokens(width, height, highDetail: true));
    }

    [Fact]
    public void SavedText_UsesDefaultMcpResponseWithoutDiagnostics()
    {
        var path = Path.GetTempFileName();
        try
        {
            var tokens = ScreenshotComparisonBenchmark.SaveText(path, new UIAutomationResult
            {
                Success = true,
                Action = "get_text",
                Text = "Alice",
                Diagnostics = new UIAutomationDiagnostics { DurationMs = 123 }
            });

            Assert.Equal("{\"success\":true,\"action\":\"get_text\",\"text\":\"Alice\"}", File.ReadAllText(path));
            Assert.True(tokens > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Report_IncludesFirstViewAndKeepsNegativeSavings()
    {
        var observations = new[]
        {
            Observation(1, 0, 1000),
            Observation(1, 1, 20),
            Observation(2, 0, 2000),
            Observation(2, 1, 40)
        };

        var report = ScreenshotComparisonBenchmark.FormatReport("Test", "Test environment", observations);

        Assert.Contains("initial view included", report, StringComparison.Ordinal);
        Assert.Contains("| Changes-only text | 1530 |", report, StringComparison.Ordinal);
        Assert.Contains("-200.0%", report, StringComparison.Ordinal);
        Assert.Contains("| 1 | 0 |", report, StringComparison.Ordinal);
        Assert.Contains("image-only", report, StringComparison.Ordinal);
    }

    private static PairedCaptureObservation Observation(int sample, int step, int tokens) =>
        new(sample, step, 512, 512, 1000, tokens, tokens, null, "full", 1, 1);
}
