using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

[Collection("ElectronHarness")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "SnapshotBenchmark")]
public sealed class ElectronSnapshotBenchmarkTests : IDisposable
{
    private readonly ElectronHarnessFixture _fixture;
    private readonly UIAutomationThread _staThread = new();
    private readonly UIAutomationService _automationService;
    private readonly ITestOutputHelper _output;

    public ElectronSnapshotBenchmarkTests(ElectronHarnessFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _automationService = new UIAutomationService(
            _staThread,
            new MonitorService(),
            new MouseInputService(),
            new KeyboardInputService(),
            new WindowActivator(),
            new ElevationDetector(),
            NullLogger<UIAutomationService>.Instance);
    }

    [Fact]
    public async Task Benchmark_RealElectronStructuralWorkflow()
    {
        var result = await SnapshotBenchmarkRunner.RunAsync(
            "Electron form, tabs, and modal",
            CreateScenarioAsync);

        _output.WriteLine(SnapshotBenchmarkRunner.FormatReport(result));
    }

    public void Dispose()
    {
        _automationService.Dispose();
        _staThread.Dispose();
    }

    private Task<SnapshotBenchmarkScenario> CreateScenarioAsync(
        SnapshotBenchmarkArm arm,
        int sample)
    {
        _fixture.Reset();

        IReadOnlyList<Func<CancellationToken, Task>> actions =
        [
            token => ClickAsync("Navigate Forms", "Button", token),
            token => ClickAsync("Navigate Data", "Button", token),
            token => ClickAsync("Navigate Settings", "Button", token),
            token => ClickAsync("Navigate Home", "Button", token)
        ];

        return Task.FromResult(new SnapshotBenchmarkScenario(
            "Electron form, tabs, and modal",
            _fixture.WindowHandleString,
            _automationService,
            actions,
            $"Electron {_fixture.ElectronVersion}; Windows {Environment.OSVersion.Version}"));
    }

    private async Task ClickAsync(string name, string controlType, CancellationToken cancellationToken)
    {
        var result = await _automationService.FindAndClickAsync(
            new ElementQuery
            {
                WindowHandle = _fixture.WindowHandleString,
                Name = name,
                ControlType = controlType,
                TimeoutMs = 10000
            },
            cancellationToken);

        Assert.True(result.Success, $"Click '{name}' failed: {result.ErrorMessage}");
    }
}
