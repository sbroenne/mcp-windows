using System.Diagnostics;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// High-level action operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndClickAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success || findResult.Elements == null || findResult.Elements.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    CreateDiagnostics(stopwatch));
            }

            var targetElement = findResult.Elements[0];
            var elementId = targetElement.ElementId;

            return await PerformClickAsync(elementId, query.WindowHandle, stopwatch, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndClickError(_logger, query.Name ?? query.AutomationId ?? "unknown", ex);
            return UIAutomationResult.CreateFailure(
                "click",
                UIAutomationErrorType.InternalError,
                $"Click failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private async Task<UIAutomationResult> PerformClickAsync(string elementId, nint? windowHandle, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        return await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.ElementNotFound,
                    $"Element with ID '{elementId}' could not be resolved.",
                    CreateDiagnostics(stopwatch));
            }

            // Ensure window is activated before clicking
            TryActivateWindowForElement(element, windowHandle);

            var rootElement = GetRootElementForScroll(element);

            // Try InvokePattern first
            if (element.TryInvoke())
            {
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                return UIAutomationResult.CreateSuccess("click", info!, CreateDiagnostics(stopwatch));
            }

            // Fall back to clicking at element's clickable point
            var clickablePoint = GetClickablePointForClick(element);
            if (clickablePoint.HasValue)
            {
                PerformPhysicalClick(clickablePoint.Value);
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                return UIAutomationResult.CreateSuccess("click", info!, CreateDiagnostics(stopwatch));
            }

            return UIAutomationResult.CreateFailure(
                "click",
                UIAutomationErrorType.PatternNotSupported,
                "Element cannot be clicked: no Invoke pattern and no clickable point available.",
                CreateDiagnostics(stopwatch));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndTypeAsync(ElementQuery query, string text, bool clearFirst, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success || findResult.Elements == null || findResult.Elements.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    CreateDiagnostics(stopwatch));
            }

            var targetElement = findResult.Elements[0];
            var elementId = targetElement.ElementId;

            return await PerformTypeAsync(elementId, text, clearFirst, query.WindowHandle, stopwatch, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndTypeError(_logger, query.Name ?? query.AutomationId ?? "unknown", ex);
            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.InternalError,
                $"Type failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private async Task<UIAutomationResult> PerformTypeAsync(string elementId, string text, bool clearFirst, nint? windowHandle, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        // First phase: resolve element and try ValuePattern (on STA thread)
        var staResult = await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.ElementNotFound,
                    $"Element with ID '{elementId}' could not be resolved.",
                    CreateDiagnostics(stopwatch)), ValuePatternSucceeded: false, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
            }

            // Ensure window is activated before typing
            TryActivateWindowForElement(element, windowHandle);

            // Try to set focus
            element.TrySetFocus();

            var rootElement = GetRootElementForScroll(element);

            // Try ValuePattern first - need to clear manually if clearFirst
            if (clearFirst)
            {
                // Try to clear via ValuePattern
                if (element.TrySetValue(""))
                {
                    // Then set new value
                    if (element.TrySetValue(text))
                    {
                        var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                        return (Success: true, Result: UIAutomationResult.CreateSuccess("type", info!, CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: element, RootElement: rootElement);
                    }
                }
            }
            else
            {
                if (element.TrySetValue(text))
                {
                    var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    return (Success: true, Result: UIAutomationResult.CreateSuccess("type", info!, CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: element, RootElement: rootElement);
                }
            }

            // Need to fall back to keyboard input
            return (Success: true, Result: (UIAutomationResult?)null, ValuePatternSucceeded: false, Element: element, RootElement: rootElement);
        }, cancellationToken);

        // Check if we already have a result (success or element not found)
        if (!staResult.Success)
        {
            return staResult.Result!;
        }

        if (staResult.ValuePatternSucceeded)
        {
            return staResult.Result!;
        }

        // Fall back to keyboard input (outside STA thread)
        if (clearFirst)
        {
            await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, 1, cancellationToken);
            await Task.Delay(50, cancellationToken);
        }

        await _keyboardService.TypeTextAsync(text, cancellationToken);

        // Get final element info (on STA thread)
        return await _staThread.ExecuteAsync(() =>
        {
            var info = ConvertToElementInfo(staResult.Element!, staResult.RootElement!, _coordinateConverter);
            return UIAutomationResult.CreateSuccess("type", info!, CreateDiagnostics(stopwatch));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> FindAndSelectAsync(ElementQuery query, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var findResult = await FindElementsAsync(query, cancellationToken);
            if (!findResult.Success || findResult.Elements == null || findResult.Elements.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "select",
                    UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    CreateDiagnostics(stopwatch));
            }

            var targetElement = findResult.Elements[0];
            var elementId = targetElement.ElementId;

            return await PerformSelectAsync(elementId, value, query.WindowHandle, stopwatch, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndSelectError(_logger, query.Name ?? query.AutomationId ?? "unknown", value, ex);
            return UIAutomationResult.CreateFailure(
                "select",
                UIAutomationErrorType.InternalError,
                $"Select failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private async Task<UIAutomationResult> PerformSelectAsync(string elementId, string value, nint? windowHandle, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        return await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return UIAutomationResult.CreateFailure(
                    "select",
                    UIAutomationErrorType.ElementNotFound,
                    $"Element with ID '{elementId}' could not be resolved.",
                    CreateDiagnostics(stopwatch));
            }

            // Ensure window is activated
            TryActivateWindowForElement(element, windowHandle);

            var rootElement = GetRootElementForScroll(element);

            // Try SelectionPattern or SelectionItemPattern
            if (TrySelectItem(element, value))
            {
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                return UIAutomationResult.CreateSuccess("select", info!, CreateDiagnostics(stopwatch));
            }

            // Try ExpandCollapse + find item
            var expandPattern = element.GetPattern<UIA.IUIAutomationExpandCollapsePattern>(UIA3PatternIds.ExpandCollapse);
            if (expandPattern != null)
            {
                try
                {
                    expandPattern.Expand();
                    Thread.Sleep(100);

                    // Find and click the item
                    var itemCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.Name, value);
                    var item = element.FindFirst(UIA.TreeScope.TreeScope_Descendants, itemCondition);

                    if (item != null)
                    {
                        if (item.TryInvoke() || TrySelectElement(item))
                        {
                            var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                            return UIAutomationResult.CreateSuccess("select", info!, CreateDiagnostics(stopwatch));
                        }
                    }
                }
                catch
                {
                    // Continue to failure
                }
            }

            return UIAutomationResult.CreateFailure(
                "select",
                UIAutomationErrorType.PatternNotSupported,
                $"Could not select value '{value}': element does not support selection.",
                CreateDiagnostics(stopwatch));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UIElementInfo?> ResolveElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        return await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return null;
            }

            var rootElement = GetRootElementForScroll(element);
            return ConvertToElementInfo(element, rootElement, _coordinateConverter);
        }, cancellationToken);
    }

    private static bool TrySelectItem(UIA.IUIAutomationElement container, string value)
    {
        // Try finding the item within the container and selecting it
        var condition = UIA3Automation.Instance.CreatePropertyCondition(UIA3PropertyIds.Name, value);
        var item = container.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);

        if (item != null)
        {
            return TrySelectElement(item);
        }

        return false;
    }

    private static bool TrySelectElement(UIA.IUIAutomationElement element)
    {
        var selectionItemPattern = element.GetPattern<UIA.IUIAutomationSelectionItemPattern>(UIA3PatternIds.SelectionItem);
        if (selectionItemPattern != null)
        {
            try
            {
                selectionItemPattern.Select();
                return true;
            }
            catch
            {
                // Pattern failed
            }
        }

        return element.TryInvoke();
    }

    private void TryActivateWindowForElement(UIA.IUIAutomationElement element, nint? windowHandle)
    {
        try
        {
            if (_windowService == null)
            {
                return;
            }

            var handle = windowHandle;
            if (!handle.HasValue || handle.Value == IntPtr.Zero)
            {
                // Walk up to find a window
                var current = element;
                var walker = Uia.ControlViewWalker;
                while (current != null)
                {
                    try
                    {
                        var hwnd = current.CurrentNativeWindowHandle;
                        if (hwnd != 0)
                        {
                            handle = new IntPtr(hwnd);
                            break;
                        }

                        current = walker.GetParentElement(current);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            if (handle.HasValue && handle.Value != IntPtr.Zero)
            {
                _windowService.ActivateWindowAsync(handle.Value).GetAwaiter().GetResult();
                Thread.Sleep(50);
            }
        }
        catch
        {
            // Best effort - continue even if activation fails
        }
    }

    private static Point? GetClickablePointForClick(UIA.IUIAutomationElement element)
    {
        try
        {
            var rect = element.CurrentBoundingRectangle;
            if (rect.right <= rect.left || rect.bottom <= rect.top)
            {
                return null;
            }

            var x = rect.left + (rect.right - rect.left) / 2;
            var y = rect.top + (rect.bottom - rect.top) / 2;
            return new Point(x, y);
        }
        catch
        {
            return null;
        }
    }

    private void PerformPhysicalClick(Point point)
    {
        _mouseService.ClickAsync(point.X, point.Y, ModifierKey.None, CancellationToken.None).GetAwaiter().GetResult();
    }
}
