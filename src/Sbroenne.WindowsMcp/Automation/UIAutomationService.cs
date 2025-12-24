using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows.Automation;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Implementation of the UI Automation service using Windows UI Automation APIs.
/// All operations are dispatched to a dedicated STA thread.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class UIAutomationService : IUIAutomationService, IDisposable
{
    private readonly UIAutomationThread _staThread;
    private readonly IMonitorService _monitorService;
    private readonly IMouseInputService _mouseService;
    private readonly IKeyboardInputService _keyboardService;
    private readonly IWindowService? _windowService;
    private readonly IElevationDetector _elevationDetector;
    private readonly ILogger<UIAutomationService> _logger;
    private readonly CoordinateConverter _coordinateConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAutomationService"/> class.
    /// </summary>
    /// <param name="staThread">The STA thread for UI Automation operations.</param>
    /// <param name="monitorService">The monitor service for coordinate conversion.</param>
    /// <param name="mouseService">The mouse input service for coordinate-based clicks.</param>
    /// <param name="keyboardService">The keyboard input service for text entry.</param>
    /// <param name="windowService">Optional window service for window activation.</param>
    /// <param name="elevationDetector">The elevation detector for checking elevated processes.</param>
    /// <param name="logger">The logger.</param>
    public UIAutomationService(
        UIAutomationThread staThread,
        IMonitorService monitorService,
        IMouseInputService mouseService,
        IKeyboardInputService keyboardService,
        IWindowService? windowService,
        IElevationDetector elevationDetector,
        ILogger<UIAutomationService> logger)
    {
        ArgumentNullException.ThrowIfNull(staThread);
        ArgumentNullException.ThrowIfNull(monitorService);
        ArgumentNullException.ThrowIfNull(mouseService);
        ArgumentNullException.ThrowIfNull(keyboardService);
        ArgumentNullException.ThrowIfNull(elevationDetector);
        ArgumentNullException.ThrowIfNull(logger);

        _staThread = staThread;
        _monitorService = monitorService;
        _mouseService = mouseService;
        _keyboardService = keyboardService;
        _windowService = windowService;
        _elevationDetector = elevationDetector;
        _logger = logger;
        _coordinateConverter = new CoordinateConverter(monitorService);
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindElementsAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                // If ParentElementId is provided, use it as the search root
                AutomationElement? rootElement;
                if (!string.IsNullOrEmpty(query.ParentElementId))
                {
                    rootElement = ElementIdGenerator.ResolveToAutomationElement(query.ParentElementId);
                    if (rootElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "find",
                            UIAutomationErrorType.ElementNotFound,
                            $"Parent element not found or stale: {query.ParentElementId}",
                            CreateDiagnostics(stopwatch, query));
                    }
                }
                else
                {
                    rootElement = GetRootElement(query.WindowHandle);
                }

                if (rootElement == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.WindowNotFound,
                        "Could not find the specified window or foreground window.",
                        CreateDiagnostics(stopwatch, query));
                }

                var condition = BuildCondition(query);
                var scope = query.MaxDepth == 0 ? TreeScope.Children : TreeScope.Descendants;

                var elements = rootElement.FindAll(scope, condition);
                var elementInfos = new List<UIElementInfo>();

                foreach (AutomationElement element in elements)
                {
                    try
                    {
                        var elementInfo = ConvertToElementInfo(element, rootElement, query.IncludeChildren);
                        if (elementInfo != null)
                        {
                            elementInfos.Add(elementInfo);
                        }
                    }
                    catch (ElementNotAvailableException)
                    {
                        // Element disappeared while iterating - skip it
                    }
                }

                stopwatch.Stop();

                if (elementInfos.Count == 0)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.ElementNotFound,
                        BuildNotFoundMessage(query),
                        CreateDiagnostics(stopwatch, query, elements.Count));
                }

                if (elementInfos.Count == 1)
                {
                    return UIAutomationResult.CreateSuccess("find", elementInfos[0], CreateDiagnostics(stopwatch, query, elements.Count));
                }

                return UIAutomationResult.CreateSuccess("find", [.. elementInfos], CreateDiagnostics(stopwatch, query, elements.Count));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindElementsError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "find",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch, query));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetTreeAsync(nint? windowHandle, string? parentElementId, int maxDepth, string? controlTypeFilter, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                // If parentElementId is provided, use it as the tree root
                AutomationElement? rootElement;
                if (!string.IsNullOrEmpty(parentElementId))
                {
                    rootElement = ElementIdGenerator.ResolveToAutomationElement(parentElementId);
                    if (rootElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "get_tree",
                            UIAutomationErrorType.ElementNotFound,
                            $"Parent element not found or stale: {parentElementId}",
                            CreateDiagnostics(stopwatch));
                    }
                }
                else
                {
                    rootElement = GetRootElement(windowHandle);
                }

                if (rootElement == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_tree",
                        UIAutomationErrorType.WindowNotFound,
                        "Could not find the specified window or foreground window.",
                        CreateDiagnostics(stopwatch));
                }

                var controlTypeSet = ParseControlTypeFilter(controlTypeFilter);
                var elementsScanned = 0;
                var tree = BuildElementTree(rootElement, rootElement, maxDepth, 0, controlTypeSet, ref elementsScanned);

                stopwatch.Stop();

                if (tree == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_tree",
                        UIAutomationErrorType.ElementNotFound,
                        "Could not build element tree.",
                        CreateDiagnostics(stopwatch, elementsScanned: elementsScanned));
                }

                return UIAutomationResult.CreateSuccess("get_tree", [tree], CreateDiagnostics(stopwatch, elementsScanned: elementsScanned));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetTreeError(_logger, windowHandle, ex);
            return UIAutomationResult.CreateFailure(
                "get_tree",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> WaitForElementAsync(ElementQuery query, int timeoutMs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var stopwatch = Stopwatch.StartNew();
        var delay = 50; // Start with 50ms polling interval
        const int MaxDelay = 500; // Cap at 500ms

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await FindElementsAsync(query with { TimeoutMs = 0 }, cancellationToken);
            if (result.Success)
            {
                return result with { Action = "wait_for" };
            }

            // Exponential backoff
            await Task.Delay(delay, cancellationToken);
            delay = Math.Min(delay * 2, MaxDelay);
        }

        stopwatch.Stop();

        return UIAutomationResult.CreateFailure(
            "wait_for",
            UIAutomationErrorType.Timeout,
            $"Element not found within {timeoutMs}ms timeout.",
            new UIAutomationDiagnostics
            {
                DurationMs = stopwatch.ElapsedMilliseconds,
                Query = query,
                ElapsedBeforeTimeout = stopwatch.ElapsedMilliseconds
            });
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> ScrollIntoViewAsync(string? elementId, ElementQuery? query, int timeoutMs, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // First, resolve the element
            AutomationElement? element = null;

            if (!string.IsNullOrEmpty(elementId))
            {
                element = await _staThread.ExecuteAsync(() =>
                    ElementIdGenerator.ResolveToAutomationElement(elementId), cancellationToken);

                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "scroll_into_view",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element with ID '{elementId}' not found.",
                        CreateDiagnostics(stopwatch));
                }
            }
            else if (query != null)
            {
                var findResult = await FindElementsAsync(query with { TimeoutMs = 0 }, cancellationToken);
                if (!findResult.Success || findResult.Element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "scroll_into_view",
                        UIAutomationErrorType.ElementNotFound,
                        findResult.ErrorMessage ?? "Element not found matching query.",
                        CreateDiagnostics(stopwatch, query));
                }

                // Resolve the found element ID to AutomationElement
                element = await _staThread.ExecuteAsync(() =>
                    ElementIdGenerator.ResolveToAutomationElement(findResult.Element.ElementId), cancellationToken);
            }
            else
            {
                return UIAutomationResult.CreateFailure(
                    "scroll_into_view",
                    UIAutomationErrorType.InvalidParameter,
                    "Either elementId or query must be provided.",
                    CreateDiagnostics(stopwatch));
            }

            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "scroll_into_view",
                    UIAutomationErrorType.ElementStale,
                    "Element could not be resolved for scrolling.",
                    CreateDiagnostics(stopwatch));
            }

            // Try ScrollItemPattern on the element
            return await _staThread.ExecuteAsync(() =>
            {
                // Get the root element for element info conversion
                var rootElement = GetRootElementForScroll(element);

                var scrollResult = TryScrollItemPattern(element);
                if (scrollResult.success)
                {
                    var elementInfo = ConvertToElementInfo(element, rootElement, false);
                    if (elementInfo == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "scroll_into_view",
                            UIAutomationErrorType.ElementStale,
                            "Element became unavailable after scrolling.",
                            CreateDiagnostics(stopwatch));
                    }
                    return UIAutomationResult.CreateSuccess("scroll_into_view", elementInfo, CreateDiagnostics(stopwatch));
                }

                // If element doesn't support ScrollItemPattern, try finding a scrollable parent
                var scrollResult2 = TryScrollParentToElement(element, stopwatch.ElapsedMilliseconds, timeoutMs);
                if (scrollResult2.success)
                {
                    var elementInfo = ConvertToElementInfo(element, rootElement, false);
                    if (elementInfo == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "scroll_into_view",
                            UIAutomationErrorType.ElementStale,
                            "Element became unavailable after scrolling.",
                            CreateDiagnostics(stopwatch));
                    }
                    return UIAutomationResult.CreateSuccess("scroll_into_view", elementInfo, CreateDiagnostics(stopwatch));
                }

                // Neither approach worked
                return UIAutomationResult.CreateFailure(
                    "scroll_into_view",
                    UIAutomationErrorType.PatternNotSupported,
                    scrollResult2.errorMessage ?? "Element does not support scrolling and no scrollable parent was found.",
                    CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogScrollIntoViewError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "scroll_into_view",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <summary>
    /// Tries to scroll element into view using ScrollItemPattern.
    /// </summary>
    private static (bool success, string? errorMessage) TryScrollItemPattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var pattern))
            {
                var scrollItemPattern = (ScrollItemPattern)pattern;
                scrollItemPattern.ScrollIntoView();
                return (true, null);
            }
            return (false, "Element does not support ScrollItemPattern.");
        }
        catch (Exception ex)
        {
            return (false, $"ScrollItemPattern failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tries to scroll a parent container to bring the element into view.
    /// </summary>
    private static (bool success, string? errorMessage) TryScrollParentToElement(AutomationElement element, long elapsedMs, int timeoutMs)
    {
        try
        {
            // Walk up the tree to find a scrollable parent
            var walker = TreeWalker.ControlViewWalker;
            var parent = walker.GetParent(element);

            while (parent != null && !Equals(parent, AutomationElement.RootElement))
            {
                if (parent.TryGetCurrentPattern(ScrollPattern.Pattern, out var pattern))
                {
                    var scrollPattern = (ScrollPattern)pattern;

                    // Check if element is currently visible
                    if (!IsElementOffscreen(element))
                    {
                        return (true, null); // Already visible
                    }

                    // Get element and parent bounds
                    var elementRect = element.Current.BoundingRectangle;
                    var parentRect = parent.Current.BoundingRectangle;

                    if (elementRect.IsEmpty || parentRect.IsEmpty)
                    {
                        // Can't determine position, try a single scroll
                        scrollPattern.ScrollVertical(ScrollAmount.LargeIncrement);
                        Thread.Sleep(100); // Allow UI to update
                        return (!IsElementOffscreen(element), null);
                    }

                    // Calculate if we need to scroll up or down
                    int scrollAttempts = 0;
                    const int MaxAttempts = 50; // Safety limit

                    while (scrollAttempts < MaxAttempts && elapsedMs < timeoutMs)
                    {
                        if (!IsElementOffscreen(element))
                        {
                            return (true, null);
                        }

                        elementRect = element.Current.BoundingRectangle;
                        parentRect = parent.Current.BoundingRectangle;

                        if (elementRect.Top < parentRect.Top)
                        {
                            // Element is above visible area, scroll up
                            if (!scrollPattern.Current.VerticallyScrollable || scrollPattern.Current.VerticalScrollPercent <= 0)
                            {
                                break;
                            }
                            scrollPattern.ScrollVertical(ScrollAmount.LargeDecrement);
                        }
                        else if (elementRect.Bottom > parentRect.Bottom)
                        {
                            // Element is below visible area, scroll down
                            if (!scrollPattern.Current.VerticallyScrollable || scrollPattern.Current.VerticalScrollPercent >= 100)
                            {
                                break;
                            }
                            scrollPattern.ScrollVertical(ScrollAmount.LargeIncrement);
                        }
                        else
                        {
                            // Element is within vertical bounds, check horizontal
                            if (elementRect.Left < parentRect.Left && scrollPattern.Current.HorizontallyScrollable)
                            {
                                scrollPattern.ScrollHorizontal(ScrollAmount.LargeDecrement);
                            }
                            else if (elementRect.Right > parentRect.Right && scrollPattern.Current.HorizontallyScrollable)
                            {
                                scrollPattern.ScrollHorizontal(ScrollAmount.LargeIncrement);
                            }
                            else
                            {
                                break; // No more scrolling needed
                            }
                        }

                        Thread.Sleep(50); // Allow UI to update
                        scrollAttempts++;
                    }

                    return (!IsElementOffscreen(element), null);
                }

                parent = walker.GetParent(parent);
            }

            return (false, "No scrollable parent found.");
        }
        catch (Exception ex)
        {
            return (false, $"Parent scroll failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if an element is offscreen.
    /// </summary>
    private static bool IsElementOffscreen(AutomationElement element)
    {
        try
        {
            return element.Current.IsOffscreen;
        }
        catch
        {
            return true; // Assume offscreen if we can't determine
        }
    }

    /// <summary>
    /// Gets the root element for an element (the containing window or desktop).
    /// </summary>
    private static AutomationElement GetRootElementForScroll(AutomationElement element)
    {
        try
        {
            var walker = TreeWalker.ControlViewWalker;
            var current = element;
            AutomationElement? lastWindow = null;

            while (current != null && !Equals(current, AutomationElement.RootElement))
            {
                if (current.Current.ControlType == ControlType.Window)
                {
                    lastWindow = current;
                }
                current = walker.GetParent(current);
            }

            return lastWindow ?? AutomationElement.RootElement;
        }
        catch
        {
            return AutomationElement.RootElement;
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetTextAsync(string? elementId, nint? windowHandle, bool includeChildren, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                AutomationElement? targetElement = null;

                if (!string.IsNullOrEmpty(elementId))
                {
                    targetElement = ElementIdGenerator.ResolveToAutomationElement(elementId);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "get_text",
                            UIAutomationErrorType.ElementNotFound,
                            $"Element with ID '{elementId}' not found.",
                            CreateDiagnostics(stopwatch));
                    }
                }
                else
                {
                    // Use window handle or foreground window
                    targetElement = GetRootElement(windowHandle);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "get_text",
                            UIAutomationErrorType.WindowNotFound,
                            "No foreground window found.",
                            CreateDiagnostics(stopwatch));
                    }
                }

                // Extract text from the element
                var text = ExtractText(targetElement, includeChildren);

                return UIAutomationResult.CreateSuccessWithText("get_text", text, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetTextError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "get_text",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <summary>
    /// Extracts text from an element using ValuePattern, TextPattern, or Name fallback.
    /// </summary>
    private static string ExtractText(AutomationElement element, bool includeChildren)
    {
        // Try ValuePattern first (for Edit, Document controls)
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? valuePattern))
        {
            var value = ((ValuePattern)valuePattern).Current.Value;
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        // Try TextPattern for rich text controls
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out object? textPattern))
        {
            var documentRange = ((TextPattern)textPattern).DocumentRange;
            var text = documentRange.GetText(-1); // -1 means get all text
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        // Fall back to Name property
        var name = element.Current.Name;
        if (!string.IsNullOrEmpty(name) && !includeChildren)
        {
            return name;
        }

        // If includeChildren, aggregate all text from descendants
        if (includeChildren)
        {
            var allText = new List<string>();
            if (!string.IsNullOrEmpty(name))
            {
                allText.Add(name);
            }

            CollectChildText(element, allText);
            return string.Join(" ", allText);
        }

        return name ?? "";
    }

    /// <summary>
    /// Recursively collects text from child elements.
    /// </summary>
    private static void CollectChildText(AutomationElement parent, List<string> textParts)
    {
        var condition = new PropertyCondition(AutomationElement.IsOffscreenProperty, false);
        var children = parent.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement child in children)
        {
            try
            {
                // Try to get text from this child
                var childText = GetElementText(child);
                if (!string.IsNullOrWhiteSpace(childText) && !textParts.Contains(childText))
                {
                    textParts.Add(childText);
                }

                // Recurse into children (limited depth)
                if (textParts.Count < 1000) // Safety limit
                {
                    CollectChildText(child, textParts);
                }
            }
            catch (ElementNotAvailableException)
            {
                // Element went away, skip it
            }
        }
    }

    /// <summary>
    /// Gets text from a single element without recursion.
    /// </summary>
    private static string? GetElementText(AutomationElement element)
    {
        try
        {
            // Try ValuePattern
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? valuePattern))
            {
                var value = ((ValuePattern)valuePattern).Current.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // Return Name property
            var name = element.Current.Name;
            return string.IsNullOrEmpty(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> InvokePatternAsync(string elementId, string pattern, string? value, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "invoke",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element not found or stale: {elementId}",
                        CreateDiagnostics(stopwatch));
                }

                // Check for elevated target before invoking pattern
                var elevationCheck = CheckElevatedTarget(element);
                if (!elevationCheck.Success)
                {
                    return elevationCheck with { Action = "invoke" };
                }

                var patternName = pattern?.ToUpperInvariant() ?? "INVOKE";
                var (success, errorMessage) = patternName switch
                {
                    "INVOKE" => TryInvokePattern(element),
                    "TOGGLE" => TryTogglePattern(element),
                    "EXPAND" => TryExpandCollapsePattern(element, expand: true),
                    "COLLAPSE" => TryExpandCollapsePattern(element, expand: false),
                    "EXPANDCOLLAPSE" => TryExpandCollapsePattern(element, expand: true),
                    "VALUE" => TryValuePattern(element, value),
                    "RANGEVALUE" => TryRangeValuePattern(element, value),
                    "SCROLL" => TryScrollPattern(element, value),
                    _ => (false, $"Unknown pattern: {pattern}. Supported patterns: Invoke, Toggle, Expand, Collapse, Value, RangeValue, Scroll")
                };

                stopwatch.Stop();

                if (!success)
                {
                    // Get available patterns for the element
                    var availablePatterns = GetAvailablePatterns(element);
                    var errorMsg = string.IsNullOrEmpty(errorMessage)
                        ? $"Pattern '{pattern}' not supported on this element. Available patterns: {string.Join(", ", availablePatterns)}"
                        : errorMessage;

                    return UIAutomationResult.CreateFailure(
                        "invoke",
                        UIAutomationErrorType.PatternNotSupported,
                        errorMsg,
                        CreateDiagnostics(stopwatch));
                }

                // Return the updated element info
                var rootElement = GetRootElementFromElementId(elementId) ?? element;
                var elementInfo = ConvertToElementInfo(element, rootElement, includeChildren: false);

                return UIAutomationResult.CreateSuccess("invoke", elementInfo!, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogInvokePatternError(_logger, pattern ?? "Invoke", elementId, ex);
            return UIAutomationResult.CreateFailure(
                "invoke",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FocusElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "focus",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element not found or stale: {elementId}",
                        CreateDiagnostics(stopwatch));
                }

                // Check for elevated target before setting focus
                var elevationCheck = CheckElevatedTarget(element);
                if (!elevationCheck.Success)
                {
                    return elevationCheck with { Action = "focus" };
                }

                try
                {
                    element.SetFocus();
                }
                catch (Exception ex)
                {
                    return UIAutomationResult.CreateFailure(
                        "focus",
                        UIAutomationErrorType.PatternNotSupported,
                        $"Failed to set focus: {ex.Message}",
                        CreateDiagnostics(stopwatch));
                }

                stopwatch.Stop();

                // Return the updated element info
                var rootElement = GetRootElementFromElementId(elementId) ?? element;
                var elementInfo = ConvertToElementInfo(element, rootElement, includeChildren: false);

                return UIAutomationResult.CreateSuccess("focus", elementInfo!, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFocusElementError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "focus",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndClickAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // First find the element
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success)
            {
                return findResult with { Action = "find_and_click" };
            }

            // Check for multiple matches
            if (findResult.Elements?.Length > 1)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_click",
                    UIAutomationErrorType.MultipleMatches,
                    $"Found {findResult.Elements.Length} elements matching criteria. Refine query with automationId or parent.",
                    new UIAutomationDiagnostics
                    {
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Query = query,
                        MultipleMatches = findResult.Elements
                    });
            }

            var element = findResult.Element ?? findResult.Elements?[0];
            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_click",
                    UIAutomationErrorType.ElementNotFound,
                    "No element found.",
                    CreateDiagnostics(stopwatch, query));
            }

            // Activate the window containing the element before interaction
            await TryActivateWindowForElementAsync(element, cancellationToken);

            // Try InvokePattern first (preferred for buttons, menu items, etc.)
            if (element.SupportedPatterns.Contains(PatternTypes.Invoke))
            {
                var invokeSuccess = await TryInvokePatternAsync(element.ElementId, cancellationToken);
                if (invokeSuccess)
                {
                    return UIAutomationResult.CreateSuccess("find_and_click", element, CreateDiagnostics(stopwatch, query));
                }
            }

            // Fallback to coordinate click
            var clickX = element.BoundingRect.CenterX;
            var clickY = element.BoundingRect.CenterY;
            var clickResult = await _mouseService.ClickAsync(clickX, clickY, cancellationToken: cancellationToken);

            if (!clickResult.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_click",
                    UIAutomationErrorType.PatternNotSupported,
                    $"Click failed: {clickResult.ErrorCode}",
                    CreateDiagnostics(stopwatch, query));
            }

            return UIAutomationResult.CreateSuccess("find_and_click", element, CreateDiagnostics(stopwatch, query));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndClickError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "find_and_click",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch, query));
        }
    }

    /// <summary>
    /// Tries to invoke the InvokePattern on an element.
    /// </summary>
    private async Task<bool> TryInvokePatternAsync(string elementId, CancellationToken cancellationToken)
    {
        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return false;
                }

                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                {
                    ((InvokePattern)pattern).Invoke();
                    return true;
                }

                return false;
            }, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to activate the window containing an element before interaction.
    /// </summary>
    /// <param name="element">The element whose window should be activated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activation succeeded or was not needed; false if activation failed.</returns>
    private async Task<bool> TryActivateWindowForElementAsync(UIElementInfo element, CancellationToken cancellationToken)
    {
        if (_windowService == null)
        {
            return true; // No window service available, proceed anyway
        }

        try
        {
            // Parse window handle from element ID
            var windowHandle = ExtractWindowHandleFromElementId(element.ElementId);
            if (windowHandle != null && windowHandle.Value != 0)
            {
                var result = await _windowService.ActivateWindowAsync(windowHandle.Value, cancellationToken);
                if (!result.Success)
                {
                    LogWindowActivationFailed(_logger, windowHandle.Value, result.Error ?? "Unknown error");
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            // Log the failure but don't fail the operation - activation is best-effort
            LogWindowActivationException(_logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Extracts the window handle from an element ID.
    /// </summary>
    private static nint? ExtractWindowHandleFromElementId(string elementId)
    {
        // Element ID format: "window:hwnd|runtime:id|path:treePath"
        var parts = elementId.Split('|');
        foreach (var part in parts)
        {
            if (part.StartsWith("window:", StringComparison.OrdinalIgnoreCase))
            {
                var handleStr = part["window:".Length..];
                if (nint.TryParse(handleStr, out var handle))
                {
                    return handle;
                }
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndTypeAsync(ElementQuery query, string text, bool clearFirst, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // First find the element
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success)
            {
                return findResult with { Action = "find_and_type" };
            }

            // Check for multiple matches
            if (findResult.Elements?.Length > 1)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_type",
                    UIAutomationErrorType.MultipleMatches,
                    $"Found {findResult.Elements.Length} elements matching criteria. Refine query with automationId or parent.",
                    new UIAutomationDiagnostics
                    {
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Query = query,
                        MultipleMatches = findResult.Elements
                    });
            }

            var element = findResult.Element ?? findResult.Elements?[0];
            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_type",
                    UIAutomationErrorType.ElementNotFound,
                    "No element found.",
                    CreateDiagnostics(stopwatch, query));
            }

            // Activate the window containing the element before interaction
            await TryActivateWindowForElementAsync(element, cancellationToken);

            // Try ValuePattern first
            if (element.SupportedPatterns.Contains(PatternTypes.Value))
            {
                var valueSuccess = await TrySetValuePatternAsync(element.ElementId, text, clearFirst, cancellationToken);
                if (valueSuccess)
                {
                    return UIAutomationResult.CreateSuccess("find_and_type", element, CreateDiagnostics(stopwatch, query));
                }
            }

            // Fallback to keyboard input
            // First click to focus the element
            var clickX = element.BoundingRect.CenterX;
            var clickY = element.BoundingRect.CenterY;
            var clickResult = await _mouseService.ClickAsync(clickX, clickY, cancellationToken: cancellationToken);

            if (!clickResult.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_type",
                    UIAutomationErrorType.PatternNotSupported,
                    $"Click to focus failed: {clickResult.ErrorCode}",
                    CreateDiagnostics(stopwatch, query));
            }

            // Wait a moment for focus to take effect
            await Task.Delay(50, cancellationToken);

            // Clear existing text if requested
            if (clearFirst)
            {
                await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, cancellationToken: cancellationToken);
            }

            // Type the text
            var typeResult = await _keyboardService.TypeTextAsync(text, cancellationToken);

            if (!typeResult.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_type",
                    UIAutomationErrorType.PatternNotSupported,
                    $"Type failed: {typeResult.ErrorCode}",
                    CreateDiagnostics(stopwatch, query));
            }

            return UIAutomationResult.CreateSuccess("find_and_type", element, CreateDiagnostics(stopwatch, query));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndTypeError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "find_and_type",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch, query));
        }
    }

    /// <summary>
    /// Tries to set text using the ValuePattern.
    /// </summary>
    private async Task<bool> TrySetValuePatternAsync(string elementId, string text, bool clearFirst, CancellationToken cancellationToken)
    {
        _ = clearFirst; // ValuePattern always replaces the entire value
        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return false;
                }

                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
                {
                    var valuePattern = (ValuePattern)pattern;
                    if (!valuePattern.Current.IsReadOnly)
                    {
                        valuePattern.SetValue(text);
                        return true;
                    }
                }

                return false;
            }, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndSelectAsync(ElementQuery query, string value, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // First find the element
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success)
            {
                return findResult with { Action = "find_and_select" };
            }

            // Check for multiple matches
            if (findResult.Elements?.Length > 1)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_select",
                    UIAutomationErrorType.MultipleMatches,
                    $"Found {findResult.Elements.Length} elements matching criteria. Refine query with automationId or parent.",
                    new UIAutomationDiagnostics
                    {
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Query = query,
                        MultipleMatches = findResult.Elements
                    });
            }

            var element = findResult.Element ?? findResult.Elements?[0];
            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "find_and_select",
                    UIAutomationErrorType.ElementNotFound,
                    "No element found.",
                    CreateDiagnostics(stopwatch, query));
            }

            // Activate the window containing the element before interaction
            await TryActivateWindowForElementAsync(element, cancellationToken);

            // Try ExpandCollapsePattern first if available (for combo boxes)
            if (element.SupportedPatterns.Contains(PatternTypes.ExpandCollapse))
            {
                await TryExpandAsync(element.ElementId, cancellationToken);
            }

            // Try SelectionPattern to select the item
            var (selectSuccess, errorMessage) = await TrySelectItemAsync(element.ElementId, value, cancellationToken);
            if (selectSuccess)
            {
                return UIAutomationResult.CreateSuccess("find_and_select", element, CreateDiagnostics(stopwatch, query));
            }

            // Fallback: Try to find and click the item
            var (itemFound, itemError) = await TryClickSelectionItemAsync(element.ElementId, value, cancellationToken);
            if (itemFound)
            {
                return UIAutomationResult.CreateSuccess("find_and_select", element, CreateDiagnostics(stopwatch, query));
            }

            return UIAutomationResult.CreateFailure(
                "find_and_select",
                UIAutomationErrorType.PatternNotSupported,
                errorMessage ?? itemError ?? $"Could not select '{value}'",
                CreateDiagnostics(stopwatch, query));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndSelectError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "find_and_select",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch, query));
        }
    }

    /// <summary>
    /// Tries to expand an element using ExpandCollapsePattern.
    /// </summary>
    private async Task<bool> TryExpandAsync(string elementId, CancellationToken cancellationToken)
    {
        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return false;
                }

                if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object pattern))
                {
                    var expandPattern = (ExpandCollapsePattern)pattern;
                    if (expandPattern.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                    {
                        expandPattern.Expand();
                        return true;
                    }
                }

                return false;
            }, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to select an item using SelectionItemPattern.
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage)> TrySelectItemAsync(string parentElementId, string itemName, CancellationToken cancellationToken)
    {
        try
        {
            return await _staThread.ExecuteAsync<(bool, string?)>(() =>
            {
                var parent = ElementIdGenerator.ResolveToAutomationElement(parentElementId);
                if (parent == null)
                {
                    return (false, "Parent element not found");
                }

                // Find the item by name
                var condition = new PropertyCondition(AutomationElement.NameProperty, itemName, PropertyConditionFlags.IgnoreCase);
                var item = parent.FindFirst(TreeScope.Descendants, condition);

                if (item == null)
                {
                    return (false, $"Item '{itemName}' not found");
                }

                // Try SelectionItemPattern
                if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object pattern))
                {
                    ((SelectionItemPattern)pattern).Select();
                    return (true, null);
                }

                return (false, "SelectionItemPattern not supported");
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Tries to click a selection item directly.
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage)> TryClickSelectionItemAsync(string parentElementId, string itemName, CancellationToken cancellationToken)
    {
        try
        {
            // Find the item coordinates
            var itemCoords = await _staThread.ExecuteAsync(() =>
            {
                var parent = ElementIdGenerator.ResolveToAutomationElement(parentElementId);
                if (parent == null)
                {
                    return ((int?)null, (int?)null);
                }

                // Find the item by name
                var condition = new PropertyCondition(AutomationElement.NameProperty, itemName, PropertyConditionFlags.IgnoreCase);
                var item = parent.FindFirst(TreeScope.Descendants, condition);

                if (item == null)
                {
                    return ((int?)null, (int?)null);
                }

                var rect = item.Current.BoundingRectangle;
                return ((int?)(int)(rect.X + rect.Width / 2), (int?)(int)(rect.Y + rect.Height / 2));
            }, cancellationToken);

            if (itemCoords.Item1.HasValue && itemCoords.Item2.HasValue)
            {
                var clickResult = await _mouseService.ClickAsync(itemCoords.Item1.Value, itemCoords.Item2.Value, cancellationToken: cancellationToken);
                return (clickResult.Success, clickResult.Success ? null : $"Click failed: {clickResult.ErrorCode}");
            }

            return (false, $"Item '{itemName}' not found in dropdown");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<UIElementInfo?> ResolveElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                return ElementIdGenerator.ResolveElement(elementId, _coordinateConverter);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            LogResolveElementError(_logger, elementId, ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // UIAutomationThread is owned by the DI container, don't dispose here
    }

    #region Private Helper Methods

    #region Pattern Invocation Helpers

    private static (bool Success, string? ErrorMessage) TryInvokePattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
            {
                ((InvokePattern)pattern).Invoke();
                return (true, null);
            }
            return (false, "Element does not support InvokePattern");
        }
        catch (Exception ex)
        {
            return (false, $"InvokePattern failed: {ex.Message}");
        }
    }

    private static (bool Success, string? ErrorMessage) TryTogglePattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object pattern))
            {
                ((TogglePattern)pattern).Toggle();
                return (true, null);
            }
            return (false, "Element does not support TogglePattern");
        }
        catch (Exception ex)
        {
            return (false, $"TogglePattern failed: {ex.Message}");
        }
    }

    private static (bool Success, string? ErrorMessage) TryExpandCollapsePattern(AutomationElement element, bool expand)
    {
        try
        {
            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object pattern))
            {
                var ecPattern = (ExpandCollapsePattern)pattern;
                if (expand)
                {
                    ecPattern.Expand();
                }
                else
                {
                    ecPattern.Collapse();
                }
                return (true, null);
            }
            return (false, "Element does not support ExpandCollapsePattern");
        }
        catch (Exception ex)
        {
            return (false, $"ExpandCollapsePattern failed: {ex.Message}");
        }
    }

    private static (bool Success, string? ErrorMessage) TryValuePattern(AutomationElement element, string? value)
    {
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
            {
                var valuePattern = (ValuePattern)pattern;
                if (valuePattern.Current.IsReadOnly)
                {
                    return (false, "Element is read-only");
                }
                valuePattern.SetValue(value ?? string.Empty);
                return (true, null);
            }
            return (false, "Element does not support ValuePattern");
        }
        catch (Exception ex)
        {
            return (false, $"ValuePattern failed: {ex.Message}");
        }
    }

    private static (bool Success, string? ErrorMessage) TryRangeValuePattern(AutomationElement element, string? value)
    {
        try
        {
            if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out object pattern))
            {
                var rangePattern = (RangeValuePattern)pattern;
                if (rangePattern.Current.IsReadOnly)
                {
                    return (false, "Element is read-only");
                }

                if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
                {
                    return (false, $"Invalid numeric value: {value}. Expected a number.");
                }

                var min = rangePattern.Current.Minimum;
                var max = rangePattern.Current.Maximum;
                if (numericValue < min || numericValue > max)
                {
                    return (false, $"Value {numericValue} is out of range [{min}, {max}]");
                }

                rangePattern.SetValue(numericValue);
                return (true, null);
            }
            return (false, "Element does not support RangeValuePattern");
        }
        catch (Exception ex)
        {
            return (false, $"RangeValuePattern failed: {ex.Message}");
        }
    }

    private static (bool Success, string? ErrorMessage) TryScrollPattern(AutomationElement element, string? value)
    {
        try
        {
            if (element.TryGetCurrentPattern(ScrollPattern.Pattern, out object pattern))
            {
                var scrollPattern = (ScrollPattern)pattern;
                var direction = value?.ToUpperInvariant() ?? "DOWN";

                // Parse direction: UP, DOWN, LEFT, RIGHT, or PAGEUP, PAGEDOWN, etc.
                var scrollAmount = ScrollAmount.SmallIncrement;
                if (direction.Contains("PAGE", StringComparison.OrdinalIgnoreCase))
                {
                    scrollAmount = ScrollAmount.LargeIncrement;
                }

                if (direction.Contains("UP", StringComparison.OrdinalIgnoreCase))
                {
                    if (scrollPattern.Current.VerticallyScrollable)
                    {
                        scrollPattern.ScrollVertical(direction.Contains("PAGE", StringComparison.OrdinalIgnoreCase)
                            ? ScrollAmount.LargeDecrement
                            : ScrollAmount.SmallDecrement);
                        return (true, null);
                    }
                    return (false, "Element is not vertically scrollable");
                }
                else if (direction.Contains("DOWN", StringComparison.OrdinalIgnoreCase))
                {
                    if (scrollPattern.Current.VerticallyScrollable)
                    {
                        scrollPattern.ScrollVertical(scrollAmount);
                        return (true, null);
                    }
                    return (false, "Element is not vertically scrollable");
                }
                else if (direction.Contains("LEFT", StringComparison.OrdinalIgnoreCase))
                {
                    if (scrollPattern.Current.HorizontallyScrollable)
                    {
                        scrollPattern.ScrollHorizontal(ScrollAmount.SmallDecrement);
                        return (true, null);
                    }
                    return (false, "Element is not horizontally scrollable");
                }
                else if (direction.Contains("RIGHT", StringComparison.OrdinalIgnoreCase))
                {
                    if (scrollPattern.Current.HorizontallyScrollable)
                    {
                        scrollPattern.ScrollHorizontal(ScrollAmount.SmallIncrement);
                        return (true, null);
                    }
                    return (false, "Element is not horizontally scrollable");
                }

                return (false, $"Unknown scroll direction: {value}. Use UP, DOWN, LEFT, RIGHT, PAGEUP, or PAGEDOWN.");
            }
            return (false, "Element does not support ScrollPattern");
        }
        catch (Exception ex)
        {
            return (false, $"ScrollPattern failed: {ex.Message}");
        }
    }

    private static List<string> GetAvailablePatterns(AutomationElement element)
    {
        var patterns = new List<string>();

        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out _))
        {
            patterns.Add("Invoke");
        }

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out _))
        {
            patterns.Add("Toggle");
        }

        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out _))
        {
            patterns.Add("ExpandCollapse");
        }

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out _))
        {
            patterns.Add("Value");
        }

        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out _))
        {
            patterns.Add("RangeValue");
        }

        if (element.TryGetCurrentPattern(ScrollPattern.Pattern, out _))
        {
            patterns.Add("Scroll");
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _))
        {
            patterns.Add("SelectionItem");
        }

        if (element.TryGetCurrentPattern(SelectionPattern.Pattern, out _))
        {
            patterns.Add("Selection");
        }

        return patterns.Count > 0 ? patterns : ["None"];
    }

    #endregion

    #region Elevation Detection Helpers

    /// <summary>
    /// Checks if the target element belongs to an elevated process.
    /// </summary>
    /// <param name="element">The automation element to check.</param>
    /// <returns>Success result if not elevated, or failure result with ElevatedTarget error.</returns>
    private UIAutomationResult CheckElevatedTarget(AutomationElement element)
    {
        try
        {
            // Get the element's bounding rectangle to check for elevation
            var rect = element.Current.BoundingRectangle;
            if (rect.IsEmpty)
            {
                // Element has no visible bounds, skip elevation check
                return new UIAutomationResult { Success = true, Action = "check" };
            }

            // Get the center point of the element
            var centerX = (int)(rect.X + rect.Width / 2);
            var centerY = (int)(rect.Y + rect.Height / 2);

            // Check if the target at these coordinates is elevated
            if (_elevationDetector.IsTargetElevated(centerX, centerY))
            {
                return UIAutomationResult.CreateFailure(
                    "check",
                    UIAutomationErrorType.ElevatedTarget,
                    "Target element belongs to an elevated (Administrator) process. Run the MCP server as Administrator to interact with elevated applications.",
                    null);
            }

            return new UIAutomationResult { Success = true, Action = "check" };
        }
        catch (Exception)
        {
            // If we can't check elevation, proceed anyway
            return new UIAutomationResult { Success = true, Action = "check" };
        }
    }

    #endregion

    #region Element Resolution Helpers

    private static AutomationElement? GetRootElementFromElementId(string elementId)
    {
        // Parse window handle from element ID to get root element
        // Format: window:<handle>|runtime:<id>|path:<path>
        var parts = elementId.Split('|');
        foreach (var part in parts)
        {
            if (part.StartsWith("window:", StringComparison.OrdinalIgnoreCase))
            {
                var handleStr = part["window:".Length..];
                if (nint.TryParse(handleStr, out var handle) && handle != 0)
                {
                    try
                    {
                        return AutomationElement.FromHandle(handle);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }
        return null;
    }

    #endregion

    private static AutomationElement? GetRootElement(nint? windowHandle)
    {
        if (windowHandle.HasValue && windowHandle.Value != 0)
        {
            return AutomationElement.FromHandle(windowHandle.Value);
        }

        // Get foreground window
        var foregroundHandle = GetForegroundWindowHandle();
        if (foregroundHandle != 0)
        {
            return AutomationElement.FromHandle(foregroundHandle);
        }

        return AutomationElement.RootElement;
    }

    private static nint GetForegroundWindowHandle()
    {
        return NativeWindow.GetForegroundWindow();
    }

    private static Condition BuildCondition(ElementQuery query)
    {
        var conditions = new List<Condition>();

        if (!string.IsNullOrEmpty(query.Name))
        {
            conditions.Add(new PropertyCondition(AutomationElement.NameProperty, query.Name, PropertyConditionFlags.IgnoreCase));
        }

        if (!string.IsNullOrEmpty(query.ControlType))
        {
            var controlType = GetControlType(query.ControlType);
            if (controlType != null)
            {
                conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
            }
        }

        if (!string.IsNullOrEmpty(query.AutomationId))
        {
            conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, query.AutomationId));
        }

        if (conditions.Count == 0)
        {
            return Condition.TrueCondition;
        }

        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        return new AndCondition([.. conditions]);
    }

    private static ControlType? GetControlType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "button" => ControlType.Button,
            "edit" => ControlType.Edit,
            "text" => ControlType.Text,
            "list" => ControlType.List,
            "listitem" => ControlType.ListItem,
            "tree" => ControlType.Tree,
            "treeitem" => ControlType.TreeItem,
            "menu" => ControlType.Menu,
            "menuitem" => ControlType.MenuItem,
            "combobox" => ControlType.ComboBox,
            "checkbox" => ControlType.CheckBox,
            "radiobutton" => ControlType.RadioButton,
            "tab" => ControlType.Tab,
            "tabitem" => ControlType.TabItem,
            "window" => ControlType.Window,
            "pane" => ControlType.Pane,
            "document" => ControlType.Document,
            "hyperlink" => ControlType.Hyperlink,
            "image" => ControlType.Image,
            "progressbar" => ControlType.ProgressBar,
            "slider" => ControlType.Slider,
            "spinner" => ControlType.Spinner,
            "statusbar" => ControlType.StatusBar,
            "toolbar" => ControlType.ToolBar,
            "tooltip" => ControlType.ToolTip,
            "group" => ControlType.Group,
            "scrollbar" => ControlType.ScrollBar,
            "datagrid" => ControlType.DataGrid,
            "dataitem" => ControlType.DataItem,
            "custom" => ControlType.Custom,
            _ => null
        };
    }

    private static HashSet<string>? ParseControlTypeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        return filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();
    }

    private UIElementInfo? ConvertToElementInfo(AutomationElement element, AutomationElement rootElement, bool includeChildren)
    {
        var children = includeChildren ? GetChildren(element, rootElement) : null;
        return ConvertToElementInfo(element, rootElement, _coordinateConverter, children);
    }

    /// <summary>
    /// Converts an AutomationElement to UIElementInfo using the specified coordinate converter.
    /// </summary>
    /// <remarks>Internal static for sharing with ElementIdGenerator to avoid code duplication.</remarks>
    internal static UIElementInfo? ConvertToElementInfo(AutomationElement element, AutomationElement rootElement, CoordinateConverter coordinateConverter, UIElementInfo[]? children = null)
    {
        try
        {
            var current = element.Current;
            var rect = current.BoundingRectangle;
            var boundingRect = BoundingRect.FromCoordinates(rect.X, rect.Y, rect.Width, rect.Height);
            var (monitorRelativeRect, monitorIndex) = coordinateConverter.ToMonitorRelative(boundingRect);

            // Create a ready-to-use clickable point at the center of the element
            var clickablePoint = ClickablePoint.FromCenter(monitorRelativeRect, monitorIndex);

            var patterns = GetSupportedPatterns(element);
            var elementId = ElementIdGenerator.GenerateId(element, rootElement);

            return new UIElementInfo
            {
                ElementId = elementId,
                AutomationId = string.IsNullOrEmpty(current.AutomationId) ? null : current.AutomationId,
                Name = string.IsNullOrEmpty(current.Name) ? null : current.Name,
                ControlType = current.ControlType.ProgrammaticName.Replace("ControlType.", ""),
                BoundingRect = boundingRect,
                MonitorRelativeRect = monitorRelativeRect,
                MonitorIndex = monitorIndex,
                ClickablePoint = clickablePoint,
                SupportedPatterns = patterns,
                Value = TryGetValue(element),
                ToggleState = TryGetToggleState(element),
                IsEnabled = current.IsEnabled,
                IsOffscreen = current.IsOffscreen,
                Children = children
            };
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private UIElementInfo? BuildElementTree(AutomationElement element, AutomationElement rootElement, int maxDepth, int currentDepth, HashSet<string>? controlTypeFilter, ref int elementsScanned)
    {
        try
        {
            elementsScanned++;
            var current = element.Current;

            // Check control type filter
            var controlTypeName = current.ControlType.ProgrammaticName.Replace("ControlType.", "").ToLowerInvariant();
            var includeElement = controlTypeFilter == null || controlTypeFilter.Contains(controlTypeName);

            var elementInfo = includeElement ? ConvertToElementInfo(element, rootElement, false) : null;

            // Get children if not at max depth
            if (currentDepth < maxDepth)
            {
                var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
                var childInfos = new List<UIElementInfo>();

                foreach (AutomationElement child in children)
                {
                    var childInfo = BuildElementTree(child, rootElement, maxDepth, currentDepth + 1, controlTypeFilter, ref elementsScanned);
                    if (childInfo != null)
                    {
                        childInfos.Add(childInfo);
                    }
                }

                if (elementInfo != null && childInfos.Count > 0)
                {
                    elementInfo = elementInfo with { Children = [.. childInfos] };
                }
                else if (elementInfo == null && childInfos.Count > 0)
                {
                    // If we're filtering and this element doesn't match, but children do, return first child
                    // TODO: Consider returning a virtual parent node
                    return childInfos[0];
                }
            }

            return elementInfo;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private UIElementInfo[]? GetChildren(AutomationElement element, AutomationElement rootElement)
    {
        try
        {
            var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            var childInfos = new List<UIElementInfo>();

            foreach (AutomationElement child in children)
            {
                var childInfo = ConvertToElementInfo(child, rootElement, false);
                if (childInfo != null)
                {
                    childInfos.Add(childInfo);
                }
            }

            return childInfos.Count > 0 ? [.. childInfos] : null;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the supported automation patterns for an element.
    /// </summary>
    /// <remarks>Internal for sharing with ElementIdGenerator.</remarks>
    internal static string[] GetSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();

        try
        {
            var supportedPatterns = element.GetSupportedPatterns();
            foreach (var pattern in supportedPatterns)
            {
                var name = pattern.ProgrammaticName.Replace("PatternIdentifiers.Pattern", "");
                patterns.Add(name);
            }
        }
        catch (ElementNotAvailableException)
        {
            // Element no longer available
        }

        return [.. patterns];
    }

    private static string? TryGetValue(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
            {
                return ((ValuePattern)pattern).Current.Value;
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static string? TryGetToggleState(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object pattern))
            {
                return ((TogglePattern)pattern).Current.ToggleState.ToString();
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static string BuildNotFoundMessage(ElementQuery query)
    {
        var criteria = new List<string>();

        if (!string.IsNullOrEmpty(query.Name))
        {
            criteria.Add($"name='{query.Name}'");
        }
        if (!string.IsNullOrEmpty(query.ControlType))
        {
            criteria.Add($"controlType='{query.ControlType}'");
        }
        if (!string.IsNullOrEmpty(query.AutomationId))
        {
            criteria.Add($"automationId='{query.AutomationId}'");
        }

        return $"No element found matching: {string.Join(", ", criteria)}";
    }

    private static UIAutomationDiagnostics CreateDiagnostics(Stopwatch stopwatch, ElementQuery? query = null, int? elementsScanned = null)
    {
        return new UIAutomationDiagnostics
        {
            DurationMs = stopwatch.ElapsedMilliseconds,
            Query = query,
            ElementsScanned = elementsScanned
        };
    }

    #endregion

    #region LoggerMessage Methods

    [LoggerMessage(Level = LogLevel.Error, Message = "Error finding elements")]
    private static partial void LogFindElementsError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error getting UI tree for window handle: {WindowHandle}")]
    private static partial void LogGetTreeError(ILogger logger, nint? windowHandle, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error scrolling element into view: {ElementId}")]
    private static partial void LogScrollIntoViewError(ILogger logger, string? elementId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error getting text from element: {ElementId}")]
    private static partial void LogGetTextError(ILogger logger, string? elementId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error invoking pattern {Pattern} on element: {ElementId}")]
    private static partial void LogInvokePatternError(ILogger logger, string pattern, string elementId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error focusing element: {ElementId}")]
    private static partial void LogFocusElementError(ILogger logger, string elementId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in find_and_click operation")]
    private static partial void LogFindAndClickError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in find_and_type operation")]
    private static partial void LogFindAndTypeError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in find_and_select operation")]
    private static partial void LogFindAndSelectError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to resolve element ID: {ElementId}")]
    private static partial void LogResolveElementError(ILogger logger, string elementId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Window activation failed for handle {WindowHandle}: {ErrorMessage}")]
    private static partial void LogWindowActivationFailed(ILogger logger, nint windowHandle, string errorMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Window activation threw an exception")]
    private static partial void LogWindowActivationException(ILogger logger, Exception exception);

    #endregion
}

/// <summary>
/// Helper for getting foreground window handle.
/// </summary>
static file class NativeWindow
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();
}
