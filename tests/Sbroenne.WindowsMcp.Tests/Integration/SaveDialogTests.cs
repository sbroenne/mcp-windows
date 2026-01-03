using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the save_dialog action.
/// Tests the SaveFileDialogAsync method against real Windows Save As dialogs.
/// </summary>
[Collection("UITestHarness")]
public sealed class SaveDialogTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly MouseInputService _mouseService;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly string _windowHandle;
    private readonly string _testOutputDir;

    public SaveDialogTests(UITestHarnessFixture fixture)
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

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        _windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        var windowService = new WindowService(
            _windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector,
            windowConfiguration);

        _mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            _mouseService,
            keyboardService,
            windowService,
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
    public async Task SaveDialog_StandardWindowsDialog_SavesFile()
    {
        // Arrange: Switch to Dialogs tab and prepare test file path
        var testFilePath = Path.Combine(_testOutputDir, $"test-{Guid.NewGuid()}.txt");

        // Ensure test file doesn't exist
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }

        // Bring window to front
        _fixture.BringToFront();
        await Task.Delay(500);

        // Verify tabs exist first
        var tabsResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TabItem",
        });
        Assert.True(tabsResult.Success, $"Find tabs failed: {tabsResult.ErrorMessage}");
        Assert.NotNull(tabsResult.Items);
        var tabNames = tabsResult.Items!.Select(e => e.Name).ToList();
        Assert.Contains("Dialogs", tabNames);

        // Find and click the Dialogs tab
        var clickTabResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Dialogs",
            ControlType = "TabItem",
        });
        Assert.True(clickTabResult.Success, $"Click Dialogs tab failed: {clickTabResult.ErrorMessage}");
        await Task.Delay(1000);  // Wait for tab to switch and button to become fully visible and interactable

        // Verify the button is now accessible by finding it first
        var buttonCheckResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Save As...",
            ControlType = "Button",
        });
        Assert.True(buttonCheckResult.Success, $"Find Save As button failed: {buttonCheckResult.ErrorMessage}");
        Assert.NotNull(buttonCheckResult.Items);
        Assert.NotEmpty(buttonCheckResult.Items!);

        // Debug output for button click coordinates
        var buttonInfo = buttonCheckResult.Items![0];
        var clickInfo = buttonInfo.Click != null && buttonInfo.Click.Length >= 2
            ? $"x={buttonInfo.Click[0]}, y={buttonInfo.Click[1]}"
            : "null or invalid";
        Assert.True(
            buttonInfo.Click != null && buttonInfo.Click.Length >= 2 && (buttonInfo.Click[0] > 0 || buttonInfo.Click[1] > 0),
            $"Button has invalid click coordinates: [{clickInfo}]. Type={buttonInfo.Type}, Name={buttonInfo.Name}");

        // Focus the button first to ensure it's ready for clicking
        var focusResult = await _automationService.FocusElementAsync(buttonInfo.Id);
        Assert.True(focusResult.Success, $"Focus on Save As button failed: {focusResult.ErrorMessage}");
        await Task.Delay(100);

        // Act: Start a task that will handle the dialog after it opens
        var dialogHandlingTask = Task.Run(async () =>
        {
            // Wait for the dialog to open
            await Task.Delay(2000);  // Increased wait time

            // Use SaveFileDialogAsync with the PARENT window handle
            // The action will find the modal Save As dialog automatically (FlaUI pattern)
            var result = await _automationService.SaveFileDialogAsync(_windowHandle, testFilePath);
            return (Success: result.Success, Error: result.ErrorMessage);
        });

        // Click the Save As button to open the dialog
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Save As...",
            ControlType = "Button",
        });
        Assert.True(clickResult.Success, $"Click Save As button failed: {clickResult.ErrorMessage}");

        // Wait for the Save As dialog to appear  
        await Task.Delay(500);

        // Verify the dialog appeared by using WaitForElement to find it
        var dialogWaitResult = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                ControlType = "Window",
                Name = "Save As",  // Default title for WinForms SaveFileDialog
            },
            timeoutMs: 5000);

        // If "Save As" not found, try broader search
        if (!dialogWaitResult.Success)
        {
            // Try searching for the FileNameControlHost directly on desktop
            dialogWaitResult = await _automationService.WaitForElementAsync(
                new ElementQuery
                {
                    AutomationId = "FileNameControlHost",
                },
                timeoutMs: 5000);
        }

        Assert.True(dialogWaitResult.Success, $"Save As dialog did not appear: {dialogWaitResult.ErrorMessage}");

        // Wait for dialog handling to complete - timeout after 30 seconds
        var dialogResult = await dialogHandlingTask.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(dialogResult.Success, $"SaveFileDialog handling failed: {dialogResult.Error}");

        // Verify the file was created
        Assert.True(File.Exists(testFilePath), $"Expected file to exist at: {testFilePath}");

        // Verify the file has content (the test harness writes a timestamp)
        var content = File.ReadAllText(testFilePath);
        Assert.NotEmpty(content);
        Assert.Contains("Test file created at", content);
    }

    [Fact]
    public async Task SaveDialog_InvalidWindowHandle_ReturnsError()
    {
        // Act
        var result = await _automationService.SaveFileDialogAsync("invalid", @"C:\test\file.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid window handle", result.ErrorMessage);
    }

    [Fact]
    public async Task SaveDialog_NonExistentWindow_ReturnsError()
    {
        // Act
        var result = await _automationService.SaveFileDialogAsync("999999999", @"C:\test\file.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Could not find", result.ErrorMessage);
    }

    [Fact]
    public async Task SaveDialog_WindowWithoutDialog_ReturnsNotFoundError()
    {
        // Act: Try to use SaveFileDialogAsync on a window that isn't a Save As dialog
        var result = await _automationService.SaveFileDialogAsync(_windowHandle, @"C:\test\file.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Could not find filename field", result.ErrorMessage);
    }
}
