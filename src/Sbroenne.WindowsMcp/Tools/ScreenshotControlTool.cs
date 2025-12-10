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
    /// Returns base64-encoded image data (JPEG by default, configurable via imageFormat parameter).
    /// LLM-optimized defaults: JPEG format at quality 85, auto-scaled to 1568px width.
    /// Use action 'list_monitors' to enumerate available monitors.
    /// Use target 'all_monitors' to capture all connected monitors as a single composite image.
    /// Respects secure desktop (UAC/lock screen) restrictions.
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="action">The action to perform. Valid values: 'capture' (take screenshot), 'list_monitors' (enumerate displays). Default: 'capture'.</param>
    /// <param name="target">Capture target. Valid values: 'primary_screen', 'monitor' (by index), 'window' (by handle), 'region' (by coordinates), 'all_monitors' (composite of all displays). Default: 'primary_screen'.</param>
    /// <param name="monitorIndex">Monitor index for 'monitor' target (0-based). Use 'list_monitors' to get available indices.</param>
    /// <param name="windowHandle">Window handle (IntPtr value) for 'window' target. Get from window_management tool.</param>
    /// <param name="regionX">X coordinate (left) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionY">Y coordinate (top) for 'region' target. Can be negative for multi-monitor setups.</param>
    /// <param name="regionWidth">Width in pixels for 'region' target. Must be positive.</param>
    /// <param name="regionHeight">Height in pixels for 'region' target. Must be positive.</param>
    /// <param name="includeCursor">Include mouse cursor in capture. Default: false.</param>
    /// <param name="imageFormat">Screenshot format: 'jpeg'/'jpg' or 'png'. Default: 'jpeg' (LLM-optimized).</param>
    /// <param name="quality">Image compression quality 1-100. Default: 85. Only affects JPEG format.</param>
    /// <param name="maxWidth">Maximum width in pixels. Image scaled down if wider (aspect ratio preserved). Default: 1568 (Claude's high-res native limit). Set to 0 to disable scaling.</param>
    /// <param name="maxHeight">Maximum height in pixels. Image scaled down if taller (aspect ratio preserved). Default: 0 (no height constraint).</param>
    /// <param name="outputMode">How to return the screenshot. 'inline' returns base64 data, 'file' saves to disk and returns path. Default: 'inline'.</param>
    /// <param name="outputPath">Directory or file path for output when outputMode is 'file'. If directory, auto-generates filename. If null, uses temp directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing base64-encoded image data or file path, dimensions, original dimensions (if scaled), file size, and error details if failed.</returns>
    [McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true, UseStructuredContent = true)]
    [return: Description("The result of the screenshot operation including success status, base64-encoded image data or file path, monitor list, and error details if failed.")]
    public async Task<ScreenshotControlResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        string? action = null,
        string? target = null,
        int? monitorIndex = null,
        long? windowHandle = null,
        int? regionX = null,
        int? regionY = null,
        int? regionWidth = null,
        int? regionHeight = null,
        bool includeCursor = false,
        string? imageFormat = null,
        int? quality = null,
        int? maxWidth = null,
        int? maxHeight = null,
        string? outputMode = null,
        string? outputPath = null,
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
                $"Invalid target: '{target}'. Valid values: 'primary_screen', 'monitor', 'window', 'region', 'all_monitors'");
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

        // Validate maxWidth/maxHeight
        var parsedMaxWidth = maxWidth ?? ScreenshotConfiguration.DefaultMaxWidth;
        var parsedMaxHeight = maxHeight ?? ScreenshotConfiguration.DefaultMaxHeight;
        if (parsedMaxWidth < 0)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                "maxWidth cannot be negative");
        }
        if (parsedMaxHeight < 0)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                "maxHeight cannot be negative");
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
            MaxWidth = parsedMaxWidth,
            MaxHeight = parsedMaxHeight,
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
            "primary_screen" or "primaryscreen" => CaptureTarget.PrimaryScreen,
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
