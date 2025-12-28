using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Tree operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetTreeAsync(string? windowHandle, string? parentElementId, int maxDepth, string? controlTypeFilter, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                UIA.IUIAutomationElement? rootElement;
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

                // Detect framework and get optimal search strategy
                var strategy = GetFrameworkStrategy(rootElement);
                var controlTypeSet = ParseControlTypeFilter(controlTypeFilter);
                var elementsScanned = 0;

                // Use framework-aware depth: if caller used default (5), use framework recommendation
                // Otherwise respect explicit caller value, but still cap at 20
                var effectiveMaxDepth = maxDepth == 5
                    ? strategy.RecommendedMaxDepth
                    : Math.Min(maxDepth, 20);

                // Create cache request for batch property retrieval (reduces COM calls)
                var cacheRequest = Uia.CreateTreeCacheRequest(includeChildren: false);

                // Get root element with cached properties for initial traversal
                var cachedRoot = rootElement.FindFirstBuildCache(
                    UIA.TreeScope.TreeScope_Element,
                    Uia.TrueCondition,
                    cacheRequest) ?? rootElement;

                UIElementInfo? tree;
                if (strategy.UsePostHocFiltering && controlTypeSet != null)
                {
                    // For Electron/Chromium: traverse full tree without filter, then apply filter post-hoc
                    tree = BuildElementTreeWithCachingPostHoc(cachedRoot, rootElement, effectiveMaxDepth, 0, controlTypeSet, cacheRequest, ref elementsScanned);
                }
                else
                {
                    // For WinForms/WPF/Win32: use inline filtering for better performance
                    tree = BuildElementTreeWithCaching(cachedRoot, rootElement, effectiveMaxDepth, 0, controlTypeSet, cacheRequest, ref elementsScanned);
                }

                var wasTruncated = elementsScanned > MaxElementsToScan;

                stopwatch.Stop();
                LogSearchPerformance(_logger, "get_tree", elementsScanned, stopwatch.ElapsedMilliseconds, tree != null ? 1 : 0);

                if (wasTruncated)
                {
                    LogTreeTruncated(_logger, elementsScanned, MaxElementsToScan);
                }

                string? windowTitle = rootElement.GetName();

                if (tree == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_tree",
                        UIAutomationErrorType.ElementNotFound,
                        "Could not build element tree.",
                        CreateDiagnosticsWithContext(stopwatch, rootElement, null, elementsScanned, windowTitle, windowHandle));
                }

                return UIAutomationResult.CreateSuccess("get_tree", [tree], CreateDiagnosticsWithContext(stopwatch, rootElement, null, elementsScanned, windowTitle, windowHandle));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogGetTreeError(_logger, windowHandle, ex);
            var errorType = COMExceptionHelper.IsElementStale(ex)
                ? UIAutomationErrorType.ElementStale
                : COMExceptionHelper.IsAccessDenied(ex)
                    ? UIAutomationErrorType.ElevatedTarget
                    : UIAutomationErrorType.InternalError;
            return UIAutomationResult.CreateFailure(
                "get_tree",
                errorType,
                COMExceptionHelper.GetErrorMessage(ex, "GetTree"),
                CreateDiagnostics(stopwatch));
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
        var delay = 50;
        const int MaxDelay = 500;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await FindElementsAsync(query with { TimeoutMs = 0 }, cancellationToken);
            if (result.Success)
            {
                return result with { Action = "wait_for" };
            }

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
    public async Task<UIAutomationResult> WaitForElementDisappearAsync(ElementQuery query, int timeoutMs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var stopwatch = Stopwatch.StartNew();
        var delay = 50;
        const int MaxDelay = 500;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await FindElementsAsync(query with { TimeoutMs = 0 }, cancellationToken);
            if (!result.Success || (result.Elements?.Length ?? 0) == 0)
            {
                // Element no longer found - success!
                stopwatch.Stop();
                return UIAutomationResult.CreateSuccess(
                    "wait_for_disappear",
                    new UIAutomationDiagnostics
                    {
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Query = query
                    });
            }

            await Task.Delay(delay, cancellationToken);
            delay = Math.Min(delay * 2, MaxDelay);
        }

        stopwatch.Stop();

        return UIAutomationResult.CreateFailure(
            "wait_for_disappear",
            UIAutomationErrorType.Timeout,
            $"Element still present after {timeoutMs}ms timeout. Expected it to disappear.",
            new UIAutomationDiagnostics
            {
                DurationMs = stopwatch.ElapsedMilliseconds,
                Query = query,
                ElapsedBeforeTimeout = stopwatch.ElapsedMilliseconds
            });
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> WaitForElementStateAsync(string elementId, string desiredState, int timeoutMs, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(elementId);
        ArgumentException.ThrowIfNullOrEmpty(desiredState);

        var stopwatch = Stopwatch.StartNew();
        var delay = 50;
        const int MaxDelay = 500;

        // Parse the desired state
        var (targetProperty, targetValue) = ParseDesiredState(desiredState.ToLowerInvariant());
        if (targetProperty == null)
        {
            return UIAutomationResult.CreateFailure(
                "wait_for_state",
                UIAutomationErrorType.InvalidParameter,
                $"Invalid desiredState '{desiredState}'. Valid values: enabled, disabled, on, off, indeterminate, visible, offscreen",
                null);
        }

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var element = await ResolveElementAsync(elementId, cancellationToken);
            if (element == null)
            {
                stopwatch.Stop();
                return UIAutomationResult.CreateFailure(
                    "wait_for_state",
                    UIAutomationErrorType.ElementNotFound,
                    $"Element '{elementId}' no longer exists (stale reference)",
                    new UIAutomationDiagnostics { DurationMs = stopwatch.ElapsedMilliseconds });
            }

            // Check if the element has reached the desired state
            var currentValue = GetElementPropertyValue(element, targetProperty);
            if (Equals(currentValue, targetValue))
            {
                stopwatch.Stop();
                return UIAutomationResult.CreateSuccess(
                    "wait_for_state",
                    [element],
                    new UIAutomationDiagnostics { DurationMs = stopwatch.ElapsedMilliseconds });
            }

            await Task.Delay(delay, cancellationToken);
            delay = Math.Min(delay * 2, MaxDelay);
        }

        stopwatch.Stop();

        // Get final state for diagnostics
        var finalElement = await ResolveElementAsync(elementId, cancellationToken);
        var finalValue = finalElement != null ? GetElementPropertyValue(finalElement, targetProperty) : "unknown";

        return UIAutomationResult.CreateFailure(
            "wait_for_state",
            UIAutomationErrorType.Timeout,
            $"Element did not reach state '{desiredState}' within {timeoutMs}ms. Current {targetProperty}: {finalValue}",
            new UIAutomationDiagnostics
            {
                DurationMs = stopwatch.ElapsedMilliseconds,
                ElapsedBeforeTimeout = stopwatch.ElapsedMilliseconds
            });
    }

    private static (string? property, object? value) ParseDesiredState(string state)
    {
        return state switch
        {
            "enabled" => ("IsEnabled", true),
            "disabled" => ("IsEnabled", false),
            "on" => ("ToggleState", "On"),
            "off" => ("ToggleState", "Off"),
            "indeterminate" => ("ToggleState", "Indeterminate"),
            "visible" => ("IsOffscreen", false),
            "offscreen" => ("IsOffscreen", true),
            _ => (null, null)
        };
    }

    private static object? GetElementPropertyValue(UIElementInfo element, string property)
    {
        return property switch
        {
            "IsEnabled" => element.IsEnabled,
            "IsOffscreen" => element.IsOffscreen,
            "ToggleState" => element.ToggleState,
            _ => null
        };
    }

    /// <summary>
    /// Builds element tree using IUIAutomationCacheRequest for batch property retrieval.
    /// This significantly reduces cross-process COM calls by fetching all properties in bulk.
    /// </summary>
    private UIElementInfo? BuildElementTreeWithCaching(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        int maxDepth,
        int currentDepth,
        HashSet<string>? controlTypeFilter,
        UIA.IUIAutomationCacheRequest cacheRequest,
        ref int elementsScanned)
    {
        try
        {
            elementsScanned++;

            if (elementsScanned > MaxElementsToScan)
            {
                return null;
            }

            // Use cached control type for filtering
            var controlTypeName = element.GetCachedControlTypeName().ToLowerInvariant();
            var includeElement = controlTypeFilter == null || controlTypeFilter.Contains(controlTypeName);

            // Use cached properties for element conversion (skips pattern detection)
            var elementInfo = includeElement
                ? ConvertToElementInfoFromCache(element, rootElement, _coordinateConverter, null, skipPatterns: true)
                : null;

            if (currentDepth < maxDepth && elementsScanned <= MaxElementsToScan)
            {
                // Use FindAllBuildCache to get children with all properties pre-cached in one COM call
                var children = element.FindAllBuildCache(
                    UIA.TreeScope.TreeScope_Children,
                    Uia.TrueCondition,
                    cacheRequest);

                var childInfos = new List<UIElementInfo>();

                if (children != null)
                {
                    for (var i = 0; i < children.Length && elementsScanned <= MaxElementsToScan; i++)
                    {
                        var child = children.GetElement(i);
                        if (child == null)
                        {
                            continue;
                        }

                        var childInfo = BuildElementTreeWithCaching(
                            child, rootElement, maxDepth, currentDepth + 1,
                            controlTypeFilter, cacheRequest, ref elementsScanned);
                        if (childInfo != null)
                        {
                            childInfos.Add(childInfo);
                        }
                    }
                }

                if (elementInfo != null && childInfos.Count > 0)
                {
                    elementInfo = elementInfo with { Children = [.. childInfos] };
                }
                else if (elementInfo == null && childInfos.Count > 0)
                {
                    return childInfos[0];
                }
            }

            return elementInfo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds element tree using post-hoc filtering with caching for Electron/Chromium apps.
    /// Combines the benefits of caching (fewer COM calls) with post-hoc filtering
    /// (doesn't prune branches containing matching elements).
    /// </summary>
    private UIElementInfo? BuildElementTreeWithCachingPostHoc(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        int maxDepth,
        int currentDepth,
        HashSet<string> controlTypeFilter,
        UIA.IUIAutomationCacheRequest cacheRequest,
        ref int elementsScanned)
    {
        try
        {
            elementsScanned++;

            if (elementsScanned > MaxElementsToScan)
            {
                return null;
            }

            // Use cached control type for filtering
            var controlTypeName = element.GetCachedControlTypeName().ToLowerInvariant();
            var matchesFilter = controlTypeFilter.Contains(controlTypeName);
            var elementInfo = ConvertToElementInfoFromCache(element, rootElement, _coordinateConverter, null, skipPatterns: true);

            // Collect children regardless of current element's match status
            var childInfos = new List<UIElementInfo>();
            if (currentDepth < maxDepth && elementsScanned <= MaxElementsToScan)
            {
                var children = element.FindAllBuildCache(
                    UIA.TreeScope.TreeScope_Children,
                    Uia.TrueCondition,
                    cacheRequest);

                if (children != null)
                {
                    for (var i = 0; i < children.Length && elementsScanned <= MaxElementsToScan; i++)
                    {
                        var child = children.GetElement(i);
                        if (child == null)
                        {
                            continue;
                        }

                        var childInfo = BuildElementTreeWithCachingPostHoc(
                            child, rootElement, maxDepth, currentDepth + 1,
                            controlTypeFilter, cacheRequest, ref elementsScanned);
                        if (childInfo != null)
                        {
                            childInfos.Add(childInfo);
                        }
                    }
                }
            }

            // Same decision logic as non-cached version
            if (matchesFilter)
            {
                if (childInfos.Count > 0 && elementInfo != null)
                {
                    return elementInfo with { Children = [.. childInfos] };
                }
                return elementInfo;
            }
            else if (childInfos.Count > 0)
            {
                if (childInfos.Count == 1)
                {
                    return childInfos[0];
                }
                if (elementInfo != null)
                {
                    return elementInfo with { Children = [.. childInfos] };
                }
                return childInfos[0];
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private UIElementInfo? BuildElementTree(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement, int maxDepth, int currentDepth, HashSet<string>? controlTypeFilter, ref int elementsScanned)
    {
        try
        {
            elementsScanned++;

            if (elementsScanned > MaxElementsToScan)
            {
                return null;
            }

            var controlTypeName = element.GetControlTypeName().ToLowerInvariant();
            var includeElement = controlTypeFilter == null || controlTypeFilter.Contains(controlTypeName);

            var elementInfo = includeElement ? ConvertToElementInfo(element, rootElement, _coordinateConverter, null) : null;

            if (currentDepth < maxDepth && elementsScanned <= MaxElementsToScan)
            {
                var children = element.FindAll(UIA.TreeScope.TreeScope_Children, Uia.TrueCondition);
                var childInfos = new List<UIElementInfo>();

                if (children != null)
                {
                    for (var i = 0; i < children.Length && elementsScanned <= MaxElementsToScan; i++)
                    {
                        var child = children.GetElement(i);
                        if (child == null)
                        {
                            continue;
                        }

                        var childInfo = BuildElementTree(child, rootElement, maxDepth, currentDepth + 1, controlTypeFilter, ref elementsScanned);
                        if (childInfo != null)
                        {
                            childInfos.Add(childInfo);
                        }
                    }
                }

                if (elementInfo != null && childInfos.Count > 0)
                {
                    elementInfo = elementInfo with { Children = [.. childInfos] };
                }
                else if (elementInfo == null && childInfos.Count > 0)
                {
                    return childInfos[0];
                }
            }

            return elementInfo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds element tree using post-hoc filtering strategy for Electron/Chromium apps.
    /// Traverses the full tree without filtering, then applies filter while preserving hierarchy.
    /// This prevents pruning branches that contain matching elements deep in the tree.
    /// </summary>
    private UIElementInfo? BuildElementTreeWithPostHocFiltering(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement, int maxDepth, int currentDepth, HashSet<string> controlTypeFilter, ref int elementsScanned)
    {
        try
        {
            elementsScanned++;

            if (elementsScanned > MaxElementsToScan)
            {
                return null;
            }

            // Always convert element (we'll filter in the result composition)
            var controlTypeName = element.GetControlTypeName().ToLowerInvariant();
            var matchesFilter = controlTypeFilter.Contains(controlTypeName);
            var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);

            // Collect children regardless of current element's match status
            var childInfos = new List<UIElementInfo>();
            if (currentDepth < maxDepth && elementsScanned <= MaxElementsToScan)
            {
                var children = element.FindAll(UIA.TreeScope.TreeScope_Children, Uia.TrueCondition);

                if (children != null)
                {
                    for (var i = 0; i < children.Length && elementsScanned <= MaxElementsToScan; i++)
                    {
                        var child = children.GetElement(i);
                        if (child == null)
                        {
                            continue;
                        }

                        var childInfo = BuildElementTreeWithPostHocFiltering(child, rootElement, maxDepth, currentDepth + 1, controlTypeFilter, ref elementsScanned);
                        if (childInfo != null)
                        {
                            childInfos.Add(childInfo);
                        }
                    }
                }
            }

            // Decision logic:
            // 1. If this element matches filter, include it with all filtered children
            // 2. If this element doesn't match but has children that matched, return a placeholder
            //    that contains those children (to preserve hierarchy)
            // 3. If nothing matches, return null

            if (matchesFilter)
            {
                // Include this element with filtered children
                if (childInfos.Count > 0 && elementInfo != null)
                {
                    return elementInfo with { Children = [.. childInfos] };
                }
                return elementInfo;
            }
            else if (childInfos.Count > 0)
            {
                // Don't match but have matching descendants - return first child or aggregate
                if (childInfos.Count == 1)
                {
                    return childInfos[0];
                }

                // Multiple children matched - include this element as a container
                // but mark it as non-matching by not including its own info
                if (elementInfo != null)
                {
                    return elementInfo with { Children = [.. childInfos] };
                }
                return childInfos[0];
            }

            return null;
        }
        catch
        {
            return null;
        }
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
}
