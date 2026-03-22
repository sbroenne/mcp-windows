using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;

/// <summary>
/// Integration tests for the save action against Electron app harness.
/// Tests the SaveAsync method using keyboard-first approach (Ctrl+S) based on FlaUI/pywinauto patterns.
/// Mirrors the WinForms and WinUI SaveTests for framework parity.
/// </summary>
[Collection("ElectronHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class ElectronSaveTests : IDisposable
{
    private readonly ElectronHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;
    private readonly string _testOutputDir;

    public ElectronSaveTests(ElectronHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        Thread.Sleep(200);

        _windowHandle = _fixture.WindowHandleString;
        _testOutputDir = Path.Combine(Path.GetTempPath(), "mcp-windows-tests-electron");
        Directory.CreateDirectory(_testOutputDir);

        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();
        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            mouseService,
            keyboardService,
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        // Dismiss any dialogs left open by the test
        _fixture.DismissDialogs();

        _staThread.Dispose();
        _automationService.Dispose();

        // Cleanup test files
        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task Save_Electron_SavesFile()
    {
        // Arrange: Prepare test file path
        var testFilePath = Path.Combine(_testOutputDir, $"electron-test-{Guid.NewGuid()}.txt");

        // Ensure test file doesn't exist
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }

        // Bring window to front
        _fixture.BringToFront();
        await Task.Delay(500);

        // Act: Call SaveAsync on the main window
        var result = await _automationService.SaveAsync(_windowHandle, testFilePath);

        // Assert
        Assert.True(result.Success, $"SaveAsync failed: {result.ErrorMessage}");

        // Wait for file system to settle
        await Task.Delay(500);

        // Verify the file was created
        Assert.True(File.Exists(testFilePath),
            $"Expected file to exist at: {testFilePath}. Duration={result.Diagnostics?.DurationMs}ms");

        // Verify the file has content (the Electron harness writes a timestamp)
        var content = File.ReadAllText(testFilePath);
        Assert.NotEmpty(content);
        Assert.Contains("Test file created at", content);
    }

    [Fact]
    public async Task Save_InvalidWindowHandle_ReturnsError()
    {
        // Act
        var result = await _automationService.SaveAsync("invalid", @"C:\test\file.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid window handle", result.ErrorMessage);
    }

    [Fact]
    public async Task Save_NonExistentWindow_ReturnsError()
    {
        // Act
        var result = await _automationService.SaveAsync("999999999", @"C:\test\file.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Could not focus", result.ErrorMessage);
    }

    [Fact]
    public async Task Save_WithFilePath_SavesFileWhenDialogAppears()
    {
        // This test verifies the Ctrl+S workflow works when a Save dialog appears
        // The Electron harness shows a Save As dialog when it receives Ctrl+S

        // Arrange
        var testFilePath = Path.Combine(_testOutputDir, $"electron-ctrlS-test-{Guid.NewGuid()}.txt");

        _fixture.BringToFront();
        await Task.Delay(300);

        // Act
        var result = await _automationService.SaveAsync(_windowHandle, testFilePath);

        // Assert
        Assert.True(result.Success, $"Save failed: {result.ErrorMessage}");

        await Task.Delay(300);
        Assert.True(File.Exists(testFilePath), $"File was not created at: {testFilePath}");
    }

    [Fact]
    public async Task Save_WithoutFilePath_JustTriggersCtrlS()
    {
        // This test verifies that Save without filePath just sends Ctrl+S
        // If a dialog appears, it returns a hint instead of failing

        // Arrange
        _fixture.BringToFront();
        await Task.Delay(300);

        // Act - no filePath provided
        var result = await _automationService.SaveAsync(_windowHandle);

        // Assert - should succeed (either saved directly or dialog hint returned)
        Assert.True(result.Success, $"Save failed: {result.ErrorMessage}");

        // Unconditionally dismiss any dialogs — the Electron harness always opens a Save As dialog
        // on Ctrl+S, and DismissDialogs() is safe to call even if no dialog is open.
        _fixture.DismissDialogs();
        await Task.Delay(200);
    }
}
