using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Find operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindElementsAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                // Get root element
                UIA.IUIAutomationElement? rootElement;
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

                // Parse inRegion if specified
                BoundingRect? regionFilter = null;
                if (!string.IsNullOrEmpty(query.InRegion))
                {
                    regionFilter = ParseRegion(query.InRegion);
                    if (regionFilter == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "find",
                            UIAutomationErrorType.InvalidParameter,
                            $"Invalid inRegion format: '{query.InRegion}'. Expected 'x,y,width,height' (e.g., '100,200,300,400').",
                            CreateDiagnostics(stopwatch, query));
                    }
                }

                // Resolve nearElement reference point if specified
                BoundingRect? referencePoint = null;
                if (!string.IsNullOrEmpty(query.NearElement))
                {
                    var refElement = ElementIdGenerator.ResolveToAutomationElement(query.NearElement);
                    if (refElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "find",
                            UIAutomationErrorType.ElementNotFound,
                            $"Reference element for nearElement not found or stale: {query.NearElement}",
                            CreateDiagnostics(stopwatch, query));
                    }
                    var refRect = refElement.CurrentBoundingRectangle;
                    referencePoint = BoundingRect.FromCoordinates(refRect.left, refRect.top, refRect.right - refRect.left, refRect.bottom - refRect.top);
                }

                // Determine if we can use fast FindAll
                var hasAdvancedCriteria = !string.IsNullOrEmpty(query.NameContains) ||
                                         !string.IsNullOrEmpty(query.NamePattern) ||
                                         query.ExactDepth.HasValue ||
                                         !string.IsNullOrEmpty(query.ClassName) ||
                                         regionFilter != null;

                var elementInfos = new List<UIElementInfo>();
                var elementsScanned = 0;
                var matchCount = 0;
                var maxResults = query.FoundIndex > 1 ? query.FoundIndex : 100;

                // Detect framework and get optimal search strategy
                var strategy = GetFrameworkStrategy(rootElement);

                // Resolve visibility filtering: explicit caller value wins; otherwise exclude
                // off-screen nodes for Chromium/Electron (huge hidden/virtualized trees), include elsewhere.
                var visibleOnly = query.VisibleOnly ?? strategy.UsePostHocFiltering;

                // R5: Chromium/Electron control view is bloated with structural, non-interactive nodes
                // that inflate FindAll result sets. Scan the leaner content view instead (meaningful,
                // user-facing elements only). Caller can force on/off via ContentViewOnly. Only applies
                // to the FindAll/cached-filter paths - ExactDepth counts control-view depth, so it keeps
                // the control view to preserve depth semantics.
                var useContentView = (query.ContentViewOnly ?? strategy.UseContentView) && !query.ExactDepth.HasValue;

                // Use framework-aware depth: if caller used default (null or 20), use framework recommendation
                // Otherwise respect explicit caller value
                var effectiveMaxDepth = !query.MaxDepth.HasValue || query.MaxDepth.Value == 20
                    ? strategy.RecommendedMaxDepth
                    : query.MaxDepth.Value;

                // Routes to the cheapest correct strategy and applies off-screen filtering.
                // Resets counters so it can be re-run for the content-view -> control-view fallback.
                // - No advanced criteria: single FindAllBuildCache (fastest).
                // - Advanced criteria without ExactDepth: cached bulk fetch + in-process filter.
                //   This avoids the per-node COM storm of the raw TreeWalker, which is the dominant
                //   cost for Chromium/Electron where nameContains/namePattern is the recommended path.
                // - ExactDepth requires depth-aware traversal, so keep the TreeWalker.
                void RunScan(UIA.IUIAutomationCondition scanCondition)
                {
                    elementInfos.Clear();
                    elementsScanned = 0;
                    matchCount = 0;

                    if (!hasAdvancedCriteria)
                    {
                        FindElementsWithFindAll(rootElement, scanCondition, query, elementInfos, ref elementsScanned, maxResults);
                    }
                    else if (!query.ExactDepth.HasValue)
                    {
                        FindElementsWithCachedFilter(rootElement, scanCondition, query, elementInfos, ref elementsScanned, ref matchCount, maxResults);
                    }
                    else
                    {
                        FindElementsWithTreeWalker(rootElement, rootElement, scanCondition, query, effectiveMaxDepth, 0, elementInfos, ref elementsScanned, ref matchCount, maxResults, query.IncludeChildren);
                    }

                    // Exclude off-screen elements when visibility filtering is in effect.
                    if (visibleOnly && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => e.IsOffscreen);
                    }
                }

                // AND the caller's condition with the predefined content-view condition so FindAll
                // returns only content-view elements (a strict subset of the control view).
                var contentCondition = useContentView
                    ? Uia.CreateAndCondition(condition, Uia.ContentViewCondition)
                    : condition;

                var usedContentView = useContentView;
                RunScan(contentCondition);

                // Guardrail: the content view can hide nodes some flows target (custom ARIA roles,
                // decorative-but-interactive widgets). Fall back to the full control view when the
                // content-view scan comes up empty so discoverability never regresses.
                if (useContentView && elementInfos.Count == 0)
                {
                    usedContentView = false;
                    RunScan(condition);
                }

                stopwatch.Stop();
                LogSearchPerformance(_logger, "find", elementsScanned, stopwatch.ElapsedMilliseconds, elementInfos.Count);

                string? windowTitle = rootElement.GetName();

                // AUTO-RECOVERY: If exact name match failed, automatically try partial match
                if (elementInfos.Count == 0 && !string.IsNullOrEmpty(query.Name) && string.IsNullOrEmpty(query.NameContains))
                {
                    // Reset and retry with nameContains instead of exact name
                    var relaxedQuery = query with { Name = null, NameContains = query.Name };
                    var relaxedCondition = BuildCondition(relaxedQuery);

                    elementInfos.Clear();
                    elementsScanned = 0;
                    matchCount = 0;

                    // Relaxation scans the control view for maximum recall.
                    usedContentView = false;
                    FindElementsWithCachedFilter(rootElement, relaxedCondition, relaxedQuery, elementInfos, ref elementsScanned, ref matchCount, maxResults);

                    if (visibleOnly && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => e.IsOffscreen);
                    }

                    if (elementInfos.Count > 0)
                    {
                        LogSearchPerformance(_logger, "find (auto-relaxed to partial match)", elementsScanned, stopwatch.ElapsedMilliseconds, elementInfos.Count);
                    }
                }

                if (elementInfos.Count == 0)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.ElementNotFound,
                        BuildNotFoundMessage(query),
                        CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle, usedContentView));
                }

                // Filter by region if specified (post-processing for FindAll path)
                if (regionFilter != null && elementInfos.Count > 0)
                {
                    elementInfos.RemoveAll(e => !IntersectsRegion(e.BoundingRect, regionFilter));
                }

                // Sort by proximity to reference element if nearElement specified
                if (referencePoint != null && elementInfos.Count > 1)
                {
                    var refCenterX = referencePoint.CenterX;
                    var refCenterY = referencePoint.CenterY;
                    elementInfos.Sort((a, b) =>
                    {
                        var distA = DistanceSquared(a.BoundingRect.CenterX, a.BoundingRect.CenterY, refCenterX, refCenterY);
                        var distB = DistanceSquared(b.BoundingRect.CenterX, b.BoundingRect.CenterY, refCenterX, refCenterY);
                        return distA.CompareTo(distB); // Ascending order (closest first)
                    });
                }
                // Sort by prominence (bounding box area) if requested - larger elements first
                else if (query.SortByProminence && elementInfos.Count > 1)
                {
                    elementInfos.Sort((a, b) =>
                    {
                        var areaA = a.BoundingRect.Width * a.BoundingRect.Height;
                        var areaB = b.BoundingRect.Width * b.BoundingRect.Height;
                        return areaB.CompareTo(areaA); // Descending order (largest first)
                    });
                }

                // Always use compact format for Find to reduce token count by ~70%
                return UIAutomationResult.CreateSuccessCompact("find", [.. elementInfos], CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle, usedContentView));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindElementsError(_logger, ex);
            var errorType = COMExceptionHelper.IsElementStale(ex)
                ? UIAutomationErrorType.ElementStale
                : COMExceptionHelper.IsAccessDenied(ex)
                    ? UIAutomationErrorType.ElevatedTarget
                    : UIAutomationErrorType.InternalError;
            return UIAutomationResult.CreateFailure(
                "find",
                errorType,
                COMExceptionHelper.GetErrorMessage(ex, "Find"),
                CreateDiagnostics(stopwatch, query));
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

    /// <summary>
    /// Fast element finding using FindAllBuildCache.
    /// Uses caching to batch all property retrieval into a single COM call per element.
    /// </summary>
    private void FindElementsWithFindAll(
        UIA.IUIAutomationElement rootElement,
        UIA.IUIAutomationCondition condition,
        ElementQuery query,
        List<UIElementInfo> results,
        ref int elementsScanned,
        int maxResults)
    {
        try
        {
            // Create cache request with all properties needed for element conversion
            // This reduces ~40+ COM calls per element to 1 bulk fetch
            var cacheRequest = Uia.CreateElementCacheRequest(UIA.TreeScope.TreeScope_Element);

            // Use FindAllBuildCache instead of FindAll - returns elements with cached properties
            var elements = rootElement.FindAllBuildCache(UIA.TreeScope.TreeScope_Descendants, condition, cacheRequest);
            if (elements == null)
            {
                return;
            }

            elementsScanned = elements.Length;

            var matchCount = 0;
            for (var i = 0; i < elements.Length && results.Count < maxResults; i++)
            {
                var element = elements.GetElement(i);
                if (element == null)
                {
                    continue;
                }

                matchCount++;

                if (matchCount >= query.FoundIndex)
                {
                    var children = query.IncludeChildren ? GetChildren(element, rootElement) : null;
                    // Use cached properties since element was retrieved with FindAllBuildCache
                    var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, children, fromCachedElement: true);
                    if (elementInfo != null)
                    {
                        results.Add(elementInfo);
                    }
                }
            }
        }
        catch
        {
            // Element disappeared during search
        }
    }

    /// <summary>
    /// Advanced-criteria finding using a single FindAllBuildCache pass plus in-process filtering.
    /// Pushes Name(exact)/AutomationId/ControlType down as native conditions, then filters
    /// nameContains/namePattern/className against cached properties. This avoids the per-node
    /// cross-process COM round-trips of the raw TreeWalker, which is the dominant cost for
    /// Chromium/Electron content where nameContains/namePattern is the recommended query path.
    /// </summary>
    private void FindElementsWithCachedFilter(
        UIA.IUIAutomationElement rootElement,
        UIA.IUIAutomationCondition condition,
        ElementQuery query,
        List<UIElementInfo> results,
        ref int elementsScanned,
        ref int matchCount,
        int maxResults)
    {
        try
        {
            // Single COM call fetches every element matching the native condition with all
            // properties cached; substring/regex/className are then evaluated in-process.
            var cacheRequest = Uia.CreateElementCacheRequest(UIA.TreeScope.TreeScope_Element);
            var elements = rootElement.FindAllBuildCache(UIA.TreeScope.TreeScope_Descendants, condition, cacheRequest);
            if (elements == null)
            {
                return;
            }

            // Bound in-process work with the same budget the tree path uses.
            var count = Math.Min(elements.Length, MaxElementsToScan);
            for (var i = 0; i < count && results.Count < maxResults; i++)
            {
                elementsScanned++;

                var element = elements.GetElement(i);
                if (element == null)
                {
                    continue;
                }

                if (!MatchesAdvancedCriteriaCached(element, query))
                {
                    continue;
                }

                matchCount++;
                if (matchCount < query.FoundIndex)
                {
                    continue;
                }

                var children = query.IncludeChildren ? GetChildren(element, rootElement) : null;
                var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, children, fromCachedElement: true);
                if (elementInfo != null)
                {
                    results.Add(elementInfo);
                }
            }
        }
        catch
        {
            // Element tree changed during search - return whatever was collected.
        }
    }

    /// <summary>
    /// Evaluates nameContains/namePattern/className against an element's cached properties.
    /// </summary>
    private static bool MatchesAdvancedCriteriaCached(UIA.IUIAutomationElement element, ElementQuery query)
    {
        try
        {
            if (!string.IsNullOrEmpty(query.NameContains))
            {
                var name = element.GetCachedName();
                if (string.IsNullOrEmpty(name) ||
                    !name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(query.NamePattern))
            {
                var name = element.GetCachedName();
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                try
                {
                    if (!Regex.IsMatch(name, query.NamePattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(query.ClassName))
            {
                var className = element.GetCachedClassName();
                if (!string.Equals(className, query.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Element finding using TreeWalker for advanced criteria.
    /// </summary>
    private void FindElementsWithTreeWalker(
        UIA.IUIAutomationElement current,
        UIA.IUIAutomationElement rootElement,
        UIA.IUIAutomationCondition condition,
        ElementQuery query,
        int maxDepth,
        int currentDepth,
        List<UIElementInfo> results,
        ref int elementsScanned,
        ref int matchCount,
        int maxResults,
        bool includeChildren)
    {
        if (currentDepth > maxDepth || results.Count >= maxResults)
        {
            return;
        }

        try
        {
            elementsScanned++;

            var shouldCheckElement = !query.ExactDepth.HasValue || currentDepth == query.ExactDepth.Value;

            if (shouldCheckElement)
            {
                if (MatchesCondition(current, condition) && MatchesAdvancedCriteria(current, query))
                {
                    matchCount++;

                    if (matchCount >= query.FoundIndex)
                    {
                        var children = includeChildren ? GetChildren(current, rootElement) : null;
                        var elementInfo = ConvertToElementInfo(current, rootElement, _coordinateConverter, children);
                        if (elementInfo != null)
                        {
                            results.Add(elementInfo);
                            if (results.Count >= maxResults)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if (query.ExactDepth.HasValue && currentDepth >= query.ExactDepth.Value)
            {
                return;
            }

            var child = current.GetFirstChild();
            while (child != null && results.Count < maxResults)
            {
                FindElementsWithTreeWalker(child, rootElement, condition, query, maxDepth, currentDepth + 1, results, ref elementsScanned, ref matchCount, maxResults, includeChildren);
                child = child.GetNextSibling();
            }
        }
        catch
        {
            // Element disappeared - skip it
        }
    }

    /// <summary>
    /// Checks if an element matches advanced query criteria.
    /// </summary>
    private static bool MatchesAdvancedCriteria(UIA.IUIAutomationElement element, ElementQuery query)
    {
        try
        {
            if (!string.IsNullOrEmpty(query.NameContains))
            {
                var elementName = element.GetName();
                if (string.IsNullOrEmpty(elementName) ||
                    !elementName.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(query.NamePattern))
            {
                var elementName = element.GetName();
                if (string.IsNullOrEmpty(elementName))
                {
                    return false;
                }

                try
                {
                    if (!Regex.IsMatch(elementName, query.NamePattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(query.ClassName))
            {
                var elementClassName = element.GetClassName();
                if (!string.Equals(elementClassName, query.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Manually evaluates if an element matches a condition.
    /// </summary>
    private static bool MatchesCondition(UIA.IUIAutomationElement element, UIA.IUIAutomationCondition condition)
    {
        // For UIA3 COM, we can use FindFirst on the element itself to check if it matches
        try
        {
            var result = element.FindFirst(UIA.TreeScope.TreeScope_Element, condition);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildNotFoundMessage(ElementQuery query)
    {
        var criteria = new List<string>();

        if (!string.IsNullOrEmpty(query.Name))
        {
            criteria.Add($"name='{query.Name}'");
        }

        if (!string.IsNullOrEmpty(query.NameContains))
        {
            criteria.Add($"nameContains='{query.NameContains}'");
        }

        if (!string.IsNullOrEmpty(query.NamePattern))
        {
            criteria.Add($"namePattern='{query.NamePattern}'");
        }

        if (!string.IsNullOrEmpty(query.ControlType))
        {
            criteria.Add($"controlType='{query.ControlType}'");
        }

        if (!string.IsNullOrEmpty(query.AutomationId))
        {
            criteria.Add($"automationId='{query.AutomationId}'");
        }

        if (!string.IsNullOrEmpty(query.ClassName))
        {
            criteria.Add($"className='{query.ClassName}'");
        }

        return $"No element found matching: {string.Join(", ", criteria)}";
    }

    /// <summary>
    /// Parses a region string in format "x,y,width,height".
    /// </summary>
    private static BoundingRect? ParseRegion(string regionString)
    {
        var parts = regionString.Split(',');
        if (parts.Length != 4)
        {
            return null;
        }

        if (!int.TryParse(parts[0].Trim(), out var x) ||
            !int.TryParse(parts[1].Trim(), out var y) ||
            !int.TryParse(parts[2].Trim(), out var width) ||
            !int.TryParse(parts[3].Trim(), out var height))
        {
            return null;
        }

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        return new BoundingRect { X = x, Y = y, Width = width, Height = height };
    }

    /// <summary>
    /// Checks if an element's bounding rect intersects with the specified region.
    /// </summary>
    private static bool IntersectsRegion(BoundingRect elementRect, BoundingRect region)
    {
        // Check if rectangles overlap
        return elementRect.X < region.X + region.Width &&
               elementRect.X + elementRect.Width > region.X &&
               elementRect.Y < region.Y + region.Height &&
               elementRect.Y + elementRect.Height > region.Y;
    }

    /// <summary>
    /// Calculates squared distance between two points (avoids sqrt for sorting).
    /// </summary>
    private static long DistanceSquared(int x1, int y1, int x2, int y2)
    {
        long dx = x1 - x2;
        long dy = y1 - y2;
        return dx * dx + dy * dy;
    }
}