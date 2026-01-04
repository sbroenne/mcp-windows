using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for capturing screenshots of screens, monitors, windows, and regions.
/// </summary>
[McpServerToolType]
public sealed partial class ScreenshotControlTool
{
    private readonly ScreenshotService _screenshotService;
    private readonly AnnotatedScreenshotService _annotatedScreenshotService;
    private readonly WindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotControlTool"/> class.
    /// </summary>
    /// <param name="screenshotService">The screenshot capture service.</param>
    /// <param name="annotatedScreenshotService">The annotated screenshot service for element discovery.</param>
    /// <param name="windowService">The window service for finding windows by title.</param>
    public ScreenshotControlTool(
        ScreenshotService screenshotService,
        AnnotatedScreenshotService annotatedScreenshotService,
        WindowService windowService)
    {
        _screenshotService = screenshotService;
        _annotatedScreenshotService = annotatedScreenshotService;
        _windowService = windowService;
    }

    /// <summary>
    /// Captures screenshots of screens, monitors, windows, or regions on Windows.
    /// </summary>
    /// <remarks>
    /// **COORDINATE SYSTEM**: Screenshot pixel coordinates = mouse coordinates. No conversion needed!
    /// If you see a button at pixel (450, 300) in the screenshot, use mouse_control(x=450, y=300, monitorIndex=N).
    ///
    /// Returns base64-encoded image data (JPEG by default, configurable via imageFormat parameter).
    /// Default: JPEG format at quality 60 (LLM-optimized), at logical resolution (matching mouse coordinate space).
    ///
    /// **ANNOTATION MODE** (annotate=true, default):
    /// - Returns element list with index, name, controlType, boundingRect, and elementId
    /// - By default, image data is NOT included (saves ~100K+ tokens per call!)
    /// - Element metadata provides all information needed for UI automation
    /// - Set includeImage=true only if you need to visually inspect the screenshot
    /// - Elements can be used directly with ui_automation actions using elementId
    ///
    /// **PLAIN SCREENSHOT** (annotate=false):
    /// - Returns full image data without element discovery
    /// - Use when you need raw visual inspection without structured elements
    ///
    /// Monitor targeting:
    /// - 'primary_screen': Captures the main display (with taskbar). Most common choice.
    /// - 'secondary_screen': Captures the other monitor. Only works with exactly 2 monitors.
    /// - 'monitor' with monitorIndex: For 3+ monitors, use list_monitors first to find the index.
    /// - 'all_monitors': Captures all monitors as a single composite image.
    ///
    /// The list_monitors action returns display_number (matches Windows Settings) and is_primary flag.
    /// Respects secure desktop (UAC/lock screen) restrictions.
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The action to perform. Valid values: 'capture' (take screenshot), 'list_monitors' (enumerate displays). Default: 'capture'.</param>
    /// <param name="annotate">Overlay numbered labels on interactive elements and return element list. Use this when you need to discover UI elements before interacting.</param>
    /// <param name="target">Capture target. Valid values: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor, only for 2-monitor setups), 'monitor' (by index for 3+ monitors), 'window' (by handle), 'region' (by coordinates), 'all_monitors' (composite of all displays). Default: 'primary_screen'.</param>
    /// <param name="monitorIndex">Monitor index for 'monitor' target (0-based). Use 'list_monitors' to get available indices.</param>
    /// <param name="windowHandle">Window handle (HWND) as a decimal string for 'window' target. Get from window_management tool output and pass it through verbatim.</param>
    /// <param name="regionX">X coordinate (left) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionY">Y coordinate (top) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionWidth">Width in pixels for 'region' target. Must be positive.</param>
    /// <param name="regionHeight">Height in pixels for 'region' target. Must be positive.</param>
    /// <param name="includeCursor">Include mouse cursor in capture. Default: false.</param>
    /// <param name="imageFormat">Screenshot format: 'jpeg'/'jpg' or 'png'. Default: 'jpeg' (LLM-optimized).</param>
    /// <param name="quality">Image compression quality 1-100. Default: 60 (LLM-optimized). Only affects JPEG format.</param>
    /// <param name="outputMode">How to return the screenshot. 'inline' returns base64 data, 'file' saves to disk and returns path. Default: 'inline'.</param>
    /// <param name="outputPath">Directory or file path for output when outputMode is 'file'. If directory, auto-generates filename. If null, uses temp directory.</param>
    /// <param name="includeImage">Include the image in the response. Default: false for annotated screenshots (element metadata is sufficient). Set true to see the actual screenshot pixels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing base64-encoded image data or file path, dimensions, original dimensions (if scaled), file size, and error details if failed.</returns>
    [McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<ScreenshotControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        string? action = null,
        bool annotate = true,
        string? target = null,
        int? monitorIndex = null,
        string? windowHandle = null,
        int? regionX = null,
        int? regionY = null,
        int? regionWidth = null,
        int? regionHeight = null,
        bool includeCursor = false,
        string? imageFormat = null,
        int? quality = null,
        string? outputMode = null,
        string? outputPath = null,
        bool? includeImage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("ScreenshotControl");
        clientLogger?.LogScreenshotOperationStarted(action ?? "capture", target ?? "primary_screen");

        // Parse action
        var screenshotAction = ParseAction(action);
        if (screenshotAction == null)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid action: '{action}'. Valid values: 'capture', 'list_monitors'");
        }

