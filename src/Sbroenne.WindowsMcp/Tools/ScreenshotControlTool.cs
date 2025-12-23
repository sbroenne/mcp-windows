using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for capturing screenshots of screens, monitors, windows, and regions.
/// </summary>
[McpServerToolType]
public sealed partial class ScreenshotControlTool
{
    private readonly Capture.IScreenshotService _screenshotService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotControlTool"/> class.
    /// </summary>
    /// <param name="screenshotService">The screenshot capture service.</param>
    public ScreenshotControlTool(Capture.IScreenshotService screenshotService)
    {
        _screenshotService = screenshotService;
    }

    /// <summary>
    /// Captures screenshots of screens, monitors, windows, or regions on Windows.
    /// </summary>
    /// <remarks>
    /// **COORDINATE SYSTEM**: Screenshot pixel coordinates = mouse coordinates. No conversion needed!
    /// If you see a button at pixel (450, 300) in the screenshot, use mouse_control(x=450, y=300, monitorIndex=N).
    ///
    /// Returns base64-encoded image data (JPEG by default, configurable via imageFormat parameter).
    /// Default: JPEG format at quality 85, at logical resolution (matching mouse coordinate space).
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
    /// <param name="target">Capture target. Valid values: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor, only for 2-monitor setups), 'monitor' (by index for 3+ monitors), 'window' (by handle), 'region' (by coordinates), 'all_monitors' (composite of all displays). Default: 'primary_screen'.</param>
    /// <param name="monitorIndex">Monitor index for 'monitor' target (0-based). Use 'list_monitors' to get available indices.</param>
    /// <param name="windowHandle">Window handle (IntPtr value) for 'window' target. Get from window_management tool.</param>
    /// <param name="regionX">X coordinate (left) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionY">Y coordinate (top) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionWidth">Width in pixels for 'region' target. Must be positive.</param>
    /// <param name="regionHeight">Height in pixels for 'region' target. Must be positive.</param>
    /// <param name="includeCursor">Include mouse cursor in capture. Default: false.</param>
    /// <param name="imageFormat">Screenshot format: 'jpeg'/'jpg' or 'png'. Default: 'jpeg' (LLM-optimized).</param>
    /// <param name="quality">Image compression quality 1-100. Default: 85. Only affects JPEG format.</param>
    /// <param name="outputMode">How to return the screenshot. 'inline' returns base64 data, 'file' saves to disk and returns path. Default: 'inline'.</param>
    /// <param name="outputPath">Directory or file path for output when outputMode is 'file'. If directory, auto-generates filename. If null, uses temp directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing base64-encoded image data or file path, dimensions, original dimensions (if scaled), file size, and error details if failed.</returns>
    [McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true, UseStructuredContent = true)]
    [Description("Captures screenshots of screens, monitors, windows, or regions on Windows. COORDINATE SYSTEM: Screenshot pixel coordinates = mouse coordinates. If you see a button at pixel (450, 300) in the screenshot, use mouse_control(x=450, y=300, monitorIndex=N). No conversion needed! Targets: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor in 2-monitor setups), 'monitor' (by index for 3+ monitors), 'window', 'region', 'all_monitors'. Use 'list_monitors' action to see available monitors.")]
    [return: Description("The result of the screenshot operation including success status, base64-encoded image data or file path, monitor list, and error details if failed.")]
    public async Task<ScreenshotControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        [Description("The action to perform. Valid values: 'capture' (take screenshot), 'list_monitors' (enumerate displays). Default: 'capture'")] string? action = null,
        [Description("Capture target. Valid values: 'primary_screen' (main display with taskbar), 'secondary_screen' (other monitor, only for 2-monitor setups), 'monitor' (by index), 'window' (by handle), 'region' (by coordinates), 'all_monitors' (composite of all displays). Default: 'primary_screen'")] string? target = null,
        [Description("Monitor index for 'monitor' target (0-based). Use 'list_monitors' to get available indices.")] int? monitorIndex = null,
        [Description("Window handle (IntPtr value) for 'window' target. Get from window_management tool.")] long? windowHandle = null,
        [Description("X coordinate (left) for 'region' target. Can be negative for multi-monitor setups.")] int? regionX = null,
        [Description("Y coordinate (top) for 'region' target. Can be negative for multi-monitor setups.")] int? regionY = null,
        [Description("Width in pixels for 'region' target. Must be positive.")] int? regionWidth = null,
        [Description("Height in pixels for 'region' target. Must be positive.")] int? regionHeight = null,
        [Description("Include mouse cursor in capture. Default: false")] bool includeCursor = false,
        [Description("Screenshot format: 'jpeg'/'jpg' or 'png'. Default: 'jpeg' (LLM-optimized).")] string? imageFormat = null,
        [Description("Image compression quality 1-100. Default: 85. Only affects JPEG format.")] int? quality = null,
        [Description("How to return the screenshot. 'inline' returns base64 data, 'file' saves to disk and returns path. Default: 'inline'.")] string? outputMode = null,
        [Description("Directory or file path for output when outputMode is 'file'. If directory, auto-generates filename. If null, uses temp directory.")] string? outputPath = null,
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
}
