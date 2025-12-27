using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Models;
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

                // Determine if we can use fast FindAll
                var hasAdvancedCriteria = !string.IsNullOrEmpty(query.NameContains) ||
                                         !string.IsNullOrEmpty(query.NamePattern) ||
                                         query.ExactDepth.HasValue ||
                                         !string.IsNullOrEmpty(query.ClassName);

                var elementInfos = new List<UIElementInfo>();
                var elementsScanned = 0;
                var matchCount = 0;
                var maxResults = query.FoundIndex > 1 ? query.FoundIndex : 100;
                var effectiveMaxDepth = query.MaxDepth.HasValue && query.MaxDepth.Value > 0 ? query.MaxDepth.Value : 20;

                if (!hasAdvancedCriteria)
                {
                    FindElementsWithFindAll(rootElement, condition, query, elementInfos, ref elementsScanned, maxResults);
                }
                else
                {
                    FindElementsWithTreeWalker(rootElement, rootElement, condition, query, effectiveMaxDepth, 0, elementInfos, ref elementsScanned, ref matchCount, maxResults, query.IncludeChildren);
                }

                stopwatch.Stop();
                LogSearchPerformance(_logger, "find", elementsScanned, stopwatch.ElapsedMilliseconds, elementInfos.Count);

                string? windowTitle = rootElement.GetName();
                var detectedFramework = DetectFramework(rootElement);

                if (elementInfos.Count == 0)
                {
                    var recoverySuggestion = BuildRecoverySuggestion(query, elementsScanned, detectedFramework);
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.ElementNotFound,
                        BuildNotFoundMessage(query),
                        CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle),
                        recoverySuggestion);
                }

                // Sort by prominence (bounding box area) if requested - larger elements first
                if (query.SortByProminence && elementInfos.Count > 1)
                {
                    elementInfos.Sort((a, b) =>
                    {
                        var areaA = a.BoundingRect.Width * a.BoundingRect.Height;
                        var areaB = b.BoundingRect.Width * b.BoundingRect.Height;
                        return areaB.CompareTo(areaA); // Descending order (largest first)
                    });
                }

                if (elementInfos.Count == 1)
                {
                    return UIAutomationResult.CreateSuccess("find", elementInfos[0], CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle));
                }

                return UIAutomationResult.CreateSuccess("find", [.. elementInfos], CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle));
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
    /// Fast element finding using FindAll.
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
            var elements = rootElement.FindAll(UIA.TreeScope.TreeScope_Descendants, condition);
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
                    var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, children);
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
    /// Builds a context-aware suggestion for when find fails.
    /// </summary>
    private static string BuildRecoverySuggestion(ElementQuery query, int elementsScanned, string? detectedFramework)
    {
        var suggestions = new List<string>();

        // Framework-specific suggestions
        if (detectedFramework == "Chromium/Electron")
        {
            suggestions.Add("This is a Chromium/Electron app (VS Code, Teams, Slack, etc.).");

            if (!string.IsNullOrEmpty(query.Name))
            {
                suggestions.Add($"Try nameContains='{query.Name}' instead of exact name match - Electron apps use ARIA labels which may differ.");
            }

            suggestions.Add("Use get_tree(maxDepth=3) to discover actual element names and automationIds.");
            suggestions.Add("For Electron apps, automationId is often more reliable than name.");
        }
        else
        {
            if (!string.IsNullOrEmpty(query.Name) && string.IsNullOrEmpty(query.NameContains))
            {
                suggestions.Add($"Try nameContains='{query.Name}' for partial matching.");
            }
        }

        // General suggestions based on what was searched
        if (elementsScanned == 0)
        {
            suggestions.Add("No elements were scanned - check if the window handle is valid and the window is visible.");
        }
        else if (elementsScanned < 10)
        {
            suggestions.Add($"Only {elementsScanned} elements scanned - the window may be minimized or the element may be in a collapsed section.");
        }
        else
        {
            suggestions.Add($"Scanned {elementsScanned} elements. Use get_tree to explore the hierarchy and find the correct element name/type.");
        }

        if (query.ExactDepth.HasValue)
        {
            suggestions.Add($"You specified exactDepth={query.ExactDepth.Value}. Try removing this constraint to search all depths.");
        }

        if (query.FoundIndex > 1)
        {
            suggestions.Add($"You requested foundIndex={query.FoundIndex} but there may be fewer matches. Try foundIndex=1 first.");
        }

        return string.Join(" ", suggestions);
    }
}
