using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.EventWaitSpike;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Spike measurements for issue #189 against the WinUI 3 harness.
/// </summary>
/// <remarks>
/// The WinUI harness is a separate process, so an element cannot be created at a known instant the
/// way it can in the in-process WinForms harness. This therefore measures the timeout path only,
/// to show that subscribing does not make waits more expensive on a modern XAML provider.
/// </remarks>
[Collection("ModernTestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class WinUIEventWaitBenchmarkTests : IDisposable
{
    private const int TimeoutMs = 3000;

    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;
    private readonly ITestOutputHelper _output;

    public WinUIEventWaitBenchmarkTests(ModernTestHarnessFixture fixture, ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _output = output;
        fixture.BringToFront();

        _windowHandle = fixture.TestWindowHandleString;
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
    public async Task EventAssistedWait_DoesNotRegressTimeoutPath_OnWinUiTree()
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

        var report = EventWaitBenchmark.FormatTimeoutReport("winui", polling, assisted, TimeoutMs);
        _output.WriteLine(report);
        EventWaitBenchmark.WriteReport("winui", report);

        Assert.False(polling.Found, $"The spike query must not match anything.\n{report}");
        Assert.False(assisted.Found, $"The spike query must not match anything.\n{report}");

        Assert.True(
            assisted.ElapsedMs < TimeoutMs * 2,
            $"Event-assisted wait overran its timeout budget on a WinUI tree.\n{report}");
    }
}
