using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// MCP tool for Windows UI Automation operations.
/// Provides 15 actions: find, get_tree, wait_for, click, type, select, toggle,
/// invoke, focus, scroll_into_view, get_text, highlight, ocr, ocr_element, ocr_status.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIAutomationTool
{
    private readonly IUIAutomationService _automationService;
    private readonly IOcrService _ocrService;
    private readonly IScreenshotService _screenshotService;
    private readonly IAnnotatedScreenshotService _annotatedScreenshotService;
    private readonly IWindowEnumerator _windowEnumerator;
    private readonly IWindowService _windowService;
    private readonly ILogger<UIAutomationTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAutomationTool"/> class.
    /// </summary>
    /// <param name="automationService">The UI Automation service.</param>
    /// <param name="ocrService">The OCR service.</param>
    /// <param name="screenshotService">The screenshot service for OCR captures.</param>
    /// <param name="annotatedScreenshotService">The annotated screenshot service.</param>
    /// <param name="windowEnumerator">The window enumerator for getting target window info.</param>
    /// <param name="windowService">The window service for window activation.</param>
    /// <param name="logger">The logger.</param>
    public UIAutomationTool(
        IUIAutomationService automationService,
        IOcrService ocrService,
        IScreenshotService screenshotService,
        IAnnotatedScreenshotService annotatedScreenshotService,
        IWindowEnumerator windowEnumerator,
        IWindowService windowService,
        ILogger<UIAutomationTool> logger)
    {
        ArgumentNullException.ThrowIfNull(automationService);
        ArgumentNullException.ThrowIfNull(ocrService);
        ArgumentNullException.ThrowIfNull(screenshotService);
        ArgumentNullException.ThrowIfNull(annotatedScreenshotService);
        ArgumentNullException.ThrowIfNull(windowEnumerator);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(logger);

        _automationService = automationService;
        _ocrService = ocrService;
        _screenshotService = screenshotService;
        _annotatedScreenshotService = annotatedScreenshotService;
        _windowEnumerator = windowEnumerator;
        _windowService = windowService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a UI Automation action.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="windowHandle">Window handle to target. For interactive actions, the window is automatically activated before the action. Get from window_management tool.</param>
    /// <param name="name">Element name to search for.</param>
    /// <param name="nameContains">Substring to search for in element names (partial match).</param>
    /// <param name="namePattern">Regex pattern to match element names.</param>
    /// <param name="controlType">Control type filter (Button, Edit, Text, etc.).</param>
    /// <param name="automationId">AutomationId to search for.</param>
    /// <param name="className">Element class name to filter by (e.g., 'Chrome_WidgetWin_1' for Chromium apps).</param>
    /// <param name="elementId">Element ID from a previous find operation.</param>
    /// <param name="parentElementId">Parent element ID to scope search to a subtree.</param>
    /// <param name="maxDepth">Maximum tree depth for get_tree (default: 5).</param>
    /// <param name="exactDepth">Search only at this exact depth (skips other depths).</param>
    /// <param name="foundIndex">Return the Nth matching element (1-based, default: 1). Use foundIndex=2 to get the 2nd match.</param>
    /// <param name="includeChildren">Include child elements in response.</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
    /// <param name="text">Text to type (for type action).</param>
    /// <param name="clearFirst">Clear existing text before typing.</param>
    /// <param name="value">Value for select or invoke actions.</param>
    /// <param name="language">OCR language code (e.g., 'en-US', 'de-DE'). Uses system default if not specified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the UI Automation operation.</returns>
    [McpServerTool(Name = "ui_automation", Title = "UI Automation", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Windows UI Automation - interact with UI elements by semantic properties. RECOMMENDED WORKFLOW: 1) Use window_management to find target window handle. 2) Use ui_automation with windowHandle to find/interact with elements. 3) Only use mouse_control as fallback with clickablePoint coordinates. 4) Use screenshot_control for verification or OCR. Actions: find (search elements), get_tree (hierarchy view), wait_for (element appears), click, type, select, toggle, invoke (patterns), focus, scroll_into_view, get_text, highlight (draws red box for human verification or demos - persists until hide_highlight called), hide_highlight (removes the red box), ocr (screen region), ocr_element (element bounds), ocr_status (engine info), get_element_at_cursor (element under mouse), get_focused_element (keyboard focus), get_ancestors (parent chain), capture_annotated (screenshot with numbered element labels - use to discover clickable elements visually). DISCOVERY TIP: Use capture_annotated first to see all interactive elements with numbers, then reference by index. MULTI-WINDOW: Pass windowHandle to target a specific window - interactive actions automatically activate the window first. ELECTRON APPS (VS Code, Teams, Slack): These work via Chromium accessibility - use get_tree first to discover element names and types. Element names come from ARIA labels. PERFORMANCE TIP: For apps with large UI trees, use hierarchical search - first find a container with get_tree(maxDepth=2), then use parentElementId to search within it. This dramatically reduces traversal time. MULTI-MONITOR: Results include 'clickablePoint' with ready-to-use coordinates for mouse_control (x, y, monitorIndex). ADVANCED SEARCH: Use nameContains for partial matching, namePattern for regex, foundIndex for Nth match (e.g., foundIndex=2 for 2nd button), exactDepth to search at specific depth. TEXT EXTRACTION: Use get_text first; use ocr_element only for images, custom-rendered controls, or when get_text returns empty. TROUBLESHOOTING: If find returns no results, use get_tree to see actual element hierarchy and adjust your query.")]
    public async Task<UIAutomationResult> ExecuteAsync(
        [Description("Action: find, get_tree, wait_for, click, type, select, toggle, invoke, focus, scroll_into_view, get_text, highlight, hide_highlight, ocr, ocr_element, ocr_status, get_element_at_cursor, get_focused_element, get_ancestors, capture_annotated")]
        UIAutomationAction action,

        [Description("Window handle to target. For interactive actions (click, type, select, toggle, invoke, focus), the window is automatically activated before the action. Get from window_management(action='find'). If not specified, uses the current foreground window.")]
        nint? windowHandle = null,

        [Description("Element name to search for (exact match, case-insensitive). For Electron apps, this is typically the ARIA label.")]
        string? name = null,

        [Description("Substring to search for in element names (partial match, case-insensitive). Use instead of 'name' when you only know part of the element's name.")]
        string? nameContains = null,

        [Description("Regex pattern to match element names. Use for complex name matching like 'Button [0-9]+' or 'Save|Cancel'.")]
        string? namePattern = null,

        [Description("Control type filter: Button, Edit, Text, List, ListItem, Tree, TreeItem, Menu, MenuItem, ComboBox, CheckBox, RadioButton, Tab, TabItem, Window, Pane, Document, Hyperlink, Image, ProgressBar, Slider, Spinner, StatusBar, ToolBar, ToolTip, Group, ScrollBar, DataGrid, DataItem, Custom")]
        string? controlType = null,

        [Description("AutomationId to search for (exact match). More reliable than name for finding elements.")]
        string? automationId = null,

        [Description("Element class name to filter by (e.g., 'Chrome_WidgetWin_1' for Chromium apps, 'Button' for Win32 buttons).")]
        string? className = null,

        [Description("Element ID from a previous find operation (for element-specific actions like toggle, invoke, focus)")]
        string? elementId = null,

        [Description("Parent element ID to scope search/tree to a subtree (for find, get_tree actions). Improves performance on complex UIs.")]
        string? parentElementId = null,

        [Description("Maximum tree depth for get_tree (default: 5, max: 20). Use lower values for faster results.")]
        int maxDepth = 5,

        [Description("Search only at this exact depth from the root (e.g., exactDepth=1 searches only immediate children). Skips elements at other depths.")]
        int? exactDepth = null,

        [Description("Return the Nth matching element (1-based, default: 1). Use foundIndex=2 to get the 2nd button, foundIndex=3 for the 3rd, etc.")]
        int foundIndex = 1,

        [Description("Include child elements in response")]
        bool includeChildren = false,

        [Description("Timeout in milliseconds for wait_for (default: 5000)")]
        int timeoutMs = 5000,

        [Description("Text to type (for type action)")]
        string? text = null,

        [Description("Clear existing text before typing (default: false)")]
        bool clearFirst = false,

        [Description("Value for select or invoke actions")]
        string? value = null,

        [Description("OCR language code (e.g., 'en-US', 'de-DE'). Uses system default if not specified.")]
        string? language = null,

        CancellationToken cancellationToken = default)
    {
        LogActionStarted(_logger, action, name, controlType, automationId);

        // Clamp maxDepth to reasonable limits
        maxDepth = Math.Clamp(maxDepth, 0, 20);
        timeoutMs = Math.Clamp(timeoutMs, 0, 60000);

        // Auto-activate target window for interactive actions when windowHandle is specified
        if (windowHandle.HasValue && windowHandle.Value != nint.Zero && IsInteractiveAction(action))
        {
            var activateResult = await ActivateTargetWindowAsync(windowHandle.Value, action.ToString().ToLowerInvariant(), cancellationToken);
            if (!activateResult.Success)
            {
                return activateResult;
            }
        }

        // Clamp foundIndex to valid range
        foundIndex = Math.Max(1, foundIndex);

        try
        {
            var result = action switch
            {
                UIAutomationAction.Find => await HandleFindAsync(windowHandle, parentElementId, name, nameContains, namePattern, controlType, automationId, className, exactDepth, foundIndex, includeChildren, timeoutMs, cancellationToken),
                UIAutomationAction.GetTree => await HandleGetTreeAsync(windowHandle, parentElementId, maxDepth, controlType, cancellationToken),
                UIAutomationAction.WaitFor => await HandleWaitForAsync(windowHandle, name, nameContains, namePattern, controlType, automationId, className, exactDepth, foundIndex, timeoutMs, cancellationToken),
                UIAutomationAction.Click => await HandleClickAsync(elementId, windowHandle, name, nameContains, namePattern, controlType, automationId, className, foundIndex, cancellationToken),
                UIAutomationAction.Type => await HandleTypeAsync(elementId, windowHandle, name, nameContains, namePattern, controlType, automationId, className, foundIndex, text, clearFirst, cancellationToken),
                UIAutomationAction.Select => await HandleSelectAsync(elementId, windowHandle, name, controlType, automationId, value, cancellationToken),
                UIAutomationAction.Toggle => await HandleToggleAsync(elementId, cancellationToken),
                UIAutomationAction.Invoke => await HandleInvokeAsync(elementId, value, cancellationToken),
                UIAutomationAction.Focus => await HandleFocusAsync(elementId, cancellationToken),
                UIAutomationAction.ScrollIntoView => await HandleScrollIntoViewAsync(elementId, windowHandle, name, controlType, automationId, timeoutMs, cancellationToken),
                UIAutomationAction.GetText => await HandleGetTextAsync(elementId, windowHandle, includeChildren, cancellationToken),
                UIAutomationAction.Highlight => await HandleHighlightAsync(elementId, cancellationToken),
                UIAutomationAction.HideHighlight => await _automationService.HideHighlightAsync(cancellationToken),
                UIAutomationAction.Ocr => await HandleOcrAsync(windowHandle, language, cancellationToken),
                UIAutomationAction.OcrElement => await HandleOcrElementAsync(elementId, language, cancellationToken),
                UIAutomationAction.OcrStatus => HandleOcrStatus(),
                UIAutomationAction.GetElementAtCursor => await _automationService.GetElementAtCursorAsync(cancellationToken),
                UIAutomationAction.GetFocusedElement => await _automationService.GetFocusedElementAsync(cancellationToken),
                UIAutomationAction.GetAncestors => await HandleGetAncestorsAsync(elementId, maxDepth, cancellationToken),
                UIAutomationAction.CaptureAnnotated => await HandleCaptureAnnotatedAsync(windowHandle, controlType, maxDepth, cancellationToken),
                _ => UIAutomationResult.CreateFailure(action.ToString(), UIAutomationErrorType.InvalidParameter, $"Unknown action: {action}", null)
            };

            // Attach target window info for actions that interact with the UI
            if (result.Success && IsInteractiveAction(action))
            {
                result = await AttachTargetWindowInfoAsync(result, cancellationToken);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogActionError(_logger, action, ex);
            return UIAutomationResult.CreateFailure(
                action.ToString(),
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Determines if an action is interactive (affects the target window).
    /// </summary>
    private static bool IsInteractiveAction(UIAutomationAction action) =>
        action is UIAutomationAction.Click or UIAutomationAction.Type
            or UIAutomationAction.Select or UIAutomationAction.Toggle
            or UIAutomationAction.Invoke or UIAutomationAction.Focus;

    /// <summary>
    /// Attaches information about the current foreground window to the result.
    /// This helps LLM agents verify that UI automation targeted the correct window.
    /// </summary>
    private async Task<UIAutomationResult> AttachTargetWindowInfoAsync(UIAutomationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var foregroundHandle = NativeMethods.GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return result; // No foreground window, return original result
            }

            var windowInfo = await _windowEnumerator.GetWindowInfoAsync(foregroundHandle, cancellationToken);
            if (windowInfo == null)
            {
                return result; // Couldn't get window info, return original result
            }

            return result with
            {
                TargetWindow = TargetWindowInfo.FromWindowInfo(windowInfo)
            };
        }
        catch
        {
            // Best effort - if we can't get the window info, just return the original result
            return result;
        }
    }

    /// <summary>
    /// Activates the specified window and brings it to the foreground.
    /// </summary>
    /// <param name="windowHandle">The window handle to activate.</param>
    /// <param name="actionName">The action name for error messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result if activation succeeded, failure result otherwise.</returns>
    private async Task<UIAutomationResult> ActivateTargetWindowAsync(nint windowHandle, string actionName, CancellationToken cancellationToken)
    {
        try
        {
            LogWindowActivation(_logger, windowHandle);

            var activationResult = await _windowService.ActivateWindowAsync(windowHandle, cancellationToken);

            if (!activationResult.Success)
            {
                return UIAutomationResult.CreateFailure(
                    actionName,
                    UIAutomationErrorType.WindowNotFound,
                    $"Failed to activate window with handle {windowHandle}: {activationResult.Error ?? "Unknown error"}. " +
                    $"Verify the window handle is valid using window_management(action='list').",
                    null);
            }

            // Small delay to ensure the window is fully activated and ready for input
            await Task.Delay(50, cancellationToken);

            return UIAutomationResult.CreateSuccess(actionName, Array.Empty<UIElementInfo>(), null);
        }
        catch (Exception ex)
        {
            return UIAutomationResult.CreateFailure(
                actionName,
                UIAutomationErrorType.InternalError,
                $"Failed to activate window: {ex.Message}",
                null);
        }
    }

    #region Action Handlers

    private async Task<UIAutomationResult> HandleFindAsync(
        nint? windowHandle, string? parentElementId, string? name, string? nameContains, string? namePattern,
        string? controlType, string? automationId, string? className, int? exactDepth, int foundIndex,
        bool includeChildren, int timeoutMs, CancellationToken cancellationToken)
    {
        var query = new ElementQuery
        {
            WindowHandle = windowHandle,
            ParentElementId = parentElementId,
            Name = name,
            NameContains = nameContains,
            NamePattern = namePattern,
            ControlType = controlType,
            AutomationId = automationId,
            ClassName = className,
            ExactDepth = exactDepth,
            FoundIndex = foundIndex,
            IncludeChildren = includeChildren,
            TimeoutMs = timeoutMs
        };

        return await _automationService.FindElementsAsync(query, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleGetTreeAsync(
        nint? windowHandle, string? parentElementId, int maxDepth, string? controlType, CancellationToken cancellationToken)
    {
        return await _automationService.GetTreeAsync(windowHandle, parentElementId, maxDepth, controlType, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleWaitForAsync(
        nint? windowHandle, string? name, string? nameContains, string? namePattern,
        string? controlType, string? automationId, string? className, int? exactDepth, int foundIndex,
        int timeoutMs, CancellationToken cancellationToken)
    {
        var query = new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = name,
            NameContains = nameContains,
            NamePattern = namePattern,
            ControlType = controlType,
            AutomationId = automationId,
            ClassName = className,
            ExactDepth = exactDepth,
            FoundIndex = foundIndex,
            TimeoutMs = timeoutMs
        };

        return await _automationService.WaitForElementAsync(query, timeoutMs, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleClickAsync(
        string? elementId, nint? windowHandle, string? name, string? nameContains, string? namePattern,
        string? controlType, string? automationId, string? className, int foundIndex,
        CancellationToken cancellationToken)
    {
        // If element ID provided, use it directly
        if (!string.IsNullOrEmpty(elementId))
        {
            return await _automationService.ClickElementAsync(elementId, windowHandle, cancellationToken);
        }

        // Otherwise, find and click
        var query = new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = name,
            NameContains = nameContains,
            NamePattern = namePattern,
            ControlType = controlType,
            AutomationId = automationId,
            ClassName = className,
            FoundIndex = foundIndex
        };

        return await _automationService.FindAndClickAsync(query, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleTypeAsync(
        string? elementId, nint? windowHandle, string? name, string? nameContains, string? namePattern,
        string? controlType, string? automationId, string? className, int foundIndex,
        string? text, bool clearFirst, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            return UIAutomationResult.CreateFailure("type", UIAutomationErrorType.InvalidParameter, "Text is required for type action", null);
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
            FoundIndex = foundIndex
        };

        return await _automationService.FindAndTypeAsync(query, text, clearFirst, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleSelectAsync(
        string? elementId, nint? windowHandle, string? name, string? controlType, string? automationId,
        string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value))
        {
            return UIAutomationResult.CreateFailure("select", UIAutomationErrorType.InvalidParameter, "Value is required for select action", null);
        }

        var query = new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = name,
            ControlType = controlType,
            AutomationId = automationId
        };

        return await _automationService.FindAndSelectAsync(query, value, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleToggleAsync(string? elementId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return UIAutomationResult.CreateFailure("toggle", UIAutomationErrorType.InvalidParameter, "Element ID is required for toggle action", null);
        }

        return await _automationService.InvokePatternAsync(elementId, PatternTypes.Toggle, null, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleInvokeAsync(string? elementId, string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return UIAutomationResult.CreateFailure("invoke", UIAutomationErrorType.InvalidParameter, "Element ID is required for invoke action", null);
        }

        return await _automationService.InvokePatternAsync(elementId, PatternTypes.Invoke, value, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleFocusAsync(string? elementId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return UIAutomationResult.CreateFailure("focus", UIAutomationErrorType.InvalidParameter, "Element ID is required for focus action", null);
        }

        return await _automationService.FocusElementAsync(elementId, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleScrollIntoViewAsync(
        string? elementId, nint? windowHandle, string? name, string? controlType, string? automationId,
        int timeoutMs, CancellationToken cancellationToken)
    {
        ElementQuery? query = null;
        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(controlType) || !string.IsNullOrEmpty(automationId))
        {
            query = new ElementQuery
            {
                WindowHandle = windowHandle,
                Name = name,
                ControlType = controlType,
                AutomationId = automationId
            };
        }

        return await _automationService.ScrollIntoViewAsync(elementId, query, timeoutMs, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleGetTextAsync(
        string? elementId, nint? windowHandle, bool includeChildren, CancellationToken cancellationToken)
    {
        return await _automationService.GetTextAsync(elementId, windowHandle, includeChildren, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleHighlightAsync(string? elementId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return UIAutomationResult.CreateFailure("highlight", UIAutomationErrorType.InvalidParameter, "Element ID is required for highlight action", null);
        }

        return await _automationService.HighlightElementAsync(elementId, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleOcrAsync(nint? windowHandle, string? language, CancellationToken cancellationToken)
    {
        try
        {
            // Determine the area to capture
            System.Drawing.Rectangle captureRect;

            if (windowHandle.HasValue && windowHandle.Value != nint.Zero)
            {
                // Get window bounds using native API
                if (!NativeMethods.GetWindowRect(windowHandle.Value, out var rect))
                {
                    return UIAutomationResult.CreateFailure("ocr", UIAutomationErrorType.ElementNotFound, "Could not get window bounds", null);
                }
                captureRect = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            else
            {
                // Capture primary screen
                captureRect = new System.Drawing.Rectangle(0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920,
                    System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080);
            }

            // Capture the screen region
            using var bitmap = new System.Drawing.Bitmap(captureRect.Width, captureRect.Height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(captureRect.Left, captureRect.Top, 0, 0, bitmap.Size);
            }

            // Perform OCR
            var ocrResult = await _ocrService.RecognizeAsync(bitmap, language, cancellationToken);

            if (!ocrResult.Success)
            {
                return UIAutomationResult.CreateFailure("ocr", UIAutomationErrorType.InternalError, ocrResult.ErrorMessage ?? "OCR failed", null);
            }

            // Build rich text response with structured OCR data
            var resultText = BuildOcrResultText(ocrResult, captureRect.X, captureRect.Y, captureRect.Width, captureRect.Height);

            return UIAutomationResult.CreateSuccessWithText("ocr", resultText, null);
        }
        catch (Exception ex)
        {
            return UIAutomationResult.CreateFailure("ocr", UIAutomationErrorType.InternalError, $"OCR failed: {ex.Message}", null);
        }
    }

    private async Task<UIAutomationResult> HandleOcrElementAsync(string? elementId, string? language, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return UIAutomationResult.CreateFailure("ocr_element", UIAutomationErrorType.InvalidParameter, "elementId is required for ocr_element action", null);
        }

        try
        {
            // Resolve the element from the automation service
            var elementInfo = await _automationService.ResolveElementAsync(elementId, cancellationToken);
            if (elementInfo == null)
            {
                return UIAutomationResult.CreateFailure("ocr_element", UIAutomationErrorType.ElementNotFound, $"Element '{elementId}' not found in cache. Use 'find' action first.", null);
            }

            // Check if element has valid bounds
            var bounds = elementInfo.BoundingRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return UIAutomationResult.CreateFailure("ocr_element", UIAutomationErrorType.InvalidParameter, "Element has no visible bounding rectangle", null);
            }

            var captureRect = new System.Drawing.Rectangle(
                (int)bounds.X,
                (int)bounds.Y,
                (int)bounds.Width,
                (int)bounds.Height);

            // Capture the element region
            using var bitmap = new System.Drawing.Bitmap(captureRect.Width, captureRect.Height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(captureRect.Left, captureRect.Top, 0, 0, bitmap.Size);
            }

            // Perform OCR
            var ocrResult = await _ocrService.RecognizeAsync(bitmap, language, cancellationToken);

            if (!ocrResult.Success)
            {
                return UIAutomationResult.CreateFailure("ocr_element", UIAutomationErrorType.InternalError, ocrResult.ErrorMessage ?? "OCR failed", null);
            }

            // Build rich text response with structured OCR data
            var resultText = BuildOcrResultText(ocrResult, captureRect.X, captureRect.Y, captureRect.Width, captureRect.Height);

            return UIAutomationResult.CreateSuccessWithText("ocr_element", resultText, null);
        }
        catch (Exception ex)
        {
            return UIAutomationResult.CreateFailure("ocr_element", UIAutomationErrorType.InternalError, $"OCR failed: {ex.Message}", null);
        }
    }

    private UIAutomationResult HandleOcrStatus()
    {
        var status = _ocrService.GetStatus();

        // Build status text response using string concatenation to avoid CA1305 on AppendLine with interpolation
        var lines = new[]
        {
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "OCR Available: {0}", status.Available),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Default Engine: {0}", status.DefaultEngine),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Legacy OCR Available: {0}", status.LegacyAvailable),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Available Languages: {0}", string.Join(", ", status.AvailableLanguages))
        };

        return UIAutomationResult.CreateSuccessWithText("ocr_status", string.Join(Environment.NewLine, lines), null);
    }

    private static string BuildOcrResultText(OcrResult ocrResult, int captureX, int captureY, int captureWidth, int captureHeight)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Engine: {ocrResult.Engine}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Language: {ocrResult.Language}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Duration: {ocrResult.DurationMs}ms");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Capture Region: ({captureX}, {captureY}) {captureWidth}x{captureHeight}");
        sb.AppendLine();
        sb.AppendLine("=== Recognized Text ===");
        sb.AppendLine(ocrResult.Text);

        if (ocrResult.Lines != null && ocrResult.Lines.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== Lines with Bounds ===");
            for (int i = 0; i < ocrResult.Lines.Length; i++)
            {
                var line = ocrResult.Lines[i];
                var rect = line.BoundingRect;
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Line {i + 1} [{rect.X},{rect.Y} {rect.Width}x{rect.Height}]: {line.Text}");
            }
        }

        return sb.ToString();
    }

    private async Task<UIAutomationResult> HandleGetAncestorsAsync(
        string? elementId, int maxDepth, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return UIAutomationResult.CreateFailure("get_ancestors", UIAutomationErrorType.InvalidParameter,
                "elementId is required for get_ancestors action. First use 'find' to get an element ID.", null);
        }

        // Use maxDepth as the ancestor depth limit (null for unlimited)
        int? depthLimit = maxDepth > 0 && maxDepth < 20 ? maxDepth : null;
        return await _automationService.GetAncestorsAsync(elementId, depthLimit, cancellationToken);
    }

    private async Task<UIAutomationResult> HandleCaptureAnnotatedAsync(
        nint? windowHandle, string? controlTypeFilter, int maxDepth, CancellationToken cancellationToken)
    {
        // Use maxDepth for capture_annotated, with sensible defaults:
        // - If maxDepth is default (5), use 15 for Electron compatibility
        // - Otherwise use the caller's value, clamped to valid range
        var searchDepth = maxDepth == 5 ? 15 : Math.Clamp(maxDepth, 1, 20);

        var result = await _annotatedScreenshotService.CaptureAsync(
            windowHandle,
            controlTypeFilter,
            maxElements: 50,
            searchDepth: searchDepth,
            Models.ImageFormat.Jpeg,
            quality: 85,
            cancellationToken);

        if (!result.Success)
        {
            return UIAutomationResult.CreateFailure("capture_annotated", UIAutomationErrorType.InternalError,
                result.ErrorMessage ?? "Failed to capture annotated screenshot", null);
        }

        // Return success with annotated screenshot data
        return new UIAutomationResult
        {
            Success = true,
            Action = "capture_annotated",
            AnnotatedImageData = result.ImageData,
            AnnotatedImageFormat = result.ImageFormat,
            AnnotatedImageWidth = result.Width,
            AnnotatedImageHeight = result.Height,
            AnnotatedElements = result.Elements,
            ElementCount = result.ElementCount,
            UsageHint = $"Screenshot with {result.ElementCount} numbered elements. Reference elements by their index number (1-{result.ElementCount}). " +
                        "Each element has an elementId you can use for subsequent operations like click, type, toggle."
        };
    }

    #endregion

    #region LoggerMessage Methods

    [LoggerMessage(Level = LogLevel.Debug, Message = "UI Automation action: {Action}, Name: {Name}, ControlType: {ControlType}, AutomationId: {AutomationId}")]
    private static partial void LogActionStarted(ILogger logger, UIAutomationAction action, string? name, string? controlType, string? automationId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing UI Automation action: {Action}")]
    private static partial void LogActionError(ILogger logger, UIAutomationAction action, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Auto-activating target window with handle: {WindowHandle}")]
    private static partial void LogWindowActivation(ILogger logger, nint windowHandle);

    #endregion
}

/// <summary>
/// UI Automation action types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UIAutomationAction
{
    /// <summary>Search for elements matching criteria.</summary>
    [Description("Search for elements matching criteria")]
    Find,

    /// <summary>Get UI element hierarchy.</summary>
    [Description("Get UI element hierarchy")]
    GetTree,

    /// <summary>Wait for an element to appear.</summary>
    [Description("Wait for an element to appear")]
    WaitFor,

    /// <summary>Click an element.</summary>
    [Description("Click an element")]
    Click,

    /// <summary>Type text into an element.</summary>
    [Description("Type text into an element")]
    Type,

    /// <summary>Select an item from a list or combo box.</summary>
    [Description("Select an item from a list or combo box")]
    Select,

    /// <summary>Toggle a checkbox or toggle button.</summary>
    [Description("Toggle a checkbox or toggle button")]
    Toggle,

    /// <summary>Invoke a pattern on an element.</summary>
    [Description("Invoke a pattern on an element")]
    Invoke,

    /// <summary>Set keyboard focus to an element.</summary>
    [Description("Set keyboard focus to an element")]
    Focus,

    /// <summary>Scroll an element into view.</summary>
    [Description("Scroll an element into view")]
    ScrollIntoView,

    /// <summary>Get text content from an element.</summary>
    [Description("Get text content from an element")]
    GetText,

    /// <summary>Visually highlight an element for debugging. Persists until HideHighlight is called.</summary>
    [Description("Visually highlight an element for debugging. Persists until HideHighlight is called.")]
    Highlight,

    /// <summary>Hide the current highlight rectangle.</summary>
    [Description("Hide the current highlight rectangle")]
    HideHighlight,

    /// <summary>Extract text from screen or window using OCR.</summary>
    [Description("Extract text from screen or window using OCR")]
    Ocr,

    /// <summary>Extract text from a specific element's bounding rectangle using OCR.</summary>
    [Description("Extract text from a specific element's bounding rectangle using OCR")]
    OcrElement,

    /// <summary>Get OCR engine availability and status information.</summary>
    [Description("Get OCR engine availability and status information")]
    OcrStatus,

    /// <summary>Get the UI element at the current cursor position.</summary>
    [Description("Get the UI element at the current cursor position")]
    GetElementAtCursor,

    /// <summary>Get the currently focused UI element.</summary>
    [Description("Get the currently focused UI element")]
    GetFocusedElement,

    /// <summary>Get all ancestor elements of an element up to the window root.</summary>
    [Description("Get all ancestor elements of an element up to the window root")]
    GetAncestors,

    /// <summary>Capture annotated screenshot with numbered labels on interactive elements.</summary>
    [Description("Capture annotated screenshot with numbered labels on interactive elements. Returns image + element mapping.")]
    CaptureAnnotated
}
