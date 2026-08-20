using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.EventWaitSpike;

/// <summary>
/// Spike measurements for issue #189 against the WinForms harness.
/// </summary>
/// <remarks>
/// This is the decisive measurement of the spike. The WinForms harness runs in-process, so a
/// control can be created at a precisely known instant and the wait's reaction time measured
/// against it. The out-of-process harnesses can only be probed indirectly, so they are used to
/// check for regressions rather than to prove the benefit.
/// </remarks>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class WinFormsEventWaitBenchmarkTests : IDisposable
{
    private const int Samples = 5;
    private const int TimeoutMs = 10000;

    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;
    private readonly ITestOutputHelper _output;

    public WinFormsEventWaitBenchmarkTests(UITestHarnessFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Reset();
        _fixture.BringToFront();

        _windowHandle = _fixture.TestWindowHandleString;
        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            new MouseInputService(),
            new KeyboardInputService(),
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        // Leave the shared service in its shipping configuration for every other test.
        UIAutomationService.EventAssistedWaitEnabled = false;
        _staThread.Dispose();
        _automationService.Dispose();
    }

    [Fact]
    public async Task EventAssistedWait_DoesNotRegressLatency_WhenElementAppearsMidWait()
    {
        var polling = new List<WaitLatencySample>(Samples);
        var assisted = new List<WaitLatencySample>(Samples);

        for (var index = 0; index < Samples; index++)
        {
            polling.Add(await MeasureOneAsync(eventAssisted: false));
            assisted.Add(await MeasureOneAsync(eventAssisted: true));
        }

        var report = EventWaitBenchmark.FormatLatencyReport("winforms", polling, assisted);
        _output.WriteLine(report);
        EventWaitBenchmark.WriteReport("winforms", report);

        Assert.All(polling, sample => Assert.True(sample.Found, $"Polling wait failed to observe the element.\n{report}"));
        Assert.All(assisted, sample => Assert.True(sample.Found, $"Event-assisted wait failed to observe the element.\n{report}"));

        var pollingMedian = EventWaitBenchmark.Median(polling.Select(static sample => sample.LatencyMs));
        var assistedMedian = EventWaitBenchmark.Median(assisted.Select(static sample => sample.LatencyMs));

        // The claim under test is only that assistance does not make things worse. A hard
        // "must be faster" threshold would be a timing assertion on a shared CI desktop, which is
        // exactly the kind of flake this whole line of work exists to remove. The magnitude of any
        // improvement is reported, not asserted.
        //
        // The tolerance is deliberately wider than the effect being measured. Individual waits were
        // observed spanning 262-1046ms, so a median-of-5 can drift well past 50ms on a loaded
        // runner. This guards against a gross regression (the mechanism deadlocking or serialising
        // waits) without asserting on noise, which would just trade one flake for another.
        const double RegressionToleranceMs = 150;

        Assert.True(
            assistedMedian <= pollingMedian + RegressionToleranceMs,
            $"Event assistance regressed wait latency.\n{report}");
    }

    private async Task<WaitLatencySample> MeasureOneAsync(bool eventAssisted)
    {
        var form = _fixture.Form ?? throw new InvalidOperationException("Harness form is not available.");
        var name = $"SpikeTarget{Guid.NewGuid():N}";
        Button? target = null;

        try
        {
            return await EventWaitBenchmark.MeasureLatencyAsync(
                (query, timeoutMs) => _automationService.WaitForElementAsync(query, timeoutMs),
                new ElementQuery
                {
                    WindowHandle = _windowHandle,
                    Name = name,
                    ControlType = "Button"
                },
                trigger: () =>
                {
                    _ = form.Invoke(() =>
                    {
                        target = new Button
                        {
                            Name = name,
                            Text = name,
                            Left = 8,
                            Top = 8,
                            Width = 220,
                            Height = 24
                        };
                        form.Controls.Add(target);
                        target.BringToFront();
                        return true;
                    });

                    return Task.CompletedTask;
                },
                eventAssisted,
                TimeoutMs);
        }
        finally
        {
            if (target is not null)
            {
                _ = form.Invoke(() =>
                {
                    form.Controls.Remove(target);
                    target.Dispose();
                    return true;
                });
            }
        }
    }
}
