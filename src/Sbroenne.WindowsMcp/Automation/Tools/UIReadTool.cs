using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for reading text from UI elements with automatic OCR fallback.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIReadTool
{
    /// <summary>
    /// Reads an observed element by ID, or an explicit whole window. Only whole-window reads may fall back to OCR.
    /// Keywords: read, read text, get text, extract text, OCR, text content, contents, value,
    /// scrape, article text, web page text, what does it say.
    /// </summary>
    /// <remarks>
    /// Extract text from observed elements or explicit windows. Selectors are not accepted.
    /// For web pages in Edge/Chrome, pass format='article' to get clean, token-efficient article text
    /// (main content only, navigation chrome and inline link URLs stripped, headings/lists as markdown).
    /// Reading the live signed-in browser window this way also works for authenticated/internal pages that
    /// an HTTP fetch cannot reach.
    /// Element failures are returned as errors, never as window text. An empty element read never
    /// widens to whole-window OCR. Omit elementId only for an intentional whole-window read.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find' or 'list'). REQUIRED.</param>
    /// <param name="elementId">Opaque ID from discovery. Required for an element read. Omit only for an explicit whole-window read.</param>
    /// <param name="includeChildren">Include child element text (default: false). Ignored when format='article'.</param>
    /// <param name="language">OCR language code (e.g., 'en-US', 'de-DE'). Uses system default if not specified. Only used if OCR fallback triggers.</param>
    /// <param name="format">Text extraction mode: 'raw' (default, complete but includes nav chrome and link URLs) or 'article' (clean main-content text for web pages, chrome and inline URLs stripped, headings/lists as markdown).</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query, elements scanned) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload of the extracted text content from the element or screen region. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "ui_read", Title = "Read Text from Element", Destructive = false, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        [DefaultValue(null)] string? elementId,
        [DefaultValue(false)] bool includeChildren,
        [DefaultValue(null)] string? language,
        [DefaultValue(null)] string? format,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "read";

        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        if (!WindowHandleParser.TryParse(windowHandle, out var hwnd) || hwnd == nint.Zero)
        {
            return WindowsToolsBase.FailResult("windowHandle must be a nonzero decimal window handle.");
        }

        if (elementId is not null && string.IsNullOrWhiteSpace(elementId))
        {
            return WindowsToolsBase.FailResult("elementId must not be empty. Omit it only for an explicit whole-window read.");
        }

        if (!TryParseTextExtractionMode(format, out var mode))
        {
            return WindowsToolsBase.FailResult(
                $"Invalid format '{format}'. Use 'raw' (default) or 'article'.");
        }

        try
        {
            var automationService = WindowsToolsBase.UIAutomationService;
            var result = await automationService.GetTextAsync(elementId, windowHandle, includeChildren, mode, cancellationToken);
            if (!ShouldTryWindowOcr(result, elementId, mode))
            {
                return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
            }

            // Fallback: try OCR on the window region
            try
            {
                if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd) ||
                    NativeMethods.IsIconic(hwnd))
                {
                    return WindowsToolsBase.ToCallToolResult(UIAutomationResult.CreateFailure(
                        actionName, UIAutomationErrorType.WindowNotFound,
                        "The requested window is unavailable for whole-window OCR."), includeDiagnostics);
                }

                if (!NativeMethods.GetWindowRect(hwnd, out var rect) ||
                    rect.Right <= rect.Left || rect.Bottom <= rect.Top)
                {
                    return WindowsToolsBase.ToCallToolResult(UIAutomationResult.CreateFailure(
                        actionName, UIAutomationErrorType.InvalidRegion,
                        "The requested window has no capturable region."), includeDiagnostics);
                }

                var captureRect = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

                using var bitmap = new System.Drawing.Bitmap(captureRect.Width, captureRect.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(captureRect.Left, captureRect.Top, 0, 0, bitmap.Size);
                }

                var ocrResult = await WindowsToolsBase.LegacyOcrService.RecognizeAsync(bitmap, language, cancellationToken);
                if (ocrResult.Success && !string.IsNullOrWhiteSpace(ocrResult.Text))
                {
                    var ocrSuccessResult = UIAutomationResult.CreateSuccessWithText("ui_read", ocrResult.Text, null) with
                    {
                        UsageHint = $"Text extracted via whole-window OCR for the explicit window read. " +
                            $"Engine: {ocrResult.Engine}, Duration: {ocrResult.DurationMs}ms"
                    };
                    return WindowsToolsBase.ToCallToolResult(ocrSuccessResult, includeDiagnostics);
                }

                if (!ocrResult.Success)
                {
                    return WindowsToolsBase.ToCallToolResult(UIAutomationResult.CreateFailure(
                        actionName, UIAutomationErrorType.InternalError,
                        $"Whole-window OCR failed: {ocrResult.ErrorMessage}") with
                    {
                        UsageHint = result.ErrorMessage
                    }, includeDiagnostics);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return WindowsToolsBase.ToCallToolResult(UIAutomationResult.CreateFailure(
                    actionName, UIAutomationErrorType.InternalError,
                    $"Whole-window OCR failed: {ex.Message}") with
                {
                    UsageHint = result.ErrorMessage
                }, includeDiagnostics);
            }

            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }

    internal static bool ShouldTryWindowOcr(UIAutomationResult result, string? elementId, TextExtractionMode mode)
    {
        if (elementId is not null || mode != TextExtractionMode.Raw)
        {
            return false;
        }

        return result.Success
            ? string.IsNullOrWhiteSpace(result.Text)
            : result.ErrorType is UIAutomationErrorType.InternalError or UIAutomationErrorType.PatternNotSupported
                or UIAutomationErrorType.NoTextFound or UIAutomationErrorType.Timeout;
    }

    private static bool TryParseTextExtractionMode(string? format, out TextExtractionMode mode)
    {
        mode = TextExtractionMode.Raw;

        if (string.IsNullOrWhiteSpace(format))
        {
            return true;
        }

        switch (format.Trim().ToLowerInvariant())
        {
            case "raw":
                mode = TextExtractionMode.Raw;
                return true;
            case "article":
            case "markdown":
            case "clean":
                mode = TextExtractionMode.Article;
                return true;
            default:
                return false;
        }
    }
}