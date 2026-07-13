using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for capturing screenshots of screens, monitors, windows, and regions.
/// </summary>
[McpServerToolType]
public static partial class ScreenshotControlTool
{
    // Default screenshot settings
    private const int DefaultQuality = 60;
    private const Models.ImageFormat DefaultImageFormat = Models.ImageFormat.Jpeg;
    private const OutputMode DefaultOutputMode = OutputMode.Inline;
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
    /// - Reference elements by name/type with ui_click and ui_type, or by their click coordinates with mouse_control
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
    /// <returns>A call result containing the screenshot as an inline image content block (when returned inline) plus a JSON text block with dimensions, file size or path, annotated elements, and error details if failed.</returns>
    [McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        [DefaultValue(null)] string? action,
        [DefaultValue(true)] bool annotate,
        [DefaultValue(null)] string? target,
        [DefaultValue(null)] int? monitorIndex,
        [DefaultValue(null)] string? windowHandle,
        [DefaultValue(null)] int? regionX,
        [DefaultValue(null)] int? regionY,
        [DefaultValue(null)] int? regionWidth,
        [DefaultValue(null)] int? regionHeight,
        [DefaultValue(false)] bool includeCursor,
        [DefaultValue(null)] string? imageFormat,
        [DefaultValue(null)] int? quality,
        [DefaultValue(null)] string? outputMode,
        [DefaultValue(null)] string? outputPath,
        [DefaultValue(false)] bool includeImage,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse action
            var screenshotAction = ParseAction(action);
            if (screenshotAction == null)
            {
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.InvalidRequest,
                        $"Invalid action: '{action}'. Valid values: 'capture', 'list_monitors'"));
            }

            // Parse target
            var captureTarget = ParseTarget(target);
            if (captureTarget == null && screenshotAction == ScreenshotAction.Capture)
            {
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.InvalidRequest,
                        $"Invalid target: '{target}'. Valid values: 'primary_screen', 'secondary_screen', 'monitor', 'window', 'region', 'all_monitors'"));
            }

            // Parse and validate image format
            var parsedImageFormat = ParseImageFormat(imageFormat);
            if (parsedImageFormat == null && imageFormat != null)
            {
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.InvalidRequest,
                        $"Invalid imageFormat: '{imageFormat}'. Valid values: 'jpeg', 'jpg', 'png'"));
            }

            // Validate quality
            var parsedQuality = quality ?? DefaultQuality;
            if (parsedQuality < 1 || parsedQuality > 100)
            {
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.InvalidRequest,
                        $"Quality must be between 1 and 100, got: {parsedQuality}"));
            }

            // Parse and validate output mode
            var parsedOutputMode = ParseOutputMode(outputMode);
            if (parsedOutputMode == null && outputMode != null)
            {
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.InvalidRequest,
                        $"Invalid outputMode: '{outputMode}'. Valid values: 'inline', 'file'"));
            }

            // Validate output path if provided
            if (outputPath != null)
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return ToCallToolResult(
                        ScreenshotControlResult.Error(
                            ScreenshotErrorCode.InvalidRequest,
                            $"Output directory does not exist: '{directory}'"));
                }
            }

            // Build region if target is region
            CaptureRegion? region = null;
            if (captureTarget == CaptureTarget.Region)
            {
                if (regionX == null || regionY == null || regionWidth == null || regionHeight == null)
                {
                    return ToCallToolResult(
                        ScreenshotControlResult.Error(
                            ScreenshotErrorCode.InvalidRegion,
                            "Region capture requires regionX, regionY, regionWidth, and regionHeight parameters"));
                }

                region = new CaptureRegion(regionX.Value, regionY.Value, regionWidth.Value, regionHeight.Value);
            }

            // Handle annotated screenshot mode
            if (annotate && screenshotAction == ScreenshotAction.Capture)
            {
                // Default: don't include image for annotated screenshots (element metadata is sufficient)
                // This saves ~100K+ tokens per screenshot
                var shouldIncludeImage = includeImage;
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
                ImageFormat = parsedImageFormat ?? DefaultImageFormat,
                Quality = parsedQuality,
                OutputMode = parsedOutputMode ?? DefaultOutputMode,
                OutputPath = outputPath
            };

            // Execute and return result
            var result = await WindowsToolsBase.ScreenshotService.ExecuteAsync(request, cancellationToken);
            return ToCallToolResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ErrorResult(WindowsToolsBase.SerializeToolError("screenshot_control", ex));
        }
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
    private static async Task<CallToolResult> CaptureAnnotatedAsync(
        string? windowHandle,
        Models.ImageFormat? imageFormat,
        int quality,
        OutputMode? outputMode,
        string? outputPath,
        bool includeImage,
        CancellationToken cancellationToken)
    {
        var format = imageFormat ?? DefaultImageFormat;
        var result = await WindowsToolsBase.AnnotatedScreenshotService.CaptureAsync(
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
            return ToCallToolResult(
                ScreenshotControlResult.Error(
                    ScreenshotErrorCode.CaptureError,
                    result.ErrorMessage ?? "Failed to capture annotated screenshot"));
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
                return ToCallToolResult(
                    ScreenshotControlResult.Error(
                        ScreenshotErrorCode.CaptureError,
                        $"Failed to save annotated image to '{outputPath}': {ex.Message}"));
            }
        }

        // Determine if we should return image data inline
        // By default, don't include image to save tokens (element metadata is sufficient for LLM)
        var returnImageInline = includeImage && outputMode != OutputMode.File && savedFilePath == null;

        return ToCallToolResult(
            ScreenshotControlResult.AnnotatedSuccess(
                returnImageInline ? result.ImageData : null,
                result.Width ?? 0,
                result.Height ?? 0,
                result.ImageFormat ?? "jpeg",
                result.Elements ?? [],
                savedFilePath,
                result.OriginalWidth,
                result.OriginalHeight));
    }

    /// <summary>
    /// Converts a screenshot result into an MCP call result. Inline image data is
    /// emitted as a dedicated image content block so the model receives rendered
    /// pixels instead of a base64 text blob; the remaining metadata travels as a
    /// JSON text block with the base64 payload stripped to avoid duplicating it.
    /// </summary>
    private static CallToolResult ToCallToolResult(ScreenshotControlResult result)
    {
        var content = new List<ContentBlock>();

        if (!string.IsNullOrEmpty(result.ImageData))
        {
            content.Add(ImageContentBlock.FromBytes(
                Convert.FromBase64String(result.ImageData),
                ToMimeType(result.Format)));
        }

        // Strip the base64 payload from the metadata: it now travels as the image block above.
        var metadata = result with { ImageData = null };
        content.Add(new TextContentBlock
        {
            Text = JsonSerializer.Serialize(metadata, WindowsToolsBase.JsonOptions)
        });

        return new CallToolResult
        {
            Content = content,
            IsError = !result.Success
        };
    }

    /// <summary>
    /// Wraps a pre-serialized JSON error payload in a failed call result.
    /// </summary>
    private static CallToolResult ErrorResult(string json) =>
        new()
        {
            Content = [new TextContentBlock { Text = json }],
            IsError = true
        };

    /// <summary>
    /// Maps an internal image format ("jpeg"/"png") to its MIME type.
    /// </summary>
    private static string ToMimeType(string? format) =>
        string.Equals(format, "png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";
}
