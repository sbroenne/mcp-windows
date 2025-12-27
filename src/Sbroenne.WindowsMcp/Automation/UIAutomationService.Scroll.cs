using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Scroll operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> ScrollIntoViewAsync(string? elementId, ElementQuery? query, int timeoutMs, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            UIA.IUIAutomationElement? element = null;

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
                var foundElement = findResult.Elements?[0];
                if (!findResult.Success || foundElement == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "scroll_into_view",
                        UIAutomationErrorType.ElementNotFound,
                        findResult.ErrorMessage ?? "Element not found matching query.",
                        CreateDiagnostics(stopwatch, query));
                }

                element = await _staThread.ExecuteAsync(() =>
                    ElementIdGenerator.ResolveToAutomationElement(foundElement.ElementId), cancellationToken);
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

            return await _staThread.ExecuteAsync(() =>
            {
                var rootElement = GetRootElementForScroll(element);

                var scrollResult = TryScrollItemPattern(element);
                if (scrollResult.success)
                {
                    var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);
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

                var scrollResult2 = TryScrollParentToElement(element, stopwatch.ElapsedMilliseconds, timeoutMs);
                if (scrollResult2.success)
                {
                    var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);
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

                return UIAutomationResult.CreateFailure(
                    "scroll_into_view",
                    UIAutomationErrorType.PatternNotSupported,
                    scrollResult2.errorMessage ?? "Element does not support scrolling and no scrollable parent was found.",
                    CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogScrollIntoViewError(_logger, elementId, ex);
            var errorType = COMExceptionHelper.IsElementStale(ex)
                ? UIAutomationErrorType.ElementStale
                : UIAutomationErrorType.InternalError;
            return UIAutomationResult.CreateFailure(
                "scroll_into_view",
                errorType,
                COMExceptionHelper.GetErrorMessage(ex, "ScrollIntoView"),
                CreateDiagnostics(stopwatch));
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

    private static (bool success, string? errorMessage) TryScrollItemPattern(UIA.IUIAutomationElement element)
    {
        try
        {
            if (element.TryScrollIntoView())
            {
                return (true, null);
            }

            return (false, "Element does not support ScrollItemPattern.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "ScrollItemPattern"));
        }
        catch (Exception ex)
        {
            return (false, $"ScrollItemPattern failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryScrollParentToElement(UIA.IUIAutomationElement element, long elapsedMs, int timeoutMs)
    {
        try
        {
            var parent = element.GetParent();

            while (parent != null)
            {
                var scrollPattern = parent.GetPattern<UIA.IUIAutomationScrollPattern>(UIA3PatternIds.Scroll);
                if (scrollPattern != null)
                {
                    if (!element.IsOffscreen())
                    {
                        return (true, null);
                    }

                    var elementRect = element.GetBoundingRectangle();
                    var parentRect = parent.GetBoundingRectangle();

                    if (elementRect.Width == 0 || parentRect.Width == 0)
                    {
                        scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeIncrement);
                        Thread.Sleep(100);
                        return (!element.IsOffscreen(), null);
                    }

                    var scrollAttempts = 0;
                    const int MaxAttempts = 50;

                    while (scrollAttempts < MaxAttempts && elapsedMs < timeoutMs)
                    {
                        if (!element.IsOffscreen())
                        {
                            return (true, null);
                        }

                        elementRect = element.GetBoundingRectangle();
                        parentRect = parent.GetBoundingRectangle();

                        if (elementRect.Y < parentRect.Y)
                        {
                            if (scrollPattern.CurrentVerticalScrollPercent <= 0)
                            {
                                break;
                            }

                            scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeDecrement);
                        }
                        else if (elementRect.Y + elementRect.Height > parentRect.Y + parentRect.Height)
                        {
                            if (scrollPattern.CurrentVerticalScrollPercent >= 100)
                            {
                                break;
                            }

                            scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeIncrement);
                        }
                        else
                        {
                            break;
                        }

                        Thread.Sleep(50);
                        scrollAttempts++;
                    }

                    return (!element.IsOffscreen(), null);
                }

                parent = parent.GetParent();
            }

            return (false, "No scrollable parent found.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "ParentScroll"));
        }
        catch (Exception ex)
        {
            return (false, $"Parent scroll failed: {ex.Message}");
        }
    }

    private static UIA.IUIAutomationElement GetRootElementForScroll(UIA.IUIAutomationElement element)
    {
        try
        {
            var current = element;
            UIA.IUIAutomationElement? lastWindow = null;

            while (current != null)
            {
                if (current.GetControlTypeId() == UIA3ControlTypeIds.Window)
                {
                    lastWindow = current;
                }

                current = current.GetParent();
            }

            return lastWindow ?? Uia.RootElement;
        }
        catch
        {
            return Uia.RootElement;
        }
    }
}
