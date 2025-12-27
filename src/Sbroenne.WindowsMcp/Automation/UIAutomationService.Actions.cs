using System.Diagnostics;
using System.Runtime.InteropServices;
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

    /// <inheritdoc/>
    public async Task<UIAutomationResult> ClickElementAsync(string elementId, nint? windowHandle, CancellationToken cancellationToken = default)
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
                        "click",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element with ID '{elementId}' could not be resolved. The element may have been removed from the UI.",
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
                    "Element cannot be clicked: no Invoke pattern and no clickable point available. Use the element's clickablePoint coordinates with mouse_control as fallback.",
                    CreateDiagnostics(stopwatch));
            }, cancellationToken);
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

                return UIAutomationResult.CreateSuccess("highlight", elementInfo!, CreateDiagnostics(stopwatch));
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
            catch (Exception ex)
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
