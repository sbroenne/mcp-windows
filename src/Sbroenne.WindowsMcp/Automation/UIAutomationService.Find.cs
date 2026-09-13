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
    internal static Task<UIAutomationResult> WaitForFindResultAsync(
        ElementQuery query,
        int timeoutMs,
        Func<Task<UIAutomationResult>> probe,
        Func<int, CancellationToken, Task> wait,
        Func<long> elapsedMilliseconds,
        CancellationToken cancellationToken) =>
        WaitForSearchResultAsync(query, timeoutMs, probe, wait, elapsedMilliseconds, disappear: false, cancellationToken);

    internal static Task<UIAutomationResult> WaitForDisappearResultAsync(
        ElementQuery query,
        int timeoutMs,
        Func<Task<UIAutomationResult>> probe,
        Func<int, CancellationToken, Task> wait,
        Func<long> elapsedMilliseconds,
        CancellationToken cancellationToken) =>
        WaitForSearchResultAsync(query, timeoutMs, probe, wait, elapsedMilliseconds, disappear: true, cancellationToken);

    private static async Task<UIAutomationResult> WaitForSearchResultAsync(
        ElementQuery query,
        int timeoutMs,
        Func<Task<UIAutomationResult>> probe,
        Func<int, CancellationToken, Task> wait,
        Func<long> elapsedMilliseconds,
        bool disappear,
        CancellationToken cancellationToken)
    {
        var delay = 50;
        var action = disappear ? "wait_for_disappear" : "wait_for";
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finalProbe = elapsedMilliseconds() >= timeoutMs;
            var result = await probe().ConfigureAwait(false);
            if (disappear)
            {
                if ((!result.Success && IsSatisfiedDisappearAbsence(result.ErrorType)) ||
                    (result.Success && (result.Items?.Length ?? 0) == 0))
                {
                    return UIAutomationResult.CreateSuccess(action,
                        new UIAutomationDiagnostics { DurationMs = elapsedMilliseconds(), Query = query });
                }

                if (!result.Success)
                {
                    return result with { Action = action };
                }
            }
            else if (result.Success || !IsRetryableWaitAbsence(result.ErrorType))
            {
                return result with { Action = action };
            }

            if (finalProbe)
            {
                var elapsed = elapsedMilliseconds();
                return UIAutomationResult.CreateFailure(
                    action,
                    UIAutomationErrorType.Timeout,
                    disappear
                        ? $"Element still present after {timeoutMs}ms timeout. Expected it to disappear."
                        : $"Element not found within {timeoutMs}ms timeout.",
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

                var elementInfos = new List<UIElementInfo>();
                var elementsScanned = 0;
                var matchCount = 0;
                var scanLimitReached = false;
                var maxResults = query.RequireUnique ? 2 : query.FoundIndex > 1 ? query.FoundIndex : 100;

                // Detect framework and get optimal search strategy
                var strategy = GetFrameworkStrategy(rootElement);

                // Resolve visibility filtering: explicit caller value wins; otherwise exclude
                // off-screen nodes for Chromium/Electron (huge hidden/virtualized trees), include elsewhere.
                var visibleOnly = query.VisibleOnly ?? strategy.UsePostHocFiltering;

                // Content view affects matching, never navigation: filtering a walker itself
                // could traverse arbitrarily many hidden nodes inside one provider call.
                var useContentView = (query.ContentViewOnly ?? strategy.UseContentView) && !query.ExactDepth.HasValue;

                // Use framework-aware depth: if caller used default (null or 20), use framework recommendation
                // Otherwise respect explicit caller value
                var effectiveMaxDepth = !query.MaxDepth.HasValue || query.MaxDepth.Value == 20
                    ? strategy.RecommendedMaxDepth
                    : query.MaxDepth.Value;

                // All selector routes share one incremental node budget, including fallback
                // passes. Exact/native conditions must not bypass the provider traversal cap.
                void RunScan(UIA.IUIAutomationCondition scanCondition)
                {
                    elementInfos.Clear();
                    matchCount = 0;
                    scanLimitReached = false;

                    scanLimitReached = FindElementsWithCachedFilter(
                        rootElement, scanCondition, query, elementInfos, ref elementsScanned,
                        ref matchCount, maxResults,
                        query.ExactDepth.HasValue ? effectiveMaxDepth : query.MaxDepth ?? int.MaxValue,
                        visibleOnly, regionFilter, cancellationToken);

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

                // Apply the content-view condition per visited element.
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
                    scanLimitReached = FindElementsWithCachedFilter(
                        rootElement, relaxedCondition, relaxedQuery, elementInfos, ref elementsScanned,
                        ref matchCount, maxResults,
                        query.ExactDepth.HasValue ? effectiveMaxDepth : query.MaxDepth ?? int.MaxValue,
                        visibleOnly, regionFilter, cancellationToken);

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
                        "The scan budget was reached or the provider changed before the search request was resolved. " +
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
            bool Visit(UIA.IUIAutomationElement element, int depth)
            {
                scanned++;
                if ((query.ExactDepth.HasValue &&
                     (depth != query.ExactDepth.Value ||
                      (depth > 0 && element.GetCachedPropertyValue(UIA3PropertyIds.IsControlElement) is not true))) ||
                    !MatchesCondition(element, condition) || !MatchesAdvancedCriteriaCached(element, query))
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
                // A full discovery page satisfies the request without proving exhaustion.
                // For RequireUnique the cap is two: ambiguity is proven, whereas a single
                // match must continue until traversal completes or the scan budget runs out.
                return results.Count == maxResults;
            }

            // ExactDepth is defined relative to the root itself, unlike descendant searches.
            if (query.ExactDepth.HasValue)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (elementsScanned >= MaxElementsToScan)
                {
                    return true;
                }

                Visit(rootElement.BuildUpdatedCache(cacheRequest), 0);
                if (query.ExactDepth.Value == 0)
                {
                    return false; // Root-only scope is provably complete without navigation.
                }
            }

            var outcome = BoundedSearchTraversal.Walk(
                rootElement,
                element => walker.GetFirstChildElementBuildCache(element, cacheRequest),
                element => walker.GetNextSiblingElementBuildCache(element, cacheRequest),
                element => element.GetCachedPropertyValue(UIA3PropertyIds.IsControlElement) is true,
                Visit,
                Math.Max(0, MaxElementsToScan - elementsScanned - scanned),
                query.ExactDepth.HasValue
                    ? Math.Min(query.ExactDepth.Value, maxDepth)
                    : Math.Max(1, maxDepth),
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
            // Validate the candidate's own snapshot, not a provider-normalized ancestor.
            // This also ensures the returned cached properties satisfy the exact selectors.
            if (!MatchesNativeSearchProperties(query, element.GetCachedName(),
                element.GetCachedAutomationId(), element.CachedControlType))
            {
                return false;
            }

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

    internal static bool MatchesNativeSearchProperties(
        ElementQuery query, string? name, string? automationId, int controlType)
    {
        var requestedType = string.IsNullOrEmpty(query.ControlType) ? 0 : GetControlTypeId(query.ControlType);
        return (string.IsNullOrEmpty(query.Name) ||
                string.Equals(name, query.Name, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(query.AutomationId) ||
                string.Equals(automationId, query.AutomationId, StringComparison.Ordinal)) &&
            (requestedType <= 0 || controlType == requestedType);
    }

    /// <summary>Tests only the candidate itself, never a provider-normalized ancestor.</summary>
    private static bool MatchesCondition(UIA.IUIAutomationElement element, UIA.IUIAutomationCondition condition)
    {
        try
        {
            var result = element.FindFirst(UIA.TreeScope.TreeScope_Element, condition);
            return result != null && element.IsSameElement(result);
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