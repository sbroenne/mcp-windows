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
    public async Task<UIAutomationResult> GetTreeAsync(nint? windowHandle, string? parentElementId, int maxDepth, string? controlTypeFilter, CancellationToken cancellationToken = default)
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

                var controlTypeSet = ParseControlTypeFilter(controlTypeFilter);
                var elementsScanned = 0;

                var tree = BuildElementTree(rootElement, rootElement, maxDepth, 0, controlTypeSet, ref elementsScanned);
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
