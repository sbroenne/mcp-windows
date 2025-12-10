using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for LLM-optimized screenshot capture features.
/// Tests the new default behavior (JPEG format, quality 85, auto-scaling to 1568px)
/// and the explicit parameter overrides.
/// </summary>
public sealed class ScreenshotLlmOptimizationTests : IDisposable
{
    private readonly ScreenshotService _screenshotService;
    private readonly MonitorService _monitorService;
    private readonly List<string> _createdFiles = [];

    public ScreenshotLlmOptimizationTests()
    {
        _monitorService = new MonitorService();
        var secureDesktopDetector = new SecureDesktopDetector();
        var imageProcessor = new ImageProcessor();
        var configuration = ScreenshotConfiguration.FromEnvironment();
        var logger = new ScreenshotOperationLogger(NullLogger<ScreenshotOperationLogger>.Instance);

        _screenshotService = new ScreenshotService(
            _monitorService,
            secureDesktopDetector,
            imageProcessor,
            configuration,
            logger);
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
    }

    #region US1 - Default JPEG Format Tests

    /// <summary>
    /// T021 [US1]: Default capture returns JPEG format.
    /// </summary>
    [Fact]
    public async Task Capture_DefaultParameters_ReturnsJpegFormat()
    {
        // Arrange - use default parameters (no format specified)
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.Message}");
        Assert.Equal("jpeg", result.Format);
        Assert.NotNull(result.ImageData);

