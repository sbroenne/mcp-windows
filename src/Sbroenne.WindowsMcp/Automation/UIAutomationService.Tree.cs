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

                // Use single-call bulk fetch: get ALL elements in one COM call, then reconstruct tree
                // This is dramatically faster than per-level FindAllBuildCache calls
                UIElementInfo? tree;
                tree = BuildTreeWithBulkFetch(rootElement, effectiveMaxDepth, controlTypeSet, strategy.UsePostHocFiltering, ref elementsScanned);

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
    /// Builds element tree using a SINGLE FindFirstBuildCache call with TreeScope_Subtree.
    /// This caches the ENTIRE subtree including children relationships in one COM call.
    /// We then traverse using GetCachedChildren() which reads from cache (no COM calls).
    /// </summary>
    private UIElementInfo? BuildTreeWithBulkFetch(
        UIA.IUIAutomationElement rootElement,
        int maxDepth,
        HashSet<string>? controlTypeFilter,
        bool usePostHocFiltering,
        ref int elementsScanned)
    {
        try
        {
            // Create cache request with TreeScope_Subtree to cache entire tree including children
            var cacheRequest = Uia.CreateCacheRequest();

            // Add all properties needed for UIElementInfo conversion
            cacheRequest.AddProperty(UIA3PropertyIds.Name);
            cacheRequest.AddProperty(UIA3PropertyIds.AutomationId);
            cacheRequest.AddProperty(UIA3PropertyIds.ControlType);
            cacheRequest.AddProperty(UIA3PropertyIds.BoundingRectangle);
            cacheRequest.AddProperty(UIA3PropertyIds.IsEnabled);
            cacheRequest.AddProperty(UIA3PropertyIds.IsOffscreen);
            cacheRequest.AddProperty(UIA3PropertyIds.FrameworkId);
            cacheRequest.AddProperty(UIA3PropertyIds.ClassName);
            cacheRequest.AddProperty(UIA3PropertyIds.NativeWindowHandle);
            cacheRequest.AddProperty(UIA3PropertyIds.RuntimeId);

            // CRITICAL: Set TreeScope to Subtree so GetCachedChildren works on ALL descendants
            // TreeScope_Subtree = Element + Descendants - caches full tree structure
            cacheRequest.TreeScope = UIA.TreeScope.TreeScope_Subtree;

            // ONE COM call to fetch entire tree with all properties and children cached
            var cachedRoot = rootElement.FindFirstBuildCache(
                UIA.TreeScope.TreeScope_Element,
                Uia.TrueCondition,
                cacheRequest);

            if (cachedRoot == null)
            {
                return null;
            }

            // Now traverse using GetCachedChildren() - NO additional COM calls!
            return BuildTreeFromCachedElement(
                cachedRoot,
                rootElement,
                maxDepth,
                0,
                controlTypeFilter,
                usePostHocFiltering,
                ref elementsScanned);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Recursively builds tree from cached element using GetCachedChildren().
    /// This makes NO COM calls since all data is already cached.
    /// </summary>
    private UIElementInfo? BuildTreeFromCachedElement(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        int maxDepth,
        int currentDepth,
        HashSet<string>? controlTypeFilter,
        bool usePostHocFiltering,
        ref int elementsScanned)
    {
        elementsScanned++;

        if (elementsScanned > MaxElementsToScan || currentDepth > maxDepth)
        {
            return null;
        }

        var controlTypeName = element.GetCachedControlTypeName().ToLowerInvariant();
        var matchesFilter = controlTypeFilter == null || controlTypeFilter.Contains(controlTypeName);

        // Get children from cache (should be NO COM call if cached correctly)
        var childInfos = new List<UIElementInfo>();
        if (currentDepth < maxDepth)
        {
            // Try to get cached children first - this is the fast path
            UIA.IUIAutomationElementArray? cachedChildren = null;
            try
            {
                cachedChildren = element.GetCachedChildren();
            }
            catch
            {
                // Children not cached - this shouldn't happen with TreeScope_Subtree
                // Fall through with null
            }

            if (cachedChildren != null && cachedChildren.Length > 0)
            {
                for (var i = 0; i < cachedChildren.Length && elementsScanned <= MaxElementsToScan; i++)
                {
                    var child = cachedChildren.GetElement(i);
                    if (child == null)
                    {
                        continue;
                    }

                    var childInfo = BuildTreeFromCachedElement(
                        child, rootElement, maxDepth, currentDepth + 1,
                        controlTypeFilter, usePostHocFiltering, ref elementsScanned);

                    if (childInfo != null)
                    {
                        childInfos.Add(childInfo);
                    }
                }
            }
        }

        // Post-hoc filtering (Electron): include if matches OR has matching descendants
        if (usePostHocFiltering && controlTypeFilter != null)
        {
            if (!matchesFilter && childInfos.Count == 0)
            {
                return null;
            }

            var elementInfo = ConvertToElementInfoFromCache(element, rootElement, _coordinateConverter, null, skipPatterns: true);
            if (elementInfo == null)
            {
                return childInfos.Count == 1 ? childInfos[0] : null;
            }

            return childInfos.Count > 0 ? elementInfo with { Children = [.. childInfos] } : elementInfo;
        }

        // Inline filtering: only include if matches
        if (!matchesFilter)
        {
            return childInfos.Count switch
            {
                0 => null,
                1 => childInfos[0],
                _ => childInfos[0]
            };
        }

        var info = ConvertToElementInfoFromCache(element, rootElement, _coordinateConverter, null, skipPatterns: true);
        if (info == null)
        {
            return null;
        }

        return childInfos.Count > 0 ? info with { Children = [.. childInfos] } : info;
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
