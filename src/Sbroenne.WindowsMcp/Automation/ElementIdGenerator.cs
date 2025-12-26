using System.Runtime.Versioning;
using UIA = Interop.UIAutomationClient;

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
    public static string GenerateId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rootElement);

        try
        {
            // Get window handle
            var windowHandle = element.GetNativeWindowHandle();
            if (windowHandle == 0)
            {
                // Try to get from parent
                var parent = element.GetParent();
                while (parent != null)
                {
                    windowHandle = parent.GetNativeWindowHandle();
                    if (windowHandle != 0)
                    {
                        break;
                    }

                    parent = parent.GetParent();
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
        catch
        {
            return $"window:0|runtime:0|path:stale";
        }
    }

    /// <summary>
    /// Resolves an element ID to an IUIAutomationElement.
    /// </summary>
    /// <param name="elementId">The element ID to resolve.</param>
    /// <returns>The IUIAutomationElement, or null if the element is stale.</returns>
    public static UIA.IUIAutomationElement? ResolveToAutomationElement(string elementId)
    {
        ArgumentNullException.ThrowIfNull(elementId);

        var parts = ParseElementId(elementId);
        if (parts == null)
        {
            return null;
        }

        try
        {
            UIA.IUIAutomationElement? element = null;

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
        catch
        {
            return null;
        }
    }

    private static string CalculateTreePath(UIA.IUIAutomationElement element, UIA.IUIAutomationElement root)
    {
        var path = new Stack<int>();
        var current = element;
        var uia = UIA3Automation.Instance;

        while (current != null)
        {
            // Check if we reached root
            if (current.IsSameElement(root))
            {
                break;
            }

            // Find parent
            var parent = current.GetParent();
            if (parent == null)
            {
                break;
            }

            // Find index among siblings
            var siblings = parent.FindAll(UIA.TreeScope.TreeScope_Children, uia.TrueCondition);
            var index = 0;
            if (siblings != null)
            {
                for (var i = 0; i < siblings.Length; i++)
                {
                    var sibling = siblings.GetElement(i);
                    if (sibling != null && sibling.IsSameElement(current))
                    {
                        path.Push(index);
                        break;
                    }

                    index++;
                }
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

    private static UIA.IUIAutomationElement? FindByRuntimeId(nint windowHandle, int[] runtimeId)
    {
        try
        {
            var uia = UIA3Automation.Instance;
            var root = windowHandle != 0
                ? uia.ElementFromHandle(windowHandle)
                : uia.RootElement;

            if (root == null)
            {
                return null;
            }

            // First check if the root element itself matches the runtime ID
            var rootRuntimeId = root.GetRuntimeId();
            if (rootRuntimeId != null && rootRuntimeId.SequenceEqual(runtimeId))
            {
                return root;
            }

            // Search descendants for the runtime ID
            var condition = uia.CreatePropertyCondition(UIA3PropertyIds.RuntimeId, runtimeId);
            return root.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);
        }
        catch
        {
            return null;
        }
    }

    private static UIA.IUIAutomationElement? FindByTreePath(nint windowHandle, string treePath)
    {
        try
        {
            var uia = UIA3Automation.Instance;
            var root = windowHandle != 0
                ? uia.ElementFromHandle(windowHandle)
                : uia.RootElement;

            if (root == null)
            {
                return null;
            }

            var indices = treePath.Split('.').Select(int.Parse).ToArray();
            var current = root;

            foreach (var index in indices)
            {
                var children = current.FindAll(UIA.TreeScope.TreeScope_Children, uia.TrueCondition);
                if (children == null || index >= children.Length)
                {
                    return null;
                }

                current = children.GetElement(index);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ElementIdParts(nint WindowHandle, string RuntimeId, string TreePath);
}