        // Verify JPEG signature (FFD8)
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length >= 2, "Image data too short");
        Assert.Equal(0xFF, imageBytes[0]);
        Assert.Equal(0xD8, imageBytes[1]);
    }

    /// <summary>
    /// T022 [US1]: Explicit PNG format override returns PNG.
    /// </summary>
    [Fact]
    public async Task Capture_ExplicitPngFormat_ReturnsPngFormat()
    {
        // Arrange - explicitly request PNG
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.Message}");
        Assert.Equal("png", result.Format);
        Assert.NotNull(result.ImageData);

        // Verify PNG signature (89504E47)
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length >= 4, "Image data too short");
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]); // 'P'
        Assert.Equal(0x4E, imageBytes[2]); // 'N'
        Assert.Equal(0x47, imageBytes[3]); // 'G'
    }

    /// <summary>
    /// T023 [US1]: Custom quality parameter affects output.
    /// </summary>
    [Fact]
    public async Task Capture_CustomQuality_AffectsFileSize()
    {
        // Arrange - capture with low quality vs high quality
        var lowQualityRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Jpeg,
            Quality = 20
        };

        var highQualityRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Jpeg,
            Quality = 95
        };

        // Act
        var lowQualityResult = await _screenshotService.ExecuteAsync(lowQualityRequest);
        var highQualityResult = await _screenshotService.ExecuteAsync(highQualityRequest);

        // Assert
        Assert.True(lowQualityResult.Success);
        Assert.True(highQualityResult.Success);

        var lowQualityBytes = Convert.FromBase64String(lowQualityResult.ImageData!);
        var highQualityBytes = Convert.FromBase64String(highQualityResult.ImageData!);

        // Lower quality should produce smaller file
        Assert.True(lowQualityBytes.Length < highQualityBytes.Length,
            $"Low quality ({lowQualityBytes.Length} bytes) should be smaller than high quality ({highQualityBytes.Length} bytes)");
    }

    /// <summary>
    /// T023b [US1]: Quality parameter is ignored for PNG format.
    /// </summary>
    [Fact]
    public async Task Capture_PngFormatWithQuality_QualityIgnored()
    {
        // Arrange - PNG with different quality values should produce same output
        var lowQualityRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png,
            Quality = 20,
            MaxWidth = 0 // Disable scaling to make comparison meaningful
        };

        var highQualityRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png,
            Quality = 95,
            MaxWidth = 0 // Disable scaling to make comparison meaningful
        };

        // Act
        var lowQualityResult = await _screenshotService.ExecuteAsync(lowQualityRequest);
        var highQualityResult = await _screenshotService.ExecuteAsync(highQualityRequest);

        // Assert - PNG is lossless, quality should have no effect
        Assert.True(lowQualityResult.Success);
        Assert.True(highQualityResult.Success);
        Assert.Equal("png", lowQualityResult.Format);
        Assert.Equal("png", highQualityResult.Format);

        // Both should be PNG (quality ignored)
        var lowQualityBytes = Convert.FromBase64String(lowQualityResult.ImageData!);
        var highQualityBytes = Convert.FromBase64String(highQualityResult.ImageData!);

        // File sizes should be similar (PNG ignores quality)
        // Allow 10% variance due to timing differences in screen content
        var sizeDiff = Math.Abs(lowQualityBytes.Length - highQualityBytes.Length);
        var maxSize = Math.Max(lowQualityBytes.Length, highQualityBytes.Length);
        Assert.True(sizeDiff < maxSize * 0.1,
            $"PNG sizes should be similar regardless of quality setting: {lowQualityBytes.Length} vs {highQualityBytes.Length}");
    }

    #endregion

    #region US2 - Auto-Scaling Tests

    /// <summary>
    /// T032 [US2]: Default capture auto-scales to 1568px width.
    /// </summary>
    [Fact]
    public async Task Capture_DefaultScaling_ScalesToDefaultMaxWidth()
    {
        // Arrange - default parameters (scaling enabled by default)
        var primaryMonitor = _monitorService.GetPrimaryMonitor();

        // Only meaningful if screen is larger than default max width
        if (primaryMonitor.Width <= ScreenshotConfiguration.DefaultMaxWidth)
        {
            return; // Skip if screen is smaller than default max
        }

        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ScreenshotConfiguration.DefaultMaxWidth, result.Width);

        // Original dimensions should be preserved in metadata
        Assert.Equal(primaryMonitor.Width, result.OriginalWidth);
        Assert.Equal(primaryMonitor.Height, result.OriginalHeight);
    }

    /// <summary>
    /// T033 [US2]: MaxWidth=0 disables auto-scaling.
    /// </summary>
    [Fact]
    public async Task Capture_MaxWidthZero_DisablesScaling()
    {
        // Arrange - explicitly disable scaling
        var primaryMonitor = _monitorService.GetPrimaryMonitor();
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            MaxWidth = 0 // Disable scaling
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(primaryMonitor.Width, result.Width);
        Assert.Equal(primaryMonitor.Height, result.Height);
    }

    /// <summary>
    /// T034 [US2]: Both MaxWidth and MaxHeight constraints work together.
    /// </summary>
    [Fact]
    public async Task Capture_BothMaxWidthAndMaxHeight_RespectsSmallestConstraint()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);

        // Capture a 400x300 region with constraints
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 400, 300),
            MaxWidth = 200,  // Would scale to 200x150
            MaxHeight = 100  // Would scale to 133x100 (more restrictive)
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);

        // Result should respect the more restrictive constraint
        Assert.True(result.Width <= 200, $"Width {result.Width} should be <= 200");
        Assert.True(result.Height <= 100, $"Height {result.Height} should be <= 100");

        // Original dimensions preserved
        Assert.Equal(400, result.OriginalWidth);
        Assert.Equal(300, result.OriginalHeight);
    }

    /// <summary>
    /// T035 [US2]: No upscaling occurs for small images.
    /// </summary>
    [Fact]
    public async Task Capture_SmallRegion_NoUpscaling()
    {
        // Arrange - capture a small region smaller than maxWidth
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 100, 75), // Smaller than default maxWidth
            MaxWidth = ScreenshotConfiguration.DefaultMaxWidth
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);

        // Should not upscale - dimensions should match original
        Assert.Equal(100, result.Width);
        Assert.Equal(75, result.Height);

        // OriginalWidth/OriginalHeight are only set when scaling occurs
        // When no scaling, they should be null (to avoid redundant data)
        Assert.Null(result.OriginalWidth);
        Assert.Null(result.OriginalHeight);
    }

    #endregion

    #region US3 - File Output Tests

    /// <summary>
    /// T044 [US3]: File output to temp directory works.
    /// </summary>
    [Fact]
    public async Task Capture_FileOutputMode_SavesFile()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            OutputMode = OutputMode.File
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success, $"Capture failed: {result.Message}");
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath), $"File should exist at {result.FilePath}");

        // Track for cleanup
        _createdFiles.Add(result.FilePath);

        // ImageData should be null when output to file
        Assert.Null(result.ImageData);

        // Verify file is valid JPEG
        var fileBytes = await File.ReadAllBytesAsync(result.FilePath);
        Assert.True(fileBytes.Length > 0);
        Assert.Equal(0xFF, fileBytes[0]);
        Assert.Equal(0xD8, fileBytes[1]);
    }

    /// <summary>
    /// T045 [US3]: File output with custom output path works.
    /// </summary>
    [Fact]
    public async Task Capture_FileOutputWithCustomPath_SavesFileToSpecifiedDirectory()
    {
        // Arrange
        var customDir = Path.Combine(Path.GetTempPath(), $"screenshot_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(customDir);

        try
        {
            var request = new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.PrimaryScreen,
                OutputMode = OutputMode.File,
                OutputPath = customDir
            };

            // Act
            var result = await _screenshotService.ExecuteAsync(request);

            // Assert
            Assert.True(result.Success, $"Capture failed: {result.Message}");
            Assert.NotNull(result.FilePath);
            Assert.StartsWith(customDir, result.FilePath);
            Assert.True(File.Exists(result.FilePath), $"File should exist at {result.FilePath}");

            // Track for cleanup
            _createdFiles.Add(result.FilePath);

            // File should be in the specified directory
            var actualDir = Path.GetDirectoryName(result.FilePath);
            Assert.Equal(customDir, actualDir);
        }
        finally
        {
            // Cleanup directory
            try
            {
                Directory.Delete(customDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// T046 [US3]: Invalid output path returns appropriate error.
    /// </summary>
    [Fact]
    public async Task Capture_FileOutputWithInvalidPath_ReturnsError()
    {
        // Arrange - use a path that doesn't exist
        var invalidPath = @"Z:\NonExistent\Directory\That\Should\Not\Exist";
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            OutputMode = OutputMode.File,
            OutputPath = invalidPath
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("path", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region US4 - Combined Optimization Tests

    /// <summary>
    /// T047 [US4]: Combined defaults work together (JPEG + scaling + inline).
    /// </summary>
    [Fact]
    public async Task Capture_CombinedDefaults_AllOptimizationsApplied()
    {
        // Arrange - use all defaults
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };
        var primaryMonitor = _monitorService.GetPrimaryMonitor();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);

        // 1. JPEG format
        Assert.Equal("jpeg", result.Format);

        // 2. Scaling applied (if screen is larger than 1568px)
        if (primaryMonitor.Width > ScreenshotConfiguration.DefaultMaxWidth)
        {
            Assert.Equal(ScreenshotConfiguration.DefaultMaxWidth, result.Width);
        }

        // 3. Inline mode (ImageData present, no FilePath)
        Assert.NotNull(result.ImageData);
        Assert.Null(result.FilePath);

        // 4. File size should be reasonable (under 500KB for JPEG at 1568px)
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.True(imageBytes.Length < 500_000,
            $"JPEG at LLM-optimized settings should be under 500KB, was {imageBytes.Length / 1024}KB");
    }

    /// <summary>
    /// T048 [US4]: Combined file output works (JPEG + scaling + file).
    /// </summary>
    [Fact]
    public async Task Capture_CombinedFileOutput_AllOptimizationsApplied()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            OutputMode = OutputMode.File
        };
        var primaryMonitor = _monitorService.GetPrimaryMonitor();

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);

        // 1. JPEG format
        Assert.Equal("jpeg", result.Format);

        // 2. Scaling applied (if screen is larger)
        if (primaryMonitor.Width > ScreenshotConfiguration.DefaultMaxWidth)
        {
            Assert.Equal(ScreenshotConfiguration.DefaultMaxWidth, result.Width);
        }

        // 3. File mode (FilePath present, no ImageData)
        Assert.NotNull(result.FilePath);
        Assert.Null(result.ImageData);

        // Track for cleanup
        _createdFiles.Add(result.FilePath);

        // 4. File exists and is valid JPEG
        Assert.True(File.Exists(result.FilePath));
        var fileBytes = await File.ReadAllBytesAsync(result.FilePath);
        Assert.Equal(0xFF, fileBytes[0]);
        Assert.Equal(0xD8, fileBytes[1]);

        // 5. File size should be reasonable
        Assert.True(fileBytes.Length < 500_000,
            $"JPEG file should be under 500KB, was {fileBytes.Length / 1024}KB");
    }

    /// <summary>
    /// T049 [US4]: Backward compatibility (PNG + no scaling).
    /// </summary>
    [Fact]
    public async Task Capture_BackwardCompatibility_PngWithNoScaling()
    {
        // Arrange - explicitly request PNG and disable scaling
        var primaryMonitor = _monitorService.GetPrimaryMonitor();
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen,
            ImageFormat = ImageFormat.Png,
            MaxWidth = 0 // Disable scaling
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert - should behave like original implementation
        Assert.True(result.Success);

        // 1. PNG format
        Assert.Equal("png", result.Format);

        // 2. No scaling (original dimensions)
        Assert.Equal(primaryMonitor.Width, result.Width);
        Assert.Equal(primaryMonitor.Height, result.Height);

        // 3. Inline mode
        Assert.NotNull(result.ImageData);
        Assert.Null(result.FilePath);

        // 4. Valid PNG
        var imageBytes = Convert.FromBase64String(result.ImageData);
        Assert.Equal(0x89, imageBytes[0]);
        Assert.Equal(0x50, imageBytes[1]);
    }

    /// <summary>
    /// T050 [US4]: All capture targets work with optimizations.
    /// </summary>
    [Fact]
    public async Task Capture_AllTargets_OptimizationsWorkCorrectly()
    {
        // Test each capture target with default optimizations

        // 1. PrimaryScreen
        var primaryRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };
        var primaryResult = await _screenshotService.ExecuteAsync(primaryRequest);
        Assert.True(primaryResult.Success, $"PrimaryScreen: {primaryResult.Message}");
        Assert.Equal("jpeg", primaryResult.Format);

        // 2. Monitor
        var monitorRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Monitor,
            MonitorIndex = 0
        };
        var monitorResult = await _screenshotService.ExecuteAsync(monitorRequest);
        Assert.True(monitorResult.Success, $"Monitor: {monitorResult.Message}");
        Assert.Equal("jpeg", monitorResult.Format);

        // 3. Region
        var (x, y) = TestMonitorHelper.GetTestCoordinates(0, 0);
        var regionRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Region,
            Region = new CaptureRegion(x, y, 200, 150)
        };
        var regionResult = await _screenshotService.ExecuteAsync(regionRequest);
        Assert.True(regionResult.Success, $"Region: {regionResult.Message}");
        Assert.Equal("jpeg", regionResult.Format);

        // 4. AllMonitors
        var allMonitorsRequest = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.AllMonitors
        };
        var allMonitorsResult = await _screenshotService.ExecuteAsync(allMonitorsRequest);
        Assert.True(allMonitorsResult.Success, $"AllMonitors: {allMonitorsResult.Message}");
        Assert.Equal("jpeg", allMonitorsResult.Format);
    }

    #endregion

    #region FileSizeBytes Tests

    /// <summary>
    /// Verify FileSizeBytes is populated in the result.
    /// </summary>
    [Fact]
    public async Task Capture_ReturnsFileSizeBytes()
    {
        // Arrange
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.PrimaryScreen
        };

        // Act
        var result = await _screenshotService.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FileSizeBytes);
        Assert.True(result.FileSizeBytes > 0);

        // Verify it matches actual data size
        var imageBytes = Convert.FromBase64String(result.ImageData!);
        Assert.Equal(imageBytes.Length, result.FileSizeBytes);
    }

    #endregion
}