        // Parse target
        var captureTarget = ParseTarget(target);
        if (captureTarget == null && screenshotAction == ScreenshotAction.Capture)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid target: '{target}'. Valid values: 'primary_screen', 'secondary_screen', 'monitor', 'window', 'region', 'all_monitors'");
        }

        // Parse and validate image format
        var parsedImageFormat = ParseImageFormat(imageFormat);
        if (parsedImageFormat == null && imageFormat != null)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid imageFormat: '{imageFormat}'. Valid values: 'jpeg', 'jpg', 'png'");
        }

        // Validate quality
        var parsedQuality = quality ?? ScreenshotConfiguration.DefaultQuality;
        if (parsedQuality < 1 || parsedQuality > 100)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Quality must be between 1 and 100, got: {parsedQuality}");
        }

        // Parse and validate output mode
        var parsedOutputMode = ParseOutputMode(outputMode);
        if (parsedOutputMode == null && outputMode != null)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid outputMode: '{outputMode}'. Valid values: 'inline', 'file'");
        }

        // Validate output path if provided
        if (outputPath != null)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                return ScreenshotControlResult.Error(
                    ScreenshotErrorCode.InvalidRequest,
                    $"Output directory does not exist: '{directory}'");
            }
        }

        // Build region if target is region
        CaptureRegion? region = null;
        if (captureTarget == CaptureTarget.Region)
        {
            if (regionX == null || regionY == null || regionWidth == null || regionHeight == null)
            {
                return ScreenshotControlResult.Error(
                    ScreenshotErrorCode.InvalidRegion,
                    "Region capture requires regionX, regionY, regionWidth, and regionHeight parameters");
            }

            region = new CaptureRegion(regionX.Value, regionY.Value, regionWidth.Value, regionHeight.Value);
        }

        // Handle annotated screenshot mode
        if (annotate && screenshotAction == ScreenshotAction.Capture)
        {
            // Default: don't include image for annotated screenshots (element metadata is sufficient)
            // This saves ~100K+ tokens per screenshot
            var shouldIncludeImage = includeImage ?? false;
            return await CaptureAnnotatedAsync(windowHandle, parsedImageFormat, parsedQuality, parsedOutputMode, outputPath, shouldIncludeImage, cancellationToken);
        }

        // Build request with all parameters
        var request = new ScreenshotControlRequest
        {
            Action = screenshotAction.Value,
            Target = captureTarget ?? CaptureTarget.PrimaryScreen,
            MonitorIndex = monitorIndex,
            WindowHandle = windowHandle,
            Region = region,
            IncludeCursor = includeCursor,
            ImageFormat = parsedImageFormat ?? ScreenshotConfiguration.DefaultImageFormat,
            Quality = parsedQuality,
            OutputMode = parsedOutputMode ?? ScreenshotConfiguration.DefaultOutputMode,
            OutputPath = outputPath
        };

        // Execute and return result
        var result = await _screenshotService.ExecuteAsync(request, cancellationToken);
        return result;
    }

    /// <summary>
    /// Parses the action string to enum.
    /// </summary>
    private static ScreenshotAction? ParseAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return ScreenshotAction.Capture;
        }

        return action.ToLowerInvariant() switch
        {
            "capture" => ScreenshotAction.Capture,
            "list_monitors" => ScreenshotAction.ListMonitors,
            _ => null
        };
    }

    /// <summary>
    /// Parses the target string to enum.
    /// </summary>
    private static CaptureTarget? ParseTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return CaptureTarget.PrimaryScreen;
        }

        return target.ToLowerInvariant() switch
        {
            "primary_screen" or "primaryscreen" or "primary" => CaptureTarget.PrimaryScreen,
            "secondary_screen" or "secondaryscreen" or "secondary" => CaptureTarget.SecondaryScreen,
            "monitor" => CaptureTarget.Monitor,
            "window" => CaptureTarget.Window,
            "region" => CaptureTarget.Region,
            "all_monitors" or "allmonitors" => CaptureTarget.AllMonitors,
            _ => null
        };
    }

    /// <summary>
    /// Parses the image format string to enum.
    /// </summary>
    private static Models.ImageFormat? ParseImageFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null; // Will use default
        }

        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => Models.ImageFormat.Jpeg,
            "png" => Models.ImageFormat.Png,
            _ => null
        };
    }

    /// <summary>
    /// Parses the output mode string to enum.
    /// </summary>
    private static OutputMode? ParseOutputMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null; // Will use default
        }

        return mode.ToLowerInvariant() switch
        {
            "inline" => OutputMode.Inline,
            "file" => OutputMode.File,
            _ => null
        };
    }

    /// <summary>
    /// Captures an annotated screenshot with numbered element labels.
    /// </summary>
    private async Task<ScreenshotControlResult> CaptureAnnotatedAsync(
        string? windowHandle,
        Models.ImageFormat? imageFormat,
        int quality,
        OutputMode? outputMode,
        string? outputPath,
        bool includeImage,
        CancellationToken cancellationToken)
    {
        var format = imageFormat ?? ScreenshotConfiguration.DefaultImageFormat;
        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle,
            controlTypeFilter: null,
            maxElements: 50,
            searchDepth: 15,
            format,
            quality,
            interactiveOnly: true,
            cancellationToken);

        if (!result.Success)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.CaptureError,
                result.ErrorMessage ?? "Failed to capture annotated screenshot");
        }

        // Save to file if outputPath or outputMode is file
        string? savedFilePath = null;
        var shouldSaveToFile = outputMode == OutputMode.File || !string.IsNullOrEmpty(outputPath);

        if (shouldSaveToFile && !string.IsNullOrEmpty(result.ImageData))
        {
            try
            {
                var imageBytes = Convert.FromBase64String(result.ImageData);
                var filePath = outputPath ?? Path.Combine(Path.GetTempPath(), $"annotated_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                // If outputPath is a directory, generate filename
                if (Directory.Exists(outputPath))
                {
                    filePath = Path.Combine(outputPath, $"annotated_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
                savedFilePath = filePath;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ScreenshotControlResult.Error(
                    ScreenshotErrorCode.CaptureError,
                    $"Failed to save annotated image to '{outputPath}': {ex.Message}");
            }
        }

        // Determine if we should return image data inline
        // By default, don't include image to save tokens (element metadata is sufficient for LLM)
        var returnImageInline = includeImage && outputMode != OutputMode.File && savedFilePath == null;

        return ScreenshotControlResult.AnnotatedSuccess(
            returnImageInline ? result.ImageData : null,
            result.Width ?? 0,
            result.Height ?? 0,
            result.ImageFormat ?? "jpeg",
            result.Elements ?? [],
            savedFilePath,
            result.OriginalWidth,
            result.OriginalHeight);
    }
}
