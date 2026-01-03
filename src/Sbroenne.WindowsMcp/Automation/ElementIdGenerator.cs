using System.Collections.Concurrent;
using System.Runtime.Versioning;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Generates and resolves unique element IDs for UI Automation elements.
/// Uses short IDs (1, 2, 3...) externally, mapped to full IDs internally.
/// Full format: "window:{hwnd}|runtime:{id}|path:{treePath}"
/// Thread-safe caching is built-in to minimize duplicate ID generation.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ElementIdGenerator
{
    // Thread-safe cache for mapping short IDs to full IDs
    private static readonly ConcurrentDictionary<string, string> s_shortToFull = new();
    private static readonly ConcurrentDictionary<string, string> s_fullToShort = new();
    private static long s_counter;

    /// <summary>
    /// Generates a unique ID for a UI Automation element.
    /// Returns a short ID (e.g., "1", "2") that maps to the full internal ID.
    /// </summary>
    /// <param name="element">The automation element.</param>
    /// <param name="rootElement">The root element (window) for path calculation.</param>
    /// <returns>A short element ID string.</returns>
    public static string GenerateId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        var fullId = GenerateFullId(element, rootElement);
        return RegisterFullId(fullId);
    }

    /// <summary>
    /// Generates a unique ID for a UI Automation element optimized for token usage.
    /// Returns a short ID (e.g., "1", "2") that maps to the full internal ID.
    /// </summary>
    /// <param name="element">The automation element.</param>
    /// <param name="rootElement">The root element (window) for path calculation.</param>
    /// <returns>A short element ID string optimized for LLM token usage.</returns>
    public static string GenerateFastId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rootElement);

        var fullId = GenerateFastFullId(element, rootElement);
        return RegisterFullId(fullId);
    }

    /// <summary>
    /// Generates a unique ID for an element using only cached properties.
    /// Returns a short ID (e.g., "1", "2") that maps to the full internal ID.
    /// </summary>
    /// <param name="element">The automation element.</param>
    /// <param name="rootElement">The root element (window) for path calculation.</param>
    /// <returns>A short element ID string.</returns>
    public static string GenerateFastIdFromCurrent(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rootElement);

        var fullId = GenerateFastFullIdFromCurrent(element, rootElement);
        return RegisterFullId(fullId);
    }

    /// <summary>
    /// Resolves a short element ID to the actual UI Automation element.
    /// </summary>
    /// <param name="elementId">The short element ID (e.g., "1", "2").</param>
    /// <returns>The UI Automation element, or null if resolution fails.</returns>
    public static UIA.IUIAutomationElement? ResolveToAutomationElement(string elementId)
    {
        ArgumentNullException.ThrowIfNull(elementId);

        // Try to resolve short ID to full ID
        var fullId = ResolveFullId(elementId) ?? elementId;

        var parts = ParseElementId(fullId);
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

    /// <summary>
    /// Generates the full internal ID for an element (not for external use).
    /// </summary>
    private static string GenerateFullId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
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
            return "window:0|runtime:0|path:stale";
        }
    }

    /// <summary>
    /// Generates the fast full ID (cached properties, no tree path).
    /// </summary>
    private static string GenerateFastFullId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        try
        {
            // Get cached window handle (fast - no COM call)
            var windowHandle = element.GetCachedNativeWindowHandle();

            // If no handle on element, use root's handle
            if (windowHandle == 0)
            {
                try
                {
                    windowHandle = rootElement.GetCachedNativeWindowHandle();
                }
                catch
                {
                    // Fall back to current property if not in cache
                    windowHandle = rootElement.GetNativeWindowHandle();
                }
            }

            // Get runtime ID - try cached first
            int[]? runtimeId = null;
            try
            {
                runtimeId = (int[]?)element.GetCachedPropertyValue(UIA3PropertyIds.RuntimeId);
            }
            catch
            {
                // Fall back to current if not cached
                runtimeId = element.GetRuntimeId();
            }

            var runtimeIdStr = runtimeId != null && runtimeId.Length > 0
                ? string.Join(".", runtimeId)
                : "0";

            // Simplified format - no tree path (expensive to calculate)
            return $"window:{windowHandle}|runtime:{runtimeIdStr}|path:cached";
        }
        catch
        {
            return "window:0|runtime:0|path:error";
        }
    }

    /// <summary>
    /// Generates the fast full ID from current properties.
    /// </summary>
    private static string GenerateFastFullIdFromCurrent(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
    {
        try
        {
            // Get window handle from current properties
            var windowHandle = element.GetNativeWindowHandle();

            // If no handle on element, use root's handle
            if (windowHandle == 0)
            {
                windowHandle = rootElement.GetNativeWindowHandle();
            }

            // Get runtime ID
            var runtimeId = element.GetRuntimeId();
            var runtimeIdStr = runtimeId != null && runtimeId.Length > 0
                ? string.Join(".", runtimeId)
                : "0";

            // Simplified format - no tree path
            return $"window:{windowHandle}|runtime:{runtimeIdStr}|path:fast";
        }
        catch
        {
            return "window:0|runtime:0|path:error";
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

    /// <summary>
    /// Registers a full element ID and returns a short ID.
    /// If the full ID is already registered, returns the existing short ID.
    /// Thread-safe with automatic deduplication.
    /// </summary>
    private static string RegisterFullId(string fullId)
    {
        ArgumentNullException.ThrowIfNull(fullId);

        // Check if already registered (common case for repeated element access)
        if (s_fullToShort.TryGetValue(fullId, out var existingShortId))
        {
            return existingShortId;
        }

        // Generate new short ID
        var shortId = Interlocked.Increment(ref s_counter).ToString();

        // Try to add to both dictionaries atomically
        // Use GetOrAdd to handle race conditions where another thread registered the same fullId
        var actualShortId = s_fullToShort.GetOrAdd(fullId, shortId);

        // Only add to shortToFull if we won the race
        if (actualShortId == shortId)
        {
            s_shortToFull[shortId] = fullId;
        }

        return actualShortId;
    }

    /// <summary>
    /// Resolves a short ID to the full element ID.
    /// </summary>
    private static string? ResolveFullId(string shortId)
    {
        ArgumentNullException.ThrowIfNull(shortId);

        return s_shortToFull.TryGetValue(shortId, out var fullId) ? fullId : null;
    }
}
