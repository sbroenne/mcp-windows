using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class CliSnapshotContinuityTests(UITestHarnessFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task SeparateProcesses_UseLatestBaselineAndRecoverFromInterleaving()
    {
        var clock = Stopwatch.StartNew();
        var first = await CaptureAsync();
        var firstMs = clock.Elapsed.TotalMilliseconds;
        using var full = JsonDocument.Parse(first);
        Assert.Equal("full", full.RootElement.GetProperty("kind").GetString());
        var firstToken = full.RootElement.GetProperty("snapshotToken").GetString();

        clock.Restart();
        var second = await CaptureAsync(firstToken);
        var secondMs = clock.Elapsed.TotalMilliseconds;
        using var diff = JsonDocument.Parse(second);
        Assert.Equal("diff", diff.RootElement.GetProperty("kind").GetString());
        Assert.Equal(firstToken, diff.RootElement.GetProperty("baseSnapshotToken").GetString());
        Assert.NotEqual(firstToken, diff.RootElement.GetProperty("snapshotToken").GetString());
        Assert.True(Encoding.UTF8.GetByteCount(second) * 100L < Encoding.UTF8.GetByteCount(first) * 80L);

        using var mismatch = JsonDocument.Parse(await CaptureAsync(firstToken));
        Assert.Equal("full", mismatch.RootElement.GetProperty("kind").GetString());
        Assert.False(mismatch.RootElement.TryGetProperty("baseSnapshotToken", out _));
        using var missing = JsonDocument.Parse(await CaptureAsync());
        Assert.Equal("full", missing.RootElement.GetProperty("kind").GetString());

        output.WriteLine(
            $"Separate CLI processes: full={Encoding.UTF8.GetByteCount(first)} bytes ({firstMs:F1} ms), " +
            $"diff={Encoding.UTF8.GetByteCount(second)} bytes ({secondMs:F1} ms). " +
            "First-call time includes startup only if no daemon was already running.");
    }

    private async Task<string> CaptureAsync(string? since = null)
    {
        var args = new List<string>
        {
            "ui", "snapshot", "--window", fixture.TestWindowHandleString, "--mode", "auto"
        };
        if (since is not null)
        {
            args.Add("--since");
            args.Add(since);
        }

        var result = await CliIntegrationTests.RunSeparateProcessAsync([.. args]);
        Assert.True(result.Code == 0, $"{result.Stderr}\n{result.Stdout}");
        Assert.Empty(result.Stderr);
        return result.Stdout;
    }
}
