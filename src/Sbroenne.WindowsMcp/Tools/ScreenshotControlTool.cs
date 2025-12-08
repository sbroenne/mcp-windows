using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for capturing screenshots of screens, monitors, windows, and regions.
/// </summary>
[McpServerToolType]
public sealed class ScreenshotControlTool
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
    /// Captures screenshots of screens, monitors, windows, or regions.
    /// </summary>
    /// <param name="action">The action to perform: 'capture' (default) or 'list_monitors'.</param>
    /// <param name="target">Capture target: 'primary_screen' (default), 'monitor', 'window', or 'region'.</param>
    /// <param name="monitorIndex">Monitor index for 'monitor' target (0-based).</param>
    /// <param name="windowHandle">Window handle for 'window' target.</param>
    /// <param name="regionX">X coordinate for 'region' target.</param>
    /// <param name="regionY">Y coordinate for 'region' target.</param>
    /// <param name="regionWidth">Width for 'region' target.</param>
    /// <param name="regionHeight">Height for 'region' target.</param>
    /// <param name="includeCursor">Whether to include the cursor in the capture (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON result with base64-encoded PNG image data or monitor list.</returns>
    [McpServerTool(Name = "screenshot_control")]
    [Description("Captures screenshots of screens, monitors, windows, or regions on Windows. Returns base64-encoded PNG image data. Use action 'list_monitors' to enumerate available monitors. Respects secure desktop (UAC/lock screen) restrictions.")]
    public async Task<string> ExecuteAsync(
        [Description("The action to perform. Valid values: 'capture' (take screenshot), 'list_monitors' (enumerate displays). Default: 'capture'")]
        string? action = null,
        [Description("Capture target. Valid values: 'primary_screen', 'monitor' (by index), 'window' (by handle), 'region' (by coordinates). Default: 'primary_screen'")]
        string? target = null,
        [Description("Monitor index for 'monitor' target (0-based). Use 'list_monitors' to get available indices.")]
        int? monitorIndex = null,
        [Description("Window handle (IntPtr value) for 'window' target. Get from window_management tool.")]
        long? windowHandle = null,
        [Description("X coordinate (left) for 'region' target. Can be negative for multi-monitor setups.")]
        int? regionX = null,
        [Description("Y coordinate (top) for 'region' target. Can be negative for multi-monitor setups.")]
        int? regionY = null,
        [Description("Width in pixels for 'region' target. Must be positive.")]
        int? regionWidth = null,
        [Description("Height in pixels for 'region' target. Must be positive.")]
        int? regionHeight = null,
        [Description("Include mouse cursor in capture. Default: false")]
        bool includeCursor = false,
        CancellationToken cancellationToken = default)
    {
        // Parse action
        var screenshotAction = ParseAction(action);
        if (screenshotAction == null)
        {
            return SerializeResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid action: '{action}'. Valid values: 'capture', 'list_monitors'"));
        }

        // Parse target
        var captureTarget = ParseTarget(target);
        if (captureTarget == null && screenshotAction == ScreenshotAction.Capture)
        {
            return SerializeResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                $"Invalid target: '{target}'. Valid values: 'primary_screen', 'monitor', 'window', 'region'"));
        }

        // Build region if target is region
        CaptureRegion? region = null;
        if (captureTarget == CaptureTarget.Region)
        {
            if (regionX == null || regionY == null || regionWidth == null || regionHeight == null)
            {
                return SerializeResult(ScreenshotControlResult.Error(
                    ScreenshotErrorCode.InvalidRegion,
                    "Region capture requires regionX, regionY, regionWidth, and regionHeight parameters"));
            }

            region = new CaptureRegion(regionX.Value, regionY.Value, regionWidth.Value, regionHeight.Value);
        }

        // Build request
        var request = new ScreenshotControlRequest
        {
            Action = screenshotAction.Value,
            Target = captureTarget ?? CaptureTarget.PrimaryScreen,
            MonitorIndex = monitorIndex,
            WindowHandle = windowHandle,
            Region = region,
            IncludeCursor = includeCursor
        };

        // Execute and return result
        var result = await _screenshotService.ExecuteAsync(request, cancellationToken);
        return SerializeResult(result);
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
            _ => null
        };
    }

    /// <summary>
    /// Serializes the result to JSON.
    /// </summary>
    private static string SerializeResult(ScreenshotControlResult result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
