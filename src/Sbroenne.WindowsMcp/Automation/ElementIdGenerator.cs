using System.Runtime.Versioning;
using System.Windows.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Generates and resolves unique element IDs for UI Automation elements.
/// Format: "window:{hwnd}|runtime:{id}|path:{treePath}"
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ElementIdGenerator
{
    /// <summary>
    /// Generates a unique ID for a UI Automation element.
    /// </summary>
    /// <param name="element">The automation element.</param>
    /// <param name="rootElement">The root element (window) for path calculation.</param>
    /// <returns>A unique element ID string.</returns>
    public static string GenerateId(AutomationElement element, AutomationElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rootElement);

        try
        {
            var current = element.Current;

            // Get window handle
            var windowHandle = current.NativeWindowHandle;
            if (windowHandle == 0)
            {
                // Try to get from parent
                var parent = TreeWalker.ControlViewWalker.GetParent(element);
                while (parent != null && parent != AutomationElement.RootElement)
                {
                    windowHandle = parent.Current.NativeWindowHandle;
                    if (windowHandle != 0)
                    {
                        break;
                    }
                    parent = TreeWalker.ControlViewWalker.GetParent(parent);
                }
            }

            // Get runtime ID
            var runtimeId = element.GetRuntimeId();
            var runtimeIdStr = runtimeId != null && runtimeId.Length > 0
                ? string.Join(".", runtimeId)
                : "0";

            // Calculate tree path from root
            var treePath = CalculateTreePath(element, rootElement);

            return $"window:{windowHandle}|runtime:{runtimeIdStr}|path:{treePath}";
        }
        catch (ElementNotAvailableException)
        {
            return $"window:0|runtime:0|path:stale";
        }
    }

    /// <summary>
    /// Resolves an element ID to an AutomationElement.
    /// </summary>
    /// <param name="elementId">The element ID to resolve.</param>
    /// <returns>The AutomationElement, or null if the element is stale.</returns>
    public static AutomationElement? ResolveToAutomationElement(string elementId)
    {
        ArgumentNullException.ThrowIfNull(elementId);

        // Parse the element ID
        var parts = ParseElementId(elementId);
        if (parts == null)
        {
            return null;
        }

        try
        {
            AutomationElement? element = null;

            // Try to find by runtime ID first (most reliable)
            if (!string.IsNullOrEmpty(parts.RuntimeId) && parts.RuntimeId != "0")
            {
                var runtimeId = parts.RuntimeId.Split('.').Select(int.Parse).ToArray();
                element = FindByRuntimeId(parts.WindowHandle, runtimeId);
            }

            // Fall back to tree path if runtime ID didn't work
            if (element == null && !string.IsNullOrEmpty(parts.TreePath) && parts.TreePath != "stale")
            {
                element = FindByTreePath(parts.WindowHandle, parts.TreePath);
            }

            return element;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves an element ID to its current element info.
    /// </summary>
    /// <param name="elementId">The element ID to resolve.</param>
    /// <param name="coordinateConverter">The coordinate converter.</param>
    /// <returns>The element info, or null if the element is stale.</returns>
    public static UIElementInfo? ResolveElement(string elementId, CoordinateConverter coordinateConverter)
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(coordinateConverter);

        // Parse the element ID
        var parts = ParseElementId(elementId);
        if (parts == null)
        {
            return null;
        }

        try
        {
            AutomationElement? element = null;

            // Try to find by runtime ID first (most reliable)
            if (!string.IsNullOrEmpty(parts.RuntimeId) && parts.RuntimeId != "0")
            {
                var runtimeId = parts.RuntimeId.Split('.').Select(int.Parse).ToArray();
                element = FindByRuntimeId(parts.WindowHandle, runtimeId);
            }

            // Fall back to tree path if runtime ID didn't work
            if (element == null && !string.IsNullOrEmpty(parts.TreePath) && parts.TreePath != "stale")
            {
                element = FindByTreePath(parts.WindowHandle, parts.TreePath);
            }

            if (element == null)
            {
                return null;
            }

            // Use shared conversion logic from UIAutomationService to avoid code duplication
            var rootElement = AutomationElement.FromHandle(parts.WindowHandle);
            return UIAutomationService.ConvertToElementInfo(element, rootElement, coordinateConverter);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static string CalculateTreePath(AutomationElement element, AutomationElement root)
    {
        var path = new Stack<int>();
        var current = element;
        var walker = TreeWalker.ControlViewWalker;

        while (current != null && current != root && current != AutomationElement.RootElement)
        {
            // Find index among siblings
            var parent = walker.GetParent(current);
            if (parent == null)
            {
                break;
            }

            var siblings = parent.FindAll(TreeScope.Children, Condition.TrueCondition);
            var index = 0;
            foreach (AutomationElement sibling in siblings)
            {
                if (Equals(sibling, current))
                {
                    path.Push(index);
                    break;
                }
                index++;
            }

            current = parent;
        }

        return path.Count > 0 ? string.Join(".", path) : "0";
    }

    private static ElementIdParts? ParseElementId(string elementId)
    {
        try
        {
            var parts = elementId.Split('|');
            if (parts.Length != 3)
            {
                return null;
            }

            var windowPart = parts[0].Replace("window:", "");
            var runtimePart = parts[1].Replace("runtime:", "");
            var pathPart = parts[2].Replace("path:", "");

            if (!nint.TryParse(windowPart, out var windowHandle))
            {
                return null;
            }

            return new ElementIdParts(windowHandle, runtimePart, pathPart);
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement? FindByRuntimeId(nint windowHandle, int[] runtimeId)
    {
        try
        {
            var root = windowHandle != 0
                ? AutomationElement.FromHandle(windowHandle)
                : AutomationElement.RootElement;

            var condition = new PropertyCondition(AutomationElement.RuntimeIdProperty, runtimeId);
            return root.FindFirst(TreeScope.Descendants, condition);
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement? FindByTreePath(nint windowHandle, string treePath)
    {
        try
        {
            var root = windowHandle != 0
                ? AutomationElement.FromHandle(windowHandle)
                : AutomationElement.RootElement;

            var indices = treePath.Split('.').Select(int.Parse).ToArray();
            var current = root;
            var walker = TreeWalker.ControlViewWalker;

            foreach (var index in indices)
            {
                var children = current.FindAll(TreeScope.Children, Condition.TrueCondition);
                if (index >= children.Count)
                {
                    return null;
                }
                current = children[index];
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    private static bool Equals(AutomationElement a, AutomationElement b)
    {
        try
        {
            var aId = a.GetRuntimeId();
            var bId = b.GetRuntimeId();
            return aId.SequenceEqual(bId);
        }
        catch
        {
            return false;
        }
    }

    private sealed record ElementIdParts(nint WindowHandle, string RuntimeId, string TreePath);
}
