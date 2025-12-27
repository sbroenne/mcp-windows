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

                if (elementInfos.Count == 0)
                {
                    return UIAutomationResult.CreateFailure(
                        "find",
                        UIAutomationErrorType.ElementNotFound,
                        BuildNotFoundMessage(query),
                        CreateDiagnosticsWithContext(stopwatch, rootElement, query, elementsScanned, windowTitle, query.WindowHandle));
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
}
