using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the generalized Open dialog handling (<see cref="UIAutomationService.OpenFileAsync"/>).
/// The test harness shows a standard Open dialog on Ctrl+O, mirroring the Save flow.
/// </summary>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class OpenFileTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;
    private readonly string _testOutputDir;

    public OpenFileTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();

        _windowHandle = _fixture.TestWindowHandleString;
        _testOutputDir = Path.Combine(Path.GetTempPath(), "mcp-windows-open-tests");
        Directory.CreateDirectory(_testOutputDir);

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
        _staThread.Dispose();
        _automationService.Dispose();

        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    [Fact]
    public async Task Open_StandardWindowsDialog_SelectsFile()
    {
        var testFilePath = Path.Combine(_testOutputDir, $"open-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(testFilePath, "content to open");

        _fixture.BringToFront();
        await Task.Delay(500);

        var result = await _automationService.OpenFileAsync(_windowHandle, testFilePath);

        Assert.True(result.Success, $"Open handling failed: {result.ErrorMessage}");

        await Task.Delay(300);

        var lastOpened = _fixture.Form!.Invoke(new Func<string?>(() => _fixture.Form!.LastOpenPath));
        Assert.Equal(testFilePath, lastOpened, ignoreCase: true);
    }

    [Fact]
    public async Task Open_InvalidWindowHandle_ReturnsError()
    {
        var result = await _automationService.OpenFileAsync("invalid", @"C:\test\file.txt");

        Assert.False(result.Success);
        Assert.Contains("Invalid window handle", result.ErrorMessage);
    }

    [Fact]
    public async Task Open_NonExistentFile_ReturnsPathError()
    {
        var missing = Path.Combine(_testOutputDir, $"missing-{Guid.NewGuid()}.txt");

        var result = await _automationService.OpenFileAsync(_windowHandle, missing);

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.ErrorMessage);
    }
}
