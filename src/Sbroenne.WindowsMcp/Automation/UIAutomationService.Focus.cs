using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Focus operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> FocusElementAsync(string elementId, CancellationToken cancellationToken = default)
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
                        "focus",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element not found or stale: {elementId}",
                        CreateDiagnostics(stopwatch));
                }

                var elevationCheck = CheckElevatedTarget(element);
                if (!elevationCheck.Success)
                {
                    return elevationCheck with { Action = "focus" };
                }

                if (!element.TrySetFocus())
                {
                    return UIAutomationResult.CreateFailure(
                        "focus",
                        UIAutomationErrorType.PatternNotSupported,
                        "Failed to set focus on element.",
                        CreateDiagnostics(stopwatch));
                }

                stopwatch.Stop();

                var rootElement = GetRootElementFromElementId(elementId) ?? element;
                var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);

                return UIAutomationResult.CreateSuccessCompact("focus", [elementInfo!], CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogFocusElementError(_logger, elementId, ex);
            var errorType = COMExceptionHelper.IsElementStale(ex)
                ? UIAutomationErrorType.ElementStale
                : COMExceptionHelper.IsAccessDenied(ex)
                    ? UIAutomationErrorType.ElevatedTarget
                    : UIAutomationErrorType.InternalError;
            return UIAutomationResult.CreateFailure(
                "focus",
                errorType,
                COMExceptionHelper.GetErrorMessage(ex, "Focus"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFocusElementError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "focus",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetFocusedElementAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var focusedElement = Uia.GetFocusedElement();
                if (focusedElement == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_focused_element",
                        UIAutomationErrorType.ElementNotFound,
                        "No element currently has focus.",
                        CreateDiagnostics(stopwatch));
                }

                var rootElement = GetRootElementForScroll(focusedElement);
                var elementInfo = ConvertToElementInfo(focusedElement, rootElement, _coordinateConverter, null);

                if (elementInfo == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_focused_element",
                        UIAutomationErrorType.ElementStale,
                        "Focused element became unavailable.",
                        CreateDiagnostics(stopwatch));
                }

                return UIAutomationResult.CreateSuccessCompact("get_focused_element", [elementInfo], CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogGetFocusedElementError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "get_focused_element",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "GetFocusedElement"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetFocusedElementError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "get_focused_element",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetElementAtCursorAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var cursorPos = GetCursorPosition();
                var element = Uia.ElementFromPoint(cursorPos.X, cursorPos.Y);

                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_element_at_cursor",
                        UIAutomationErrorType.ElementNotFound,
                        $"No element found at cursor position ({cursorPos.X}, {cursorPos.Y}).",
                        CreateDiagnostics(stopwatch));
                }

                var rootElement = GetRootElementForScroll(element);
                var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);

                if (elementInfo == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_element_at_cursor",
                        UIAutomationErrorType.ElementStale,
                        "Element at cursor became unavailable.",
                        CreateDiagnostics(stopwatch));
                }

                return UIAutomationResult.CreateSuccessCompact("get_element_at_cursor", [elementInfo], CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogGetElementAtCursorError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "get_element_at_cursor",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "GetElementAtCursor"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetElementAtCursorError(_logger, ex);
            return UIAutomationResult.CreateFailure(
                "get_element_at_cursor",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetAncestorsAsync(string elementId, int? maxDepth, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var effectiveMaxLevels = maxDepth ?? 100;

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
                if (element == null)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_ancestors",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element not found or stale: {elementId}",
                        CreateDiagnostics(stopwatch));
                }

                var ancestors = new List<UIElementInfo>();
                var current = element.GetParent();
                var level = 0;
                var desktopRoot = Uia.RootElement;

                while (current != null && level < effectiveMaxLevels)
                {
                    if (current.IsSameElement(desktopRoot))
                    {
                        break;
                    }

                    var ancestorInfo = ConvertToElementInfo(current, desktopRoot, _coordinateConverter, null);
                    if (ancestorInfo != null)
                    {
                        ancestors.Add(ancestorInfo);
                    }

                    current = current.GetParent();
                    level++;
                }

                if (ancestors.Count == 0)
                {
                    return UIAutomationResult.CreateFailure(
                        "get_ancestors",
                        UIAutomationErrorType.ElementNotFound,
                        "Element has no ancestors.",
                        CreateDiagnostics(stopwatch));
                }

                return UIAutomationResult.CreateSuccessCompact("get_ancestors", [.. ancestors], CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogGetAncestorsError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "get_ancestors",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "GetAncestors"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetAncestorsError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "get_ancestors",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static POINT GetCursorPosition()
    {
        GetCursorPos(out var point);
        return point;
    }
}
