using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SharpToken;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

internal sealed record PairedCaptureObservation(
    int Sample,
    int Step,
    int Width,
    int Height,
    int ImageBytes,
    int FullTokens,
    int AutoTokens,
    int? FocusedTokens,
    string AutoKind,
    double TreeMs,
    double ImageMs);

internal static class ScreenshotComparisonBenchmark
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("o200k_base");

    internal static int ImageTokens(int width, int height, bool highDetail)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (!highDetail)
        {
            return 85;
        }

        // GPT-4.1: never enlarge; fit 2048 square, then reduce the short side to 768.
        var longest = Math.Max(width, height);
        if (longest > 2048)
        {
            width = Math.Max(1, (int)((long)width * 2048 / longest));
            height = Math.Max(1, (int)((long)height * 2048 / longest));
        }

        var shortest = Math.Min(width, height);
        if (shortest > 768)
        {
            width = (int)((long)width * 768 / shortest);
            height = (int)((long)height * 768 / shortest);
        }

        return 85 + 170 * ((width + 511) / 512) * ((height + 511) / 512);
    }

    internal static async Task<string> RunAsync(
        string name,
        Func<SnapshotBenchmarkArm, int, Task<SnapshotBenchmarkScenario>> createScenario,
        CancellationToken cancellationToken = default)
    {
        var safeName = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-'));
        var directory = Path.Combine(
            SnapshotBenchmarkRunner.ReportDirectory,
            $"screenshot-{safeName}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var captures = new List<PairedCaptureObservation>();
        var screenshotService = new ScreenshotService(
            new MonitorService(), new SecureDesktopDetector(), new ImageProcessor());
        string? environment = null;

        for (var sample = 1; sample <= SnapshotBenchmarkRunner.DefaultSamples; sample++)
        {
            await using var scenario = await createScenario(SnapshotBenchmarkArm.Auto, sample).ConfigureAwait(false);
            environment ??= scenario.Environment;
            using var autoState = new SnapshotStateService();
            using var fullState = new SnapshotStateService();
            for (var step = 0; step <= scenario.Actions.Count; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (step > 0)
                {
                    await scenario.Actions[step - 1](cancellationToken).ConfigureAwait(false);
                }

                var handle = scenario.CurrentWindowHandle?.Invoke() ?? scenario.WindowHandle;
                var clock = Stopwatch.StartNew();
                var tree = await scenario.AutomationService.GetTreeAsync(
                    handle, null, scenario.MaxDepth, scenario.ControlTypeFilter, cancellationToken).ConfigureAwait(false);
                clock.Stop();
                var treeMs = clock.Elapsed.TotalMilliseconds;
                RequireComplete(tree);
                Assert.NotEmpty(tree.Tree!);

                clock.Restart();
                var screenshot = await screenshotService.ExecuteAsync(
                    new ScreenshotControlRequest
                    {
                        Target = CaptureTarget.Window,
                        WindowHandle = handle,
                        ImageFormat = ImageFormat.Jpeg,
                        Quality = 60,
                        IncludeCursor = false
                    },
                    cancellationToken).ConfigureAwait(false);
                clock.Stop();
                var imageMs = clock.Elapsed.TotalMilliseconds;
                Assert.True(screenshot.Success, screenshot.Message);
                Assert.NotNull(screenshot.ImageData);
                Assert.NotNull(screenshot.Width);
                Assert.NotNull(screenshot.Height);
                var image = Convert.FromBase64String(screenshot.ImageData);
                using (var stream = new MemoryStream(image))
                using (var bitmap = new Bitmap(stream))
                {
                    Assert.Equal(screenshot.Width.Value, bitmap.Width);
                    Assert.Equal(screenshot.Height.Value, bitmap.Height);
                }

                // Bracket the image with control reads: never pair different application states.
                var after = await scenario.AutomationService.GetTreeAsync(
                    handle, null, scenario.MaxDepth, scenario.ControlTypeFilter, cancellationToken).ConfigureAwait(false);
                RequireComplete(after);
                Assert.NotEmpty(after.Tree!);
                Assert.Equal(
                    JsonSerializer.Serialize(tree.Tree, WindowsToolsBase.JsonOptions),
                    JsonSerializer.Serialize(after.Tree, WindowsToolsBase.JsonOptions));

                var key = SnapshotRequestKey.Create(handle, null, scenario.MaxDepth, scenario.ControlTypeFilter);
                var full = await fullState.CaptureAsync(
                    key, SnapshotMode.Full, _ => Task.FromResult(tree), cancellationToken).ConfigureAwait(false);
                var auto = await autoState.CaptureAsync(
                    key, SnapshotMode.Auto, _ => Task.FromResult(tree), cancellationToken).ConfigureAwait(false);
                RequireComplete(full);
                RequireComplete(auto);
                Assert.NotNull(auto.Kind);
                if (step == 0)
                {
                    Assert.Equal("full", auto.Kind);
                }

                var prefix = Path.Combine(directory, $"sample-{sample}-step-{step}");
                File.WriteAllBytes($"{prefix}.jpg", image);
                File.WriteAllText(
                    $"{prefix}-image.json",
                    JsonSerializer.Serialize(screenshot with { ImageData = null }, WindowsToolsBase.JsonOptions));
                var fullTokens = SaveText($"{prefix}-full.json", full);
                var autoTokens = SaveText($"{prefix}-auto.json", auto);
                int? focusedTokens = null;
                if (scenario.FocusedRead is not null)
                {
                    var replies = await scenario.FocusedRead(cancellationToken).ConfigureAwait(false);
                    Assert.NotEmpty(replies);
                    focusedTokens = 0;
                    for (var reply = 0; reply < replies.Count; reply++)
                    {
                        RequireComplete(replies[reply]);
                        focusedTokens += SaveText($"{prefix}-focused-{reply}.json", replies[reply]);
                    }
                }

                captures.Add(new PairedCaptureObservation(
                    sample, step, screenshot.Width.Value, screenshot.Height.Value,
                    image.Length, fullTokens, autoTokens, focusedTokens, auto.Kind, treeMs, imageMs));
            }
        }

        Assert.NotNull(environment);
        var report = FormatReport(name, environment, captures);
        File.WriteAllText(Path.Combine(directory, "observations.json"), JsonSerializer.Serialize(captures));
        File.WriteAllText(Path.Combine(directory, "report.md"), report);
        return $"{report}{System.Environment.NewLine}Artifacts: {directory}";
    }

    internal static string FormatReport(
        string name, string environment, IReadOnlyList<PairedCaptureObservation> captures)
    {
        Assert.NotEmpty(captures);
        var samples = captures.GroupBy(capture => capture.Sample).ToArray();
        Assert.All(samples, sample => Assert.Single(sample, capture => capture.Step == 0));
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"# Screenshot versus UI Automation: {name}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"\nEnvironment: {environment}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Samples: {samples.Length}; observations: {captures.Count}; initial view included.");
        builder.AppendLine("""

Model accounting: GPT-4.1; SharpToken `o200k_base` for default MCP JSON text replies (diagnostics off).
Images: documented GPT-4.1 high-detail 512px tiles (85 base + 170/tile), with
85-token low-detail sensitivity. These are estimates, not model API usage.
Screenshot comparisons use image-only input: no screenshot metadata, tool schemas,
requests, action replies, reasoning, history replay, or caching is charged to either route.
Screenshots are window-only JPEG quality 60, no cursor or annotations.
No actions occur between paired reads. Control trees must match before and after each image.
Full and changes-only replies are generated from the same captured tree.
Focused reads, when present, include complete task-specific discovery and read replies,
but contain less information than a whole window. They are not a full-window substitute.
Capture timings exclude the stability check; they are not whole-task timings.
Negative savings are retained. No assertion requires UI Automation to win.

Sources:
- https://developers.openai.com/api/docs/guides/images-vision#tile-based-image-tokenization
- https://github.com/openai/tiktoken/blob/main/tiktoken/model.py

## Per-sample totals (including the first view)

| Route | Median estimated tokens | Median paired saving vs high-detail images |
|---|---:|---:|
""");
        AppendSummary("Full text", capture => capture.FullTokens);
        AppendSummary("Changes-only text", capture => capture.AutoTokens);
        if (captures.All(capture => capture.FocusedTokens.HasValue))
        {
            AppendSummary("Focused discovery + read", capture => capture.FocusedTokens!.Value);
        }

        AppendSummary("Screenshots high detail", capture => ImageTokens(capture.Width, capture.Height, true));
        AppendSummary("Screenshots low detail", capture => ImageTokens(capture.Width, capture.Height, false));
        builder.AppendLine("""

## Paired observations

Step 0 is the initial view. Image bytes are file size, not token count.
Each row has a saved JPEG, image metadata, and full/auto JSON; focused JSON is saved when applicable.

| Sample | Step | Image size | Image bytes | Image high tokens | Image low tokens | Full text tokens | Auto text tokens | Focused text tokens | Auto kind | Tree ms | Image ms |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---|---:|---:|
""");
        foreach (var capture in captures)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {capture.Sample} | {capture.Step} | {capture.Width}x{capture.Height} | {capture.ImageBytes} | " +
                $"{ImageTokens(capture.Width, capture.Height, true)} | 85 | {capture.FullTokens} | " +
                $"{capture.AutoTokens} | {capture.FocusedTokens?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} | " +
                $"{capture.AutoKind} | {capture.TreeMs:F1} | {capture.ImageMs:F1} |");
        }

        return builder.ToString();

        void AppendSummary(string label, Func<PairedCaptureObservation, int> tokens)
        {
            var totals = samples.Select(sample => (double)sample.Sum(tokens));
            var savings = samples.Select(sample => 100.0 * (1.0 -
                (double)sample.Sum(tokens) / sample.Sum(capture => ImageTokens(capture.Width, capture.Height, true))));
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {label} | {Median(totals):F0} | {Median(savings):F1}% |");
        }
    }

    internal static int SaveText(string path, UIAutomationResult result)
    {
        var json = WindowsToolsBase.SerializeUIResult(result, includeDiagnostics: false);
        File.WriteAllText(path, json);
        return Encoding.Encode(json).Count;
    }

    private static void RequireComplete(UIAutomationResult result)
    {
        Assert.True(result.Success, result.ErrorMessage);
        Assert.DoesNotContain(result.Diagnostics?.Warnings ?? [],
            warning => warning.Contains("truncated", StringComparison.OrdinalIgnoreCase));
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
    }
}
