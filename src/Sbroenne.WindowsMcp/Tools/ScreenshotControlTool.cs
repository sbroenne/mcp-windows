using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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
    /// Returns base64-encoded PNG image data. Use action 'list_monitors' to enumerate available monitors.
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing base64-encoded PNG image data or monitor list.</returns>
    [McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true, UseStructuredContent = true)]
    [return: Description("The result of the screenshot operation including success status, base64-encoded PNG image data, monitor list, and error details if failed.")]
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

}
