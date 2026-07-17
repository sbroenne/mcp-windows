using System.Diagnostics;
using System.Runtime.InteropServices;
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

            var rootElement = await _staThread.ExecuteAsync(
                () => GetRootElementForScroll(element),
                cancellationToken);

            var scrollResult = await _staThread.ExecuteAsync(
                () => TryScrollItemPattern(element),
                cancellationToken);
            if (scrollResult.success)
            {
                var verified = await WaitForElementConditionAsync(
                    element,
                    () => !element.IsOffscreen(),
                    cancellationToken,
                    TimeSpan.FromMilliseconds(Math.Min(timeoutMs, 500)));
                if (verified.Observed)
                {
                    return await CreateScrollSuccessAsync(element, rootElement, stopwatch, cancellationToken);
                }
            }

            var parentScrollResult = await TryScrollParentToElementAsync(
                element,
                stopwatch.ElapsedMilliseconds,
                timeoutMs,
                cancellationToken);
            if (parentScrollResult.success)
            {
                return await CreateScrollSuccessAsync(element, rootElement, stopwatch, cancellationToken);
            }

            return UIAutomationResult.CreateFailure(
                "scroll_into_view",
                UIAutomationErrorType.PatternNotSupported,
                parentScrollResult.errorMessage ?? scrollResult.errorMessage ?? "Element does not support scrolling and no scrollable parent was found.",
                CreateDiagnostics(stopwatch));
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, $"ScrollItemPattern failed: {ex.Message}");
        }
    }

    private async Task<(bool success, string? errorMessage)> TryScrollParentToElementAsync(
        UIA.IUIAutomationElement element,
        long elapsedMs,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var remainingTimeout = TimeSpan.FromMilliseconds(Math.Max(1, timeoutMs - elapsedMs));
            var stopwatch = Stopwatch.StartNew();
            var parent = await _staThread.ExecuteAsync(() => element.GetParent(), cancellationToken);

            while (parent != null)
            {
                var scrollPattern = await _staThread.ExecuteAsync(
                    () => parent.GetPattern<UIA.IUIAutomationScrollPattern>(UIA3PatternIds.Scroll),
                    cancellationToken);
                if (scrollPattern != null)
                {
                    if (await _staThread.ExecuteAsync(() => !element.IsOffscreen(), cancellationToken))
                    {
                        return (true, null);
                    }

                    var rectangles = await _staThread.ExecuteAsync(
                        () => (Element: element.GetBoundingRectangle(), Parent: parent.GetBoundingRectangle()),
                        cancellationToken);
                    var elementRect = rectangles.Element;
                    var parentRect = rectangles.Parent;

                    if (elementRect.Width == 0 || parentRect.Width == 0)
                    {
                        var previousPercent = await _staThread.ExecuteAsync(
                            () =>
                            {
                                var percent = scrollPattern.CurrentVerticalScrollPercent;
                                scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeIncrement);
                                return percent;
                            },
                            cancellationToken);
                        _ = await WaitForElementConditionAsync(
                            element,
                            () =>
                                !element.IsOffscreen() ||
                                scrollPattern.CurrentVerticalScrollPercent != previousPercent,
                            cancellationToken,
                            Min(remainingTimeout, TimeSpan.FromMilliseconds(250)));
                        return (await _staThread.ExecuteAsync(() => !element.IsOffscreen(), cancellationToken), null);
                    }

                    var scrollAttempts = 0;
                    const int MaxAttempts = 50;

                    while (scrollAttempts < MaxAttempts && stopwatch.Elapsed < remainingTimeout)
                    {
                        if (await _staThread.ExecuteAsync(() => !element.IsOffscreen(), cancellationToken))
                        {
                            return (true, null);
                        }

                        var scrollStep = await _staThread.ExecuteAsync(() =>
                        {
                            elementRect = element.GetBoundingRectangle();
                            parentRect = parent.GetBoundingRectangle();
                            var previous = scrollPattern.CurrentVerticalScrollPercent;
                            if (elementRect.Y < parentRect.Y && previous > 0)
                            {
                                scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeDecrement);
                                return (Scrolled: true, PreviousPercent: previous);
                            }
                            if (elementRect.Y + elementRect.Height > parentRect.Y + parentRect.Height && previous < 100)
                            {
                                scrollPattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_LargeIncrement);
                                return (Scrolled: true, PreviousPercent: previous);
                            }
                            return (Scrolled: false, PreviousPercent: previous);
                        }, cancellationToken);
                        if (!scrollStep.Scrolled)
                        {
                            break;
                        }

                        var waitBudget = remainingTimeout - stopwatch.Elapsed;
                        if (waitBudget > TimeSpan.Zero)
                        {
                            _ = await WaitForElementConditionAsync(
                                element,
                                () =>
                                    !element.IsOffscreen() ||
                                    scrollPattern.CurrentVerticalScrollPercent != scrollStep.PreviousPercent,
                                cancellationToken,
                                Min(waitBudget, TimeSpan.FromMilliseconds(250)));
                        }
                        scrollAttempts++;
                    }

                    return (await _staThread.ExecuteAsync(() => !element.IsOffscreen(), cancellationToken), null);
                }

                parent = await _staThread.ExecuteAsync(() => parent.GetParent(), cancellationToken);
            }

            return (false, "No scrollable parent found.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "ParentScroll"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, $"Parent scroll failed: {ex.Message}");
        }
    }

    private async Task<UIAutomationResult> CreateScrollSuccessAsync(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        Stopwatch stopwatch,
        CancellationToken cancellationToken) =>
        await _staThread.ExecuteAsync(() =>
        {
            var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);
            return elementInfo == null
                ? UIAutomationResult.CreateSuccessWithHint(
                    "scroll_into_view",
                    "Scroll succeeded. Element closed its parent window.",
                    CreateDiagnostics(stopwatch))
                : UIAutomationResult.CreateSuccessCompact(
                    "scroll_into_view",
                    [elementInfo],
                    CreateDiagnostics(stopwatch));
        }, cancellationToken);

    private static TimeSpan Min(TimeSpan left, TimeSpan right) =>
        left < right ? left : right;

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