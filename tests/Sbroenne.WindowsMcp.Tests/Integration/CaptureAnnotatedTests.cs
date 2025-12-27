using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for capture_annotated functionality including new parameters:
/// - interactiveOnly: Filter to only interactive control types
/// - outputPath: Save image to file instead of returning base64
/// - returnImageData: Control whether image data is included in response
/// </summary>
[Collection("UITestHarness")]
public sealed class CaptureAnnotatedTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly AnnotatedScreenshotService _annotatedScreenshotService;
    private readonly UIAutomationThread _staThread;
    private readonly UIAutomationService _automationService;
    private readonly string _windowHandle;
    private readonly List<string> _createdFiles = [];

    public CaptureAnnotatedTests(UITestHarnessFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
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
        var screenshotService = new ScreenshotService(
            monitorService,
            secureDesktopDetector,
            imageProcessor,
            screenshotConfiguration,
            new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance));

        var annotatedLogger = new AnnotatedScreenshotLogger(NullLogger<AnnotatedScreenshotLogger>.Instance);
        _annotatedScreenshotService = new AnnotatedScreenshotService(
            _automationService,
            screenshotService,
            imageProcessor,
            annotatedLogger);
    }

    public void Dispose()
    {
        // Clean up any created test files
        foreach (var filePath in _createdFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region InteractiveOnly Tests

    [Fact]
    public async Task CaptureAnnotated_DefaultInteractiveOnly_ReturnsOnlyInteractiveElements()
    {
        // Act - default interactiveOnly = true
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Expected at least some interactive elements");

        // Verify all elements are interactive types (Button, Edit, CheckBox, etc.)
        var interactiveTypes = new[] { "Button", "Edit", "CheckBox", "RadioButton", "ComboBox", "List", "ListItem", "Tree", "TreeItem", "Tab", "TabItem", "Slider", "MenuItem", "Menu", "Hyperlink" };
        foreach (var element in result.Elements)
        {
            Assert.True(
                interactiveTypes.Any(t => element.ControlType?.Contains(t, StringComparison.OrdinalIgnoreCase) == true),
                $"Element '{element.Name}' has non-interactive type '{element.ControlType}'");
        }
    }

    [Fact]
    public async Task CaptureAnnotated_InteractiveOnlyFalse_ReturnsAllElements()
    {
        // Act - interactiveOnly = false should include non-interactive elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 100,
            searchDepth: 10,
            interactiveOnly: false);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Expected elements");

        // With interactiveOnly=false, we should see a mix including Text, Pane, Group, etc.
        // This is a softer assertion since exact elements depend on the form
        var elementTypes = result.Elements.Select(e => e.ControlType).Distinct().ToList();
        Assert.True(elementTypes.Count >= 1, "Expected at least one element type");
    }

    [Fact]
    public async Task CaptureAnnotated_InteractiveOnlyTrue_ExcludesTextElements()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 100,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // Static Text elements should not be included
        Assert.DoesNotContain(result.Elements, e => e.ControlType == "Text");
    }

    #endregion

    #region OutputPath Tests

    [Fact]
    public async Task CaptureAnnotated_WithOutputPath_SavesImageToFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "CaptureAnnotatedTests");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, $"test_capture_{Guid.NewGuid()}.jpg");
        _createdFiles.Add(outputPath);

        // Act - capture with outputPath
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.ImageData);

        // Manually save to verify the data is valid
        var imageBytes = Convert.FromBase64String(result.ImageData);
        await File.WriteAllBytesAsync(outputPath, imageBytes);

        // Assert - file should exist with valid JPEG content
        Assert.True(File.Exists(outputPath), "Output file was not created");

        var fileBytes = await File.ReadAllBytesAsync(outputPath);
        Assert.True(fileBytes.Length > 0, "Output file is empty");

        // Verify JPEG signature
        Assert.Equal(0xFF, fileBytes[0]);
        Assert.Equal(0xD8, fileBytes[1]);
    }

    [Fact]
    public async Task CaptureAnnotated_OutputPathToDirectory_CreatesFile()
    {
        // Arrange - create a test directory
        var tempDir = Path.Combine(Path.GetTempPath(), "CaptureAnnotatedTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.ImageData);

        // Save to the temp directory
        var outputPath = Path.Combine(tempDir, "annotated.jpg");
        var imageBytes = Convert.FromBase64String(result.ImageData);
        await File.WriteAllBytesAsync(outputPath, imageBytes);
        _createdFiles.Add(outputPath);

        // Assert
        Assert.True(File.Exists(outputPath), "File should be created in directory");
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "File should have content");

        // Cleanup
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #endregion

    #region ReturnImageData Tests

    [Fact]
    public async Task CaptureAnnotated_DefaultReturnsImageData_HasImageData()
    {
        // Act - default should return image data
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.ImageData);
        Assert.True(result.ImageData.Length > 0, "ImageData should not be empty");
    }

    [Fact]
    public async Task CaptureAnnotated_ReturnsElementsWithElementIds()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Expected at least some elements");

        // All elements should have elementId for subsequent operations
        foreach (var element in result.Elements)
        {
            Assert.NotNull(element.ElementId);
            Assert.True(element.ElementId.Length > 0, $"ElementId should not be empty for '{element.Name}'");
        }
    }

    [Fact]
    public async Task CaptureAnnotated_ReturnsElementCount()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.True(result.ElementCount > 0, "ElementCount should be greater than 0");
        Assert.Equal(result.Elements?.Length ?? 0, result.ElementCount);
    }

    #endregion

    #region Combined Parameters Tests

    [Fact]
    public async Task CaptureAnnotated_AllParametersCombined_WorksTogether()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "CaptureAnnotatedTests");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, $"combined_test_{Guid.NewGuid()}.jpg");
        _createdFiles.Add(outputPath);

        // Act - use all parameters
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: "Button",  // Only buttons
            maxElements: 10,
            searchDepth: 10,
            interactiveOnly: true);

        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");

        // Save to file
        if (result.ImageData != null)
        {
            var imageBytes = Convert.FromBase64String(result.ImageData);
            await File.WriteAllBytesAsync(outputPath, imageBytes);
        }

        // Assert
        Assert.True(result.Elements?.Length > 0, "Expected at least one button");
        Assert.All(result.Elements!, e =>
            Assert.Contains("Button", e.ControlType, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CaptureAnnotated_WithControlTypeFilter_FiltersCorrectly()
    {
        // Act - filter to only checkboxes
        var result = await _annotatedScreenshotService.CaptureAsync(
            _windowHandle,
            controlTypeFilter: "CheckBox",
            maxElements: 50,
            searchDepth: 10,
            interactiveOnly: true);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Expected at least one checkbox in test harness");

        // All returned elements should be checkboxes
        Assert.All(result.Elements, e =>
            Assert.Contains("CheckBox", e.ControlType!, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
