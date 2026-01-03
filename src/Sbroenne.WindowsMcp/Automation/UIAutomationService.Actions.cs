using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Office;
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
            if (!findResult.Success || findResult.Items == null || findResult.Items.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    CreateDiagnostics(stopwatch));
            }

            var targetElement = findResult.Items[0];
            var elementId = targetElement.Id;

            // Extract pre-computed click coordinates as fallback
            // FindElementsAsync uses cached bounds which may differ from current bounds
            // (e.g., WinForms TabPage children report 0,0,0,0 current bounds even when visible)
            Point? fallbackClickPoint = null;
            if (targetElement.Click != null && targetElement.Click.Length >= 2)
            {
                // Convert monitor-relative coordinates back to screen coordinates
                var monitorIndex = targetElement.Click.Length >= 3 ? targetElement.Click[2] : 0;
                var monitorOrigin = _coordinateConverter.GetMonitorOrigin(monitorIndex);
                fallbackClickPoint = new Point(
                    targetElement.Click[0] + monitorOrigin.X,
                    targetElement.Click[1] + monitorOrigin.Y);
            }

            return await PerformClickAsync(elementId, query.WindowHandle, fallbackClickPoint, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndClickError(_logger, query.Name ?? query.AutomationId ?? "unknown", ex);
            return UIAutomationResult.CreateFailure(
                "click",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Click"),
                CreateDiagnostics(stopwatch));
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

    private async Task<UIAutomationResult> PerformClickAsync(string elementId, string? windowHandle, Point? fallbackClickPoint, Stopwatch stopwatch, CancellationToken cancellationToken)
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

            nint? activationHandle = null;
            if (!string.IsNullOrWhiteSpace(windowHandle))
            {
                if (!WindowHandleParser.TryParse(windowHandle, out var parsedHandle))
                {
                    return UIAutomationResult.CreateFailure(
                        "click",
                        UIAutomationErrorType.InvalidParameter,
                        $"Invalid windowHandle '{windowHandle}'. Expected decimal string from window_management(handle).",
                        CreateDiagnostics(stopwatch));
                }

                activationHandle = parsedHandle;
            }

            // Ensure window is activated before clicking
            TryActivateWindowForElement(element, activationHandle);

            var rootElement = GetRootElementForScroll(element);
            var controlType = element.GetControlTypeId();

            // For TabItems: prefer physical click over SelectionItemPattern.Select()
            // In WinForms, Select() changes selection state but doesn't always trigger visual update.
            // Physical click simulates user interaction and properly switches visible tab content.
            if (controlType == UIA3ControlTypeIds.TabItem)
            {
                var tabClickPoint = GetClickablePointForClick(element) ?? fallbackClickPoint;
                if (tabClickPoint.HasValue)
                {
                    PerformPhysicalClick(tabClickPoint.Value);
                    // Give UI time to process the tab switch (WinForms needs this)
                    Thread.Sleep(50);
                    var tabInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    return UIAutomationResult.CreateSuccessCompact("click", tabInfo != null ? [tabInfo] : [], CreateDiagnostics(stopwatch));
                }
                // Fall back to Select() if no clickable point (e.g., tab header not visible)
                if (element.TrySelect())
                {
                    Thread.Sleep(50);
                    var tabInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    return UIAutomationResult.CreateSuccessCompact("click", tabInfo != null ? [tabInfo] : [], CreateDiagnostics(stopwatch));
                }
            }

            // For Buttons: prefer physical click to ensure click handlers fire
            // InvokePattern.Invoke() may report success but not trigger the actual click handler
            // in some WinForms scenarios (especially for buttons on TabPages).
            if (controlType == UIA3ControlTypeIds.Button)
            {
                var buttonClickPoint = GetClickablePointForClick(element) ?? fallbackClickPoint;
                if (buttonClickPoint.HasValue)
                {
                    PerformPhysicalClick(buttonClickPoint.Value);
                    Thread.Sleep(50);
                    var buttonInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    if (buttonInfo == null)
                    {
                        // Click succeeded but element became unavailable (e.g., dialog opened/closed).
                        return UIAutomationResult.CreateSuccessWithHint("click", "Click succeeded. Element may have triggered a dialog.", CreateDiagnostics(stopwatch));
                    }
                    return UIAutomationResult.CreateSuccessCompact("click", [buttonInfo], CreateDiagnostics(stopwatch));
                }
                // Fall back to InvokePattern if no clickable point
                if (element.TryInvoke())
                {
                    Thread.Sleep(50);
                    var buttonInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    if (buttonInfo == null)
                    {
                        return UIAutomationResult.CreateSuccessWithHint("click", "Click succeeded. Element may have triggered a dialog.", CreateDiagnostics(stopwatch));
                    }
                    return UIAutomationResult.CreateSuccessCompact("click", [buttonInfo], CreateDiagnostics(stopwatch));
                }
            }

            // Use the appropriate pattern based on control type
            var clicked = controlType switch
            {
                // ListItem, TreeItem, RadioButton: Use SelectionItemPattern
                UIA3ControlTypeIds.ListItem or
                UIA3ControlTypeIds.TreeItem or
                UIA3ControlTypeIds.RadioButton => element.TrySelect(),

                // CheckBox: Use TogglePattern
                UIA3ControlTypeIds.CheckBox => element.TryToggle(),

                // MenuItem, Hyperlink, SplitButton: Use InvokePattern
                UIA3ControlTypeIds.MenuItem or
                UIA3ControlTypeIds.Hyperlink or
                UIA3ControlTypeIds.SplitButton => element.TryInvoke(),

                // All other control types: Use physical click at coordinates
                _ => false
            };

            // If pattern-based click succeeded, return success
            if (clicked)
            {
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                if (info == null)
                {
                    // Click succeeded but element became unavailable (e.g., dialog closed).
                    // This is expected behavior for buttons that close their parent window.
                    return UIAutomationResult.CreateSuccessWithHint("click", "Click succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
                }
                return UIAutomationResult.CreateSuccessCompact("click", [info], CreateDiagnostics(stopwatch));
            }

            // For control types without specific patterns, or if pattern failed, use physical click
            // Try current bounds first, then fall back to pre-computed coordinates from find
            var clickablePoint = GetClickablePointForClick(element) ?? fallbackClickPoint;
            if (clickablePoint.HasValue)
            {
                PerformPhysicalClick(clickablePoint.Value);
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                if (info == null)
                {
                    // Click succeeded but element became unavailable (e.g., dialog closed).
                    // This is expected behavior for buttons that close their parent window.
                    return UIAutomationResult.CreateSuccessWithHint("click", "Click succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
                }
                return UIAutomationResult.CreateSuccessCompact("click", [info], CreateDiagnostics(stopwatch));
            }

            return UIAutomationResult.CreateFailure(
                "click",
                UIAutomationErrorType.PatternNotSupported,
                $"Element (ControlType={UIA3ControlTypeIds.ToName(controlType)}) cannot be clicked: pattern not supported and no clickable point available.",
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
            var searchQueries = BuildTypeSearchQueries(query);
            UIAutomationResult? lastResult = null;

            foreach (var searchQuery in searchQueries)
            {
                var findResult = await FindElementsAsync(searchQuery, cancellationToken);
                lastResult = findResult;

                if (findResult.Success && findResult.Items is { Length: > 0 })
                {
                    var targetElement = findResult.Items[0];
                    var elementId = targetElement.Id;

                    return await PerformTypeAsync(elementId, text, clearFirst, searchQuery.WindowHandle, stopwatch, cancellationToken);
                }
            }

            var errorMessage = lastResult?.ErrorMessage ?? "Element not found.";
            if (searchQueries.Count > 1 && string.IsNullOrEmpty(lastResult?.ErrorMessage))
            {
                errorMessage = "Element not found. Tried default Document/Edit controls. Provide elementId or search criteria (name, controlType, automationId).";
            }

            return UIAutomationResult.CreateFailure(
                "type",
                lastResult?.ErrorType ?? UIAutomationErrorType.ElementNotFound,
                errorMessage,
                CreateDiagnostics(stopwatch));
        }
        catch (COMException ex)
        {
            LogFindAndTypeError(_logger, query.Name ?? query.AutomationId ?? "unknown", ex);
            return UIAutomationResult.CreateFailure(
                "type",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Type"),
                CreateDiagnostics(stopwatch));
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

    private static List<ElementQuery> BuildTypeSearchQueries(ElementQuery baseQuery)
    {
        var queries = new List<ElementQuery>();

        var hasSelector = !string.IsNullOrEmpty(baseQuery.Name) ||
                          !string.IsNullOrEmpty(baseQuery.NameContains) ||
                          !string.IsNullOrEmpty(baseQuery.NamePattern) ||
                          !string.IsNullOrEmpty(baseQuery.AutomationId) ||
                          !string.IsNullOrEmpty(baseQuery.ClassName) ||
                          !string.IsNullOrEmpty(baseQuery.ControlType);

        // For plain "type" without selectors, prefer typical text controls first.
        if (!hasSelector)
        {
            queries.Add(baseQuery with { ControlType = "Document" });
            queries.Add(baseQuery with { ControlType = "Edit" });
        }

        queries.Add(baseQuery);

        return queries;
    }

    private async Task<UIAutomationResult> PerformTypeAsync(string elementId, string text, bool clearFirst, string? windowHandle, Stopwatch stopwatch, CancellationToken cancellationToken)
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

            nint? activationHandle = null;
            if (!string.IsNullOrWhiteSpace(windowHandle))
            {
                if (!WindowHandleParser.TryParse(windowHandle, out var parsedHandle))
                {
                    return (Success: false, Result: UIAutomationResult.CreateFailure(
                        "type",
                        UIAutomationErrorType.InvalidParameter,
                        $"Invalid windowHandle '{windowHandle}'. Expected decimal string from window_management(handle).",
                        CreateDiagnostics(stopwatch)), ValuePatternSucceeded: false, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                }

                activationHandle = parsedHandle;
            }

            // Ensure window is activated before typing
            TryActivateWindowForElement(element, activationHandle);

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
                        if (info == null)
                        {
                            // Type succeeded but element became unavailable (e.g., dialog closed).
                            // This is expected behavior for text fields that close their parent window.
                            return (Success: true, Result: UIAutomationResult.CreateSuccessWithHint("type", "Type succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                        }
                        return (Success: true, Result: UIAutomationResult.CreateSuccessCompact("type", [info], CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: element, RootElement: rootElement);
                    }
                }
            }
            else
            {
                if (element.TrySetValue(text))
                {
                    var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    if (info == null)
                    {
                        // Type succeeded but element became unavailable (e.g., dialog closed).
                        // This is expected behavior for text fields that close their parent window.
                        return (Success: true, Result: UIAutomationResult.CreateSuccessWithHint("type", "Type succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                    }
                    return (Success: true, Result: UIAutomationResult.CreateSuccessCompact("type", [info], CreateDiagnostics(stopwatch)), ValuePatternSucceeded: true, Element: element, RootElement: rootElement);
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
            if (info == null)
            {
                // Type succeeded but element became unavailable (e.g., dialog closed).
                // This is expected behavior for text fields that close their parent window.
                return UIAutomationResult.CreateSuccessWithHint("type", "Type succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
            }
            return UIAutomationResult.CreateSuccessCompact("type", [info], CreateDiagnostics(stopwatch));
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
            if (!findResult.Success || findResult.Items == null || findResult.Items.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "select",
                    UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    CreateDiagnostics(stopwatch));
            }

            var targetElement = findResult.Items[0];
            var elementId = targetElement.Id;

            return await PerformSelectAsync(elementId, value, query.WindowHandle, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndSelectError(_logger, query.Name ?? query.AutomationId ?? "unknown", value, ex);
            return UIAutomationResult.CreateFailure(
                "select",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Select"),
                CreateDiagnostics(stopwatch));
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

    private async Task<UIAutomationResult> PerformSelectAsync(string elementId, string value, string? windowHandle, Stopwatch stopwatch, CancellationToken cancellationToken)
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

            nint? activationHandle = null;
            if (!string.IsNullOrWhiteSpace(windowHandle))
            {
                if (!WindowHandleParser.TryParse(windowHandle, out var parsedHandle))
                {
                    return UIAutomationResult.CreateFailure(
                        "select",
                        UIAutomationErrorType.InvalidParameter,
                        $"Invalid windowHandle '{windowHandle}'. Expected decimal string from window_management(handle).",
                        CreateDiagnostics(stopwatch));
                }

                activationHandle = parsedHandle;
            }

            // Ensure window is activated
            TryActivateWindowForElement(element, activationHandle);

            var rootElement = GetRootElementForScroll(element);

            // Try SelectionPattern or SelectionItemPattern
            if (TrySelectItem(element, value))
            {
                var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                if (info == null)
                {
                    // Select succeeded but element became unavailable (e.g., dialog closed).
                    // This is expected behavior for elements that close their parent window.
                    return UIAutomationResult.CreateSuccessWithHint("select", "Select succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
                }
                return UIAutomationResult.CreateSuccessCompact("select", [info], CreateDiagnostics(stopwatch));
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
                            if (info == null)
                            {
                                // Select succeeded but element became unavailable (e.g., dialog closed).
                                // This is expected behavior for elements that close their parent window.
                                return UIAutomationResult.CreateSuccessWithHint("select", "Select succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
                            }
                            return UIAutomationResult.CreateSuccessCompact("select", [info], CreateDiagnostics(stopwatch));
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

    /// <inheritdoc/>
    public async Task<UIAutomationResult> ClickElementAsync(string elementId, string? windowHandle, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ClickElementAsync is used when clicking by element ID (not from find result)
            // No pre-computed click coordinates available, so pass null
            return await PerformClickAsync(elementId, windowHandle, fallbackClickPoint: null, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndClickError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "click",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Click"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndClickError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "click",
                UIAutomationErrorType.InternalError,
                $"Click failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> HighlightElementAsync(string elementId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "highlight",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element with ID '{elementId}' could not be resolved.",
                        CreateDiagnostics(stopwatch));
                }

                var rect = element.CurrentBoundingRectangle;
                if (rect.right <= rect.left || rect.bottom <= rect.top)
                {
                    return UIAutomationResult.CreateFailure(
                        "highlight",
                        UIAutomationErrorType.InvalidParameter,
                        "Element has no visible bounding rectangle.",
                        CreateDiagnostics(stopwatch));
                }

                // Draw highlight rectangle using GDI
                DrawHighlightRectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

                var rootElement = GetRootElementForScroll(element);
                var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                if (elementInfo == null)
                {
                    // Highlight succeeded but element became unavailable (e.g., dialog closed).
                    // This is expected behavior for elements that close their parent window.
                    return UIAutomationResult.CreateSuccessWithHint("highlight", "Highlight succeeded. Element closed its parent window.", CreateDiagnostics(stopwatch));
                }
                return UIAutomationResult.CreateSuccessCompact("highlight", [elementInfo], CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure(
                "highlight",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Highlight"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return UIAutomationResult.CreateFailure(
                "highlight",
                UIAutomationErrorType.InternalError,
                $"Highlight failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    // Static field to track the current highlight form for explicit hide control
    private static HighlightForm? s_currentHighlightForm;
    private static readonly object s_highlightLock = new();

    /// <inheritdoc/>
    public Task<UIAutomationResult> HideHighlightAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        lock (s_highlightLock)
        {
            if (s_currentHighlightForm == null)
            {
                return Task.FromResult(UIAutomationResult.CreateSuccess("hide_highlight", CreateDiagnostics(stopwatch)));
            }

            try
            {
                // Close the form on its owning thread
                var form = s_currentHighlightForm;
                s_currentHighlightForm = null;

                if (form.InvokeRequired)
                {
                    form.BeginInvoke(() => form.Close());
                }
                else
                {
                    form.Close();
                }

                return Task.FromResult(UIAutomationResult.CreateSuccess("hide_highlight", CreateDiagnostics(stopwatch)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Task.FromResult(UIAutomationResult.CreateFailure(
                    "hide_highlight",
                    UIAutomationErrorType.InternalError,
                    $"Failed to hide highlight: {ex.Message}",
                    CreateDiagnostics(stopwatch)));
            }
        }
    }

    private static void DrawHighlightRectangle(int x, int y, int width, int height)
    {
        // Close any existing highlight first
        lock (s_highlightLock)
        {
            if (s_currentHighlightForm != null)
            {
                try
                {
                    var oldForm = s_currentHighlightForm;
                    s_currentHighlightForm = null;
                    if (oldForm.InvokeRequired)
                    {
                        oldForm.BeginInvoke(() => oldForm.Close());
                    }
                    else
                    {
                        oldForm.Close();
                    }
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }

        // Use a separate thread to manage the highlight (STA required for WinForms)
        var highlightThread = new Thread(() =>
        {
            try
            {
                var form = new HighlightForm(x, y, width, height);
                lock (s_highlightLock)
                {
                    s_currentHighlightForm = form;
                }
                form.Show();

                // Run message loop until form is closed
                System.Windows.Forms.Application.Run(form);
            }
            catch
            {
                // Best effort highlight - ignore errors
            }
            finally
            {
                lock (s_highlightLock)
                {
                    s_currentHighlightForm = null;
                }
            }
        });
        highlightThread.SetApartmentState(ApartmentState.STA);
        highlightThread.IsBackground = true;
        highlightThread.Start();

        // Don't wait for the highlight - return immediately
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> SaveFileDialogAsync(string windowHandle, string filePath, CancellationToken cancellationToken = default)
    {
        // For Office apps (Word, Excel, PowerPoint, Visio, Publisher), use COM Interop
        // instead of UI Automation. This is more reliable as modern Office apps don't
        // use standard Save As dialogs.
        var processName = OfficeComHelper.GetProcessNameFromHandle(windowHandle);
        var officeAppType = OfficeComHelper.GetOfficeAppType(processName);

        if (officeAppType != OfficeComHelper.OfficeAppType.None)
        {
            var comResult = OfficeComHelper.SaveDocument(officeAppType, filePath);
            if (comResult.IsSuccess)
            {
                return UIAutomationResult.CreateSuccessWithHint("save", comResult.Message);
            }
            else
            {
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.InternalError,
                    comResult.Message);
            }
        }

        // Non-Office apps: Use UI Automation to interact with Save As dialog
        // Implementation based on FlaUI patterns:
        // 1. Get the parent window element from handle
        // 2. Find the modal Save As dialog as a child/descendant window (like FlaUI's ModalWindows property)
        // 3. Find filename field by AutomationId "FileNameControlHost" (ComboBox containing Edit)
        // 4. Type full path into the field (Windows handles folder navigation)
        // 5. Click "Save" button by name
        // 6. Handle "Confirm Save As" overwrite dialog if it appears
        // 7. For Office apps: Detect Backstage and navigate through it if needed

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Phase 1: Find dialog and elements on STA thread
            var findResult = await _staThread.ExecuteAsync(() =>
            {
                // Validate window handle format
                if (!nint.TryParse(windowHandle, out var hwnd))
                {
                    return (Error: UIAutomationResult.CreateFailure(
                        "save",
                        UIAutomationErrorType.InvalidParameter,
                        $"Invalid window handle format: '{windowHandle}'",
                        CreateDiagnostics(stopwatch)), FilenameElement: (UIA.IUIAutomationElement?)null, SaveButton: (UIA.IUIAutomationElement?)null, IsOfficeBackstage: false);
                }

                // Strategy: Search desktop first for Save As dialog (more reliable when modal is showing)
                // This avoids timeout issues when parent window is blocked by modal dialog.
                var filenameCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "FileNameControlHost");
                UIA.IUIAutomationElement? dialogElement = null;
                UIA.IUIAutomationElement? filenameCombo = null;

                // Try 1: Search desktop children for "Save As" window (common file dialogs are top-level)
                try
                {
                    var saveAsCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "Save As"));
                    dialogElement = Uia.RootElement.FindFirst(UIA.TreeScope.TreeScope_Children, saveAsCondition);

                    if (dialogElement != null)
                    {
                        filenameCombo = dialogElement.FindFirst(UIA.TreeScope.TreeScope_Descendants, filenameCondition);
                    }
                }
                catch (COMException)
                {
                    // Desktop search failed - continue with other strategies
                }
                catch (TimeoutException)
                {
                    // Desktop search timed out - continue with other strategies
                }

                // Try 2: Check if the provided handle IS the dialog itself
                if (filenameCombo == null)
                {
                    try
                    {
                        var handleElement = Uia.ElementFromHandle(hwnd);
                        if (handleElement != null)
                        {
                            // Check if this element itself has the FileNameControlHost
                            filenameCombo = handleElement.FindFirst(UIA.TreeScope.TreeScope_Descendants, filenameCondition);
                            if (filenameCombo != null)
                            {
                                dialogElement = handleElement;
                            }
                        }
                    }
                    catch (COMException)
                    {
                        // Parent window may be blocked/unresponsive - continue
                    }
                    catch (TimeoutException)
                    {
                        // Parent window may timeout - continue
                    }
                }

                if (filenameCombo != null && dialogElement != null)
                {
                    // Found standard dialog - find edit and save button
                    var editCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Edit);
                    var editElement = filenameCombo.FindFirst(UIA.TreeScope.TreeScope_Descendants, editCondition);
                    var targetElement = editElement ?? filenameCombo;

                    // Find Save button
                    var saveCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "Save"));
                    var saveButton = dialogElement.FindFirst(UIA.TreeScope.TreeScope_Descendants, saveCondition);

                    if (saveButton == null)
                    {
                        var buttonById = Uia.CreateAndCondition(
                            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                            Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1"));
                        saveButton = dialogElement.FindFirst(UIA.TreeScope.TreeScope_Descendants, buttonById);
                    }

                    return (Error: (UIAutomationResult?)null, FilenameElement: targetElement, SaveButton: saveButton, IsOfficeBackstage: false);
                }

                // Try Office Backstage - look for Browse button (only if we have a dialog element)
                if (dialogElement != null)
                {
                    var browseCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "Browse");
                    var browseButton = dialogElement.FindFirst(UIA.TreeScope.TreeScope_Descendants, browseCondition);

                    if (browseButton != null)
                    {
                        return (Error: (UIAutomationResult?)null, FilenameElement: (UIA.IUIAutomationElement?)null, SaveButton: browseButton, IsOfficeBackstage: true);
                    }
                }

                return (Error: UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not find filename field. Neither standard Windows dialog (FileNameControlHost) nor Office Backstage detected.",
                    CreateDiagnostics(stopwatch),
                    "Ensure the Save As dialog is open and visible."), FilenameElement: (UIA.IUIAutomationElement?)null, SaveButton: (UIA.IUIAutomationElement?)null, IsOfficeBackstage: false);
            }, cancellationToken);

            // Check for errors
            if (findResult.Error != null)
            {
                return findResult.Error;
            }

            // Phase 2: Handle Office Backstage if detected
            if (findResult.IsOfficeBackstage && findResult.SaveButton != null)
            {
                // Click Browse to open standard file dialog
                await _staThread.ExecuteAsync(() =>
                {
                    if (!findResult.SaveButton.TryInvoke())
                    {
                        var rect = findResult.SaveButton.CurrentBoundingRectangle;
                        _mouseService.ClickAsync((rect.left + rect.right) / 2, (rect.top + rect.bottom) / 2, cancellationToken: cancellationToken).GetAwaiter().GetResult();
                    }
                    return true;
                }, cancellationToken);

                Thread.Sleep(500); // Wait for file dialog to open

                // Recursively call to handle the now-open standard dialog
                // Find the new Save As dialog window
                var newDialogHandle = await _staThread.ExecuteAsync(() =>
                {
                    var saveAsCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "Save As");
                    var saveAsDialog = Uia.RootElement.FindFirst(UIA.TreeScope.TreeScope_Children, saveAsCondition);
                    if (saveAsDialog != null)
                    {
                        return saveAsDialog.CurrentNativeWindowHandle.ToString();
                    }
                    return windowHandle; // Fallback to original
                }, cancellationToken);

                return await SaveFileDialogAsync(newDialogHandle, filePath, cancellationToken);
            }

            // Phase 3: Standard dialog - set value and click save
            if (findResult.FilenameElement == null)
            {
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not find filename field.",
                    CreateDiagnostics(stopwatch));
            }

            // Set the filename value
            bool valueSet = await _staThread.ExecuteAsync(() =>
            {
                return findResult.FilenameElement.TrySetValue(filePath);
            }, cancellationToken);

            // Fallback: Use keyboard input
            if (!valueSet)
            {
                await _staThread.ExecuteAsync(() =>
                {
                    try
                    {
                        findResult.FilenameElement.SetFocus();
                    }
                    catch
                    {
                        // Best effort focus
                    }
                    return true;
                }, cancellationToken);

                Thread.Sleep(50);
                await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, cancellationToken: cancellationToken);
                Thread.Sleep(30);
                await _keyboardService.TypeTextAsync(filePath, cancellationToken);
                Thread.Sleep(50);
            }

            // Click Save button
            if (findResult.SaveButton == null)
            {
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not find Save button in the dialog.",
                    CreateDiagnostics(stopwatch),
                    "The filename was entered but the Save button could not be found.");
            }

            bool clicked = await _staThread.ExecuteAsync(() =>
            {
                return findResult.SaveButton.TryInvoke();
            }, cancellationToken);

            if (!clicked)
            {
                var rect = await _staThread.ExecuteAsync(() => findResult.SaveButton.CurrentBoundingRectangle, cancellationToken);
                await _mouseService.ClickAsync((rect.left + rect.right) / 2, (rect.top + rect.bottom) / 2, cancellationToken: cancellationToken);
            }

            Thread.Sleep(200);

            // Handle overwrite confirmation
            await HandleOverwriteConfirmationAsync(cancellationToken);

            return UIAutomationResult.CreateSuccess("save", CreateDiagnostics(stopwatch));
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure(
                "save",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Save"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return UIAutomationResult.CreateFailure(
                "save",
                UIAutomationErrorType.InternalError,
                $"Save failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <summary>
    /// Handles the "Confirm Save As" overwrite confirmation dialog if it appears.
    /// </summary>
    private async Task HandleOverwriteConfirmationAsync(CancellationToken cancellationToken)
    {
        // Check for common overwrite confirmation dialogs
        var confirmNames = new[] { "Confirm Save As", "Replace or Skip Files", "Confirm" };
        var buttonNames = new[] { "Yes", "Replace", "Confirm", "&Yes" };

        var buttonToClick = await _staThread.ExecuteAsync(() =>
        {
            foreach (var confirmName in confirmNames)
            {
                var confirmDialogCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.Name, confirmName);
                var confirmDialog = Uia.RootElement.FindFirst(UIA.TreeScope.TreeScope_Children, confirmDialogCondition);

                if (confirmDialog != null)
                {
                    foreach (var buttonName in buttonNames)
                    {
                        var yesCondition = Uia.CreateAndCondition(
                            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                            Uia.CreatePropertyCondition(UIA3PropertyIds.Name, buttonName));
                        var yesButton = confirmDialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, yesCondition);

                        if (yesButton != null)
                        {
                            return yesButton;
                        }
                    }
                }
            }
            return (UIA.IUIAutomationElement?)null;
        }, cancellationToken);

        if (buttonToClick != null)
        {
            bool clicked = await _staThread.ExecuteAsync(() => buttonToClick.TryInvoke(), cancellationToken);

            if (!clicked)
            {
                var rect = await _staThread.ExecuteAsync(() => buttonToClick.CurrentBoundingRectangle, cancellationToken);
                await _mouseService.ClickAsync(
                    (rect.left + rect.right) / 2,
                    (rect.top + rect.bottom) / 2,
                    cancellationToken: cancellationToken);
            }
        }
    }

    /// <summary>
    /// A transparent form with a colored border for highlighting UI elements.
    /// </summary>
    private sealed class HighlightForm : System.Windows.Forms.Form
    {
        private const int BorderThickness = 3;

        public HighlightForm(int x, int y, int width, int height)
        {
            // Set form properties for a transparent overlay
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            BackColor = System.Drawing.Color.Red;
            TransparencyKey = System.Drawing.Color.Magenta;

            // Position and size
            Location = new System.Drawing.Point(x - BorderThickness, y - BorderThickness);
            Size = new System.Drawing.Size(width + 2 * BorderThickness, height + 2 * BorderThickness);
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw outer rectangle (border)
            using var pen = new System.Drawing.Pen(System.Drawing.Color.Red, BorderThickness);
            e.Graphics.DrawRectangle(pen, BorderThickness / 2, BorderThickness / 2, Width - BorderThickness, Height - BorderThickness);

            // Fill inner area with transparency key color
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Magenta);
            e.Graphics.FillRectangle(brush, BorderThickness, BorderThickness, Width - 2 * BorderThickness, Height - 2 * BorderThickness);
        }

        protected override System.Windows.Forms.CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000020 | 0x00080000 | 0x00000080 | 0x08000000;
                return cp;
            }
        }
    }
}
