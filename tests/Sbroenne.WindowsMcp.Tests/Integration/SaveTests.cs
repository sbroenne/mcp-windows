using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the save action.
/// Tests the SaveAsync method using keyboard-first approach (Ctrl+S) based on FlaUI/pywinauto patterns.
/// </summary>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class SaveTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly MouseInputService _mouseService;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly string _windowHandle;
    private readonly string _testOutputDir;

    public SaveTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;
        _testOutputDir = Path.Combine(Path.GetTempPath(), "mcp-windows-tests");
        Directory.CreateDirectory(_testOutputDir);

        // Create real services for integration testing
        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        _windowEnumerator = new WindowEnumerator(elevationDetector);
        var windowActivator = new WindowActivator();
        var windowService = new WindowService(
            _windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector);

        _mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            _mouseService,
            keyboardService,
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
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
    public async Task Save_StandardWindowsDialog_SavesFile()
    {
        // Arrange: Prepare test file path
        var testFilePath = Path.Combine(_testOutputDir, $"test-{Guid.NewGuid()}.txt");

        // Ensure test file doesn't exist
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }

        // Bring window to front
        _fixture.BringToFront();
        await Task.Delay(500);

        // Act: Call SaveAsync on the main window
        // This sends Ctrl+S, which triggers the Save As dialog in the test harness
        // Then it fills in the filename and presses Enter
        var result = await _automationService.SaveAsync(_windowHandle, testFilePath);

        // Assert
        Assert.True(result.Success, $"Save handling failed: {result.ErrorMessage}");

        // Wait for file system to settle
        await Task.Delay(500);

        // Verify the file was created
        Assert.True(File.Exists(testFilePath), $"Expected file to exist at: {testFilePath}");

        // Verify the file has content (the test harness writes a timestamp)
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
        // The test harness shows a Save As dialog when it receives Ctrl+S

        // Arrange
        var testFilePath = Path.Combine(_testOutputDir, $"ctrlS-test-{Guid.NewGuid()}.txt");

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

        // Cleanup: if a dialog was opened (hint returned), close it with Escape
        if (result.UsageHint != null && result.UsageHint.Contains("dialog"))
        {
            var keyboardService = new KeyboardInputService();
            await keyboardService.PressKeyAsync("Escape");
            await Task.Delay(200);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Save_NonExistentDirectory_ReturnsPathError()
    {
        // This test verifies that saving to a non-existent directory returns an error
        // It uses real Notepad to trigger the actual Windows "Path does not exist" dialog

        // Arrange: Launch Notepad
        var notepad = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "notepad.exe",
            UseShellExecute = true
        });

        try
        {
            await Task.Delay(2000); // Wait for Notepad to fully launch

            // Find the Notepad window
            var windows = await _windowEnumerator.EnumerateWindowsAsync();
            var notepadWindow = windows.FirstOrDefault(w =>
                w.ProcessName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) &&
                w.Title.Contains("Notepad"));

            Assert.NotNull(notepadWindow);
            var notepadHandle = notepadWindow.Handle.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Create path to non-existent directory
            var nonExistentPath = Path.Combine(
                Path.GetTempPath(),
                $"nonexistent-directory-{Guid.NewGuid()}",
                "test.txt");

            // Ensure directory really doesn't exist
            var directory = Path.GetDirectoryName(nonExistentPath)!;
            Assert.False(Directory.Exists(directory), $"Directory should not exist: {directory}");

            // Act: Try to save to non-existent path
            var result = await _automationService.SaveAsync(notepadHandle, nonExistentPath);

            // Assert: Should fail with PathError
            Assert.False(result.Success, "Save should fail for non-existent path");
            Assert.Equal(Models.UIAutomationErrorType.PathError, result.ErrorType);
            Assert.Contains("does not exist", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Cleanup: Kill Notepad
            try
            {
                notepad?.Kill();
                notepad?.WaitForExit(1000);
            }
            catch { }
            notepad?.Dispose();
        }
    }
}
