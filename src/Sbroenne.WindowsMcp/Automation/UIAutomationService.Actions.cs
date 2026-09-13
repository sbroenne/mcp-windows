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
    /// Timeout for waiting for a dialog to close after the save has been committed. This is a
    /// different kind of wait from locating a dialog or a control that is already on screen: the
    /// application still has to write the file and tear the dialog down, which on a contended
    /// desktop (or for a larger file) routinely takes longer than <see cref="SaveDialogTimeout"/>.
    /// Reusing the short discovery budget here reported a spurious "save could not be verified"
    /// failure even though the save had actually succeeded.
    /// </summary>
    private static readonly TimeSpan SaveDialogCloseTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Polling interval for dialog detection retry loop.
    /// </summary>
    private static readonly TimeSpan SaveDialogPollInterval = TimeSpan.FromMilliseconds(100);
    /// <inheritdoc/>
    internal async Task<UIAutomationResult> FindAndClickAsync(ElementQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var actionQuery = query with
            {
                VisibleOnly = query.VisibleOnly ?? true,
                EnabledOnly = query.EnabledOnly ?? true
            };
            var findResult = await FindElementsAsync(actionQuery, cancellationToken);
            if (!findResult.Success || findResult.Items == null || findResult.Items.Length == 0)
            {
                return UIAutomationResult.CreateFailure(
                    "click",
                    findResult.ErrorType ?? UIAutomationErrorType.ElementNotFound,
                    findResult.ErrorMessage ?? "Element not found.",
                    findResult.Diagnostics ?? CreateDiagnostics(stopwatch, actionQuery));
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
                COMExceptionHelper.GetErrorType(ex),
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
                    UIAutomationErrorType.ElementStale,
                    $"Element with ID '{elementId}' is stale and was not retargeted by name.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null);
            }

            var elementWindowHandle = ResolveElementWindowHandle(element);
            if (!IsRequestedWindowHandleCompatible(elementWindowHandle, activationHandle))
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The resolved element no longer belongs to the requested window.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null);
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

            if (element.IsOffscreen())
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "click",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is off-screen and cannot be safely clicked. " +
                    "Refresh the UI state, scroll it into view, or target the active dialog.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null);
            }

            fallbackClickPoint ??= GetVerifiedCachedClickPoint(element);
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
                CreateActionDiagnostics(
                    stopwatch,
                    prepared.Element,
                    outcome.ActionPath ?? "unverified"));
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
                    CreateActionDiagnostics(stopwatch, prepared.Element, outcome.ActionPath ?? "unknown"))
                : UIAutomationResult.CreateSuccessCompact(
                    "click",
                    [info],
                    CreateActionDiagnostics(stopwatch, prepared.Element, outcome.ActionPath ?? "unknown"));
        }, cancellationToken);
    }

    /// <summary>
    /// Types into a previously discovered element using auto, value, or keyboard input.
    /// </summary>
    public async Task<UIAutomationResult> TypeIntoElementAsync(
        string elementId,
        string text,
        bool clearFirst,
        string? windowHandle,
        string inputMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(elementId);
        var stopwatch = Stopwatch.StartNew();

        if (!TryNormalizeInputMode(inputMode, out var normalizedInputMode))
        {
            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.InvalidParameter,
                $"Invalid inputMode '{inputMode}'. Valid values: auto, keyboard, value.",
                CreateDiagnostics(stopwatch));
        }

        // Preserve path normalization for file-path inputs.
        text = PathNormalizer.NormalizeWindowsPath(text);

        try
        {
            return await PerformTypeAsync(
                elementId,
                text,
                clearFirst,
                normalizedInputMode,
                windowHandle,
                stopwatch,
                cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndTypeError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "type",
                COMExceptionHelper.GetErrorType(ex),
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

    private async Task<UIAutomationResult> PerformTypeAsync(
        string elementId,
        string text,
        bool clearFirst,
        string inputMode,
        string? windowHandle,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var staResult = await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.ElementStale,
                    $"Element with ID '{elementId}' is stale and was not retargeted by name.",
                    CreateDiagnostics(stopwatch)), UseKeyboard: false, IsPassword: false, InitialValue: (string?)null, TargetWindowHandle: IntPtr.Zero, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
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
                        CreateDiagnostics(stopwatch)), UseKeyboard: false, IsPassword: false, InitialValue: (string?)null, TargetWindowHandle: IntPtr.Zero, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                }

                activationHandle = parsedHandle;
            }

            var elementWindowHandle = ResolveElementWindowHandle(element);
            if (!IsRequestedWindowHandleCompatible(elementWindowHandle, activationHandle))
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The resolved element no longer belongs to the requested window.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), UseKeyboard: false, IsPassword: false, InitialValue: (string?)null, TargetWindowHandle: IntPtr.Zero, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
            }

            if (!element.IsEnabled())
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is disabled and cannot receive text. " +
                    "Wait for it to become enabled or target a different field.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), UseKeyboard: false, IsPassword: false, InitialValue: (string?)null, TargetWindowHandle: IntPtr.Zero, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
            }

            if (element.IsOffscreen())
            {
                return (Success: false, Result: UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is off-screen and cannot safely receive input. " +
                    "Refresh the UI state, scroll it into view, or target the active dialog.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), UseKeyboard: false, IsPassword: false, InitialValue: (string?)null, TargetWindowHandle: IntPtr.Zero, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
            }

            var rootElement = GetRootElementForScroll(element);
            var isPassword = element.CurrentIsPassword != 0;
            var initialValue = ReadEditableValue(element);
            var detectedFramework = DetectFramework(rootElement);
            var useKeyboard = inputMode == "keyboard" ||
                (inputMode == "auto" &&
                 string.Equals(detectedFramework, "Chromium/Electron", StringComparison.Ordinal));

            if (!useKeyboard)
            {
                if (clearFirst && !element.TrySetValue(""))
                {
                    useKeyboard = inputMode == "auto";
                }

                if (!useKeyboard && element.TrySetValue(text))
                {
                    var info = ConvertToElementInfo(element, rootElement, _coordinateConverter);
                    if (info == null)
                    {
                        return (Success: true, Result: UIAutomationResult.CreateSuccessWithHint(
                            "type",
                            "Type succeeded. Element closed its parent window.",
                            CreateActionDiagnostics(stopwatch, element, "value_pattern")), UseKeyboard: false, IsPassword: isPassword, InitialValue: initialValue, TargetWindowHandle: elementWindowHandle, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                    }

                    return (Success: true, Result: UIAutomationResult.CreateSuccessCompact(
                        "type",
                        [info],
                        CreateActionDiagnostics(stopwatch, element, "value_pattern")), UseKeyboard: false, IsPassword: isPassword, InitialValue: initialValue, TargetWindowHandle: elementWindowHandle, Element: element, RootElement: rootElement);
                }

                if (!useKeyboard && inputMode == "auto")
                {
                    useKeyboard = true;
                }

                if (!useKeyboard)
                {
                    return (Success: false, Result: UIAutomationResult.CreateFailure(
                        "type",
                        UIAutomationErrorType.PatternNotSupported,
                        "The element did not accept ValuePattern input. Use inputMode='keyboard' to emit normal focus and keyboard events.",
                        CreateActionDiagnostics(stopwatch, element, "value_pattern")), UseKeyboard: false, IsPassword: isPassword, InitialValue: initialValue, TargetWindowHandle: elementWindowHandle, Element: (UIA.IUIAutomationElement?)null, RootElement: (UIA.IUIAutomationElement?)null);
                }
            }

            // Request foreground activation, but use the target element's actual keyboard-focus
            // state below as the final safety gate. Some UI providers can focus correctly even when
            // GetForegroundWindow is unavailable to the automation process.
            _ = ActivateWindowForElement(element, activationHandle);
            element.TrySetFocus();
            return (Success: true, Result: (UIAutomationResult?)null, UseKeyboard: true, IsPassword: isPassword, InitialValue: initialValue, TargetWindowHandle: elementWindowHandle, Element: element, RootElement: rootElement);
        }, cancellationToken);

        if (!staResult.Success)
        {
            return staResult.Result!;
        }

        if (!staResult.UseKeyboard)
        {
            if (staResult.Element == null)
            {
                return staResult.Result!;
            }

            if (staResult.IsPassword)
            {
                return staResult.Result!;
            }

            var verified = await WaitForElementConditionAsync(
                staResult.Element,
                () => string.Equals(ReadEditableValue(staResult.Element), text, StringComparison.Ordinal),
                cancellationToken);
            if (verified.Observed)
            {
                return staResult.Result!;
            }

            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.PatternNotSupported,
                "ValuePattern accepted the text, but the requested value was not observable before the bounded timeout.",
                CreateActionDiagnostics(stopwatch, staResult.Element, "value_pattern"));
        }

        var expectedWindowHandle = staResult.TargetWindowHandle;
        if (!IsExpectedForegroundWindow(expectedWindowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.WrongTargetWindow,
                "The target window is not foreground, so no keyboard input was sent.",
                CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
        }

        var focusReady = await WaitForElementConditionAsync(
            staResult.Element!,
            () => staResult.Element!.CurrentHasKeyboardFocus != 0,
            cancellationToken,
            TimeSpan.FromMilliseconds(750));
        if (!focusReady.Observed)
        {
            var clickPoint = await _staThread.ExecuteAsync(
                () => GetPhysicalClickPoint(staResult.Element!),
                cancellationToken);
            if (clickPoint.HasValue)
            {
                if (!IsExpectedForegroundWindow(expectedWindowHandle))
                {
                    return UIAutomationResult.CreateFailure(
                        "type",
                        UIAutomationErrorType.WrongTargetWindow,
                        "The target window lost foreground ownership before the focus click.",
                        CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
                }

                var click = await _mouseService.ClickAsync(
                    clickPoint.Value.X,
                    clickPoint.Value.Y,
                    ModifierKey.None,
                    expectedWindowHandle,
                    cancellationToken: cancellationToken);
                if (!click.Success)
                {
                    return UIAutomationResult.CreateFailure(
                        "type",
                        UIAutomationErrorType.WrongTargetWindow,
                        "The text field could not be focused before keyboard input.",
                        CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
                }
            }

            focusReady = await WaitForElementConditionAsync(
                staResult.Element!,
                () => staResult.Element!.CurrentHasKeyboardFocus != 0,
                cancellationToken,
                TimeSpan.FromMilliseconds(750));
            if (!focusReady.Observed)
            {
                return UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The text field did not obtain keyboard focus, so no text was sent.",
                    CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
            }
        }

        if (clearFirst)
        {
            if (!IsExpectedForegroundWindow(expectedWindowHandle))
            {
                return UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The target window lost foreground ownership before clearing the field.",
                    CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
            }

            await _keyboardService.PressKeyAsync(
                "a",
                ModifierKey.Ctrl,
                1,
                expectedWindowHandle,
                cancellationToken);
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
            if (!IsExpectedForegroundWindow(expectedWindowHandle))
            {
                return UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The target window lost foreground ownership while clearing the field.",
                    CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
            }

            await _keyboardService.PressKeyAsync(
                "Delete",
                ModifierKey.None,
                1,
                expectedWindowHandle,
                cancellationToken);
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
        }

        if (!IsExpectedForegroundWindow(expectedWindowHandle))
        {
            return UIAutomationResult.CreateFailure(
                "type",
                UIAutomationErrorType.WrongTargetWindow,
                "The target window lost foreground ownership before text input.",
                CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
        }

        var keyboardResult = await _keyboardService.TypeTextAsync(
            text,
            expectedWindowHandle,
            cancellationToken);
        if (!keyboardResult.Success)
        {
            return UIAutomationResult.CreateFailure(
                "type",
                keyboardResult.ErrorCode == KeyboardControlErrorCode.WrongTargetWindow
                    ? UIAutomationErrorType.WrongTargetWindow
                    : UIAutomationErrorType.InternalError,
                keyboardResult.Error ?? "Keyboard input failed.",
                CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
        }

        if (!staResult.IsPassword)
        {
            var observed = await WaitForElementConditionAsync(
                staResult.Element!,
                () =>
                {
                    var current = ReadEditableValue(staResult.Element!);
                    return clearFirst
                        ? string.Equals(current, text, StringComparison.Ordinal)
                        : !string.Equals(current, staResult.InitialValue, StringComparison.Ordinal) &&
                          (current?.Contains(text, StringComparison.Ordinal) == true);
                },
                cancellationToken);
            if (!observed.Observed)
            {
                return UIAutomationResult.CreateFailure(
                    "type",
                    UIAutomationErrorType.PatternNotSupported,
                    "Keyboard input was sent, but the requested text change was not observable. " +
                    "Refresh the UI state and verify the field still has focus.",
                    CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
            }
        }

        return await _staThread.ExecuteAsync(() =>
        {
            var info = ConvertToElementInfo(staResult.Element!, staResult.RootElement!, _coordinateConverter);
            if (info == null)
            {
                return UIAutomationResult.CreateSuccessWithHint(
                    "type",
                    "Type succeeded. Element closed its parent window.",
                    CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
            }
            return UIAutomationResult.CreateSuccessCompact(
                "type",
                [info],
                CreateActionDiagnostics(stopwatch, staResult.Element, "keyboard"));
        }, cancellationToken);
    }

    private static bool TryNormalizeInputMode(string? inputMode, out string normalized)
    {
        normalized = string.IsNullOrWhiteSpace(inputMode)
            ? "auto"
            : inputMode.Trim().ToLowerInvariant();
        return normalized is "auto" or "keyboard" or "value";
    }

    private static string? ReadEditableValue(UIA.IUIAutomationElement element) =>
        element.TryGetValue() ?? element.GetText();

    internal Task<UIAutomationResult?> ValidateElementTargetAsync(
        string elementId, string? windowHandle, CancellationToken cancellationToken) =>
        _staThread.ExecuteAsync<UIAutomationResult?>(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element is null)
            {
                return UIAutomationResult.CreateFailure("wait", UIAutomationErrorType.ElementStale,
                    "The observed element is stale. Rediscover it before waiting.");
            }
            if (windowHandle is not null &&
                (!WindowHandleParser.TryParse(windowHandle, out var handle) ||
                !IsRequestedWindowHandleCompatible(ResolveElementWindowHandle(element), handle)))
            {
                return UIAutomationResult.CreateFailure("wait", UIAutomationErrorType.WrongTargetWindow,
                    "The observed element does not belong to the requested window.");
            }
            return null;
        }, cancellationToken);

    private static Point? GetVerifiedCachedClickPoint(UIA.IUIAutomationElement element)
    {
        // Some WinForms TabPage providers have valid cached bounds but empty current bounds.
        // Use them only after resolving the original live identity and hit-testing that identity.
        try
        {
            var bounds = element.CachedBoundingRectangle;
            if (bounds.right <= bounds.left || bounds.bottom <= bounds.top)
            {
                return null;
            }
            var point = new Point((bounds.left + bounds.right) / 2, (bounds.top + bounds.bottom) / 2);
            var hit = UIA3Automation.Instance.Automation.ElementFromPoint(
                new UIA.tagPOINT { x = point.X, y = point.Y });
            for (var current = hit; current is not null; current = current.GetParent())
            {
                if (current.IsSameElement(element))
                {
                    return point;
                }
            }
        }
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementFailure(ex))
        {
            // Cache-only properties can report E_INVALIDARG as ArgumentException.
        }
        return null;
    }

    /// <summary>Select an option within the exact previously observed control.</summary>
    public async Task<UIAutomationResult> SelectElementAsync(
        string elementId, string value, string? windowHandle, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await PerformSelectAsync(elementId, value, windowHandle, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure("select", COMExceptionHelper.GetErrorType(ex),
                COMExceptionHelper.GetErrorMessage(ex, "Select"), CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return UIAutomationResult.CreateFailure("select", UIAutomationErrorType.InternalError,
                $"Select failed: {ex.Message}", CreateDiagnostics(stopwatch));
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
                    UIAutomationErrorType.ElementStale,
                    $"Element with ID '{elementId}' is stale. Refresh UI state before selecting.",
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

            var elementWindowHandle = ResolveElementWindowHandle(element);
            if (!IsRequestedWindowHandleCompatible(elementWindowHandle, activationHandle))
            {
                return UIAutomationResult.CreateFailure(
                    "select",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The resolved element does not belong to the requested window or one of its owned dialogs.",
                    CreateActionDiagnostics(stopwatch, element, "selection_pattern"));
            }

            if (!element.IsEnabled() || element.CurrentIsOffscreen != 0)
            {
                return UIAutomationResult.CreateFailure(
                    "select",
                    UIAutomationErrorType.ElementStale,
                    "The resolved element is no longer visible and enabled. Refresh UI state before selecting.",
                    CreateActionDiagnostics(stopwatch, element, "selection_pattern"));
            }

            // Selection patterns are semantic UIA actions, so foreground activation is best effort.
            // Only physical keyboard or mouse injection must require foreground confirmation.
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

    private void TryActivateWindowForElement(UIA.IUIAutomationElement element, nint? windowHandle) =>
        _ = ActivateWindowForElement(element, windowHandle);

    private static bool IsRequestedWindowHandleCompatible(nint elementWindowHandle, nint? requestedWindowHandle)
    {
        if (!requestedWindowHandle.HasValue || requestedWindowHandle.Value == IntPtr.Zero)
        {
            return true;
        }

        if (elementWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (requestedWindowHandle.Value == elementWindowHandle)
        {
            return true;
        }

        var elementRoot = NativeMethods.GetAncestor(elementWindowHandle, NativeConstants.GA_ROOT);
        var requestedRoot = NativeMethods.GetAncestor(requestedWindowHandle.Value, NativeConstants.GA_ROOT);
        if (elementRoot != IntPtr.Zero && requestedRoot != IntPtr.Zero)
        {
            if (elementRoot == requestedRoot)
            {
                return true;
            }

            var elementRootOwner = NativeMethods.GetAncestor(
                elementWindowHandle,
                NativeConstants.GA_ROOTOWNER);
            var requestedRootOwner = NativeMethods.GetAncestor(
                requestedWindowHandle.Value,
                NativeConstants.GA_ROOTOWNER);
            return elementRootOwner != IntPtr.Zero &&
                requestedRootOwner != IntPtr.Zero &&
                elementRootOwner == requestedRootOwner;
        }

        return false;
    }

    private static nint ResolveElementWindowHandle(UIA.IUIAutomationElement element)
    {
        var current = element;
        var walker = Uia.ControlViewWalker;
        var visited = new HashSet<UIA.IUIAutomationElement>();

        while (current != null)
        {
            try
            {
                if (!visited.Add(current))
                {
                    break;
                }

                var hwnd = TryGetNativeWindowHandle(current);
                if (hwnd != IntPtr.Zero)
                {
                    return NormalizeTopLevelWindowHandle(hwnd);
                }

                current = walker.GetParentElement(current);
            }
            catch
            {
                break;
            }
        }

        try
        {
            var root = GetRootElementForScroll(element);
            var rootHandle = TryGetNativeWindowHandle(root);
            if (rootHandle != IntPtr.Zero)
            {
                return NormalizeTopLevelWindowHandle(rootHandle);
            }
        }
        catch
        {
            // Fall through to the screen-point fallback below.
        }

        try
        {
            var clickablePoint = GetPhysicalClickPoint(element);
            if (!clickablePoint.HasValue)
            {
                return IntPtr.Zero;
            }

            var point = new global::Sbroenne.WindowsMcp.Native.POINT(
                (int)Math.Round((double)clickablePoint.Value.X, MidpointRounding.AwayFromZero),
                (int)Math.Round((double)clickablePoint.Value.Y, MidpointRounding.AwayFromZero));
            var hwnd = NativeMethods.WindowFromPoint(point);
            if (hwnd == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var rootHwnd = NativeMethods.GetAncestor(hwnd, NativeConstants.GA_ROOT);
            return rootHwnd != IntPtr.Zero ? rootHwnd : hwnd;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static bool TryParseElementWindowHandle(string elementId, out nint windowHandle) =>
        ElementIdGenerator.TryResolveWindowHandle(elementId, out windowHandle);

    private static nint NormalizeTopLevelWindowHandle(nint windowHandle)
    {
        var root = NativeMethods.GetAncestor(windowHandle, NativeConstants.GA_ROOT);
        return root != IntPtr.Zero ? root : windowHandle;
    }

    private static bool IsExpectedForegroundWindow(nint expectedWindowHandle)
    {
        if (expectedWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        return foreground != IntPtr.Zero &&
            NormalizeTopLevelWindowHandle(foreground) ==
            NormalizeTopLevelWindowHandle(expectedWindowHandle);
    }

    private static nint TryGetNativeWindowHandle(UIA.IUIAutomationElement? element)
    {
        if (element == null)
        {
            return IntPtr.Zero;
        }

        try
        {
            var hwnd = element.CurrentNativeWindowHandle;
            if (hwnd != 0)
            {
                return new IntPtr(hwnd);
            }
        }
        catch
        {
            // Ignore and fall back below.
        }

        try
        {
            var hwnd = element.CachedNativeWindowHandle;
            if (hwnd != 0)
            {
                return new IntPtr(hwnd);
            }
        }
        catch
        {
            // Ignore and fall back below.
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Activates the window that hosts <paramref name="element"/> and reports whether it is confirmed to be the
    /// foreground window afterwards. Semantic actions can ignore the result; anything that injects physical input
    /// must not, because a denied activation sends that input to whichever window is in front instead.
    /// </summary>
    private bool ActivateWindowForElement(UIA.IUIAutomationElement element, nint? windowHandle)
    {
        try
        {
            if (_windowActivator == null)
            {
                return false;
            }

            var elementWindowHandle = ResolveElementWindowHandle(element);
            if (windowHandle.HasValue && windowHandle.Value != IntPtr.Zero)
            {
                if (elementWindowHandle != IntPtr.Zero && !IsRequestedWindowHandleCompatible(elementWindowHandle, windowHandle))
                {
                    return false;
                }

                var handleToActivate = elementWindowHandle != IntPtr.Zero
                    ? elementWindowHandle
                    : windowHandle.Value;
                _windowActivator.ActivateWindowAsync(handleToActivate, cancellationToken: CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                return DeterministicWait.Until(
                    () => _windowActivator.IsForegroundWindow(handleToActivate),
                    TimeSpan.FromMilliseconds(500),
                    ActionVerificationPollInterval);
            }

            var handle = elementWindowHandle == IntPtr.Zero
                ? TryGetNativeWindowHandle(GetRootElementForScroll(element))
                : elementWindowHandle;
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            _windowActivator.ActivateWindowAsync(handle, cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            return DeterministicWait.Until(
                () => _windowActivator.IsForegroundWindow(handle),
                TimeSpan.FromMilliseconds(500),
                ActionVerificationPollInterval);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Click point for actions that are always physical: asks UIA for a visible clickable point first (so a
    /// partially covered element is not hit through the overlay) and only falls back to the bounding-rect center.
    /// </summary>
    private static Point? GetPhysicalClickPoint(UIA.IUIAutomationElement element)
    {
        try
        {
            if (element.TryGetClickablePoint(out var clickableX, out var clickableY))
            {
                return new Point(clickableX, clickableY);
            }

            var rect = element.CurrentBoundingRectangle;
            return rect.right > rect.left && rect.bottom > rect.top
                ? new Point(rect.left + (rect.right - rect.left) / 2, rect.top + (rect.bottom - rect.top) / 2)
                : null;
        }
        catch
        {
            return null;
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
                COMExceptionHelper.GetErrorType(ex),
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

    /// <summary>
    /// Double-clicks an element addressed by its stable element id.
    /// </summary>
    /// <param name="elementId">The element id from a prior find/snapshot.</param>
    /// <param name="windowHandle">Optional window handle to activate before clicking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the double-click.</returns>
    /// <remarks>
    /// UI Automation has no double-click pattern, so this is always a physical double-click at the element's
    /// clickable point - unlike <see cref="ClickElementAsync"/>, which prefers semantic patterns like Invoke.
    /// </remarks>
    public async Task<UIAutomationResult> DoubleClickElementAsync(string elementId, string? windowHandle, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await PerformDoubleClickAsync(elementId, windowHandle, fallbackClickPoint: null, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFindAndClickError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "double_click",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Double-click"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFindAndClickError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "double_click",
                UIAutomationErrorType.InternalError,
                $"Double-click failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private async Task<UIAutomationResult> PerformDoubleClickAsync(
        string elementId,
        string? windowHandle,
        Point? fallbackClickPoint,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        nint? activationHandle = null;
        if (!string.IsNullOrWhiteSpace(windowHandle))
        {
            if (!WindowHandleParser.TryParse(windowHandle, out var parsedHandle))
            {
                return UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.InvalidParameter,
                    $"Invalid windowHandle '{windowHandle}'. Expected decimal string from window_management(handle).",
                    CreateDiagnostics(stopwatch));
            }

            activationHandle = parsedHandle;
        }
        else if (TryParseElementWindowHandle(elementId, out var parsedElementHandle))
        {
            activationHandle = parsedElementHandle;
        }

        var prepared = await _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element == null)
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.ElementStale,
                    $"Element with ID '{elementId}' is stale. Refresh UI state before double-clicking.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null, Point: (Point?)null, WindowHandle: IntPtr.Zero, Initial: default(ElementActionState));
            }

            var elementWindowHandle = ResolveElementWindowHandle(element);
            if (!IsRequestedWindowHandleCompatible(elementWindowHandle, activationHandle))
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.WrongTargetWindow,
                    "The resolved element no longer belongs to the requested window.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null, Point: (Point?)null, WindowHandle: IntPtr.Zero, Initial: default(ElementActionState));
            }

            // A double-click is always physical input, so a window that could not be brought to the foreground
            // must fail here rather than receive nothing while another window receives two clicks.
            if (!ActivateWindowForElement(element, activationHandle))
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.WindowNotFound,
                    "The element's window could not be activated and confirmed as the foreground window, so the double-click was not sent. " +
                    "Activate the window first with window_management(action='activate') and retry.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null, Point: (Point?)null, WindowHandle: IntPtr.Zero, Initial: default(ElementActionState));
            }

            if (!element.IsEnabled())
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is disabled and cannot be double-clicked. " +
                    "Wait for it to become enabled (e.g., after filling required fields) or target a different element.",
                    CreateDiagnostics(stopwatch)), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null, Point: (Point?)null, WindowHandle: IntPtr.Zero, Initial: default(ElementActionState));
            }

            if (element.IsOffscreen())
            {
                return (Failure: UIAutomationResult.CreateFailure(
                    "double_click",
                    UIAutomationErrorType.InvalidParameter,
                    $"Element with ID '{elementId}' is off-screen and cannot be safely double-clicked. " +
                    "Refresh the UI state, scroll it into view, or target the active dialog.",
                    CreateActionDiagnostics(stopwatch, element, "target_validation")), Element: (UIA.IUIAutomationElement?)null, Root: (UIA.IUIAutomationElement?)null, Point: (Point?)null, WindowHandle: IntPtr.Zero, Initial: default(ElementActionState));
            }

            var root = GetRootElementForScroll(element);
            var initial = new ElementActionState(
                element.GetControlTypeId(),
                GetToggleStateValue(element),
                GetSelectionState(element),
                GetElementState(element),
                GetObservableFingerprint(root));

            return (Failure: (UIAutomationResult?)null, Element: element, Root: root, Point: GetPhysicalClickPoint(element) ?? GetVerifiedCachedClickPoint(element) ?? fallbackClickPoint, WindowHandle: elementWindowHandle, Initial: initial);
        }, cancellationToken);

        if (prepared.Failure != null)
        {
            return prepared.Failure;
        }

        if (!prepared.Point.HasValue)
        {
            return UIAutomationResult.CreateFailure(
                "double_click",
                UIAutomationErrorType.PatternNotSupported,
                $"Element with ID '{elementId}' has no clickable point, so it cannot be double-clicked.",
                CreateDiagnostics(stopwatch));
        }

        var clickResult = await _mouseService.DoubleClickAsync(
            prepared.Point.Value.X,
            prepared.Point.Value.Y,
            ModifierKey.None,
            prepared.WindowHandle,
            cancellationToken);
        if (!clickResult.Success)
        {
            return UIAutomationResult.CreateFailure(
                "double_click",
                UIAutomationErrorType.InternalError,
                clickResult.Error ?? "The double-click could not be completed.",
                CreateDiagnostics(stopwatch));
        }

        // SendInput only queues the events. Wait until the target has visibly processed them so the next batch
        // step or a trailing snapshot does not observe the pre-double-click UI. A stale element is an observation
        // too: a double-click commonly opens or closes the thing it targeted.
        var clicked = prepared.Element!;
        var before = prepared.Initial;
        var outcome = await WaitForElementConditionAsync(
            clicked,
            () =>
                HasObservableStateChanged(clicked, before.ToggleState, before.IsSelected) ||
                GetElementState(clicked) != before.ElementState ||
                GetObservableFingerprint(prepared.Root!) != before.RootFingerprint,
            cancellationToken);

        return await _staThread.ExecuteAsync(() =>
        {
            // A double-click commonly opens/closes the element's window, so a stale element here is success.
            UIElementInfo? info;
            try
            {
                info = ConvertToElementInfo(prepared.Element!, GetRootElementForScroll(prepared.Element!), _coordinateConverter);
            }
            catch (COMException)
            {
                info = null;
            }

            if (info is null)
            {
                return UIAutomationResult.CreateSuccessWithHint(
                    "double_click",
                    "Double-click succeeded. Element closed or changed its parent window or dialog.",
                    CreateDiagnostics(stopwatch));
            }

            // Unlike a single click, the effect of a double-click frequently lands in a different top-level
            // window (an opened file or dialog), so an unchanged source tree is a hint, not a failure.
            return outcome.Observed
                ? UIAutomationResult.CreateSuccessCompact("double_click", [info], CreateDiagnostics(stopwatch))
                : UIAutomationResult.CreateSuccessWithHint(
                    "double_click",
                    "Double-click was delivered, but no change was observed in this window within the verification window. " +
                    "If it should have opened something, look for a new window with window_management(action='list').",
                    CreateDiagnostics(stopwatch));
        }, cancellationToken);
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
                COMExceptionHelper.GetErrorType(ex),
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
    /// 5. Wait for dialog to close
    /// 6. When a path was supplied, wait for an observable file creation or change before success
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

            var foregroundReady = await DeterministicWait.UntilAsync(
                () => NativeMethods.GetForegroundWindow() == hwnd,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(25),
                cancellationToken: cancellationToken);
            if (!foregroundReady)
            {
                return UIAutomationResult.CreateFailure(
                    "save", UIAutomationErrorType.WrongTargetWindow,
                    "The target window did not become foreground; no save shortcut was sent.",
                    CreateDiagnostics(stopwatch));
            }

            SaveFileObservation? fileBeforeSave = null;
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

                fileBeforeSave = SaveFileObservation.Read(filePath);
            }

            // Step 2: Send Ctrl+S (universal save - pywinauto/FlaUI pattern)
            var shortcut = await _keyboardService.PressKeyAsync("s", ModifierKey.Ctrl, 1, hwnd, cancellationToken);
            if (!shortcut.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "save", UIAutomationErrorType.InternalError,
                    $"Could not send the save shortcut: {shortcut.Error}",
                    CreateDiagnostics(stopwatch));
            }

            // Step 3: Wait for Save dialog using retry loop (FlaUI Retry.WhileEmpty pattern)
            var dialogObservations = new HashSet<string>();
            var dialog = await WaitForSaveDialogAsync(hwnd, dialogObservations, cancellationToken);

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

            if (!string.IsNullOrWhiteSpace(filePath) &&
                !await DeterministicWait.UntilAsync(
                    () => SaveFileObservation.HasChanged(filePath, fileBeforeSave),
                    SaveDialogCloseTimeout,
                    SaveDialogPollInterval,
                    cancellationToken: cancellationToken))
            {
                return UIAutomationResult.CreateFailure(
                    "save", UIAutomationErrorType.Timeout,
                    "No file creation or change was observed at the requested path. " +
                    (dialog.HasValue
                        ? "A Save dialog was confirmed and closed. "
                        : $"No ready Save dialog was observed. Discovery: {string.Join("; ", dialogObservations)}. ") +
                    "The save shortcut was sent, but its outcome could not be verified.",
                    CreateDiagnostics(stopwatch));
            }

            return UIAutomationResult.CreateSuccess("save", CreateDiagnostics(stopwatch));
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure(
                "save",
                COMExceptionHelper.GetErrorType(ex),
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
        var activated = _windowActivator is not null &&
            await _windowActivator.ActivateWindowAsync(
                hwnd,
                cancellationToken: cancellationToken);

        var focused = await _staThread.ExecuteAsync(() =>
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

        return activated || focused;
    }

    /// <summary>
    /// Waits for a Save dialog to appear using FlaUI-style retry loop.
    /// Returns the dialog element and its name, or null if no dialog appeared.
    /// </summary>
    private async Task<(UIA.IUIAutomationElement element, string name)?> WaitForSaveDialogAsync(
        nint parentHwnd, HashSet<string> observations, CancellationToken cancellationToken)
    {
        // Common save dialog title patterns (case-insensitive matching)
        string[] dialogPatterns = ["Save As", "Save as", "Save this file", "Save"];

        // Selection now includes filename-field readiness, using the former discovery and field budgets.
        var deadline = DateTime.UtcNow + SaveDialogTimeout + SaveDialogTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (UIA.IUIAutomationElement element, string name)? result;
            try
            {
                result = await _staThread.ExecuteAsync(() =>
                {
                    // Owned dialogs can be UIA children without reporting IsModal. Enumerate native
                    // top-level windows so discovery does not depend on that provider-specific layout.
                    var handles = new List<nint>();
                    if (!NativeMethods.EnumWindows((candidate, _) =>
                    {
                        if (candidate != parentHwnd &&
                            NativeMethods.IsWindowVisible(candidate) &&
                            IsSaveDialogWindow(candidate, parentHwnd))
                        {
                            handles.Add(candidate);
                        }

                        return true;
                    }, nint.Zero))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }

                    if (handles.Count == 0 && observations.Count < 8)
                    {
                        observations.Add("No visible owned windows");
                    }

                    foreach (var handle in handles)
                    {
                        try
                        {
                            var dialog = Uia.ElementFromHandle(handle);
                            var name = dialog?.CurrentName ?? "";
                            if (dialog == null ||
                                !dialogPatterns.Any(pattern => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            if (FindSaveDialogEditField(dialog) is not null)
                            {
                                return (element: dialog, name: name);
                            }

                            if (observations.Count < 8)
                            {
                                observations.Add($"Window {handle} '{name}': filename field unavailable");
                            }
                        }
                        catch (COMException exception) when (COMExceptionHelper.IsTransientProviderFailure(exception))
                        {
                            if (observations.Count < 8)
                            {
                                observations.Add($"Window {handle}: provider unavailable (0x{exception.HResult:X8})");
                            }
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

        var dialogHandle = await _staThread.ExecuteAsync(
            () => ResolveElementWindowHandle(dialog), cancellationToken);
        if (dialogHandle == nint.Zero)
        {
            return UIAutomationResult.CreateFailure(
                "save", UIAutomationErrorType.WindowNotFound,
                "The Save dialog has no valid window handle; no filename input was sent.",
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
            var focusClick = await _mouseService.ClickAsync(
                editFieldCenter[0], editFieldCenter[1], ModifierKey.None, dialogHandle,
                cancellationToken: cancellationToken);
            if (!focusClick.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "save", UIAutomationErrorType.WrongTargetWindow,
                    $"Could not focus the filename field: {focusClick.Error}",
                    CreateDiagnostics(stopwatch));
            }
        }

        var filenameFocused = await DeterministicWait.UntilAsync(
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
        if (!filenameFocused)
        {
            return UIAutomationResult.CreateFailure(
                "save", UIAutomationErrorType.WrongTargetWindow,
                "The filename field did not obtain keyboard focus; no path was typed.",
                CreateDiagnostics(stopwatch));
        }

        await _keyboardService.ReleaseAllKeysAsync(cancellationToken);

        // Normalize path to Windows format (backslashes)
        var normalizedPath = filePath.Replace('/', '\\');

        // Select all existing text and type the new path.
        // We use keyboard input rather than Value Pattern because the Windows File Dialog
        // only updates its internal path state from keyboard input, not from UIA Value Pattern.
        var selectAll = await _keyboardService.PressKeyAsync(
            "a", ModifierKey.Ctrl, 1, dialogHandle, cancellationToken);
        if (!selectAll.Success)
        {
            return UIAutomationResult.CreateFailure(
                "save", UIAutomationErrorType.InternalError,
                $"Could not select the existing filename: {selectAll.Error}",
                CreateDiagnostics(stopwatch));
        }
        _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
        var typed = await _keyboardService.TypeTextAsync(normalizedPath, dialogHandle, cancellationToken);
        if (!typed.Success)
        {
            return UIAutomationResult.CreateFailure(
                "save", UIAutomationErrorType.InternalError,
                $"Could not type the requested filename: {typed.Error}",
                CreateDiagnostics(stopwatch));
        }

        var filenameObserved = await DeterministicWait.UntilAsync(
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
        if (!filenameObserved)
        {
            return UIAutomationResult.CreateFailure(
                "save", UIAutomationErrorType.Timeout,
                "The filename field did not contain the requested path; Save was not pressed.",
                CreateDiagnostics(stopwatch));
        }

        // Click the Save button directly — more reliable than Enter which can interact
        // with autocomplete dropdowns in the Windows file dialog (FlaUI pattern).
        var saveClicked = await ClickSaveButtonAsync(dialog, cancellationToken);

        if (!saveClicked)
        {
            // Fallback: press Enter
            var confirm = await _keyboardService.PressKeyAsync(
                "Return", ModifierKey.None, 1, dialogHandle, cancellationToken);
            if (!confirm.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "save", UIAutomationErrorType.InternalError,
                    $"Could not confirm the Save dialog: {confirm.Error}",
                    CreateDiagnostics(stopwatch));
            }
        }

        // Check for error dialogs (e.g., "Path does not exist")
        var errorResult = await HandleSaveErrorDialogAsync(dialogHandle, cancellationToken);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Handle overwrite confirmation if it appears
        await HandleOverwriteConfirmationAsync(dialogHandle, cancellationToken);

        return UIAutomationResult.CreateSuccess("save", CreateDiagnostics(stopwatch));
    }

    private static UIA.IUIAutomationElement? FindSaveDialogEditField(UIA.IUIAutomationElement dialog)
    {
        var editCondition = Uia.CreatePropertyCondition(
            UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Edit);

        // Vista-style common dialog: the File name combo has AutomationId "FileNameControlHost".
        // Use its inner Edit so the shell updates its internal path state rather than only changing
        // the displayed text.
        var fileNameHost = dialog.FindFirst(
            UIA.TreeScope.TreeScope_Descendants,
            Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "FileNameControlHost"));
        if (fileNameHost != null)
        {
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

        // Classic Win32 file dialog (what the Open dialog renders as): the File name edit has the
        // well-known control id 1148. Require the Edit control type so we get the inner edit rather
        // than the wrapping ComboBox that shares the same id.
        var classicFileNameEdit = dialog.FindFirst(
            UIA.TreeScope.TreeScope_Descendants,
            Uia.CreateAndCondition(
                editCondition,
                Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1148")));
        if (classicFileNameEdit != null)
        {
            return classicFileNameEdit;
        }

        // Name-based fallback for localized/variant dialogs whose File name edit lacks id 1148.
        var namedFileNameEdit = dialog.FindFirst(
            UIA.TreeScope.TreeScope_Descendants,
            Uia.CreateAndCondition(
                editCondition,
                Uia.CreatePropertyCondition(UIA3PropertyIds.Name, "File name:")));
        if (namedFileNameEdit != null)
        {
            return namedFileNameEdit;
        }

        // Return null (never a blind Edit match) when the File name field has not realized yet. The
        // caller polls this finder, so returning any early Edit is dangerous: AutomationId "1001" is
        // the address/breadcrumb bar and "SearchEditBox" is the search box, and typing a path into
        // either leaves the dialog open. Keep polling until a positively-identified File name edit
        // appears. All lookups above are cheap FindFirst calls so polling cannot starve the shared
        // UIA STA thread.
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
    /// <param name="dialog">The dialog element to watch.</param>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    /// <param name="timeout">
    /// How long to wait. Defaults to <see cref="SaveDialogCloseTimeout"/>; callers that are merely
    /// tidying up after an already-failed operation pass the shorter <see cref="SaveDialogTimeout"/>
    /// so a failure is not made slower than it needs to be.
    /// </param>
    private async Task<bool> WaitForDialogCloseAsync(
        UIA.IUIAutomationElement dialog,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? SaveDialogCloseTimeout);

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
    private async Task<UIAutomationResult?> HandleSaveErrorDialogAsync(nint saveDialogHandle, CancellationToken cancellationToken)
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
                    var foregroundHwnd = NativeMethods.GetForegroundWindow();
                    if (foregroundHwnd == IntPtr.Zero ||
                        !IsSaveDialogWindow(foregroundHwnd, saveDialogHandle))
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

                string? cleanupWarning = null;
                if (await WaitForDialogCloseAsync(errorInfo.dialog, cancellationToken, SaveDialogTimeout))
                {
                    var cancelled = await _keyboardService.PressKeyAsync(
                        "Escape", ModifierKey.None, 1, saveDialogHandle, cancellationToken);
                    if (!cancelled.Success)
                    {
                        cleanupWarning = $"The Save dialog could not be closed safely: {cancelled.Error}";
                    }
                }
                else
                {
                    cleanupWarning = "The error dialog remained open; no Escape key was sent.";
                }

                // Return error to LLM
                return UIAutomationResult.CreateFailure(
                    "save",
                    UIAutomationErrorType.PathError,
                    $"Save failed: {errorInfo.errorText}",
                    CreateDiagnostics(stopwatch)) with
                {
                    UsageHint = cleanupWarning
                };
            }

            await Task.Delay(100, cancellationToken);
        }

        return null; // No error dialog found
    }

    /// <summary>
    /// Handles the "Confirm Save As" overwrite confirmation dialog if it appears.
    /// Based on pywinauto pattern: check for Yes/Replace button and click it.
    /// </summary>
    private async Task HandleOverwriteConfirmationAsync(nint saveDialogHandle, CancellationToken cancellationToken)
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
                if (!IsSaveDialogWindow(ResolveElementWindowHandle(window), saveDialogHandle))
                {
                    continue;
                }

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

    private static bool IsSaveDialogWindow(nint candidate, nint saveDialogHandle)
    {
        if (candidate == nint.Zero || saveDialogHandle == nint.Zero || !NativeMethods.IsWindow(saveDialogHandle))
        {
            return false;
        }

        var current = NativeMethods.GetAncestor(candidate, NativeConstants.GA_ROOT);
        for (var depth = 0; depth < 64 && current != nint.Zero; depth++)
        {
            if (current == saveDialogHandle)
            {
                return true;
            }

            current = NativeMethods.GetWindow(current, NativeConstants.GW_OWNER);
        }

        return false;
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
