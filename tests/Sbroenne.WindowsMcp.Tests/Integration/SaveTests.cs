using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Native;
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
    private readonly string _windowHandle;
    private readonly string _testOutputDir;

    public SaveTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();

        _windowHandle = _fixture.TestWindowHandleString;
        _testOutputDir = Path.Combine(Path.GetTempPath(), "mcp-windows-tests");
        Directory.CreateDirectory(_testOutputDir);

        // Create real services for integration testing
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
        Assert.True(File.Exists(testFilePath), "Save must observe the requested file before reporting success.");

        // Wait for the harness to finish writing the file. Waiting on the content rather than on
        // File.Exists also rules out observing a file that exists but has not been flushed yet.
        var content = string.Empty;
        var saved = await TestWait.UntilAsync(
            () =>
            {
                try
                {
                    content = File.ReadAllText(testFilePath);
                }
                catch (IOException)
                {
                    return false;
                }

                return content.Contains("Test file created at", StringComparison.Ordinal);
            },
            TimeSpan.FromSeconds(10));

        Assert.True(saved, $"Expected file to exist with harness content at: {testFilePath}");
        Assert.NotEmpty(content);
        Assert.Contains("Test file created at", content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_MissingDialogAndFileChange_DoesNotReportSuccess(bool existingFile)
    {
        var fixtureForm = _fixture.Form!;
        System.Windows.Forms.Form? target = null;
        var handle = (nint)fixtureForm.Invoke(() =>
        {
            target = new System.Windows.Forms.Form { Text = "Save without a handler" };
            target.Show();
            return target.Handle;
        });
        var path = Path.Combine(_testOutputDir, $"not-saved-{Guid.NewGuid():N}.txt");

        try
        {
            if (existingFile)
            {
                await File.WriteAllTextAsync(path, "Unchanged existing file");
            }

            var result = await _automationService.SaveAsync(WindowHandleParser.Format(handle), path);

            Assert.False(result.Success);
            Assert.Equal(Models.UIAutomationErrorType.Timeout, result.ErrorType);
            Assert.Contains("could not be verified", result.ErrorMessage, StringComparison.Ordinal);
            Assert.Equal(existingFile, File.Exists(path));
            if (existingFile)
            {
                Assert.Equal("Unchanged existing file", await File.ReadAllTextAsync(path));
            }
        }
        finally
        {
            fixtureForm.Invoke(() => target?.Dispose());
        }
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
    public async Task Save_DoesNotConfirmAnUnrelatedWindow()
    {
        var fixtureForm = _fixture.Form!;
        System.Windows.Forms.Form? unrelated = null;
        var unrelatedHandle = (nint)fixtureForm.Invoke(() =>
        {
            unrelated = new System.Windows.Forms.Form { Text = "Confirm Save As" };
            var status = new System.Windows.Forms.Label
            {
                Name = "ForeignConfirmStatus",
                Text = "Untouched",
                AutoSize = true
            };
            var confirm = new System.Windows.Forms.Button { Text = "Yes", Top = 40 };
            confirm.Click += (_, _) => status.Text = "Confirmed";
            unrelated.Controls.Add(status);
            unrelated.Controls.Add(confirm);
            unrelated.Show(fixtureForm);
            return unrelated.Handle;
        });
        var path = Path.Combine(_testOutputDir, $"owned-save-{Guid.NewGuid():N}.txt");

        try
        {
            var result = await _automationService.SaveAsync(_windowHandle, path);
            var failureState = result.Success ? null : await _automationService.GetTextAsync(
                null, _windowHandle, includeChildren: true);
            Assert.True(result.Success,
                $"{result.ErrorMessage}. Window text: {failureState?.Text ?? failureState?.ErrorMessage}. " +
                $"Files in the owned output directory: {string.Join(", ", Directory.GetFiles(_testOutputDir))}");
            Assert.True(File.Exists(path));

            var foreignWindow = WindowHandleParser.Format(unrelatedHandle);
            var found = await _automationService.FindElementsAsync(new Models.ElementQuery
            {
                WindowHandle = foreignWindow,
                AutomationId = "ForeignConfirmStatus",
                RequireUnique = true
            });
            Assert.True(found.Success, found.ErrorMessage);
            var read = await _automationService.GetTextAsync(
                Assert.Single(found.Items!).Id, foreignWindow, includeChildren: false);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.Equal("Untouched", read.Text);
        }
        finally
        {
            fixtureForm.Invoke(() => unrelated?.Dispose());
        }
    }

    [Fact]
    public async Task Save_AcceptedDialogWithoutFile_DoesNotReportSuccess()
    {
        var fixtureForm = _fixture.Form!;
        System.Windows.Forms.Form? target = null;
        string? acceptedPath = null;
        var handle = (nint)fixtureForm.Invoke(() =>
        {
            target = new System.Windows.Forms.Form { Text = "Save without writing", KeyPreview = true };
            target.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == System.Windows.Forms.Keys.S)
                {
                    e.SuppressKeyPress = true;
                    using var dialog = new System.Windows.Forms.SaveFileDialog();
                    if (dialog.ShowDialog(target) == System.Windows.Forms.DialogResult.OK)
                    {
                        acceptedPath = dialog.FileName;
                    }
                }
            };
            target.Show();
            return target.Handle;
        });
        var path = Path.Combine(_testOutputDir, $"accepted-not-saved-{Guid.NewGuid():N}.txt");

        try
        {
            var result = await _automationService.SaveAsync(WindowHandleParser.Format(handle), path);

            Assert.Equal(path, (string?)fixtureForm.Invoke(() => acceptedPath));
            Assert.False(result.Success);
            Assert.Equal(Models.UIAutomationErrorType.Timeout, result.ErrorType);
            Assert.Contains("could not be verified", result.ErrorMessage, StringComparison.Ordinal);
            Assert.False(File.Exists(path));
        }
        finally
        {
            fixtureForm.Invoke(() => target?.Dispose());
        }
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

        var created = await TestWait.UntilAsync(
            () => File.Exists(testFilePath),
            TimeSpan.FromSeconds(10));
        Assert.True(created, $"File was not created at: {testFilePath}");
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
        // before focusing or sending input to the existing test harness window.
        var nonExistentPath = Path.Combine(
            Path.GetTempPath(),
            $"nonexistent-directory-{Guid.NewGuid()}",
            "test.txt");
        var directory = Path.GetDirectoryName(nonExistentPath)!;
        Assert.False(Directory.Exists(directory), $"Directory should not exist: {directory}");

        var result = await _automationService.SaveAsync(_windowHandle, nonExistentPath);

        Assert.False(result.Success, "Save should fail for non-existent path");
        Assert.Equal(Models.UIAutomationErrorType.PathError, result.ErrorType);
        Assert.Contains("does not exist", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
