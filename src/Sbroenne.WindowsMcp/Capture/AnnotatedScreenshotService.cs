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
        int searchDepth = 15,
        ImageFormat format = ImageFormat.Jpeg,
        int quality = 85,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogCaptureStarted(windowHandle);

            // Clamp searchDepth to valid range (1-20)
            searchDepth = Math.Clamp(searchDepth, 1, 20);

            // Step 1: Get interactive UI elements
            var elementsResult = await GetInteractiveElementsAsync(windowHandle, controlTypeFilter, maxElements, searchDepth, cancellationToken);
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
    /// <param name="windowHandle">Window handle to search.</param>
    /// <param name="controlTypeFilter">Optional control type filter.</param>
    /// <param name="maxElements">Maximum number of elements to return.</param>
    /// <param name="searchDepth">Maximum depth to search. Use 15 for Electron/Chromium, 5-8 for WinForms, 8-10 for WPF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<UIAutomationResult> GetInteractiveElementsAsync(
        nint? windowHandle,
        string? controlTypeFilter,
        int maxElements,
        int searchDepth,
        CancellationToken cancellationToken)
    {
        // Default control types that are typically interactive
        // Includes Document for Electron apps and TreeItem for file explorers
        var defaultInteractiveTypes = "Button,Edit,CheckBox,RadioButton,ComboBox,List,ListItem,Tab,TabItem,MenuItem,Hyperlink,Slider,Spinner,TreeItem,Document";
        var filterTypes = string.IsNullOrEmpty(controlTypeFilter) ? defaultInteractiveTypes : controlTypeFilter;

        // Parse the control type filter into a set for efficient lookup
        var allowedControlTypes = filterTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        // Get the full tree WITHOUT control type filter to avoid pruning branches
        // The tree walker prunes branches when parent elements don't match the filter,
        // which causes us to miss deeply nested elements in apps like Electron/Chromium
        var result = await _automationService.GetTreeAsync(
            windowHandle,
            parentElementId: null,
            maxDepth: searchDepth,
            controlTypeFilter: null, // No filter - we'll filter after flattening
            cancellationToken);

        if (!result.Success || result.Elements == null)
        {
            return result;
        }

        // GetTreeAsync returns a hierarchical tree - flatten it to get all elements
        var allElements = new List<UIElementInfo>();
        foreach (var element in result.Elements)
        {
            FlattenTree(element, allElements);
        }

        // Filter to only allowed control types, visible elements, and limit count
        var visibleElements = allElements
            .Where(e => IsInteractiveControlType(e, allowedControlTypes) && IsVisibleOnScreen(e))
            .Take(maxElements)
            .ToArray();

        if (visibleElements.Length == 0)
        {
            return UIAutomationResult.CreateFailure(
                "get_interactive_elements",
                UIAutomationErrorType.ElementNotFound,
                $"No interactive elements found matching control types: {filterTypes}",
                result.Diagnostics);
        }

        return UIAutomationResult.CreateSuccess(result.Action, visibleElements, result.Diagnostics);
    }

    /// <summary>
    /// Flattens a hierarchical UI element tree into a flat list.
    /// </summary>
    /// <param name="element">The element to flatten.</param>
    /// <param name="result">The list to add elements to.</param>
    private static void FlattenTree(UIElementInfo element, List<UIElementInfo> result)
    {
        // Add this element (without children to avoid duplication in the flat list)
        result.Add(element with { Children = null });

        // Recursively process children
        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                FlattenTree(child, result);
            }
        }
    }

    /// <summary>
    /// Checks if an element's control type is in the allowed set.
    /// </summary>
    private static bool IsInteractiveControlType(UIElementInfo element, HashSet<string> allowedControlTypes)
    {
        if (string.IsNullOrEmpty(element.ControlType))
        {
            return false;
        }

        return allowedControlTypes.Contains(element.ControlType.ToLowerInvariant());
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
        var placedLabels = new List<Rectangle>(); // Track placed label positions to avoid overlaps

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

                // Calculate label position with anti-overlap logic
                var labelText = index.ToString(CultureInfo.InvariantCulture);
                var labelSize = graphics.MeasureString(labelText, font);
                var labelWidth = (int)labelSize.Width + (LabelPadding * 2);
                var labelHeight = (int)labelSize.Height + (LabelPadding * 2);

                var labelRect = FindNonOverlappingLabelPosition(
                    relativeLeft, relativeTop, relativeRight, relativeBottom,
                    labelWidth, labelHeight,
                    originalBitmap.Width, originalBitmap.Height,
                    placedLabels);

                placedLabels.Add(labelRect);

                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, labelRect);

                using var textBrush = new SolidBrush(Color.White);
                graphics.DrawString(labelText, font, textBrush, labelRect.X + LabelPadding, labelRect.Y + LabelPadding);

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

    /// <summary>
    /// Finds a non-overlapping position for a label near an element's bounding box.
    /// Tries multiple positions around the element and offsets if all primary positions overlap.
    /// </summary>
    private static Rectangle FindNonOverlappingLabelPosition(
        int elementLeft, int elementTop, int elementRight, int elementBottom,
        int labelWidth, int labelHeight,
        int imageWidth, int imageHeight,
        List<Rectangle> placedLabels)
    {
        // Define candidate positions (in order of preference):
        // 1. Top-right corner (above element)
        // 2. Top-left corner (above element)
        // 3. Bottom-right corner (below element)
        // 4. Bottom-left corner (below element)
        // 5. Inside top-right corner
        // 6. Inside top-left corner
        var candidatePositions = new System.Drawing.Point[]
        {
            new System.Drawing.Point(Math.Max(0, Math.Min(elementRight - labelWidth, imageWidth - labelWidth)),
                      Math.Max(0, elementTop - labelHeight)),
            new System.Drawing.Point(Math.Max(0, elementLeft),
                      Math.Max(0, elementTop - labelHeight)),
            new System.Drawing.Point(Math.Max(0, Math.Min(elementRight - labelWidth, imageWidth - labelWidth)),
                      Math.Min(elementBottom, imageHeight - labelHeight)),
            new System.Drawing.Point(Math.Max(0, elementLeft),
                      Math.Min(elementBottom, imageHeight - labelHeight)),
            new System.Drawing.Point(Math.Max(0, Math.Min(elementRight - labelWidth, imageWidth - labelWidth)),
                      Math.Max(0, elementTop)),
            new System.Drawing.Point(Math.Max(0, elementLeft),
                      Math.Max(0, elementTop)),
        };

        // Try each candidate position
        foreach (var pos in candidatePositions)
        {
            var candidateRect = new Rectangle(pos.X, pos.Y, labelWidth, labelHeight);
            if (!OverlapsAnyLabel(candidateRect, placedLabels))
            {
                return candidateRect;
            }
        }

        // All preferred positions overlap - find an offset position
        // Start from top-right and try shifting down or left
        var baseX = Math.Max(0, Math.Min(elementRight - labelWidth, imageWidth - labelWidth));
        var baseY = Math.Max(0, elementTop - labelHeight);

        // Try offsets in a spiral pattern
        for (int offset = labelHeight; offset < imageHeight; offset += labelHeight)
        {
            // Try below
            var offsetRect = new Rectangle(baseX, Math.Min(baseY + offset, imageHeight - labelHeight), labelWidth, labelHeight);
            if (!OverlapsAnyLabel(offsetRect, placedLabels))
            {
                return offsetRect;
            }

            // Try to the left
            offsetRect = new Rectangle(Math.Max(0, baseX - offset), baseY, labelWidth, labelHeight);
            if (!OverlapsAnyLabel(offsetRect, placedLabels))
            {
                return offsetRect;
            }
        }

        // Fallback: return the original preferred position even if it overlaps
        return new Rectangle(baseX, baseY, labelWidth, labelHeight);
    }

    /// <summary>
    /// Checks if a rectangle overlaps with any previously placed labels.
    /// </summary>
    private static bool OverlapsAnyLabel(Rectangle candidate, List<Rectangle> placedLabels)
    {
        foreach (var placed in placedLabels)
        {
            if (candidate.IntersectsWith(placed))
            {
                return true;
            }
        }
        return false;
    }
}
