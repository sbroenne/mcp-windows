using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Pattern invocation operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> InvokePatternAsync(string elementId, string pattern, string? value, CancellationToken cancellationToken = default)
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
                        "invoke",
                        UIAutomationErrorType.ElementNotFound,
                        $"Element not found or stale: {elementId}",
                        CreateDiagnostics(stopwatch));
                }

                var elevationCheck = CheckElevatedTarget(element);
                if (!elevationCheck.Success)
                {
                    return elevationCheck with { Action = "invoke" };
                }

                var patternName = pattern?.ToUpperInvariant() ?? "INVOKE";
                var (success, errorMessage) = patternName switch
                {
                    "INVOKE" => TryInvokePattern(element),
                    "TOGGLE" => TryTogglePattern(element),
                    "EXPAND" => TryExpandCollapsePattern(element, expand: true),
                    "COLLAPSE" => TryExpandCollapsePattern(element, expand: false),
                    "EXPANDCOLLAPSE" => TryExpandCollapsePattern(element, expand: true),
                    "VALUE" => TryValuePattern(element, value),
                    "RANGEVALUE" => TryRangeValuePattern(element, value),
                    "SCROLL" => TryScrollPattern(element, value),
                    _ => (false, $"Unknown pattern: {pattern}. Supported patterns: Invoke, Toggle, Expand, Collapse, Value, RangeValue, Scroll")
                };

                stopwatch.Stop();

                if (!success)
                {
                    var availablePatterns = element.GetSupportedPatternNames();
                    var errorMsg = string.IsNullOrEmpty(errorMessage)
                        ? $"Pattern '{pattern}' not supported on this element. Available patterns: {string.Join(", ", availablePatterns)}"
                        : errorMessage;

                    return UIAutomationResult.CreateFailure(
                        "invoke",
                        UIAutomationErrorType.PatternNotSupported,
                        errorMsg,
                        CreateDiagnostics(stopwatch));
                }

                var rootElement = GetRootElementFromElementId(elementId) ?? element;
                var elementInfo = ConvertToElementInfo(element, rootElement, _coordinateConverter, null);

                return UIAutomationResult.CreateSuccess("invoke", elementInfo!, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogInvokePatternError(_logger, pattern ?? "Invoke", elementId, ex);

            // Provide detailed error based on HRESULT
            var errorType = COMExceptionHelper.IsElementStale(ex)
                ? UIAutomationErrorType.ElementStale
                : COMExceptionHelper.IsAccessDenied(ex)
                    ? UIAutomationErrorType.ElevatedTarget
                    : UIAutomationErrorType.InternalError;

            return UIAutomationResult.CreateFailure(
                "invoke",
                errorType,
                COMExceptionHelper.GetErrorMessage(ex, pattern ?? "Invoke"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogInvokePatternError(_logger, pattern ?? "Invoke", elementId, ex);
            return UIAutomationResult.CreateFailure(
                "invoke",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private static (bool success, string? errorMessage) TryInvokePattern(UIA.IUIAutomationElement element)
    {
        try
        {
            if (element.TryInvoke())
            {
                return (true, null);
            }

            return (false, "Element does not support InvokePattern.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "Invoke"));
        }
        catch (Exception ex)
        {
            return (false, $"Invoke failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryTogglePattern(UIA.IUIAutomationElement element)
    {
        try
        {
            if (element.TryToggle())
            {
                return (true, null);
            }

            return (false, "Element does not support TogglePattern.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "Toggle"));
        }
        catch (Exception ex)
        {
            return (false, $"Toggle failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryExpandCollapsePattern(UIA.IUIAutomationElement element, bool expand)
    {
        try
        {
            if (expand)
            {
                if (element.TryExpand())
                {
                    return (true, null);
                }
            }
            else
            {
                if (element.TryCollapse())
                {
                    return (true, null);
                }
            }

            return (false, "Element does not support ExpandCollapsePattern.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "ExpandCollapse"));
        }
        catch (Exception ex)
        {
            return (false, $"ExpandCollapse failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryValuePattern(UIA.IUIAutomationElement element, string? value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                return (false, "Value is required for ValuePattern.");
            }

            if (element.TrySetValue(value))
            {
                return (true, null);
            }

            return (false, "Element does not support ValuePattern or is read-only.");
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "SetValue"));
        }
        catch (Exception ex)
        {
            return (false, $"SetValue failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryRangeValuePattern(UIA.IUIAutomationElement element, string? value)
    {
        try
        {
            if (string.IsNullOrEmpty(value) || !double.TryParse(value, out var numericValue))
            {
                return (false, "Valid numeric value is required for RangeValuePattern.");
            }

            var pattern = element.GetPattern<UIA.IUIAutomationRangeValuePattern>(UIA3PatternIds.RangeValue);
            if (pattern == null)
            {
                return (false, "Element does not support RangeValuePattern.");
            }

            if (pattern.CurrentIsReadOnly != 0)
            {
                return (false, "Element is read-only.");
            }

            pattern.SetValue(numericValue);
            return (true, null);
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "SetRangeValue"));
        }
        catch (Exception ex)
        {
            return (false, $"SetRangeValue failed: {ex.Message}");
        }
    }

    private static (bool success, string? errorMessage) TryScrollPattern(UIA.IUIAutomationElement element, string? value)
    {
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationScrollPattern>(UIA3PatternIds.Scroll);
            if (pattern == null)
            {
                return (false, "Element does not support ScrollPattern.");
            }

            var direction = value?.ToUpperInvariant() ?? "DOWN";
            switch (direction)
            {
                case "UP":
                    pattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_SmallDecrement);
                    break;
                case "DOWN":
                    pattern.Scroll(UIA.ScrollAmount.ScrollAmount_NoAmount, UIA.ScrollAmount.ScrollAmount_SmallIncrement);
                    break;
                case "LEFT":
                    pattern.Scroll(UIA.ScrollAmount.ScrollAmount_SmallDecrement, UIA.ScrollAmount.ScrollAmount_NoAmount);
                    break;
                case "RIGHT":
                    pattern.Scroll(UIA.ScrollAmount.ScrollAmount_SmallIncrement, UIA.ScrollAmount.ScrollAmount_NoAmount);
                    break;
                default:
                    return (false, $"Unknown scroll direction: {value}. Use UP, DOWN, LEFT, RIGHT.");
            }

            return (true, null);
        }
        catch (COMException ex)
        {
            return (false, COMExceptionHelper.GetErrorMessage(ex, "Scroll"));
        }
        catch (Exception ex)
        {
            return (false, $"Scroll failed: {ex.Message}");
        }
    }
}
