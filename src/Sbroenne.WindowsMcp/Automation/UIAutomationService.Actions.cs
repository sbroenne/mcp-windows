using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Utilities;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// High-level action operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <summary>
    /// Default timeout for waiting for dialogs to appear after Ctrl+S.
    /// </summary>
    private static readonly TimeSpan SaveDialogTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Polling interval for dialog detection retry loop.
    /// </summary>
    private static readonly TimeSpan SaveDialogPollInterval = TimeSpan.FromMilliseconds(100);
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

        // Normalize Windows file paths: convert forward slashes to backslashes
        // This handles paths like "D:/folder/file.txt" → "D:\folder\file.txt"
        // Required because Save As dialogs reject forward slashes
        text = PathNormalizer.NormalizeWindowsPath(text);

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
        // Try Document first (modern apps like Win11 Notepad), then Edit (classic apps).
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
            if (_windowActivator == null)
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
                _windowActivator.ActivateWindowAsync(handle.Value).GetAwaiter().GetResult();
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
    /// <remarks>
    /// Implementation based on FlaUI, pywinauto, and White Framework patterns:
    /// 1. Focus window and send Ctrl+S (universal save shortcut)
    /// 2. Wait for modal dialog using retry loop (FlaUI pattern)
    /// 3. If dialog appears and filePath provided: type path + Enter (pywinauto pattern)
    /// 4. Handle overwrite confirmation dialogs
    /// 5. Wait for dialog to close (completion detection)
    /// </remarks>
    public async Task<UIAutomationResult> SaveAsync(string windowHandle, string? filePath = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate window handle format
            if (!nint.TryParse(windowHandle, out var hwnd) || hwnd == IntPtr.Zero)
            {
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.InvalidParameter,
                    $"Invalid window handle format: '{windowHandle}'",
                    CreateDiagnostics(stopwatch));
            }

            // Normalize file path if provided
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                filePath = Path.GetFullPath(filePath);
            }

            // Step 1: Focus the target window (FlaUI/White pattern)
            var focusResult = await FocusWindowAsync(hwnd, cancellationToken);
            if (!focusResult)
            {
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not focus the target window.",
                    CreateDiagnostics(stopwatch));
            }

            await Task.Delay(100, cancellationToken); // Brief pause for focus

            // Step 2: Send Ctrl+S (universal save - pywinauto/FlaUI pattern)
            await _keyboardService.PressKeyAsync("s", ModifierKey.Ctrl, cancellationToken: cancellationToken);

            // Step 3: Wait for Save dialog using retry loop (FlaUI Retry.WhileEmpty pattern)
            var dialog = await WaitForSaveDialogAsync(hwnd, cancellationToken);

            if (dialog != null)
            {
                // Dialog appeared - need to fill in filename if provided
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var dialogResult = await FillSaveDialogAsync(dialog.Value.element, filePath, cancellationToken);
                    if (!dialogResult.Success)
                    {
                        return dialogResult;
                    }

                    // Wait for dialog to close (completion detection - White pattern)
                    await WaitForDialogCloseAsync(dialog.Value.element, cancellationToken);
                }
                else
                {
                    // No filePath - return hint that dialog is open
                    return UIAutomationResult.CreateSuccessWithHint(
                        "save",
                        "Save dialog opened. Use ui_automation to interact with it or provide filePath to auto-fill.",
                        CreateDiagnostics(stopwatch));
                }
            }

            // No dialog appeared = file was saved directly (already had a name)
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
    /// Focuses a window by handle.
    /// </summary>
    private async Task<bool> FocusWindowAsync(nint hwnd, CancellationToken cancellationToken)
    {
        return await _staThread.ExecuteAsync(() =>
        {
            var element = Uia.ElementFromHandle(hwnd);
            if (element == null)
            {
                return false;
            }

            try
            {
                element.SetFocus();
                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Waits for a Save dialog to appear using FlaUI-style retry loop.
    /// Returns the dialog element and its name, or null if no dialog appeared.
    /// </summary>
    private async Task<(UIA.IUIAutomationElement element, string name)?> WaitForSaveDialogAsync(
        nint parentHwnd, CancellationToken cancellationToken)
    {
        // Common save dialog title patterns (case-insensitive matching)
        string[] dialogPatterns = ["Save As", "Save as", "Save this file", "Save"];

        var deadline = DateTime.UtcNow + SaveDialogTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _staThread.ExecuteAsync(() =>
            {
                // First, check for modal windows of the parent (FlaUI pattern: window.ModalWindows)
                var parentElement = Uia.ElementFromHandle(parentHwnd);
                if (parentElement != null)
                {
                    // Search for modal windows
                    var windowCondition = Uia.CreatePropertyCondition(
                        UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window);
                    var children = parentElement.FindAll(UIA.TreeScope.TreeScope_Children, windowCondition);

                    if (children != null)
                    {
                        for (int i = 0; i < children.Length; i++)
                        {
                            var child = children.GetElement(i);
                            var windowPattern = child.GetPattern<UIA.IUIAutomationWindowPattern>(UIA3PatternIds.Window);
                            if (windowPattern != null)
                            {
                                try
                                {
                                    if (windowPattern.CurrentIsModal != 0)
                                    {
                                        var name = child.CurrentName ?? "";
                                        // Check if it matches any dialog pattern
                                        foreach (var pattern in dialogPatterns)
                                        {
                                            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                                            {
                                                return (element: child, name: name);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // Skip this element
                                }
                            }
                        }
                    }
                }

                // Fallback: search top-level windows (for system dialogs)
                foreach (var pattern in dialogPatterns)
                {
                    var condition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.Name, pattern));
                    var dialog = Uia.RootElement.FindFirst(UIA.TreeScope.TreeScope_Children, condition);
                    if (dialog != null)
                    {
                        return (element: dialog, name: pattern);
                    }
                }

                return ((UIA.IUIAutomationElement element, string name)?)null;
            }, cancellationToken);

            if (result.HasValue)
            {
                return result;
            }

            await Task.Delay(SaveDialogPollInterval, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Fills a Save dialog with the filename and confirms (pywinauto pattern).
    /// </summary>
    private async Task<UIAutomationResult> FillSaveDialogAsync(
        UIA.IUIAutomationElement dialog, string filePath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Focus the dialog first
        await _staThread.ExecuteAsync(() =>
        {
            try
            {
                dialog.SetFocus();
            }
            catch
            {
                // Best effort
            }
            return true;
        }, cancellationToken);

        await Task.Delay(100, cancellationToken);

        // Find the filename edit field (common AutomationIds: FileNameControlHost, 1001, Edit)
        var editField = await _staThread.ExecuteAsync(() =>
        {
            // Try by AutomationId first (most reliable)
            string[] editAutomationIds = ["FileNameControlHost", "1001"];
            foreach (var autoId in editAutomationIds)
            {
                var condition = Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, autoId);
                var field = dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);
                if (field != null)
                {
                    return field;
                }
            }

            // Fallback: find any Edit control
            var editCondition = Uia.CreatePropertyCondition(
                UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Edit);
            return dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, editCondition);
        }, cancellationToken);

        if (editField == null)
        {
            return UIAutomationResult.CreateFailure(
                "save",
                UIAutomationErrorType.ElementNotFound,
                "Could not find filename field in save dialog.",
                CreateDiagnostics(stopwatch));
        }

        // Focus the edit field
        await _staThread.ExecuteAsync(() =>
        {
            editField.TrySetFocus();
            return true;
        }, cancellationToken);

        await Task.Delay(50, cancellationToken);

        // Clear existing text and type new path (pywinauto pattern: Ctrl+A then type)
        await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, cancellationToken: cancellationToken);
        await Task.Delay(50, cancellationToken);

        // Normalize path to Windows format (backslashes)
        var normalizedPath = filePath.Replace('/', '\\');
        await _keyboardService.TypeTextAsync(normalizedPath, cancellationToken);
        await Task.Delay(100, cancellationToken);

        // Press Enter to save (equivalent to clicking Save button)
        await _keyboardService.PressKeyAsync("Return", cancellationToken: cancellationToken);
        await Task.Delay(300, cancellationToken);

        // Handle overwrite confirmation if it appears
        await HandleOverwriteConfirmationAsync(cancellationToken);

        return UIAutomationResult.CreateSuccess("save", CreateDiagnostics(stopwatch));
    }

    /// <summary>
    /// Waits for a dialog to close (White Framework pattern: WaitWhileBusy).
    /// </summary>
    private async Task WaitForDialogCloseAsync(UIA.IUIAutomationElement dialog, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + SaveDialogTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stillExists = await _staThread.ExecuteAsync(() =>
            {
                try
                {
                    // Check if element is still valid
                    var name = dialog.CurrentName;
                    var rect = dialog.CurrentBoundingRectangle;
                    return rect.right > rect.left && rect.bottom > rect.top;
                }
                catch
                {
                    // Element became stale = dialog closed
                    return false;
                }
            }, cancellationToken);

            if (!stillExists)
            {
                return;
            }

            await Task.Delay(SaveDialogPollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// Handles the "Confirm Save As" overwrite confirmation dialog if it appears.
    /// Based on pywinauto pattern: check for Yes/Replace button and click it.
    /// </summary>
    private async Task HandleOverwriteConfirmationAsync(CancellationToken cancellationToken)
    {
        // Check for common overwrite confirmation dialogs
        string[] confirmPatterns = ["Confirm Save As", "Replace or Skip Files", "Confirm", "already exists"];
        string[] buttonNames = ["Yes", "Replace", "Confirm", "&Yes"];

        var buttonToClick = await _staThread.ExecuteAsync(() =>
        {
            // Search for confirmation dialogs
            var windowCondition = Uia.CreatePropertyCondition(
                UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window);
            var windows = Uia.RootElement.FindAll(UIA.TreeScope.TreeScope_Children, windowCondition);

            if (windows == null)
            {
                return (UIA.IUIAutomationElement?)null;
            }

            for (int i = 0; i < windows.Length; i++)
            {
                var window = windows.GetElement(i);
                var windowName = window.CurrentName ?? "";

                // Check if window matches any confirmation pattern
                bool isConfirmDialog = false;
                foreach (var pattern in confirmPatterns)
                {
                    if (windowName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isConfirmDialog = true;
                        break;
                    }
                }

                if (!isConfirmDialog)
                {
                    continue;
                }

                // Look for Yes/Replace button
                foreach (var buttonName in buttonNames)
                {
                    var buttonCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.Name, buttonName));
                    var button = window.FindFirst(UIA.TreeScope.TreeScope_Descendants, buttonCondition);
                    if (button != null)
                    {
                        return button;
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
                // Fallback to physical click
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
