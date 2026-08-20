using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.EventWaitSpike;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;

/// <summary>
/// Spike measurements for issue #189 against the Electron harness.
/// </summary>
/// <remarks>
/// Electron is the documented worst case for this idea: a Chromium accessibility tree churns, so
/// a subtree structure-changed subscription could deliver more events than polling costs. This
/// measures the timeout path, where a wait sits subscribed for its whole budget and finds nothing,
/// which is where an event storm would show up as wasted re-checks and CPU.
/// </remarks>
[Collection("ElectronHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class ElectronEventWaitBenchmarkTests : IDisposable
{
    private const int TimeoutMs = 3000;

    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;
    private readonly ITestOutputHelper _output;

    public ElectronEventWaitBenchmarkTests(ElectronHarnessFixture fixture, ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _output = output;
        fixture.Reset();

        _windowHandle = fixture.WindowHandleString;
        _staThread = new UIAutomationThread();

        _automationService = new UIAutomationService(
            _staThread,
            new MonitorService(),
            new MouseInputService(),
            new KeyboardInputService(),
            new WindowActivator(),
            new ElevationDetector(),
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        UIAutomationService.EventAssistedWaitEnabled = false;
        _staThread.Dispose();
        _automationService.Dispose();
    }

    [Fact]
    public async Task EventAssistedWait_DoesNotRegressTimeoutPath_OnChromiumTree()
    {
        var query = new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "NoSuchElementForSpike189",
            ControlType = "Button"
        };

        var polling = await EventWaitBenchmark.MeasureTimeoutCostAsync(
            (elementQuery, timeoutMs) => _automationService.WaitForElementAsync(elementQuery, timeoutMs),
            query,
            eventAssisted: false,
            TimeoutMs);

        var assisted = await EventWaitBenchmark.MeasureTimeoutCostAsync(
            (elementQuery, timeoutMs) => _automationService.WaitForElementAsync(elementQuery, timeoutMs),
            query,
            eventAssisted: true,
            TimeoutMs);

        var report = EventWaitBenchmark.FormatTimeoutReport("electron", polling, assisted, TimeoutMs);
        _output.WriteLine(report);
        EventWaitBenchmark.WriteReport("electron", report);

        Assert.False(polling.Found, $"The spike query must not match anything.\n{report}");
        Assert.False(assisted.Found, $"The spike query must not match anything.\n{report}");

        // A wait must still honour its timeout. If an event storm were driving re-checks faster
        // than the tree can answer them, the wall clock would overrun the requested budget.
        Assert.True(
            assisted.ElapsedMs < TimeoutMs * 2,
            $"Event-assisted wait overran its timeout budget on a Chromium tree.\n{report}");
    }
}
