using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Utilities;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Generalized common-dialog handling. Extends the Save-As automation to the standard Windows
/// <b>Open</b> file dialog: send Ctrl+O, wait for the dialog, type the path into the shared
/// File name field, and click Open. Reuses the same field-discovery and dialog-close helpers as
/// <see cref="SaveAsync(string, string?, CancellationToken)"/> so both flows share one battle-tested code path.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <summary>
    /// The File name combo (FileNameControlHost) can realize noticeably later than the address bar
    /// on a contended desktop, so allow more time than <c>SaveDialogTimeout</c> to find it rather
    /// than giving up and mis-targeting another edit.
    /// </summary>
    private static readonly TimeSpan OpenDialogEditFieldTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Opens a file through an application's standard Open dialog (Ctrl+O). Focuses the window,
    /// invokes the Open command, fills the File name field with <paramref name="filePath"/>, and
    /// confirms. The file must already exist so the operation is deterministic and never hangs on
    /// a "file not found" prompt.
    /// </summary>
    /// <param name="windowHandle">Target application window handle (decimal string).</param>
    /// <param name="filePath">Absolute path of an existing file to open.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result describing whether the Open dialog was driven successfully.</returns>
    public async Task<UIAutomationResult> OpenFileAsync(
        string windowHandle, string filePath, CancellationToken cancellationToken = default)
        => await OpenFileAsync(
            windowHandle,
            filePath,
            triggerMode: "shortcut",
            timeoutMs: (int)SaveDialogTimeout.TotalMilliseconds,
            cancellationToken);

    /// <summary>
    /// Opens a file through a standard Open dialog. Use triggerMode "shortcut" to send Ctrl+O,
    /// or "wait" when a prior semantic browser/app click is expected to open the native dialog.
    /// </summary>
    public async Task<UIAutomationResult> OpenFileAsync(
        string windowHandle,
        string filePath,
        string triggerMode,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var normalizedTrigger = string.IsNullOrWhiteSpace(triggerMode)
            ? "shortcut"
            : triggerMode.Trim().ToLowerInvariant();

        try
        {
            if (normalizedTrigger is not ("shortcut" or "wait"))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    $"Invalid triggerMode '{triggerMode}'. Valid values: shortcut, wait.",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            if (timeoutMs <= 0 || timeoutMs > 60000)
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    "timeoutMs must be between 1 and 60000.",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            if (!nint.TryParse(windowHandle, out var hwnd) || hwnd == nint.Zero)
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    $"Invalid window handle format: '{windowHandle}'",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    "filePath is required for open. Provide the absolute path of an existing file.",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.PathError,
                    $"Open failed: file '{filePath}' does not exist. Provide the path of an existing file.",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            if (normalizedTrigger == "shortcut" &&
                !await FocusWindowAsync(hwnd, cancellationToken))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not focus the target window.",
                    CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
            }

            if (normalizedTrigger == "shortcut")
            {
                var foregroundReady = await DeterministicWait.UntilAsync(
                    () => NativeMethods.GetForegroundWindow() == hwnd,
                    TimeSpan.FromMilliseconds(500),
                    TimeSpan.FromMilliseconds(25),
                    cancellationToken: cancellationToken);
                if (!foregroundReady)
                {
                    return UIAutomationResult.CreateFailure(
                        "open",
                        UIAutomationErrorType.WrongTargetWindow,
                        "The target window could not be confirmed as foreground, so Ctrl+O was not sent.",
                        CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
                }

                var shortcutResult = await _keyboardService.PressKeyAsync(
                    "o",
                    ModifierKey.Ctrl,
                    1,
                    hwnd,
                    cancellationToken);
                if (!shortcutResult.Success)
                {
                    return UIAutomationResult.CreateFailure(
                        "open",
                        shortcutResult.ErrorCode == KeyboardControlErrorCode.WrongTargetWindow
                            ? UIAutomationErrorType.WrongTargetWindow
                            : UIAutomationErrorType.InternalError,
                        shortcutResult.Error ?? "Ctrl+O could not be sent.",
                        CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
                }
            }

            var dialog = await WaitForOpenDialogAsync(
                hwnd,
                TimeSpan.FromMilliseconds(timeoutMs),
                allowStructuralMatch: normalizedTrigger == "shortcut",
                cancellationToken);
            if (dialog == null)
            {
                var candidates = await DescribeOpenDialogCandidatesAsync(hwnd, cancellationToken);
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.Timeout,
                    normalizedTrigger == "wait"
                        ? "The expected native Open dialog did not appear after the prior UI action."
                        : "No native Open dialog appeared after Ctrl+O.",
                    CreateDiagnostics(stopwatch) with
                    {
                        ActionPath = normalizedTrigger,
                        Warnings = [$"Observed candidate windows: {candidates}"]
                    });
            }

            return await FillOpenDialogAsync(
                dialog,
                filePath,
                stopwatch,
                normalizedTrigger,
                cancellationToken);
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure(
                "open",
                COMExceptionHelper.GetErrorType(ex),
                COMExceptionHelper.GetErrorMessage(ex, "Open"),
                CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return UIAutomationResult.CreateFailure(
                "open",
                UIAutomationErrorType.InternalError,
                $"Open failed: {ex.Message}",
                CreateDiagnostics(stopwatch) with { ActionPath = normalizedTrigger });
        }
    }

    /// <summary>
    /// Waits for a standard Open dialog to appear (modal child of the app or a top-level shell window).
    /// Mirrors <see cref="WaitForSaveDialogAsync"/> but matches Open dialog titles.
    /// </summary>
    private async Task<UIA.IUIAutomationElement?> WaitForOpenDialogAsync(
        nint parentHwnd,
        TimeSpan timeout,
        bool allowStructuralMatch,
        CancellationToken cancellationToken)
    {
        string[] dialogPatterns = ["Open", "Select a file", "Choose File", "Browse"];

        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UIA.IUIAutomationElement? result;
            try
            {
                result = await _staThread.ExecuteAsync(() =>
                {
                    var enabledPopup = NativeMethods.GetWindow(
                        parentHwnd,
                        NativeConstants.GW_ENABLEDPOPUP);
                    if (enabledPopup != IntPtr.Zero &&
                        enabledPopup != parentHwnd &&
                        NativeMethods.IsWindowVisible(enabledPopup))
                    {
                        var popup = Uia.ElementFromHandle(enabledPopup);
                        if (popup != null &&
                            MatchesExpectedFileDialog(popup, dialogPatterns, allowStructuralMatch))
                        {
                            return popup;
                        }
                    }

                    var parentElement = Uia.ElementFromHandle(parentHwnd);
                    if (parentElement != null)
                    {
                        var windowCondition = Uia.CreatePropertyCondition(
                            UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window);
                        var children = parentElement.FindAll(UIA.TreeScope.TreeScope_Children, windowCondition);
                        if (children != null)
                        {
                            for (int i = 0; i < children.Length; i++)
                            {
                                var child = children.GetElement(i);
                                var windowPattern = child.GetPattern<UIA.IUIAutomationWindowPattern>(UIA3PatternIds.Window);
                                if (windowPattern == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    if (windowPattern.CurrentIsModal == 0)
                                    {
                                        continue;
                                    }

                                    if (MatchesExpectedFileDialog(
                                        child,
                                        dialogPatterns,
                                        allowStructuralMatch))
                                    {
                                        return child;
                                    }
                                }
                                catch
                                {
                                    // Skip unstable elements.
                                }
                            }
                        }
                    }

                    // Fallback: only accept a visible top-level window owned by the requested app.
                    var topWindowCondition = Uia.CreatePropertyCondition(
                        UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window);
                    var topWindows = Uia.RootElement.FindAll(UIA.TreeScope.TreeScope_Children, topWindowCondition);
                    if (topWindows != null)
                    {
                        for (int i = 0; i < topWindows.Length; i++)
                        {
                            var window = topWindows.GetElement(i);
                            var candidateHandle = new IntPtr(window.CurrentNativeWindowHandle);
                            var rootOwner = NativeMethods.GetAncestor(
                                candidateHandle,
                                NativeConstants.GA_ROOTOWNER);
                            var parentRootOwner = NativeMethods.GetAncestor(
                                parentHwnd,
                                NativeConstants.GA_ROOTOWNER);
                            var owner = NativeMethods.GetWindow(
                                candidateHandle,
                                NativeConstants.GW_OWNER);
                            if (candidateHandle != parentHwnd &&
                                owner != IntPtr.Zero &&
                                MatchesExpectedFileDialog(
                                    window,
                                    dialogPatterns,
                                    allowStructuralMatch) &&
                                NativeMethods.IsWindowVisible(candidateHandle) &&
                                (rootOwner == parentRootOwner ||
                                 owner == parentHwnd))
                            {
                                return window;
                            }
                        }
                    }

                    return (UIA.IUIAutomationElement?)null;
                }, cancellationToken);
            }
            catch (COMException exception) when (COMExceptionHelper.IsTransientProviderFailure(exception))
            {
                result = null;
            }

            if (result != null)
            {
                return result;
            }

            await Task.Delay(SaveDialogPollInterval, cancellationToken);
        }

        return null;
    }

    private static bool MatchesOpenDialog(string name, string[] dialogPatterns)
    {
        foreach (var pattern in dialogPatterns)
        {
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesExpectedFileDialog(
        UIA.IUIAutomationElement dialog,
        string[] titlePatterns,
        bool allowStructuralMatch)
    {
        if (MatchesOpenDialog(dialog.CurrentName ?? string.Empty, titlePatterns))
        {
            return true;
        }

        if (!allowStructuralMatch)
        {
            return false;
        }

        // Localized Windows installations do not use English dialog titles. Standard shell file
        // dialogs can still be identified by their file-name field and default confirmation button.
        if (FindSaveDialogEditField(dialog) is null)
        {
            return false;
        }

        var defaultButtonCondition = Uia.CreateAndCondition(
            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
            Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1"));
        return dialog.FindFirst(
            UIA.TreeScope.TreeScope_Descendants,
            defaultButtonCondition) is not null;
    }

    /// <summary>
    /// Diagnostic aid: lists the Edit and ComboBox descendants (AutomationId + Name) of the Open
    /// dialog so a "File name field not found" failure on CI reveals the real control identifiers.
    /// Runs a single enumeration on the failure path only, never per poll.
    /// </summary>
    private async Task<string> DumpDialogEditControlsAsync(
        UIA.IUIAutomationElement dialog, CancellationToken cancellationToken)
    {
        return await _staThread.ExecuteAsync(
            () =>
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var controlType in new[] { UIA3ControlTypeIds.Edit, UIA3ControlTypeIds.ComboBox })
                    {
                        var matches = dialog.FindAll(
                            UIA.TreeScope.TreeScope_Descendants,
                            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, controlType));
                        var label = controlType == UIA3ControlTypeIds.Edit ? "Edit" : "ComboBox";
                        for (var i = 0; i < matches.Length && i < 40; i++)
                        {
                            var element = matches.GetElement(i);
                            var id = element.GetAutomationId() ?? string.Empty;
                            var name = element.GetName() ?? string.Empty;
                            sb.Append(label).Append("(id='").Append(id).Append("',name='").Append(name).Append("') ");
                        }
                    }

                    var text = sb.ToString();
                    return text.Length == 0 ? "<none>" : text;
                }
                catch (COMException exception)
                {
                    return "dump-failed: " + exception.Message;
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Types the path into the Open dialog's File name field and clicks Open. Reuses
    /// <see cref="FindSaveDialogEditField"/> (the File name control is identical across the
    /// Save and Open shell dialogs) and <see cref="WaitForDialogCloseAsync"/>.
    /// </summary>
    private async Task<UIAutomationResult> FillOpenDialogAsync(
        UIA.IUIAutomationElement dialog,
        string filePath,
        Stopwatch stopwatch,
        string actionPath,
        CancellationToken cancellationToken)
    {
        var dialogHwnd = await _staThread.ExecuteAsync(
            () => NormalizeTopLevelWindowHandle(
                new IntPtr(dialog.CurrentNativeWindowHandle)),
            cancellationToken);

        UIA.IUIAutomationElement? editField = null;
        var editFieldFound = await DeterministicWait.UntilAsync(
            async () =>
            {
                editField = await _staThread.ExecuteAsync(() => FindSaveDialogEditField(dialog), cancellationToken);
                return editField != null;
            },
            OpenDialogEditFieldTimeout,
            SaveDialogPollInterval,
            transientException: exception =>
                exception is COMException comException &&
                COMExceptionHelper.IsTransientProviderFailure(comException),
            cancellationToken: cancellationToken);

        if (!editFieldFound || editField == null)
        {
            var controlDump = await DumpDialogEditControlsAsync(dialog, cancellationToken);
            return UIAutomationResult.CreateFailure(
                "open",
                UIAutomationErrorType.ElementNotFound,
                "Could not find the File name field in the Open dialog. Edit/ComboBox descendants: " +
                controlDump,
                CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
        }

        int[]? editFieldCenter = await _staThread.ExecuteAsync<int[]?>(() =>
        {
            var rect = editField.GetBoundingRectangle();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            return [(int)Math.Round(rect.X + (rect.Width / 2)), (int)Math.Round(rect.Y + (rect.Height / 2))];
        }, cancellationToken);

        var normalizedPath = filePath.Replace('/', '\\');

        var assignment = await _staThread.ExecuteAsync(
            () =>
            {
                var pattern = editField.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value);
                if (pattern is null)
                {
                    return (Dispatched: false, ReadOnly: false);
                }
                if (pattern.CurrentIsReadOnly != 0)
                {
                    return (Dispatched: false, ReadOnly: true);
                }

                pattern.SetValue(normalizedPath);
                return (Dispatched: true, ReadOnly: false);
            },
            cancellationToken);
        if (assignment.ReadOnly)
        {
            return UIAutomationResult.CreateFailure(
                "open", UIAutomationErrorType.InvalidParameter,
                "The Open dialog's filename field is read-only. No path input was sent.",
                CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
        }

        if (!assignment.Dispatched)
        {
            if (dialogHwnd == IntPtr.Zero || _windowActivator == null)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.WrongTargetWindow,
                    "The native Open dialog could not be activated before keyboard input.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }

            var activated = await _windowActivator.ActivateWindowAsync(
                dialogHwnd, cancellationToken: cancellationToken);
            if (!activated || !_windowActivator.IsForegroundWindow(dialogHwnd))
            {
                return CreateOpenDialogForegroundFailure(stopwatch, actionPath);
            }

            await _keyboardService.ReleaseAllKeysAsync(cancellationToken);
            await _staThread.ExecuteAsync(() => { editField.TrySetFocus(); return true; }, cancellationToken);
            if (editFieldCenter is { Length: 2 })
            {
                if (!_windowActivator.IsForegroundWindow(dialogHwnd))
                {
                    return CreateOpenDialogForegroundFailure(stopwatch, actionPath);
                }

                var focusClick = await _mouseService.ClickAsync(
                    editFieldCenter[0],
                    editFieldCenter[1],
                    ModifierKey.None,
                    dialogHwnd,
                    cancellationToken);
                if (!focusClick.Success)
                {
                    return UIAutomationResult.CreateFailure(
                        "open", UIAutomationErrorType.WrongTargetWindow,
                        $"Could not focus the filename field: {focusClick.Error}",
                        CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
                }
            }

            var focusReady = await WaitForElementConditionAsync(
                editField, () => editField.CurrentHasKeyboardFocus != 0, cancellationToken);
            if (!focusReady.Observed || focusReady.ElementUnavailable)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.WrongTargetWindow,
                    "The filename field did not obtain focus; no path input was sent.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }

            var selected = await _keyboardService.PressKeyAsync(
                "a", ModifierKey.Ctrl, 1, dialogHwnd, cancellationToken);
            if (!selected.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.VerificationFailed,
                    $"Could not select the filename: {selected.Error}. No further input was sent.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);

            var cleared = await _keyboardService.PressKeyAsync(
                "Delete", ModifierKey.None, 1, dialogHwnd, cancellationToken);
            if (!cleared.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.VerificationFailed,
                    $"Could not clear the filename: {cleared.Error}. No further input was sent.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);

            var empty = await WaitForElementConditionAsync(
                editField, () => editField.GetText() == "", cancellationToken);
            if (!empty.Observed || empty.ElementUnavailable)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.VerificationFailed,
                    "The filename did not become empty. No replacement path was sent; inspect the dialog.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }

            var typed = await _keyboardService.TypeTextAsync(normalizedPath, dialogHwnd, cancellationToken);
            if (!typed.Success)
            {
                return UIAutomationResult.CreateFailure(
                    "open", UIAutomationErrorType.VerificationFailed,
                    $"Filename input failed: {typed.Error}. No retry or submission was sent; inspect the dialog.",
                    CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
            }
        }

        var verified = await WaitForElementConditionAsync(
            editField, () => PathMatches(editField.GetText(), normalizedPath), cancellationToken);
        if (!verified.Observed || verified.ElementUnavailable)
        {
            return UIAutomationResult.CreateFailure(
                "open", UIAutomationErrorType.VerificationFailed,
                "The filename field did not show the requested path. No correction or submission was sent. " +
                "Inspect the dialog before deciding how to continue.",
                CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
        }

        var openOutcome = await ClickOpenButtonAsync(dialog, cancellationToken);
        if (!openOutcome.Success)
        {
            return UIAutomationResult.CreateFailure(
                "open", UIAutomationErrorType.VerificationFailed,
                $"Open could not be activated or verified: {openOutcome.ErrorMessage}. No second submission was sent.",
                CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
        }

        if (await WaitForDialogCloseAsync(dialog, cancellationToken))
        {
            return UIAutomationResult.CreateSuccess(
                "open",
                CreateDiagnostics(stopwatch) with
                {
                    ActionPath = assignment.Dispatched && openOutcome.ActionPath == "semantic_invoke"
                        ? actionPath + "+semantic"
                        : actionPath
                });
        }

        return UIAutomationResult.CreateFailure(
            "open",
            UIAutomationErrorType.VerificationFailed,
            "Open was sent, but the dialog remained open. No second submission was sent. " +
            "Inspect the dialog; the application may have rejected the file or requested a decision.",
            CreateDiagnostics(stopwatch) with { ActionPath = actionPath });
    }

    private static UIAutomationResult CreateOpenDialogForegroundFailure(
        Stopwatch stopwatch,
        string actionPath) =>
        UIAutomationResult.CreateFailure(
            "open",
            UIAutomationErrorType.WrongTargetWindow,
            "The native Open dialog lost foreground ownership, so no further file-path input was sent.",
            CreateDiagnostics(stopwatch) with { ActionPath = actionPath });

    private async Task<string> DescribeOpenDialogCandidatesAsync(
        nint parentHwnd,
        CancellationToken cancellationToken)
    {
        return await _staThread.ExecuteAsync(() =>
        {
            var descriptions = new List<string>();
            var popup = NativeMethods.GetWindow(parentHwnd, NativeConstants.GW_ENABLEDPOPUP);
            if (popup != IntPtr.Zero && popup != parentHwnd)
            {
                var popupElement = Uia.ElementFromHandle(popup);
                descriptions.Add(
                    $"enabledPopup(handle={popup}, name='{popupElement?.CurrentName ?? string.Empty}')");
            }

            var windows = Uia.RootElement.FindAll(
                UIA.TreeScope.TreeScope_Children,
                Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window));
            var parentRootOwner = NativeMethods.GetAncestor(
                parentHwnd,
                NativeConstants.GA_ROOTOWNER);
            for (var index = 0; index < Math.Min(windows?.Length ?? 0, 12); index++)
            {
                var window = windows!.GetElement(index);
                var handle = new IntPtr(window.CurrentNativeWindowHandle);
                var rootOwner = NativeMethods.GetAncestor(
                    handle,
                    NativeConstants.GA_ROOTOWNER);
                var owner = NativeMethods.GetWindow(handle, NativeConstants.GW_OWNER);
                if (handle != parentHwnd &&
                    rootOwner != parentRootOwner &&
                    owner != parentHwnd)
                {
                    continue;
                }

                descriptions.Add(
                    $"window(handle={handle}, name='{window.CurrentName ?? string.Empty}', visible={NativeMethods.IsWindowVisible(handle)})");
            }

            return descriptions.Count == 0 ? "<none>" : string.Join("; ", descriptions);
        }, cancellationToken);
    }

    /// <summary>
    /// Whether the File name field's observed text corresponds to the requested path. The shell may
    /// show the full path with surrounding quotes, so normalize those quotes before comparison.
    /// Require the complete absolute path: accepting only a matching file-name suffix can hide
    /// dropped leading keystrokes and cause the dialog to open an unintended file.
    /// </summary>
    private static bool PathMatches(string? observed, string normalizedPath)
    {
        if (string.IsNullOrEmpty(observed))
        {
            return false;
        }

        var trimmed = observed.Trim().Trim('"');
        return trimmed.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds and clicks the Open button in an Open dialog. Mirrors <see cref="ClickSaveButtonAsync"/>.
    /// </summary>
    private async Task<ElementActionOutcome> ClickOpenButtonAsync(
        UIA.IUIAutomationElement dialog,
        CancellationToken cancellationToken)
    {
        UIA.IUIAutomationElement? openButton = null;
        var found = await DeterministicWait.UntilAsync(
            async () =>
            {
                openButton = await _staThread.ExecuteAsync(() =>
                {
                    // AutomationId "1" is the default confirmation button in standard shell
                    // dialogs. Prefer it over accessible name because the dialog can contain
                    // another visible "Open" command in its navigation surface.
                    var idCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1"));
                    var defaultButton = dialog.FindFirst(
                        UIA.TreeScope.TreeScope_Descendants,
                        idCondition);
                    if (defaultButton is not null &&
                        defaultButton.IsEnabled() &&
                        !defaultButton.IsOffscreen())
                    {
                        return defaultButton;
                    }

                    string[] openButtonNames = ["Open", "&Open"];
                    foreach (var name in openButtonNames)
                    {
                        var condition = Uia.CreateAndCondition(
                            Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                            Uia.CreatePropertyCondition(UIA3PropertyIds.Name, name));
                        var button = dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);
                        if (button is not null &&
                            button.IsEnabled() &&
                            !button.IsOffscreen())
                        {
                            return button;
                        }
                    }

                    return null;
                }, cancellationToken);
                return openButton != null;
            },
            SaveDialogTimeout,
            SaveDialogPollInterval,
            transientException: exception =>
                exception is COMException comException &&
                COMExceptionHelper.IsTransientProviderFailure(comException),
            cancellationToken: cancellationToken);

        if (!found || openButton == null)
        {
            return new ElementActionOutcome(
                false,
                ErrorMessage: "No visible Open button was found in the native dialog.",
                ActionPath: "open_button_find");
        }

        return await ExecuteElementActionAsync(
            openButton,
            dialog,
            fallbackClickPoint: null,
            cancellationToken);
    }
}
