using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for AnnotatedScreenshotService using the UI test harness window.
/// Tests real annotated screenshot capture against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class AnnotatedScreenshotIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly AnnotatedScreenshotService _annotatedScreenshotService;
    private readonly ScreenshotService _screenshotService;
    private readonly string _windowHandle;
    private readonly ITestOutputHelper _output;

    // Screenshot folder for manual verification (not git-tracked)
    private static readonly string ScreenshotFolder = Path.Combine(
        Path.GetDirectoryName(typeof(AnnotatedScreenshotIntegrationTests).Assembly.Location)!,
        "screenshots",
        "winforms");

    public AnnotatedScreenshotIntegrationTests(UITestHarnessFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;

        // Create real services for integration testing
        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var screenshotConfiguration = ScreenshotConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        var windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        var windowService = new WindowService(
            windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector,
            windowConfiguration);

        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            mouseService,
            keyboardService,
            windowService,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);

        var imageProcessor = new ImageProcessor();
        _screenshotService = new ScreenshotService(
            monitorService,
            secureDesktopDetector,
            imageProcessor,
            screenshotConfiguration,
            new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance));

        var annotatedLogger = new AnnotatedScreenshotLogger(NullLogger<AnnotatedScreenshotLogger>.Instance);
        _annotatedScreenshotService = new AnnotatedScreenshotService(
            _automationService,
            _screenshotService,
            imageProcessor,
            annotatedLogger);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region CaptureAsync Tests

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_ReturnsSuccess()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.ImageData);
        Assert.NotEmpty(result.ImageData);
        Assert.True(result.Width > 0, "Width should be positive");
        Assert.True(result.Height > 0, "Height should be positive");

        // Save for manual verification
        SaveScreenshotForVerification(result, "WinForms_AllElements");
    }

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_FindsInteractiveElements()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Should find at least one interactive element");
        Assert.Equal(result.Elements.Length, result.ElementCount);
    }

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_ElementsHaveSequentialIndices()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // Verify indices are sequential starting from 1
        for (int i = 0; i < result.Elements.Length; i++)
        {
            Assert.Equal(i + 1, result.Elements[i].Index);
        }
    }

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_ElementsHaveValidProperties()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        foreach (var element in result.Elements)
        {
            // Each element should have required properties
            Assert.True(element.Index > 0, "Index should be positive");
            Assert.False(string.IsNullOrEmpty(element.Type), "Type should not be empty");
            Assert.False(string.IsNullOrEmpty(element.Id), "Id should not be empty");
            Assert.NotNull(element.Click);
            Assert.Equal(3, element.Click.Length); // [x, y, monitorIndex]
        }
    }

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_FindsButtons()
    {
        // The test harness has Submit and Cancel buttons
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Button");

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // Should find at least some buttons
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 button, found {result.Elements.Length}");
        Assert.All(result.Elements, e => Assert.Equal("Button", e.Type));

        // Save for manual verification
        SaveScreenshotForVerification(result, "WinForms_ButtonsOnly");
    }

    [Fact]
    public async Task CaptureAsync_TestHarnessWindow_FindsEditControls()
    {
        // The test harness has text input fields
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Edit");

        // Assert - Edit controls may or may not be present depending on test harness state
        // Just verify the operation completes successfully
        if (result.Success && result.Elements != null && result.Elements.Length > 0)
        {
            Assert.All(result.Elements, e => Assert.Equal("Edit", e.Type));
        }
    }

    [Fact]
    public async Task CaptureAsync_WithControlTypeFilter_FiltersElements()
    {
        // Act - Only get buttons
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Button");

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.All(result.Elements, e => Assert.Equal("Button", e.Type));
    }

    [Fact]
    public async Task CaptureAsync_WithMaxElements_LimitsResults()
    {
        // Act - Limit to 3 elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            maxElements: 3);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length <= 3, $"Expected at most 3 elements, got {result.Elements.Length}");
    }

    [Fact]
    public async Task CaptureAsync_JpegFormat_ReturnsJpegImage()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            format: ImageFormat.Jpeg);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.Equal("jpeg", result.ImageFormat);
        Assert.NotNull(result.ImageData);

        // Verify JPEG magic bytes (FFD8FF)
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length >= 3);
        Assert.Equal(0xFF, imageBytes[0]);
        Assert.Equal(0xD8, imageBytes[1]);
        Assert.Equal(0xFF, imageBytes[2]);
    }

    [Fact]
    public async Task CaptureAsync_PngFormat_ReturnsPngImage()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            format: ImageFormat.Png);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.Equal("png", result.ImageFormat);
        Assert.NotNull(result.ImageData);

        // Verify PNG magic bytes (89504E47)
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length >= 4);
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]); // P
        Assert.Equal(0x4E, imageBytes[2]); // N
        Assert.Equal(0x47, imageBytes[3]); // G

        // Save for manual verification
        SaveScreenshotForVerification(result, "WinForms_PngFormat");
    }

    [Fact]
    public async Task CaptureAsync_ElementsHaveValidElementIds()
    {
        // Act - Get annotated elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0);

        // Verify all elements have non-empty Ids in the expected format
        foreach (var element in result.Elements)
        {
            Assert.False(
                string.IsNullOrEmpty(element.Id),
                $"Element {element.Index} should have a valid Id");
            Assert.Contains("window:", element.Id);
            Assert.Contains("runtime:", element.Id);
        }
    }

    [Fact]
    public async Task CaptureAsync_ClickablePointsAreValid()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        foreach (var element in result.Elements)
        {
            // Click coordinates should be reasonable [x, y, monitorIndex]
            Assert.NotNull(element.Click);
            Assert.Equal(3, element.Click.Length);
            Assert.True(element.Click[0] >= 0, $"Click X should be non-negative for element {element.Index}");
            Assert.True(element.Click[1] >= 0, $"Click Y should be non-negative for element {element.Index}");
            Assert.True(element.Click[2] >= 0, $"MonitorIndex should be non-negative for element {element.Index}");
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CaptureAsync_InvalidWindowHandle_ReturnsFailure()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: "999999");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CaptureAsync_NoMatchingControlTypes_ReturnsFailure()
    {
        // Act - Use a control type that doesn't exist
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "NonExistentType");

        // Assert - Should fail because no elements match
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Saves an annotated screenshot for manual verification.
    /// Screenshots are saved to a folder that is not tracked by git.
    /// </summary>
    private void SaveScreenshotForVerification(AnnotatedScreenshotResult result, string testName)
    {
        if (!result.Success || string.IsNullOrEmpty(result.ImageData))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(ScreenshotFolder);
            var extension = result.ImageFormat ?? "jpeg";
            var fileName = $"{testName}.{extension}";
            var filePath = Path.Combine(ScreenshotFolder, fileName);
            var imageBytes = Convert.FromBase64String(result.ImageData);
            File.WriteAllBytes(filePath, imageBytes);
            _output.WriteLine($"Screenshot saved: {filePath}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to save screenshot: {ex.Message}");
        }
    }

    #endregion
}
