using System.Diagnostics;
using System.Runtime.InteropServices;
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

        var prepared = await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.ElementNotFound,
                    $"Element with ID '{elementId}' could not be resolved.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null);
            }

            TryActivateWindowForElement(element, activationHandle);
            if (!element.IsEnabled())
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is disabled and cannot be clicked. " +
                    "Wait for it to become enabled (e.g., after filling required fields) or target a different element.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null);
            }

            return (Failure: (UIAutomationResult?)null, Element: element, Root: GetRootElementForScroll(element));
        }, cancellationToken);

        if (prepared.Failure != null)
        {
            return prepared.Failure;
        }

        var outcome = await ExecuteElementActionAsync(
            prepared.Element!,
            prepared.Root!,
            fallbackClickPoint,
            cancellationToken);
        if (!outcome.Success)
        {
            return UIAutomationResult.CreateFailure(
                "click",
                UIAutomationErrorType.PatternNotSupported,
                outcome.ErrorMessage ?? "The element action could not be completed.",
                CreateDiagnostics(stopwatch));
        }

        return await _staThread.ExecuteAsync(() =>
        {
            var info = outcome.ElementUnavailable
                ? null
                : ConvertToElementInfo(prepared.Element!, prepared.Root!, _coordinateConverter);
            return info is null
                ? UIAutomationResult.CreateSuccessWithHint(
                    "click",
                    "Click succeeded. Element closed or changed its parent window or dialog.",
                    CreateDiagnostics(stopwatch))
                : UIAutomationResult.CreateSuccessCompact("click", [info], CreateDiagnostics(stopwatch));
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

    /// <summary>
    /// Types text into a previously-discovered element addressed by its stable element id
    /// (from ui_find/ui_snapshot), skipping the find step. Useful for reusing a known element
    /// across multiple actions without re-querying.
    /// </summary>
    public async Task<UIAutomationResult> TypeIntoElementAsync(string elementId, string text, bool clearFirst, string? windowHandle, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(elementId);
        var stopwatch = Stopwatch.StartNew();

        // Normalize Windows file paths for consistency with FindAndTypeAsync.
        text = PathNormalizer.NormalizeWindowsPath(text);

        try
        {
            return await PerformTypeAsync(elementId, text, clearFirst, windowHandle, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndTypeError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "type",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Type"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndTypeError(_logger, elementId, ex);
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

            // Actionability gate: a disabled field cannot receive text. Fail fast with guidance.
            if (!element.IsEnabled())
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is disabled and cannot receive text. " +
                    "Wait for it to become enabled or target a different field.",
                    CreateDiagnostics(stopwatch)), ValuePatternSucceeded: false, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
            }

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
            if (staResult.Element == null)
            {
                return staResult.Result!;
            }

            var isPassword = await _staThread.ExecuteAsync(
                () => staResult.Element.CurrentIsPassword != 0,
                cancellationToken);
            if (isPassword)
            {
                // UIA intentionally does not expose password values. The successful provider
                // SetValue call is the strongest observable completion signal available.
                return staResult.Result!;
            }

            var verified = await WaitForElementConditionAsync(
                staResult.Element,
                () => string.Equals(staResult.Element.TryGetValue(), text, StringComparison.Ordinal),
                cancellationToken);
            if (verified.Observed)
            {
                return staResult.Result!;
            }

            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.PatternNotSupported,
                "ValuePattern accepted the text, but the requested value was not observable before the bounded timeout.",
                CreateDiagnostics(stopwatch));
        }

        // Fall back to keyboard input (outside STA thread)
        if (clearFirst)
        {
            await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, 1, cancellationToken);
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
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
                    _ = DeterministicWait.Until(
                        () => element.FindFirst(
                            UIA.TreeScope.TreeScope_Descendants,
                            Uia.CreatePropertyCondition(UIA3PropertyIds.Name, value)) != null,
                        TimeSpan.FromMilliseconds(500),
                        ActionVerificationPollInterval,
                        cancellationToken: cancellationToken);

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
                _windowActivator.ActivateWindowAsync(handle.Value, cancellationToken: CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                _ = DeterministicWait.Until(
                    () => _windowActivator.IsForegroundWindow(handle.Value),
                    TimeSpan.FromMilliseconds(500),
                    ActionVerificationPollInterval);
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
            if (rect.right > rect.left && rect.bottom > rect.top)
            {
                return new Point(
                    rect.left + (rect.right - rect.left) / 2,
                    rect.top + (rect.bottom - rect.top) / 2);
            }

            return element.TryGetClickablePoint(out var clickableX, out var clickableY)
                ? new Point(clickableX, clickableY)
                : null;
        }
        catch
        {
            return null;
        }
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

            _ = await DeterministicWait.UntilAsync(
                () => NativeMethods.GetForegroundWindow() == hwnd,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(25),
                cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return UIAutomationResult.CreateFailure(
                        "save",
                        UIAutomationErrorType.PathError,
                        $"Save failed: directory '{directory}' does not exist.",
                        CreateDiagnostics(stopwatch));
                }
            }

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
                    if (!await WaitForDialogCloseAsync(dialog.Value.element, cancellationToken))
                    {
                        return UIAutomationResult.CreateFailure(
                            "save",
                            UIAutomationErrorType.Timeout,
                            "Save could not be verified because the Save dialog remained open.",
                            CreateDiagnostics(stopwatch));
                    }
                }
                else
                {
                    // No filePath - return hint that dialog is open
                    return UIAutomationResult.CreateSuccessWithHint(
                        "save",
                        "Save dialog opened. Provide filePath to auto-fill it, or use ui_type and ui_click to interact with the dialog manually.",
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

            (UIA.IUIAutomationElement element, string name)? result;
            try
            {
                result = await _staThread.ExecuteAsync(() =>
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
            }
            catch (COMException exception) when (COMExceptionHelper.IsTransientProviderFailure(exception))
            {
                result = null;
            }

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

        _ = await DeterministicWait.UntilAsync(
            async () => await _staThread.ExecuteAsync(
                () =>
                {
                    try
                    {
                        return dialog.CurrentHasKeyboardFocus != 0;
                    }
                    catch (COMException)
                    {
                        return false;
                    }
                },
                cancellationToken),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(25),
            cancellationToken: cancellationToken);

        // The shell dialog can publish its top-level window before the filename control's
        // accessibility provider is stable. Retry the observable control discovery.
        UIA.IUIAutomationElement? editField = null;
        var editFieldFound = await DeterministicWait.UntilAsync(
            async () =>
            {
                editField = await _staThread.ExecuteAsync(
                    () => FindSaveDialogEditField(dialog),
                    cancellationToken);
                return editField != null;
            },
            SaveDialogTimeout,
            SaveDialogPollInterval,
            transientException: exception =>
                exception is COMException comException &&
                COMExceptionHelper.IsTransientProviderFailure(comException),
            cancellationToken: cancellationToken);

        if (!editFieldFound || editField == null)
        {
            return UIAutomationResult.CreateFailure(
                "save",
                UIAutomationErrorType.ElementNotFound,
                "Could not find filename field in save dialog.",
                CreateDiagnostics(stopwatch));
        }

        // Focus the edit field and click it to ensure keyboard input goes here
        int[]? editFieldCenter = await _staThread.ExecuteAsync<int[]?>(() =>
        {
            editField.TrySetFocus();
            var rect = editField.GetBoundingRectangle();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            return [(int)Math.Round(rect.X + (rect.Width / 2)), (int)Math.Round(rect.Y + (rect.Height / 2))];
        }, cancellationToken);

        if (editFieldCenter is { Length: 2 })
        {
            await _mouseService.ClickAsync(editFieldCenter[0], editFieldCenter[1], cancellationToken: cancellationToken);
        }

        _ = await DeterministicWait.UntilAsync(
            async () => await _staThread.ExecuteAsync(
                () =>
                {
                    try
                    {
                        return editField.CurrentHasKeyboardFocus != 0;
                    }
                    catch (COMException)
                    {
                        return false;
                    }
                },
                cancellationToken),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(25),
            cancellationToken: cancellationToken);

        await _keyboardService.ReleaseAllKeysAsync(cancellationToken);

        // Normalize path to Windows format (backslashes)
        var normalizedPath = filePath.Replace('/', '\\');

        // Select all existing text and type the new path.
        // We use keyboard input rather than Value Pattern because the Windows File Dialog
        // only updates its internal path state from keyboard input, not from UIA Value Pattern.
        await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, cancellationToken: cancellationToken);
        _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
        await _keyboardService.TypeTextAsync(normalizedPath, cancellationToken);

        _ = await DeterministicWait.UntilAsync(
            async () => await _staThread.ExecuteAsync(
                () =>
                {
                    var currentValue = editField.TryGetValue();
                    return currentValue != null &&
                        (string.Equals(currentValue, normalizedPath, StringComparison.OrdinalIgnoreCase) ||
                         currentValue.EndsWith(Path.GetFileName(normalizedPath), StringComparison.OrdinalIgnoreCase));
                },
                cancellationToken),
            TimeSpan.FromMilliseconds(750),
            TimeSpan.FromMilliseconds(25),
            transientException: exception => exception is COMException,
            cancellationToken: cancellationToken);

        // Click the Save button directly — more reliable than Enter which can interact
        // with autocomplete dropdowns in the Windows file dialog (FlaUI pattern).
        var saveClicked = await ClickSaveButtonAsync(dialog, cancellationToken);

        if (!saveClicked)
        {
            // Fallback: press Enter
            await _keyboardService.PressKeyAsync("Return", cancellationToken: cancellationToken);
        }

        // Check for error dialogs (e.g., "Path does not exist")
        var errorResult = await HandleSaveErrorDialogAsync(cancellationToken);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Handle overwrite confirmation if it appears
        await HandleOverwriteConfirmationAsync(cancellationToken);

        return UIAutomationResult.CreateSuccess("save", CreateDiagnostics(stopwatch));
    }

    private static UIA.IUIAutomationElement? FindSaveDialogEditField(UIA.IUIAutomationElement dialog)
    {
        // FileNameControlHost is a ComboBox — use its inner Edit so the shell updates
        // its internal path state rather than only changing the displayed text.
        var fileNameHost = dialog.FindFirst(
            UIA.TreeScope.TreeScope_Descendants,
            Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "FileNameControlHost"));
        if (fileNameHost != null)
        {
            var editCondition = Uia.CreatePropertyCondition(
                UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Edit);
            var innerEdit = fileNameHost.FindFirst(UIA.TreeScope.TreeScope_Descendants, editCondition);
            if (innerEdit != null)
            {
                return innerEdit;
            }

            if (fileNameHost.CurrentControlType == UIA3ControlTypeIds.Edit)
            {
                return fileNameHost;
            }
        }

        // Return null (not a blind Edit match) when the File name combo has not realized yet. The
        // caller polls this finder, so returning any early Edit is dangerous:
        //   - AutomationId "1001" on the modern IFileDialog is the address/breadcrumb bar, and a
        //     blind first-Edit match grabs it too. Typing then lands in the wrong control and the
        //     dialog never closes (observed value "Address: C:\...").
        //   - A heavy FindAll(Descendants)+per-element property scan on every poll starves the shared
        //     UIA STA thread, which stalls GetTree in other tests running in parallel.
        // Both standard Save and Open dialogs expose FileNameControlHost, so waiting for it is
        // reliable; keep polling until it appears.
        return null;
    }

    /// <summary>
    /// Finds and clicks the Save button in a Save As dialog.
    /// Returns true if the button was found and clicked, false otherwise.
    /// </summary>
    private async Task<bool> ClickSaveButtonAsync(UIA.IUIAutomationElement dialog, CancellationToken cancellationToken)
    {
        UIA.IUIAutomationElement? saveButton = null;
        var saveButtonFound = await DeterministicWait.UntilAsync(
            async () =>
            {
                saveButton = await _staThread.ExecuteAsync(() =>
                {
                    // Standard Save button names (includes accelerator variants)
                    string[] saveButtonNames = ["Save", "&Save"];
                    foreach (var name in saveButtonNames)
                    {
                        var condition = Uia.CreateAndCondition(
                            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                            Uia.CreatePropertyCondition(UIA3PropertyIds.Name, name));
                        var button = dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);
                        if (button != null)
                        {
                            return button;
                        }
                    }

                    // Fallback: search by AutomationId "1" (common for Save button in file dialogs)
                    var idCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1"));
                    return dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, idCondition);
                }, cancellationToken);
                return saveButton != null;
            },
            SaveDialogTimeout,
            SaveDialogPollInterval,
            transientException: exception =>
                exception is COMException comException &&
                COMExceptionHelper.IsTransientProviderFailure(comException),
            cancellationToken: cancellationToken);

        if (!saveButtonFound || saveButton == null)
        {
            return false;
        }

        var outcome = await ExecuteElementActionAsync(
            saveButton,
            dialog,
            fallbackClickPoint: null,
            cancellationToken);
        return outcome.Success;
    }

    /// <summary>
    /// Waits for a dialog to close (White Framework pattern: WaitWhileBusy).
    /// </summary>
    private async Task<bool> WaitForDialogCloseAsync(UIA.IUIAutomationElement dialog, CancellationToken cancellationToken)
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
                return true;
            }

            await Task.Delay(SaveDialogPollInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Checks for and handles error dialogs that appear during save (e.g., "Path does not exist").
    /// Returns an error result if an error dialog was found and handled, null otherwise.
    /// Polls for error dialogs for the bounded save-dialog timeout to handle timing variations.
    /// Only checks the FOREGROUND window to avoid false positives from unrelated windows.
    /// </summary>
    private async Task<UIAutomationResult?> HandleSaveErrorDialogAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Error dialog text patterns (case-insensitive)
        // Must be specific to actual Windows error dialogs
        string[] errorPatterns = [
            "Path does not exist",
            "could not find the path",
            "cannot find the path",
            "is not valid",
            "access is denied"
        ];

        var deadline = DateTime.UtcNow + SaveDialogTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (UIA.IUIAutomationElement? dialog, UIA.IUIAutomationElement? okButton, string? errorText) errorInfo;
            try
            {
                errorInfo = await _staThread.ExecuteAsync(() =>
                {
                    // CRITICAL: Only check the FOREGROUND window to avoid false positives
                    // from other applications (e.g., VS Code chat showing "does not exist" text)
                    var foregroundHwnd = NativeMethods.GetForegroundWindow();
                    if (foregroundHwnd == IntPtr.Zero)
                    {
                        return (dialog: (UIA.IUIAutomationElement?)null, okButton: (UIA.IUIAutomationElement?)null, errorText: (string?)null);
                    }

                    var window = Uia.ElementFromHandle(foregroundHwnd);
                    if (window == null)
                    {
                        return (dialog: (UIA.IUIAutomationElement?)null, okButton: (UIA.IUIAutomationElement?)null, errorText: (string?)null);
                    }

                    var windowName = window.CurrentName ?? "";

                    // Error dialogs from Save must have "Save" in the title
                    if (!windowName.Contains("Save", StringComparison.OrdinalIgnoreCase))
                    {
                        return (dialog: (UIA.IUIAutomationElement?)null, okButton: (UIA.IUIAutomationElement?)null, errorText: (string?)null);
                    }

                    // Look for error text in the dialog
                    var textCondition = Uia.CreatePropertyCondition(
                        UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Text);
                    var textElements = window.FindAll(UIA.TreeScope.TreeScope_Descendants, textCondition);

                    if (textElements != null)
                    {
                        for (int j = 0; j < textElements.Length; j++)
                        {
                            var textElement = textElements.GetElement(j);
                            var text = textElement.CurrentName ?? "";

                            foreach (var pattern in errorPatterns)
                            {
                                if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Found error - now find the OK button
                                    var buttonCondition = Uia.CreateAndCondition(
                                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                                        Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "OK"));
                                    var okBtn = window.FindFirst(UIA.TreeScope.TreeScope_Descendants, buttonCondition);

                                    return (dialog: (UIA.IUIAutomationElement?)window, okButton: (UIA.IUIAutomationElement?)okBtn, errorText: (string?)text);
                                }
                            }
                        }
                    }

                    return (dialog: (UIA.IUIAutomationElement?)null, okButton: (UIA.IUIAutomationElement?)null, errorText: (string?)null);
                }, cancellationToken);
            }
            catch (COMException exception) when (COMExceptionHelper.IsTransientProviderFailure(exception))
            {
                await Task.Delay(SaveDialogPollInterval, cancellationToken);
                continue;
            }

            if (errorInfo.dialog != null)
            {
                // Found error dialog - click OK to dismiss it
                if (errorInfo.okButton != null)
                {
                    _ = await ExecuteElementActionAsync(
                        errorInfo.okButton,
                        errorInfo.dialog,
                        fallbackClickPoint: null,
                        cancellationToken);
                }

                _ = await WaitForDialogCloseAsync(errorInfo.dialog, cancellationToken);

                // Press Escape to close the Save As dialog
                await _keyboardService.PressKeyAsync("Escape", cancellationToken: cancellationToken);

                // Return error to LLM
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.PathError,
                    $"Save failed: {errorInfo.errorText}. The directory does not exist. Create the directory first or use an existing path.",
                    CreateDiagnostics(stopwatch));
            }

            await Task.Delay(100, cancellationToken);
        }

        return null; // No error dialog found
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
            var root = await _staThread.ExecuteAsync(
                () => GetRootElementForScroll(buttonToClick),
                cancellationToken);
            _ = await ExecuteElementActionAsync(
                buttonToClick,
                root,
                fallbackClickPoint: null,
                cancellationToken);
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
