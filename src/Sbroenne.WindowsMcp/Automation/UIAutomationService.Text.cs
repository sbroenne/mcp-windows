using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Text operations for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    /// <inheritdoc/>
    public async Task<UIAutomationResult> GetTextAsync(string? elementId, string? windowHandle, bool includeChildren, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await _staThread.ExecuteAsync(() =>
            {
                UIA.IUIAutomationElement? targetElement;

                if (!string.IsNullOrEmpty(elementId))
                {
                    targetElement = ElementIdGenerator.ResolveToAutomationElement(elementId);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "get_text",
                            UIAutomationErrorType.ElementNotFound,
                            $"Element with ID '{elementId}' not found.",
                            CreateDiagnostics(stopwatch));
                    }
                }
                else
                {
                    targetElement = GetRootElement(windowHandle);
                    if (targetElement == null)
                    {
                        return UIAutomationResult.CreateFailure(
                            "get_text",
                            UIAutomationErrorType.WindowNotFound,
                            "No foreground window found.",
                            CreateDiagnostics(stopwatch));
                    }
                }

                var text = ExtractText(targetElement, includeChildren);

                return UIAutomationResult.CreateSuccessWithText("get_text", text, CreateDiagnostics(stopwatch));
            }, cancellationToken);
        }
        catch (COMException ex)
        {
            LogGetTextError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "get_text",
                COMExceptionHelper.IsElementStale(ex) ? UIAutomationErrorType.ElementStale : UIAutomationErrorType.InternalError,
                COMExceptionHelper.GetErrorMessage(ex, "GetText"),
                CreateDiagnostics(stopwatch));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogGetTextError(_logger, elementId, ex);
            return UIAutomationResult.CreateFailure(
                "get_text",
                UIAutomationErrorType.InternalError,
                $"An error occurred: {ex.Message}",
                CreateDiagnostics(stopwatch));
        }
    }

    private static string ExtractText(UIA.IUIAutomationElement element, bool includeChildren)
    {
        // Try TextPattern first
        var text = element.GetText();
        if (!string.IsNullOrEmpty(text) && !includeChildren)
        {
            return text;
        }

        // Try ValuePattern
        var value = element.TryGetValue();
        if (!string.IsNullOrEmpty(value) && !includeChildren)
        {
            return value;
        }

        // Fall back to Name
        var name = element.GetName();
        if (!string.IsNullOrEmpty(name) && !includeChildren)
        {
            return name;
        }

        // If includeChildren, aggregate all text from descendants
        if (includeChildren)
        {
            var allText = new List<string>();
            if (!string.IsNullOrEmpty(name))
            {
                allText.Add(name);
            }

            if (!string.IsNullOrEmpty(value) && value != name)
            {
                allText.Add(value);
            }

            if (!string.IsNullOrEmpty(text) && text != name && text != value)
            {
                allText.Add(text);
            }

            CollectChildText(element, allText);
            return string.Join(" ", allText);
        }

        return name ?? value ?? text ?? "";
    }

    private static void CollectChildText(UIA.IUIAutomationElement parent, List<string> textParts)
    {
        var condition = UIA3Automation.Instance.CreatePropertyCondition(UIA3PropertyIds.IsOffscreen, false);
        var children = parent.FindAll(UIA.TreeScope.TreeScope_Children, condition);

        if (children == null)
        {
            return;
        }

        for (var i = 0; i < children.Length && textParts.Count < 1000; i++)
        {
            try
            {
                var child = children.GetElement(i);
                if (child == null)
                {
                    continue;
                }

                var childText = GetElementText(child);
                if (!string.IsNullOrWhiteSpace(childText) && !textParts.Contains(childText))
                {
                    textParts.Add(childText);
                }

                CollectChildText(child, textParts);
            }
            catch
            {
                // Element went away, skip it
            }
        }
    }

    private static string? GetElementText(UIA.IUIAutomationElement element)
    {
        try
        {
            var value = element.TryGetValue();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return element.GetName();
        }
        catch
        {
            return null;
        }
    }
}
