using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using ImageFormat = Sbroenne.WindowsMcp.Models.ImageFormat;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Service for creating annotated screenshots with numbered UI element labels.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AnnotatedScreenshotService : IAnnotatedScreenshotService
{
    private readonly IUIAutomationService _automationService;
    private readonly IScreenshotService _screenshotService;
    private readonly IImageProcessor _imageProcessor;
    private readonly AnnotatedScreenshotLogger _logger;

    // Annotation styling constants
    private const int LabelFontSize = 11;
    private const int LabelPadding = 3;
    private const int BoundingBoxLineWidth = 2;

    // Color palette for annotations (high contrast, easy to distinguish)
    private static readonly Color[] _annotationColors =
    [
        Color.FromArgb(220, 53, 69),    // Red
        Color.FromArgb(0, 123, 255),    // Blue
        Color.FromArgb(40, 167, 69),    // Green
        Color.FromArgb(255, 193, 7),    // Yellow
        Color.FromArgb(111, 66, 193),   // Purple
        Color.FromArgb(23, 162, 184),   // Cyan
        Color.FromArgb(253, 126, 20),   // Orange
        Color.FromArgb(108, 117, 125),  // Gray
        Color.FromArgb(32, 201, 151),   // Teal
        Color.FromArgb(232, 62, 140),   // Pink
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnotatedScreenshotService"/> class.
    /// </summary>
    public AnnotatedScreenshotService(
        IUIAutomationService automationService,
        IScreenshotService screenshotService,
        IImageProcessor imageProcessor,
        AnnotatedScreenshotLogger logger)
    {
        _automationService = automationService ?? throw new ArgumentNullException(nameof(automationService));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AnnotatedScreenshotResult> CaptureAsync(
        nint? windowHandle = null,
        string? controlTypeFilter = null,
        int maxElements = 50,
        ImageFormat format = ImageFormat.Jpeg,
        int quality = 85,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogCaptureStarted(windowHandle);

            // Step 1: Get interactive UI elements
            var elementsResult = await GetInteractiveElementsAsync(windowHandle, controlTypeFilter, maxElements, cancellationToken);
            if (!elementsResult.Success || elementsResult.Elements == null || elementsResult.Elements.Length == 0)
            {
                return AnnotatedScreenshotResult.CreateFailure(
                    elementsResult.ErrorMessage ?? "No interactive elements found");
            }

            // Step 2: Capture screenshot
            var targetHandle = windowHandle ?? NativeMethods.GetForegroundWindow();
            var screenshotResult = await CaptureWindowScreenshotAsync(targetHandle, cancellationToken);
            if (!screenshotResult.Success || screenshotResult.ImageData == null)
            {
                return AnnotatedScreenshotResult.CreateFailure(
                    screenshotResult.Message ?? "Failed to capture screenshot");
            }

            // Step 3: Get window bounds for coordinate translation
            var windowRect = GetWindowRect(targetHandle);
            if (windowRect == null)
            {
                return AnnotatedScreenshotResult.CreateFailure("Failed to get window bounds");
            }

            // Step 4: Draw annotations on the screenshot
            var (annotatedImageData, annotatedElements) = DrawAnnotations(
                screenshotResult.ImageData,
                elementsResult.Elements,
                windowRect.Value,
                format,
                quality);

            _logger.LogCaptureSuccess(annotatedElements.Length);

            return AnnotatedScreenshotResult.CreateSuccess(
                annotatedImageData,
                format == ImageFormat.Jpeg ? "jpeg" : "png",
                screenshotResult.Width ?? 0,
                screenshotResult.Height ?? 0,
                annotatedElements);
        }
        catch (Exception ex)
        {
            _logger.LogCaptureError(ex, ex.Message);
            return AnnotatedScreenshotResult.CreateFailure($"Failed to capture annotated screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets interactive UI elements from the specified window.
    /// </summary>
    private async Task<UIAutomationResult> GetInteractiveElementsAsync(
        nint? windowHandle,
        string? controlTypeFilter,
        int maxElements,
        CancellationToken cancellationToken)
    {
        // Use a reasonable depth for finding interactive elements
        const int SearchDepth = 10;

        // Default control types that are typically interactive
        var defaultInteractiveTypes = "Button,Edit,CheckBox,RadioButton,ComboBox,List,ListItem,Tab,TabItem,MenuItem,Hyperlink,Slider,Spinner";
        var filterTypes = string.IsNullOrEmpty(controlTypeFilter) ? defaultInteractiveTypes : controlTypeFilter;

        var result = await _automationService.GetTreeAsync(
            windowHandle,
            parentElementId: null,
            maxDepth: SearchDepth,
            controlTypeFilter: filterTypes,
            cancellationToken);

        if (!result.Success || result.Elements == null)
        {
            return result;
        }

        // Filter to only visible, on-screen elements and limit count
        var visibleElements = result.Elements
            .Where(e => IsVisibleOnScreen(e))
            .Take(maxElements)
            .ToArray();

        return UIAutomationResult.CreateSuccess(result.Action, visibleElements, result.Diagnostics);
    }

    /// <summary>
    /// Checks if an element is visible on screen (has positive dimensions).
    /// </summary>
    private static bool IsVisibleOnScreen(UIElementInfo element)
    {
        var bounds = element.BoundingRect;
        return bounds.Width > 5 && bounds.Height > 5 &&
               bounds.X >= -10000 && bounds.Y >= -10000;
    }

    /// <summary>
    /// Captures a screenshot of the specified window.
    /// </summary>
    private async Task<ScreenshotControlResult> CaptureWindowScreenshotAsync(nint windowHandle, CancellationToken cancellationToken)
    {
        var request = new ScreenshotControlRequest
        {
            Action = ScreenshotAction.Capture,
            Target = CaptureTarget.Window,
            WindowHandle = windowHandle,
            ImageFormat = ImageFormat.Png, // Use PNG for processing, convert later
            Quality = 100,
            OutputMode = OutputMode.Inline
        };

        return await _screenshotService.ExecuteAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets the window rectangle in screen coordinates.
    /// </summary>
    private static RECT? GetWindowRect(nint windowHandle)
    {
        if (NativeMethods.GetWindowRect(windowHandle, out var rect))
        {
            return rect;
        }
        return null;
    }

    /// <summary>
    /// Draws numbered annotations on the screenshot and returns the annotated image with element mapping.
    /// </summary>
    private (string ImageData, AnnotatedElement[] Elements) DrawAnnotations(
        string originalImageBase64,
        UIElementInfo[] elements,
        RECT windowRect,
        ImageFormat format,
        int quality)
    {
        // Decode the original image
        var imageBytes = Convert.FromBase64String(originalImageBase64);
        using var imageStream = new MemoryStream(imageBytes);
        using var originalBitmap = new Bitmap(imageStream);
        using var annotatedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);

        var annotatedElements = new List<AnnotatedElement>();

        using (var graphics = Graphics.FromImage(annotatedBitmap))
        {
            // Copy original image
            graphics.DrawImage(originalBitmap, 0, 0);

            // Set up high-quality rendering
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var font = new Font("Segoe UI", LabelFontSize, FontStyle.Bold);

            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var bounds = element.BoundingRect;

                // Translate from screen coordinates to window-relative coordinates
                var relativeLeft = bounds.X - windowRect.Left;
                var relativeTop = bounds.Y - windowRect.Top;
                var relativeRight = bounds.X + bounds.Width - windowRect.Left;
                var relativeBottom = bounds.Y + bounds.Height - windowRect.Top;

                // Skip elements that are outside the window bounds
                if (relativeRight < 0 || relativeBottom < 0 ||
                    relativeLeft >= originalBitmap.Width || relativeTop >= originalBitmap.Height)
                {
                    continue;
                }

                // Clamp to image bounds
                relativeLeft = Math.Max(0, relativeLeft);
                relativeTop = Math.Max(0, relativeTop);
                relativeRight = Math.Min(originalBitmap.Width - 1, relativeRight);
                relativeBottom = Math.Min(originalBitmap.Height - 1, relativeBottom);

                var index = annotatedElements.Count + 1;
                var color = _annotationColors[index % _annotationColors.Length];

                // Draw bounding box
                using var pen = new Pen(color, BoundingBoxLineWidth);
                var rect = new Rectangle(relativeLeft, relativeTop,
                    relativeRight - relativeLeft, relativeBottom - relativeTop);
                graphics.DrawRectangle(pen, rect);

                // Draw label background and text
                var labelText = index.ToString(CultureInfo.InvariantCulture);
                var labelSize = graphics.MeasureString(labelText, font);
                var labelWidth = (int)labelSize.Width + (LabelPadding * 2);
                var labelHeight = (int)labelSize.Height + (LabelPadding * 2);

                // Position label at top-right corner of bounding box
                var labelX = Math.Max(0, Math.Min(relativeRight - labelWidth, originalBitmap.Width - labelWidth));
                var labelY = Math.Max(0, relativeTop - labelHeight);

                // If no room above, put it inside the top of the box
                if (labelY < 0)
                {
                    labelY = relativeTop;
                }

                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, labelX, labelY, labelWidth, labelHeight);

                using var textBrush = new SolidBrush(Color.White);
                graphics.DrawString(labelText, font, textBrush, labelX + LabelPadding, labelY + LabelPadding);

                // Create annotated element entry
                annotatedElements.Add(new AnnotatedElement
                {
                    Index = index,
                    Name = element.Name,
                    ControlType = element.ControlType,
                    AutomationId = element.AutomationId,
                    ElementId = element.ElementId,
                    ClickablePoint = element.ClickablePoint,
                    BoundingBox = element.BoundingRect
                });
            }
        }

        // Encode to final format
        var processed = _imageProcessor.Process(annotatedBitmap, format, quality);
        var base64 = Convert.ToBase64String(processed.Data);

        return (base64, [.. annotatedElements.OrderBy(e => e.Index)]);
    }
}
