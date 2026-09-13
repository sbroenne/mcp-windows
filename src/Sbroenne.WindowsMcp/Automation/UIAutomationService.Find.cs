using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Native;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Find operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <summary>
    /// Shares the production deadline policy with deterministic clock/probe regressions.
    /// One probe must start at/after the deadline, even if the preceding probe crossed it.
    /// </summary>
    internal static async Task<UIAutomationResult> WaitForFindResultAsync(
        ElementQuery query,
        int timeoutMs,
        Func<Task<UIAutomationResult>> probe,
        Func<int, CancellationToken, Task> wait,
        Func<long> elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        var delay = 50;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finalProbe = elapsedMilliseconds() >= timeoutMs;
            var result = await probe().ConfigureAwait(false);
            if (result.Success || !IsRetryableWaitAbsence(result.ErrorType))
            {
                return result with { Action = "wait_for" };
            }

            if (finalProbe)
            {
                var elapsed = elapsedMilliseconds();
                return UIAutomationResult.CreateFailure(
                    "wait_for",
                    UIAutomationErrorType.Timeout,
                    $"Element not found within {timeoutMs}ms timeout.",
                    new UIAutomationDiagnostics
                    {
                        DurationMs = elapsed,
                        Query = query,
                        ElapsedBeforeTimeout = elapsed
                    });
            }

            var remaining = timeoutMs - elapsedMilliseconds();
            if (remaining > 0)
            {
                await wait((int)Math.Min(delay, remaining), cancellationToken).ConfigureAwait(false);
                delay = Math.Min(delay * 2, 500);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindElementsAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.RequireUnique && query.FoundIndex != 1)
        {
            return UIAutomationResult.CreateFailure(
                "find",
                UIAutomationErrorType.InvalidParameter,
                "requireUnique cannot be combined with foundIndex other than 1. Refine the selector instead.",
                CreateDiagnostics(Stopwatch.StartNew(), query));
        }

        if (query.TimeoutMs <= 0)
        {
            return await FindElementsOnceAsync(query, cancellationToken).ConfigureAwait(false);
        }

        var result = await WaitForElementAsync(
            query with { TimeoutMs = 0 },
            query.TimeoutMs,
            cancellationToken).ConfigureAwait(false);

        return result with
        {
            Action = "find",
            Diagnostics = result.Diagnostics is null
                ? null
                : result.Diagnostics with { Query = query }
        };
    }

    private async Task<UIAutomationResult> FindElementsOnceAsync(
        ElementQuery query,
        CancellationToken cancellationToken)
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
                    if (!ReferenceMatchesWindow(query.ParentElementId, query.WindowHandle))
                    {
                        return UIAutomationResult.CreateFailure(
                            "find", UIAutomationErrorType.InvalidParameter,
                            "The parent reference does not belong to the requested window. Rediscover within that window.");
                    }

                    rootElement = ElementIdGenerator.ResolveToAutomationElement(
                        query.ParentElementId);
                    if (rootElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "find",
                            UIAutomationErrorType.ElementStale,
                            $"Parent element is stale: {query.ParentElementId}. Refresh UI state before searching within it.",
                            CreateDiagnostics(stopwatch, query));
                    }
                }
                else
                {
                    var normalizedScope = string.IsNullOrWhiteSpace(query.Scope)
                        ? "window"
                        : query.Scope.Trim().ToLowerInvariant();
                    if (normalizedScope is not ("window" or "active_dialog"))
                    {
                        return UIAutomationResult.CreateFailure(
                            "find",
                            UIAutomationErrorType.InvalidParameter,
                            $"Invalid scope '{query.Scope}'. Valid values: window, active_dialog.",
                            CreateDiagnostics(stopwatch, query));
                    }

                    rootElement = normalizedScope == "active_dialog"
                        ? GetActiveDialogRoot(query.WindowHandle)
                        : GetRootElement(query.WindowHandle);
                }

                if (rootElement == null)
                {
                    var requestedActiveDialog = string.Equals(
                        query.Scope,
                        "active_dialog",
                        StringComparison.OrdinalIgnoreCase);
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.WindowNotFound,
                        requestedActiveDialog
                            ? "No visible enabled dialog is currently owned by the requested window."
                            : "Could not find the specified window or foreground window.",
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
                    if (!ReferenceMatchesWindow(query.NearElement, query.WindowHandle))
                    {
                        return UIAutomationResult.CreateFailure(
                            "find", UIAutomationErrorType.InvalidParameter,
                            "The proximity reference does not belong to the requested window. Rediscover within that window.");
                    }

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

                // Determine if we can use fast FindAll.
                // ClassName is deliberately absent: it is a native UIA property, so BuildCondition
                // pushes it into the FindAll condition instead of forcing a bulk fetch + in-process scan.
                var hasAdvancedCriteria = !string.IsNullOrEmpty(query.NameContains) ||
                                         !string.IsNullOrEmpty(query.NamePattern) ||
                                         query.ExactDepth.HasValue ||
                                         regionFilter != null;

                var elementInfos = new List<UIElementInfo>();
                var elementsScanned = 0;
                var matchCount = 0;
                var scanLimitReached = false;
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
                // Managed scan passes share one node budget, including fallback passes.
                // - No advanced criteria: single FindAllBuildCache (fastest).
                // - Managed criteria: single-node raw navigation with element-only caches.
                //   Counting nonmatching raw nodes bounds work before provider materialization.
                // - ExactDepth requires depth-aware traversal, so keep the TreeWalker.
                void RunScan(UIA.IUIAutomationCondition scanCondition)
                {
                    elementInfos.Clear();
                    matchCount = 0;
                    scanLimitReached = false;

                    if (!hasAdvancedCriteria)
                    {
                        FindElementsWithFindAll(rootElement, scanCondition, query, elementInfos, ref elementsScanned, maxResults);
                    }
                    else if (!query.ExactDepth.HasValue)
                    {
                        scanLimitReached = FindElementsWithCachedFilter(
                            rootElement, scanCondition, query, elementInfos, ref elementsScanned,
                            ref matchCount, maxResults, query.MaxDepth ?? int.MaxValue,
                            visibleOnly, regionFilter, cancellationToken);
                    }
                    else
                    {
                        FindElementsWithTreeWalker(
                            rootElement, rootElement, scanCondition, query, effectiveMaxDepth, 0, elementInfos,
                            ref elementsScanned, ref matchCount, maxResults, query.IncludeChildren,
                            ref scanLimitReached, cancellationToken);
                    }

                    // Exclude off-screen elements when visibility filtering is in effect.
                    if (visibleOnly && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => e.IsOffscreen);
                    }

                    if (query.EnabledOnly == true && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => !e.IsEnabled);
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
                if (useContentView && elementInfos.Count == 0 && !scanLimitReached)
                {
                    usedContentView = false;
                    RunScan(condition);
                }

                stopwatch.Stop();
                LogSearchPerformance(_logger, "find", elementsScanned, stopwatch.ElapsedMilliseconds, elementInfos.Count);

                string? windowTitle = rootElement.GetName();

                // AUTO-RECOVERY: If exact name match failed, automatically try partial match
                if (!scanLimitReached && elementInfos.Count == 0 &&
                    !string.IsNullOrEmpty(query.Name) && string.IsNullOrEmpty(query.NameContains))
                {
                    // Reset and retry with nameContains instead of exact name
                    var relaxedQuery = query with { Name = null, NameContains = query.Name };
                    var relaxedCondition = BuildCondition(relaxedQuery);

                    elementInfos.Clear();
                    matchCount = 0;

                    // Relaxation scans the control view for maximum recall.
                    usedContentView = false;
                    if (query.ExactDepth.HasValue)
                    {
                        FindElementsWithTreeWalker(
                            rootElement, rootElement, relaxedCondition, relaxedQuery, effectiveMaxDepth, 0,
                            elementInfos, ref elementsScanned, ref matchCount, maxResults, query.IncludeChildren,
                            ref scanLimitReached, cancellationToken);
                    }
                    else
                    {
                        scanLimitReached = FindElementsWithCachedFilter(
                            rootElement, relaxedCondition, relaxedQuery, elementInfos, ref elementsScanned,
                            ref matchCount, maxResults, query.MaxDepth ?? int.MaxValue,
                            visibleOnly, regionFilter, cancellationToken);
                    }

                    if (visibleOnly && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => e.IsOffscreen);
                    }

                    if (query.EnabledOnly == true && elementInfos.Count > 0)
                    {
                        elementInfos.RemoveAll(e => !e.IsEnabled);
                    }

                    if (elementInfos.Count > 0)
                    {
                        LogSearchPerformance(_logger, "find (auto-relaxed to partial match)", elementsScanned, stopwatch.ElapsedMilliseconds, elementInfos.Count);
                    }
                }

                if (scanLimitReached)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.SearchIncomplete,
                        $"Search incomplete: checked {elementsScanned} candidates within the {MaxElementsToScan}-element scan budget. " +
                        "The scan limit was reached or the provider changed before enumeration completed. " +
                        "Remaining candidates were not checked.",
                        CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle, usedContentView));
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

                if (query.RequireUnique && elementInfos.Count > 1)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.MultipleMatches,
                        $"{elementInfos.Count} visible matching elements were found. Add automationId, " +
                        "use scope='active_dialog' or parentElementId, or set foundIndex explicitly.",
                        CreateDiagnosticsWithContext(
                            stopwatch,
                            rootElement,
                            query,
                            elementsScanned,
                            windowTitle,
                            query.WindowHandle,
                            usedContentView) with
                        {
                            MultipleMatches = elementInfos
                                .Take(10)
                                .Select(info => new UIAutomationTargetDiagnostics
                                {
                                    ElementId = info.ElementId,
                                    Name = info.Name,
                                    AutomationId = info.AutomationId,
                                    ControlType = info.ControlType,
                                    IsEnabled = info.IsEnabled,
                                    IsOffscreen = info.IsOffscreen
                                })
                                .ToArray()
                        });
                }

                // Always use compact format for Find to reduce token count by ~70%
                return UIAutomationResult.CreateSuccessCompact("find", [.. elementInfos], CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle, usedContentView));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindElementsError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "find",
                COMExceptionHelper.GetErrorType(ex),
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
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
        {
            // Element disappeared during search
        }
    }

    /// <summary>
    /// Managed filtering over bounded, single-element cached navigation. Do not replace this
    /// with bulk retrieval: limiting an array after fetching it does not bound provider work.
    /// </summary>
    private bool FindElementsWithCachedFilter(
        UIA.IUIAutomationElement rootElement,
        UIA.IUIAutomationCondition condition,
        ElementQuery query,
        List<UIElementInfo> results,
        ref int elementsScanned,
        ref int matchCount,
        int maxResults,
        int maxDepth,
        bool visibleOnly,
        BoundingRect? regionFilter,
        CancellationToken cancellationToken)
    {
        var scanned = 0;
        var matches = matchCount;
        try
        {
            var cacheRequest = Uia.CreateElementCacheRequest(UIA.TreeScope.TreeScope_Element);
            cacheRequest.AddProperty(UIA3PropertyIds.IsControlElement);
            cacheRequest.TreeFilter = Uia.TrueCondition;
            var walker = Uia.Automation.RawViewWalker;
            var outcome = BoundedSearchTraversal.Walk(
                rootElement,
                element => walker.GetFirstChildElementBuildCache(element, cacheRequest),
                element => walker.GetNextSiblingElementBuildCache(element, cacheRequest),
                element => element.GetCachedPropertyValue(UIA3PropertyIds.IsControlElement) is true,
                (element, _) =>
                {
                    scanned++;
                    if (!MatchesCondition(element, condition) || !MatchesAdvancedCriteriaCached(element, query))
                    {
                        return false;
                    }

                    var info = ConvertToElementInfo(element, rootElement, _coordinateConverter, fromCachedElement: true);
                    if (info is null || (visibleOnly && info.IsOffscreen) ||
                        (query.EnabledOnly == true && !info.IsEnabled) ||
                        (regionFilter is not null && !IntersectsRegion(info.BoundingRect, regionFilter)))
                    {
                        return false;
                    }

                    matches++;
                    if (matches < query.FoundIndex)
                    {
                        return false;
                    }

                    if (query.IncludeChildren)
                    {
                        info = info with { Children = GetChildren(element, rootElement) };
                    }

                    results.Add(info);
                    return results.Count >= maxResults;
                },
                Math.Max(0, MaxElementsToScan - elementsScanned),
                Math.Max(1, maxDepth),
                cancellationToken);
            return outcome.LimitReached;
        }
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
        {
            // A provider failure leaves unvisited nodes; never turn partial evidence into absence.
            return true;
        }
        finally
        {
            elementsScanned += scanned;
            matchCount = matches;
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
                catch (ArgumentException)
                {
                    return false;
                }
                catch (RegexMatchTimeoutException)
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
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
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
        bool includeChildren,
        ref bool scanLimitReached,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (currentDepth > maxDepth || results.Count >= maxResults)
        {
            return;
        }

        if (elementsScanned >= MaxElementsToScan)
        {
            scanLimitReached = true;
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
            while (child != null && results.Count < maxResults && !scanLimitReached)
            {
                FindElementsWithTreeWalker(
                    child, rootElement, condition, query, maxDepth, currentDepth + 1, results,
                    ref elementsScanned, ref matchCount, maxResults, includeChildren,
                    ref scanLimitReached, cancellationToken);
                child = child.GetNextSibling();
            }
        }
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
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
                catch (ArgumentException)
                {
                    return false;
                }
                catch (RegexMatchTimeoutException)
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
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
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
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementTraversalFailure(ex))
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

    private UIA.IUIAutomationElement? GetActiveDialogRoot(string? windowHandle)
    {
        if (!WindowHandleParser.TryParse(windowHandle, out var parentHandle))
        {
            return null;
        }

        var popupHandle = NativeMethods.GetWindow(parentHandle, NativeConstants.GW_ENABLEDPOPUP);
        if (popupHandle != IntPtr.Zero &&
            popupHandle != parentHandle &&
            NativeMethods.IsWindowVisible(popupHandle))
        {
            return Uia.ElementFromHandle(popupHandle);
        }

        var parent = Uia.ElementFromHandle(parentHandle);
        if (parent == null)
        {
            return null;
        }

        var windows = parent.FindAll(
            UIA.TreeScope.TreeScope_Children,
            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window));
        for (var index = 0; index < (windows?.Length ?? 0); index++)
        {
            var candidate = windows!.GetElement(index);
            var pattern = candidate.GetPattern<UIA.IUIAutomationWindowPattern>(UIA3PatternIds.Window);
            if (pattern?.CurrentIsModal != 0)
            {
                return candidate;
            }
        }

        return null;
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