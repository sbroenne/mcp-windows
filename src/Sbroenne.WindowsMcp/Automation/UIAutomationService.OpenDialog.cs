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
/// <see cref="SaveAsync"/> so both flows share one battle-tested code path.
/// </summary>
public sealed partial class UIAutomationService
{
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
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!nint.TryParse(windowHandle, out var hwnd) || hwnd == nint.Zero)
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    $"Invalid window handle format: '{windowHandle}'",
                    CreateDiagnostics(stopwatch));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.InvalidParameter,
                    "filePath is required for open. Provide the absolute path of an existing file.",
                    CreateDiagnostics(stopwatch));
            }

            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.PathError,
                    $"Open failed: file '{filePath}' does not exist. Provide the path of an existing file.",
                    CreateDiagnostics(stopwatch));
            }

            // Step 1: focus the target window.
            if (!await FocusWindowAsync(hwnd, cancellationToken))
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.ElementNotFound,
                    "Could not focus the target window.",
                    CreateDiagnostics(stopwatch));
            }

            _ = await DeterministicWait.UntilAsync(
                () => NativeMethods.GetForegroundWindow() == hwnd,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(25),
                cancellationToken: cancellationToken);

            // Step 2: invoke the Open command (universal Ctrl+O).
            await _keyboardService.PressKeyAsync("o", ModifierKey.Ctrl, cancellationToken: cancellationToken);

            // Step 3: wait for the Open dialog.
            var dialog = await WaitForOpenDialogAsync(hwnd, cancellationToken);
            if (dialog == null)
            {
                return UIAutomationResult.CreateFailure(
                    "open",
                    UIAutomationErrorType.Timeout,
                    "No Open dialog appeared after Ctrl+O. The app may use a different shortcut or menu; " +
                    "open the dialog manually with ui_click, then type the path with ui_type.",
                    CreateDiagnostics(stopwatch));
            }

            // Step 4: fill the File name field and confirm.
            return await FillOpenDialogAsync(dialog, filePath, stopwatch, cancellationToken);
        }
        catch (COMException ex)
        {
            return UIAutomationResult.CreateFailure(
                "open",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "Open"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return UIAutomationResult.CreateFailure(
                "open",
                UIAutomationErrorType.InternalError,
                $"Open failed: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <summary>
    /// Waits for a standard Open dialog to appear (modal child of the app or a top-level shell window).
    /// Mirrors <see cref="WaitForSaveDialogAsync"/> but matches Open dialog titles.
    /// </summary>
    private async Task<UIA.IUIAutomationElement?> WaitForOpenDialogAsync(
        nint parentHwnd, CancellationToken cancellationToken)
    {
        string[] dialogPatterns = ["Open", "Select a file", "Choose File", "Browse"];

        var deadline = DateTime.UtcNow + SaveDialogTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UIA.IUIAutomationElement? result;
            try
            {
                result = await _staThread.ExecuteAsync(() =>
                {
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

                                    var name = child.CurrentName ?? string.Empty;
                                    if (MatchesOpenDialog(name, dialogPatterns))
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

                    // Fallback: top-level shell dialog windows.
                    var topWindowCondition = Uia.CreatePropertyCondition(
                        UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Window);
                    var topWindows = Uia.RootElement.FindAll(UIA.TreeScope.TreeScope_Children, topWindowCondition);
                    if (topWindows != null)
                    {
                        for (int i = 0; i < topWindows.Length; i++)
                        {
                            var window = topWindows.GetElement(i);
                            var name = window.CurrentName ?? string.Empty;
                            if (MatchesOpenDialog(name, dialogPatterns))
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

    /// <summary>
    /// Types the path into the Open dialog's File name field and clicks Open. Reuses
    /// <see cref="FindSaveDialogEditField"/> (the File name control is identical across the
    /// Save and Open shell dialogs) and <see cref="WaitForDialogCloseAsync"/>.
    /// </summary>
    private async Task<UIAutomationResult> FillOpenDialogAsync(
        UIA.IUIAutomationElement dialog, string filePath, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        await _staThread.ExecuteAsync(() =>
        {
            try
            {
                dialog.SetFocus();
            }
            catch
            {
                // Best effort.
            }
            return true;
        }, cancellationToken);

        UIA.IUIAutomationElement? editField = null;
        var editFieldFound = await DeterministicWait.UntilAsync(
            async () =>
            {
                editField = await _staThread.ExecuteAsync(() => FindSaveDialogEditField(dialog), cancellationToken);
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
                "open",
                UIAutomationErrorType.ElementNotFound,
                "Could not find the File name field in the Open dialog.",
                CreateDiagnostics(stopwatch));
        }

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

        await _keyboardService.ReleaseAllKeysAsync(cancellationToken);

        var normalizedPath = filePath.Replace('/', '\\');
        var expectedFileName = Path.GetFileName(normalizedPath);

        // Enter the path robustly, verify it actually landed in the File name field, and only then
        // confirm. Two mechanisms are combined because a loaded, shared CI desktop can drop focus or
        // keystrokes:
        //   1. SendInput typing (TypeTextAsync) - the same mechanism the proven-reliable Save flow uses
        //      (FillSaveDialogAsync). Real keyboard input is what feeds the shell dialog's CheckFileExists
        //      resolver, so it is required for the dialog to actually close.
        //   2. ValuePattern SetValue as a fallback populate when typing did not register (e.g. focus was
        //      lost) - it deterministically writes the inner Edit's text without needing focus.
        // We read the field back (ValuePattern or TextPattern) and retype/repopulate until it holds the
        // expected file name before clicking Open, so we never confirm an empty or garbled path. The
        // Open button is clicked rather than Enter, which can commit an autocomplete suggestion instead.
        string? lastObservedValue = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await _staThread.ExecuteAsync(() => { editField.TrySetFocus(); return true; }, cancellationToken);
            if (editFieldCenter is { Length: 2 })
            {
                await _mouseService.ClickAsync(editFieldCenter[0], editFieldCenter[1], cancellationToken: cancellationToken);
            }

            // Wait for the edit field to actually own keyboard focus so the typed path lands in it.
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

            await _keyboardService.PressKeyAsync("a", ModifierKey.Ctrl, cancellationToken: cancellationToken);
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);
            await _keyboardService.PressKeyAsync("Delete", cancellationToken: cancellationToken);
            await _keyboardService.TypeTextAsync(normalizedPath, cancellationToken);
            _ = await _keyboardService.WaitForIdleAsync(cancellationToken);

            // Verify the path landed; if typing was dropped, force it via ValuePattern and re-read.
            lastObservedValue = await _staThread.ExecuteAsync(
                () =>
                {
                    var current = editField.GetText();
                    if (!PathMatches(current, normalizedPath, expectedFileName))
                    {
                        editField.TrySetValue(normalizedPath);
                        current = editField.GetText();
                    }

                    return current;
                },
                cancellationToken);

            var openClicked = await ClickOpenButtonAsync(dialog, cancellationToken);
            if (!openClicked)
            {
                await _keyboardService.PressKeyAsync("Return", cancellationToken: cancellationToken);
            }

            if (await WaitForDialogCloseAsync(dialog, cancellationToken))
            {
                return UIAutomationResult.CreateSuccess("open", CreateDiagnostics(stopwatch));
            }
        }

        return UIAutomationResult.CreateFailure(
            "open",
            UIAutomationErrorType.Timeout,
            "Open could not be verified because the Open dialog remained open after entering " +
            $"'{normalizedPath}'. Last File name value observed: '{lastObservedValue ?? "<null>"}'. " +
            "The path may be invalid or the app rejected the file; open the dialog manually with " +
            "ui_click, type the path with ui_type, then confirm.",
            CreateDiagnostics(stopwatch));
    }

    /// <summary>
    /// Whether the File name field's observed text corresponds to the requested path. The shell may
    /// show just the file name, the full path, or a value with surrounding quotes, so accept any of
    /// those rather than requiring an exact match.
    /// </summary>
    private static bool PathMatches(string? observed, string normalizedPath, string expectedFileName)
    {
        if (string.IsNullOrEmpty(observed))
        {
            return false;
        }

        var trimmed = observed.Trim().Trim('"');
        return trimmed.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith(expectedFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds and clicks the Open button in an Open dialog. Mirrors <see cref="ClickSaveButtonAsync"/>.
    /// </summary>
    private async Task<bool> ClickOpenButtonAsync(UIA.IUIAutomationElement dialog, CancellationToken cancellationToken)
    {
        UIA.IUIAutomationElement? openButton = null;
        var found = await DeterministicWait.UntilAsync(
            async () =>
            {
                openButton = await _staThread.ExecuteAsync(() =>
                {
                    string[] openButtonNames = ["Open", "&Open"];
                    foreach (var name in openButtonNames)
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

                    // AutomationId "1" is the default OK/Open button in shell file dialogs.
                    var idCondition = Uia.CreateAndCondition(
                        Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, UIA3ControlTypeIds.Button),
                        Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, "1"));
                    return dialog.FindFirst(UIA.TreeScope.TreeScope_Descendants, idCondition);
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
            return false;
        }

        var outcome = await ExecuteElementActionAsync(openButton, dialog, fallbackClickPoint: null, cancellationToken);
        return outcome.Success;
    }
}
