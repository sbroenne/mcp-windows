using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "SnapshotBenchmark")]
public sealed class WinFormsScreenshotBenchmarkTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationThread _thread = new();
    private readonly UIAutomationService _automation;
    private readonly ITestOutputHelper _output;

    public WinFormsScreenshotBenchmarkTests(UITestHarnessFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _automation = new UIAutomationService(
            _thread, new MonitorService(), new MouseInputService(), new KeyboardInputService(),
            new WindowActivator(), new ElevationDetector(), NullLogger<UIAutomationService>.Instance);
    }

    [Fact]
    public async Task Benchmark_ScreenshotVersusControls_WinForms()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        _output.WriteLine(await ScreenshotComparisonBenchmark.RunAsync(
            "WinForms field editing", CreateScenarioAsync, timeout.Token));
    }

    public void Dispose()
    {
        _automation.Dispose();
        _thread.Dispose();
    }

    private async Task<SnapshotBenchmarkScenario> CreateScenarioAsync(SnapshotBenchmarkArm arm, int sample)
    {
        _fixture.Reset();
        _fixture.BringToFront();
        var expectedText = "Baseline";
        var setup = await _automation.ObserveAndTypeAsync(Query(), expectedText, clearFirst: true);
        Assert.True(setup.Success, setup.ErrorMessage);
        string[] values = ["Alice", "Alice Smith", "Bob", "Carol"];
        var actions = values.Select(value => (Func<CancellationToken, Task>)(async token =>
        {
            var result = await _automation.ObserveAndTypeAsync(
                Query(), value, clearFirst: true, cancellationToken: token);
            Assert.True(result.Success, result.ErrorMessage);
            expectedText = value;
        })).ToArray();

        return new SnapshotBenchmarkScenario(
            "WinForms field editing", _fixture.TestWindowHandleString, _automation, actions,
            $"WinForms .NET {System.Environment.Version}; Windows {System.Environment.OSVersion.Version}; " +
            "task-specific query: UsernameInput, then ui_read of its observed ID",
            FocusedRead: async token =>
            {
                var found = await _automation.FindElementsAsync(Query(), token);
                Assert.True(found.Success, found.ErrorMessage);
                var element = Assert.Single(found.Items!);
                var read = await _automation.GetTextAsync(
                    element.Id, _fixture.TestWindowHandleString, includeChildren: false, cancellationToken: token);
                Assert.True(read.Success, read.ErrorMessage);
                Assert.Equal(expectedText, read.Text);
                return new[] { found, read };
            });
    }

    private ElementQuery Query() => new()
    {
        WindowHandle = _fixture.TestWindowHandleString,
        AutomationId = "UsernameInput",
        ControlType = "Edit",
        RequireUnique = true,
        TimeoutMs = 10000
    };
}
