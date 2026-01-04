using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for reading text from UI elements with automatic OCR fallback.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIReadTool
{
    private readonly UIAutomationService _automationService;
    private readonly LegacyOcrService _ocrService;
    private readonly ScreenshotService _screenshotService;
    private readonly ILogger<UIReadTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIReadTool"/> class.
    /// </summary>
    public UIReadTool(
        UIAutomationService automationService,
        LegacyOcrService ocrService,
        ScreenshotService screenshotService,
        ILogger<UIReadTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(ocrService);
        ArgumentNullException.ThrowIfNull(screenshotService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _ocrService = ocrService;
        _screenshotService = screenshotService;
        _logger = logger;
    }

    /// <summary>
    /// Reads text from a UI element. If UIA text extraction fails, automatically tries OCR.
    /// </summary>
    /// <remarks>
    /// Extract text from UI elements or screen regions. Auto-falls back to OCR if normal text extraction fails.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="name">Element name (exact match, case-insensitive).</param>
    /// <param name="nameContains">Substring in element name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Text, Edit, Document, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="foundIndex">Return Nth match (1-based, default: 1).</param>
    /// <param name="includeChildren">Include child element text (default: false).</param>
    /// <param name="language">OCR language code (e.g., 'en-US', 'de-DE'). Uses system default if not specified. Only used if OCR fallback triggers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted text content from the element or screen region.</returns>
    [McpServerTool(Name = "ui_read", Title = "Read Text from Element", Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    public async Task<UIAutomationResult> ExecuteAsync(
        string windowHandle,
        string? name = null,
        string? nameContains = null,
        string? namePattern = null,
        string? controlType = null,
        string? automationId = null,
        string? className = null,
        int foundIndex = 1,
        bool includeChildren = false,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "ui_read",
                UIAutomationErrorType.InvalidParameter,
                "windowHandle is required. Get it from window_management(action='find').",
                null);
        }

        var query = new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = name,
            NameContains = nameContains,
            NamePattern = namePattern,
            ControlType = controlType,
            AutomationId = automationId,
            ClassName = className,
            FoundIndex = Math.Max(1, foundIndex)
        };

        // Try normal text extraction first
        // If no specific element criteria, just read from the window
        string? elementIdToRead = null;
        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(nameContains) || !string.IsNullOrEmpty(namePattern) ||
            !string.IsNullOrEmpty(controlType) || !string.IsNullOrEmpty(automationId) || !string.IsNullOrEmpty(className))
        {
            // Find the element first
            var findResult = await _automationService.FindElementsAsync(query, cancellationToken);
            if (findResult.Success && findResult.Items?.Length > 0)
            {
                elementIdToRead = findResult.Items[0].Id;
            }
        }

        var result = await _automationService.GetTextAsync(elementIdToRead, windowHandle, includeChildren, cancellationToken);
        if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
        {
            return result;
        }

        // Fallback: try OCR on the window region
        try
        {
            if (!nint.TryParse(windowHandle, out var hwnd) || hwnd == IntPtr.Zero)
            {
                return result; // Return original result if window handle invalid
            }

            if (!NativeMethods.GetWindowRect(hwnd, out var rect))
            {
                return result; // Return original result if can't get bounds
            }

            var captureRect = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            using var bitmap = new System.Drawing.Bitmap(captureRect.Width, captureRect.Height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(captureRect.Left, captureRect.Top, 0, 0, bitmap.Size);
            }

            var ocrResult = await _ocrService.RecognizeAsync(bitmap, language, cancellationToken);
            if (ocrResult.Success && !string.IsNullOrWhiteSpace(ocrResult.Text))
            {
                return UIAutomationResult.CreateSuccessWithText("ui_read", ocrResult.Text, null) with
                {
                    UsageHint = $"Text extracted via OCR (fallback). Engine: {ocrResult.Engine}, Duration: {ocrResult.DurationMs}ms"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // OCR fallback failed - ignore and return original result
        }

        return result;
    }
}
