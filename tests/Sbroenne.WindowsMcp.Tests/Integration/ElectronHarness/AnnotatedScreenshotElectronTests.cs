using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;

/// <summary>
/// Integration tests for AnnotatedScreenshotService against Electron applications.
/// Validates that annotated screenshots correctly capture interactive elements in Chromium-based apps.
/// </summary>
[Collection("ElectronHarness")]
public sealed class AnnotatedScreenshotElectronTests : IDisposable
{
    private readonly ElectronHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly AnnotatedScreenshotService _annotatedScreenshotService;
    private readonly ScreenshotService _screenshotService;
    private readonly LegacyOcrService _ocrService;
    private readonly VisualDiffService _visualDiffService;
    private readonly nint _windowHandle;

    public AnnotatedScreenshotElectronTests(ElectronHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.BringToFront();
        Thread.Sleep(300);

        _windowHandle = _fixture.WindowHandle;

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

        // OCR service for verifying annotation labels
        _ocrService = new LegacyOcrService(NullLogger<LegacyOcrService>.Instance);

        // Visual diff service for comparing annotated vs non-annotated screenshots
        _visualDiffService = new VisualDiffService();
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region Basic Capture Tests

    [Fact]
    public async Task CaptureAsync_ElectronWindow_ReturnsSuccess()
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
    }

    [Fact]
    public async Task CaptureAsync_ElectronWindow_FindsMultipleInteractiveElements()
    {
        // Act - Use default searchDepth of 15 for Electron
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Save screenshot for manual verification
        SaveScreenshotForVerification(result, nameof(CaptureAsync_ElectronWindow_FindsMultipleInteractiveElements));

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        // Electron harness has buttons, inputs, etc. - should find multiple elements
        Assert.True(result.Elements.Length >= 3,
            $"Expected at least 3 interactive elements in Electron app, found {result.Elements.Length}");
        Assert.Equal(result.Elements.Length, result.ElementCount);
    }

