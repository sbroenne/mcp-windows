using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the generalized Open dialog handling.
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

    [SkippableFact]
    public async Task Open_StandardWindowsDialog_SelectsFile()
    {
        DesktopInputTests.SkipUnlessEnabled();

        await TestRetry.RunAsync(async _ =>
        {
            var testFilePath = Path.Combine(_testOutputDir, $"open-{Guid.NewGuid()}.txt");
            await File.WriteAllTextAsync(testFilePath, "content to open");

            _fixture.BringToFront();
            await Task.Delay(500);

            var result = await _automationService.OpenFileAsync(_windowHandle, testFilePath);

            Assert.True(result.Success, $"Open handling failed: {result.ErrorMessage}");

            // Wait for the harness to record the opened path instead of assuming it has done so.
            string? lastOpened = null;
            await TestWait.UntilAsync(
                () =>
                {
                    lastOpened = _fixture.Form!.Invoke(new Func<string?>(() => _fixture.Form!.LastOpenPath));
                    return string.Equals(testFilePath, lastOpened, StringComparison.OrdinalIgnoreCase);
                },
                TimeSpan.FromSeconds(10));

            // Assert on the observed value rather than on the wait result: if the wait times out this
            // reports the expected and actual paths, which is more actionable than "the wait expired".
            Assert.Equal(testFilePath, lastOpened, ignoreCase: true);
        });
    }

    [SkippableFact]
    public async Task Open_WaitForExistingDialog_SelectsFileOpenedByPriorAction()
    {
        DesktopInputTests.SkipUnlessEnabled();

        var testFilePath = Path.Combine(_testOutputDir, $"handoff-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(testFilePath, "content to open");

        _fixture.Form!.BeginInvoke(_fixture.Form.ShowOpenDialogForTesting);

        var result = await _automationService.OpenFileAsync(
            _windowHandle,
            testFilePath,
            triggerMode: "wait",
            timeoutMs: 5000);

        Assert.True(
            result.Success,
            $"Open handoff failed: {result.ErrorMessage} Diagnostics: {string.Join("; ", result.Diagnostics?.Warnings ?? [])}");
        Assert.Equal("wait+semantic", result.Diagnostics?.ActionPath);

        Assert.True(await TestWait.UntilAsync(
            () => string.Equals(
                testFilePath,
                _fixture.Form!.Invoke(new Func<string?>(() => _fixture.Form.LastOpenPath)),
                StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Open_WaitMode_DoesNotSubmitOwnedSaveDialog()
    {
        var testFilePath = Path.Combine(_testOutputDir, $"not-for-save-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(testFilePath, "must not be saved");

        _fixture.Form!.BeginInvoke(_fixture.Form.ShowSaveDialogForTesting);
        try
        {
            var result = await _automationService.OpenFileAsync(
                _windowHandle,
                testFilePath,
                triggerMode: "wait",
                timeoutMs: 500);

            Assert.False(result.Success);
            Assert.Equal(UIAutomationErrorType.Timeout, result.ErrorType);
            Assert.Null(_fixture.Form.LastSavePath);
        }
        finally
        {
            _fixture.CloseOwnedDialogs();
        }
    }

    [Fact]
    public async Task Find_ActiveDialogScope_TargetsNativeOpenDialog()
    {
        _fixture.Form!.BeginInvoke(_fixture.Form.ShowOpenDialogForTesting);

        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Scope = "active_dialog",
                ControlType = "Edit",
                VisibleOnly = true,
            },
            timeoutMs: 5000);

        Assert.True(result.Success, $"Active dialog search failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.Items!);

        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Scope = "active_dialog",
            Name = "Cancel",
            ControlType = "Button",
            RequireUnique = true,
        });
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
