using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class CliSnapshotContinuityTests(UITestHarnessFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task SeparateProcesses_TargetedReadAndClick_RejectSameLabelReplacement()
    {
        fixture.Reset();
        fixture.BringToFront();
        var form = Assert.IsType<UITestHarnessForm>(fixture.Form);
        try
        {
            var original = await FindSubmitAsync();
            var read = await RunTargetedAsync("read", original);
            Assert.True(read.Code == 0, read.Stderr + read.Stdout);
            Assert.Contains("Submit", read.Stdout, StringComparison.Ordinal);

            var click = await RunTargetedAsync("click", original);
            Assert.True(click.Code == 0, click.Stderr + click.Stdout);
            Assert.True(await TestWait.UntilAsync(() => ReadClickCount(form) == 1));

            form.Invoke(() => form.ReplaceSubmitButtonForTesting());
            var replacement = await FindSubmitAsync();
            Assert.NotEqual(original, replacement);
            var staleRead = await RunTargetedAsync("read", original);
            var staleClick = await RunTargetedAsync("click", original);
            Assert.Equal(1, staleRead.Code);
            Assert.Equal(1, staleClick.Code);
            using var staleReadJson = JsonDocument.Parse(staleRead.Stdout);
            using var staleClickJson = JsonDocument.Parse(staleClick.Stdout);
            // Reads report unresolved IDs as not found; clicks explicitly classify stale targets.
            // Both must fail closed rather than retargeting the same-label replacement.
            Assert.Equal(UIAutomationErrorType.ElementNotFound,
                staleReadJson.RootElement.GetProperty("errorType").GetString());
            Assert.False(staleReadJson.RootElement.TryGetProperty("text", out _));
            Assert.Equal(UIAutomationErrorType.ElementStale,
                staleClickJson.RootElement.GetProperty("errorType").GetString());
            Assert.Equal(1, ReadClickCount(form));

            var replacementRead = await RunTargetedAsync("read", replacement);
            Assert.True(replacementRead.Code == 0, replacementRead.Stderr + replacementRead.Stdout);
            using var replacementReadJson = JsonDocument.Parse(replacementRead.Stdout);
            Assert.Contains("Submit", replacementReadJson.RootElement.GetProperty("text").GetString(),
                StringComparison.Ordinal);

            var replacementClick = await RunTargetedAsync("click", replacement);
            Assert.True(replacementClick.Code == 0, replacementClick.Stderr + replacementClick.Stdout);
            Assert.True(await TestWait.UntilAsync(() => ReadClickCount(form) == 2));
        }
        finally
        {
            await CliIntegrationTests.RunSeparateProcessAsync("service", "stop");
            fixture.Reset();
        }
    }

    private static int ReadClickCount(UITestHarnessForm form) =>
        (int)form.Invoke(() => form.SubmitClickCount);

    private async Task<string> FindSubmitAsync()
    {
        var found = await CliIntegrationTests.RunSeparateProcessAsync(
            "ui", "find", "--window", fixture.TestWindowHandleString,
            "--automation-id", "SubmitButton", "--control-type", "Button");
        Assert.True(found.Code == 0, found.Stderr + found.Stdout);
        using var json = JsonDocument.Parse(found.Stdout);
        return Assert.IsType<string>(Assert.Single(json.RootElement.GetProperty("items").EnumerateArray())
            .GetProperty("id").GetString());
    }

    private Task<(int Code, string Stdout, string Stderr)> RunTargetedAsync(string action, string id) =>
        CliIntegrationTests.RunSeparateProcessAsync(
            "ui", action, "--window", fixture.TestWindowHandleString, "--element-id", id);

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