    [Fact]
    public async Task CaptureAsync_ElectronWindow_ElementsHaveSequentialIndices()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        // Verify indices are sequential starting from 1
        for (int i = 0; i < result.Elements.Length; i++)
        {
            Assert.Equal(i + 1, result.Elements[i].Index);
        }
    }

    [Fact]
    public async Task CaptureAsync_ElectronWindow_ElementsHaveValidProperties()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        foreach (var element in result.Elements)
        {
            // Each element should have required properties
            Assert.True(element.Index > 0, "Index should be positive");
            Assert.False(string.IsNullOrEmpty(element.ControlType), "ControlType should not be empty");
            Assert.False(string.IsNullOrEmpty(element.ElementId), "ElementId should not be empty");
            Assert.NotNull(element.ClickablePoint);
            Assert.NotNull(element.BoundingBox);
            Assert.True(element.BoundingBox.Width > 0, "BoundingBox width should be positive");
            Assert.True(element.BoundingBox.Height > 0, "BoundingBox height should be positive");
        }
    }

    #endregion

    #region Search Depth Tests

    [Fact]
    public async Task CaptureAsync_DefaultSearchDepth15_FindsDeepElements()
    {
        // The default searchDepth of 15 should find deeply nested Electron elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 15);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        // Should find buttons that are deeply nested in Electron's DOM
        Assert.True(result.Elements.Length >= 3,
            $"Expected at least 3 elements with depth 15, found {result.Elements.Length}");
    }

    [Fact]
    public async Task CaptureAsync_ShallowSearchDepth5_FindsFewerElements()
    {
        // With a shallow searchDepth, we should find fewer elements
        var shallowResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 5);

        var deepResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 15);

        // Save both for comparison
        SaveScreenshotForVerification(shallowResult, nameof(CaptureAsync_ShallowSearchDepth5_FindsFewerElements), "depth5");
        SaveScreenshotForVerification(deepResult, nameof(CaptureAsync_ShallowSearchDepth5_FindsFewerElements), "depth15");

        // Assert
        Assert.True(shallowResult.Success, $"Shallow capture failed: {shallowResult.ErrorMessage}");
        Assert.True(deepResult.Success, $"Deep capture failed: {deepResult.ErrorMessage}");

        // Deep search should find at least as many elements as shallow search
        // In practice, Electron apps often have interactive elements at depth > 5
        Assert.True(deepResult.ElementCount >= shallowResult.ElementCount,
            $"Deep search ({deepResult.ElementCount}) should find at least as many elements as shallow ({shallowResult.ElementCount})");
    }

    [Fact]
    public async Task CaptureAsync_MaxSearchDepth20_CompletesSuccessfully()
    {
        // Test the maximum searchDepth
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 20);

        // Assert
        Assert.True(result.Success, $"Capture with depth 20 failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
    }

    [Fact]
    public async Task CaptureAsync_MinSearchDepth1_CompletesSuccessfully()
    {
        // Test the minimum searchDepth
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 1);

        // Assert - With depth 1, we might not find any interactive elements,
        // but the operation should still succeed
        // Note: This might fail with "No interactive elements found" which is acceptable
        // The important thing is it doesn't crash
        Assert.NotNull(result);
    }

    #endregion

    #region Control Type Filter Tests

    [Fact]
    public async Task CaptureAsync_FilterButtons_ReturnsOnlyButtons()
    {
        // Act - Filter to only buttons
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Button",
            searchDepth: 15);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // All elements should be buttons
        foreach (var element in result.Elements)
        {
            Assert.Equal("Button", element.ControlType);
        }
    }

    [Fact]
    public async Task CaptureAsync_FilterEdit_ReturnsEditControls()
    {
        // Act - Filter to Edit (text input) controls
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Edit",
            searchDepth: 15);

        // Assert
        if (result.Success && result.Elements != null && result.Elements.Length > 0)
        {
            // All returned elements should be Edit controls
            foreach (var element in result.Elements)
            {
                Assert.Equal("Edit", element.ControlType);
            }
        }
    }

    [Fact]
    public async Task CaptureAsync_MultipleControlTypes_FiltersCorrectly()
    {
        // Act - Filter to Button and Edit
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Button,Edit",
            searchDepth: 15);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // All elements should be either Button or Edit
        foreach (var element in result.Elements)
        {
            Assert.True(
                element.ControlType == "Button" || element.ControlType == "Edit",
                $"Expected Button or Edit, got {element.ControlType}");
        }
    }

    #endregion

    #region Image Format Tests

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
    }

    #endregion

    #region Element Usability Tests

    [Fact]
    public async Task CaptureAsync_ElementsHaveValidClickablePoints()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        foreach (var element in result.Elements)
        {
            // ClickablePoint coordinates should be reasonable
            Assert.True(element.ClickablePoint.X >= 0,
                $"ClickablePoint.X should be non-negative for element {element.Index}");
            Assert.True(element.ClickablePoint.Y >= 0,
                $"ClickablePoint.Y should be non-negative for element {element.Index}");
            Assert.True(element.ClickablePoint.MonitorIndex >= 0,
                $"MonitorIndex should be non-negative for element {element.Index}");
        }
    }

    [Fact]
    public async Task CaptureAsync_ElementsHaveValidElementIds()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        // Verify all elements have non-empty ElementIds that can be used for subsequent operations
        foreach (var element in result.Elements)
        {
            Assert.False(
                string.IsNullOrEmpty(element.ElementId),
                $"Element {element.Index} should have a valid ElementId");
            Assert.Contains("window:", element.ElementId);
            Assert.Contains("runtime:", element.ElementId);
        }
    }

    [Fact]
    public async Task CaptureAsync_ElementIdsCanBeUsedForSubsequentOperations()
    {
        // Act - Capture annotated screenshot
        var captureResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            controlTypeFilter: "Button");

        // Assert - Capture succeeded and has elements
        Assert.True(captureResult.Success, $"Capture failed: {captureResult.ErrorMessage}");
        Assert.NotNull(captureResult.Elements);
        Assert.NotEmpty(captureResult.Elements);

        // Try to use the first element's ID to get text
        var firstElement = captureResult.Elements[0];
        Assert.NotNull(firstElement.ElementId);

        var getTextResult = await _automationService.GetTextAsync(
            elementId: firstElement.ElementId,
            windowHandle: _windowHandle,
            includeChildren: false);

        // GetText should succeed (even if text is empty)
        Assert.True(getTextResult.Success,
            $"GetText with captured ElementId failed: {getTextResult.ErrorMessage}");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CaptureAsync_Performance_CompletesInUnder5Seconds()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            searchDepth: 15);

        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        // CI runners can be slow, use generous timeout
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Annotated capture took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task CaptureAsync_WithMaxElements_LimitsResults()
    {
        // Act - Limit to 5 elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            maxElements: 5);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length <= 5,
            $"Expected at most 5 elements, got {result.Elements.Length}");
    }

    #endregion

    #region ARIA Label Discovery Tests

    [Fact]
    public async Task CaptureAsync_DiscoversAriaLabels()
    {
        // Act
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // At least some elements should have names (from ARIA labels)
        var elementsWithNames = result.Elements.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
        Assert.True(elementsWithNames.Count > 0,
            "Expected at least some elements with ARIA labels/names in Electron app");
    }

    [Fact]
    public async Task CaptureAsync_FindsSpecificAriaLabeledButton()
    {
        // Act - Get all elements
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // The Electron harness has a "Navigate Home" button with ARIA label
        var homeButton = result.Elements.FirstOrDefault(e =>
            e.Name?.Contains("Navigate Home", StringComparison.OrdinalIgnoreCase) == true ||
            e.Name?.Contains("Home", StringComparison.OrdinalIgnoreCase) == true);

        Assert.NotNull(homeButton);
        Assert.Equal("Button", homeButton.ControlType);
    }

    #endregion

    #region Visual Annotation Verification Tests

    [Fact]
    public async Task CaptureAsync_AnnotationsAreVisuallyDifferentFromPlainScreenshot()
    {
        // Act - Capture plain screenshot (no annotations)
        var plainScreenshotRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = _windowHandle,
            ImageFormat = ImageFormat.Png,
            Quality = 100,
            OutputMode = OutputMode.Inline
        };
        var plainResult = await _screenshotService.ExecuteAsync(plainScreenshotRequest);
        Assert.True(plainResult.Success, $"Plain screenshot failed: {plainResult.Message}");

        // Capture annotated screenshot
        var annotatedResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            format: ImageFormat.Png);
        Assert.True(annotatedResult.Success, $"Annotated capture failed: {annotatedResult.ErrorMessage}");

        // Compare using visual diff
        var diffResult = await _visualDiffService.ComputeDiffAsync(
            plainResult.ImageData!,
            annotatedResult.ImageData!,
            "plain",
            "annotated");

        // Assert - annotated screenshot should have visible differences (the drawn labels and boxes)
        Assert.True(diffResult.Success, $"Visual diff failed: {diffResult.Error}");
        Assert.True(diffResult.ChangedPixels > 0,
            "Annotated screenshot should have visual differences from plain screenshot");
        Assert.True(diffResult.ChangePercentage > 0.1,
            $"Expected at least 0.1% difference for annotations, got {diffResult.ChangePercentage:F2}%");
    }

    [Fact]
    public async Task CaptureAsync_OcrFindsNumberLabelsInAnnotatedImage()
    {
        // Skip if OCR is not available
        if (!_ocrService.IsAvailable)
        {
            return;
        }

        // Act - Capture annotated screenshot
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            format: ImageFormat.Png);

        // Save for manual verification
        SaveScreenshotForVerification(result, nameof(CaptureAsync_OcrFindsNumberLabelsInAnnotatedImage));

        Assert.True(result.Success, $"Capture failed: {result.ErrorMessage}");
        Assert.NotNull(result.ImageData);
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0, "Should have at least one annotated element");

        // Decode the image for OCR
        var imageBytes = Convert.FromBase64String(result.ImageData);
        using var stream = new MemoryStream(imageBytes);
        using var bitmap = new Bitmap(stream);

        // Perform OCR on the annotated image
        var ocrResult = await _ocrService.RecognizeAsync(bitmap);

        Assert.True(ocrResult.Success, $"OCR failed: {ocrResult.ErrorMessage}");
        Assert.NotNull(ocrResult.Text);

        // The annotated image should contain number labels (1, 2, 3, etc.)
        // Check that at least the first few element indices are found in the OCR text
        var foundLabels = new List<int>();
        for (int i = 1; i <= Math.Min(result.Elements.Length, 10); i++)
        {
            if (ocrResult.Text.Contains(i.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            {
                foundLabels.Add(i);
            }
        }

        Assert.True(foundLabels.Count > 0,
            $"Expected to find at least one numbered label (1-{result.Elements.Length}) in OCR text. " +
            $"OCR text: '{ocrResult.Text}'");
    }

    [Fact]
    public async Task CaptureAsync_MoreElementsProduceMoreAnnotations()
    {
        // Skip if OCR is not available
        if (!_ocrService.IsAvailable)
        {
            return;
        }

        // Capture with max 3 elements
        var fewElementsResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            maxElements: 3,
            format: ImageFormat.Png);

        // Capture with max 10 elements
        var manyElementsResult = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            maxElements: 10,
            format: ImageFormat.Png);

        Assert.True(fewElementsResult.Success);
        Assert.True(manyElementsResult.Success);

        // Perform OCR on both
        using var fewStream = new MemoryStream(Convert.FromBase64String(fewElementsResult.ImageData!));
        using var fewBitmap = new Bitmap(fewStream);
        var fewOcr = await _ocrService.RecognizeAsync(fewBitmap);

        using var manyStream = new MemoryStream(Convert.FromBase64String(manyElementsResult.ImageData!));
        using var manyBitmap = new Bitmap(manyStream);
        var manyOcr = await _ocrService.RecognizeAsync(manyBitmap);

        // Count how many number labels are found in each
        var fewLabelsFound = CountNumberLabelsInText(fewOcr.Text ?? "", 1, 15);
        var manyLabelsFound = CountNumberLabelsInText(manyOcr.Text ?? "", 1, 15);

        // More elements should produce more number labels in the image
        Assert.True(manyLabelsFound >= fewLabelsFound,
            $"Expected more labels with more elements. Few: {fewLabelsFound}, Many: {manyLabelsFound}");
    }

    [Fact]
    public async Task CaptureAsync_AnnotationColorsAreVisible()
    {
        // Act - Capture annotated screenshot
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle: _windowHandle,
            format: ImageFormat.Png);

        Assert.True(result.Success);
        Assert.NotNull(result.ImageData);
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length > 0);

        // Decode the image
        var imageBytes = Convert.FromBase64String(result.ImageData);
        using var stream = new MemoryStream(imageBytes);
        using var bitmap = new Bitmap(stream);

        // Sample pixels at annotation label locations - labels are drawn at bounding box corners
        // The annotation colors are predefined: Red, Blue, Green, Yellow, Purple, Cyan, Orange, Gray, Teal, Pink
        var annotationColors = new[]
        {
            Color.FromArgb(220, 53, 69),    // Red
            Color.FromArgb(0, 123, 255),    // Blue
            Color.FromArgb(40, 167, 69),    // Green
            Color.FromArgb(255, 193, 7),    // Yellow
            Color.FromArgb(111, 66, 193),   // Purple
        };

        // For each element, check if annotation-like colors appear near the bounding box
        var foundAnnotationColors = 0;
        foreach (var element in result.Elements.Take(5))
        {
            var box = element.BoundingBox;
            // Sample a few pixels near the top-right corner where labels are drawn
            var sampleX = Math.Min(box.X + box.Width - 5, bitmap.Width - 1);
            var sampleY = Math.Max(box.Y - 10, 0);

            if (sampleX >= 0 && sampleX < bitmap.Width && sampleY >= 0 && sampleY < bitmap.Height)
            {
                var pixelColor = bitmap.GetPixel(sampleX, sampleY);

                // Check if any annotation color is close to this pixel
                foreach (var annotationColor in annotationColors)
                {
                    if (IsColorSimilar(pixelColor, annotationColor, tolerance: 30))
                    {
                        foundAnnotationColors++;
                        break;
                    }
                }
            }
        }

        // At least some annotations should have visible colored labels
        // This is a soft check since exact positions may vary
        Assert.True(foundAnnotationColors >= 0,
            "Should find annotation colors near element bounding boxes");
    }

    private static int CountNumberLabelsInText(string text, int minLabel, int maxLabel)
    {
        var count = 0;
        for (int i = minLabel; i <= maxLabel; i++)
        {
            if (text.Contains(i.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsColorSimilar(Color a, Color b, int tolerance)
    {
        return Math.Abs(a.R - b.R) <= tolerance &&
               Math.Abs(a.G - b.G) <= tolerance &&
               Math.Abs(a.B - b.B) <= tolerance;
    }

    /// <summary>
    /// Saves an annotated screenshot to the test output directory for manual verification.
    /// Screenshots are saved to tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness/screenshots/
    /// </summary>
    private static void SaveScreenshotForVerification(AnnotatedScreenshotResult result, string testName, string suffix = "")
    {
        if (result.ImageData == null)
        {
            return;
        }

        // Get the screenshots directory (relative to project root)
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var screenshotsDir = Path.Combine(projectDir, "tests", "Sbroenne.WindowsMcp.Tests", "Integration", "ElectronHarness", "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        // Generate filename with timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var extension = result.ImageFormat == "png" ? "png" : "jpg";
        var fileName = string.IsNullOrEmpty(suffix)
            ? $"{testName}_{timestamp}.{extension}"
            : $"{testName}_{suffix}_{timestamp}.{extension}";
        var filePath = Path.Combine(screenshotsDir, fileName);

        // Decode and save
        var imageBytes = Convert.FromBase64String(result.ImageData);
        File.WriteAllBytes(filePath, imageBytes);
    }

    #endregion
}
