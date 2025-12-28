using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Extension methods for working with UIA3 COM objects.
/// </summary>
[SupportedOSPlatform("windows")]
public static class UIA3Extensions
{
    #region Cached Property Access

    /// <summary>
    /// Gets the element name from cache.
    /// </summary>
    public static string? GetCachedName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedName;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the element automation ID from cache.
    /// </summary>
    public static string? GetCachedAutomationId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedAutomationId;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the cached control type ID.
    /// </summary>
    public static int GetCachedControlTypeId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedControlType;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the cached control type name.
    /// </summary>
    public static string GetCachedControlTypeName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return UIA3ControlTypeIds.ToName(element.GetCachedControlTypeId());
    }

    /// <summary>
    /// Gets the cached bounding rectangle.
    /// </summary>
    public static (double X, double Y, double Width, double Height) GetCachedBoundingRectangle(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var rect = element.CachedBoundingRectangle;
            return (rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
        catch (COMException)
        {
            return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Gets the cached IsEnabled state.
    /// </summary>
    public static bool GetCachedIsEnabled(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedIsEnabled != 0;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the cached IsOffscreen state.
    /// </summary>
    public static bool GetCachedIsOffscreen(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedIsOffscreen != 0;
        }
        catch (COMException)
        {
            return true;
        }
    }

    /// <summary>
    /// Gets the cached framework ID.
    /// </summary>
    public static string? GetCachedFrameworkId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedFrameworkId;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the cached class name.
    /// </summary>
    public static string? GetCachedClassName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedClassName;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the cached native window handle.
    /// </summary>
    public static nint GetCachedNativeWindowHandle(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CachedNativeWindowHandle;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the cached children elements.
    /// </summary>
    public static UIA.IUIAutomationElementArray? GetCachedChildren(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.GetCachedChildren();
        }
        catch (COMException)
        {
            return null;
        }
    }

    #endregion

    #region Current Property Access

    /// <summary>
    /// Gets the element name safely.
    /// </summary>
    public static string? GetName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentName;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the element automation ID safely.
    /// </summary>
    public static string? GetAutomationId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentAutomationId;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the element class name safely.
    /// </summary>
    public static string? GetClassName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentClassName;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the control type ID.
    /// </summary>
    public static int GetControlTypeId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentControlType;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the control type name.
    /// </summary>
    public static string GetControlTypeName(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return UIA3ControlTypeIds.ToName(element.GetControlTypeId());
    }

    /// <summary>
    /// Gets the native window handle.
    /// </summary>
    public static nint GetNativeWindowHandle(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentNativeWindowHandle;
        }
        catch (COMException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets whether the element is enabled.
    /// </summary>
    public static bool IsEnabled(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentIsEnabled != 0;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets whether the element is offscreen.
    /// </summary>
    public static bool IsOffscreen(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentIsOffscreen != 0;
        }
        catch (COMException)
        {
            return true;
        }
    }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    public static (double X, double Y, double Width, double Height) GetBoundingRectangle(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var rect = element.CurrentBoundingRectangle;
            return (rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
        catch (COMException)
        {
            return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Gets the runtime ID as an array.
    /// </summary>
    public static int[]? GetRuntimeId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.GetRuntimeId();
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the framework ID.
    /// </summary>
    public static string? GetFrameworkId(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return element.CurrentFrameworkId;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sets focus on the element.
    /// </summary>
    public static bool TrySetFocus(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            element.SetFocus();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a pattern from the element.
    /// </summary>
    public static T? GetPattern<T>(this UIA.IUIAutomationElement element, int patternId) where T : class
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetCurrentPattern(patternId);
            return pattern as T;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the element supports a pattern.
    /// </summary>
    public static bool SupportsPattern(this UIA.IUIAutomationElement element, int patternId)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetCurrentPattern(patternId);
            return pattern != null;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all supported pattern IDs by checking each known pattern.
    /// </summary>
    public static int[] GetSupportedPatternIds(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var supportedPatterns = new List<int>();

        // Check each known pattern ID
        int[] allPatternIds =
        [
            UIA3PatternIds.Invoke,
            UIA3PatternIds.Value,
            UIA3PatternIds.Toggle,
            UIA3PatternIds.ExpandCollapse,
            UIA3PatternIds.Selection,
            UIA3PatternIds.SelectionItem,
            UIA3PatternIds.Scroll,
            UIA3PatternIds.ScrollItem,
            UIA3PatternIds.RangeValue,
            UIA3PatternIds.Text,
            UIA3PatternIds.Window,
            UIA3PatternIds.Transform,
            UIA3PatternIds.Grid,
            UIA3PatternIds.GridItem,
            UIA3PatternIds.Table,
            UIA3PatternIds.TableItem,
            UIA3PatternIds.Dock,
            UIA3PatternIds.MultipleView,
            UIA3PatternIds.LegacyIAccessible,
            UIA3PatternIds.ItemContainer,
            UIA3PatternIds.VirtualizedItem,
            UIA3PatternIds.SynchronizedInput,
            UIA3PatternIds.Annotation,
            UIA3PatternIds.Text2,
            UIA3PatternIds.Styles,
            UIA3PatternIds.Spreadsheet,
            UIA3PatternIds.SpreadsheetItem,
            UIA3PatternIds.Transform2,
            UIA3PatternIds.TextChild,
            UIA3PatternIds.Drag,
            UIA3PatternIds.DropTarget,
            UIA3PatternIds.TextEdit,
            UIA3PatternIds.CustomNavigation,
            UIA3PatternIds.Selection2,
        ];

        foreach (var patternId in allPatternIds)
        {
            if (element.SupportsPattern(patternId))
            {
                supportedPatterns.Add(patternId);
            }
        }

        return [.. supportedPatterns];
    }

    /// <summary>
    /// Gets all supported pattern names.
    /// </summary>
    public static string[] GetSupportedPatternNames(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var ids = element.GetSupportedPatternIds();
        return ids.Select(UIA3PatternIds.ToName).ToArray();
    }

    /// <summary>
    /// Finds the first matching element.
    /// </summary>
    public static UIA.IUIAutomationElement? FindFirst(this UIA.IUIAutomationElement element, UIA.TreeScope scope, UIA.IUIAutomationCondition condition)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(condition);
        try
        {
            return element.FindFirst(scope, condition);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Finds all matching elements.
    /// </summary>
    public static UIA.IUIAutomationElementArray? FindAll(this UIA.IUIAutomationElement element, UIA.TreeScope scope, UIA.IUIAutomationCondition condition)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(condition);
        try
        {
            return element.FindAll(scope, condition);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the parent element using control view.
    /// </summary>
    public static UIA.IUIAutomationElement? GetParent(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return UIA3Automation.Instance.ControlViewWalker.GetParentElement(element);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the first child element using control view.
    /// </summary>
    public static UIA.IUIAutomationElement? GetFirstChild(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return UIA3Automation.Instance.ControlViewWalker.GetFirstChildElement(element);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the next sibling element using control view.
    /// </summary>
    public static UIA.IUIAutomationElement? GetNextSibling(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            return UIA3Automation.Instance.ControlViewWalker.GetNextSiblingElement(element);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts element array to a list.
    /// </summary>
    public static List<UIA.IUIAutomationElement> ToList(this UIA.IUIAutomationElementArray? array)
    {
        var result = new List<UIA.IUIAutomationElement>();
        if (array == null)
        {
            return result;
        }

        try
        {
            for (var i = 0; i < array.Length; i++)
            {
                var element = array.GetElement(i);
                if (element != null)
                {
                    result.Add(element);
                }
            }
        }
        catch (COMException)
        {
            // Ignore errors during enumeration
        }

        return result;
    }

    /// <summary>
    /// Tries to get the Value pattern's current value.
    /// </summary>
    public static string? TryGetValue(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value);
            return pattern?.CurrentValue;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tries to set the Value pattern's value.
    /// </summary>
    public static bool TrySetValue(this UIA.IUIAutomationElement element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value);
            if (pattern == null || pattern.CurrentIsReadOnly != 0)
            {
                return false;
            }

            pattern.SetValue(value);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to invoke the element.
    /// </summary>
    public static bool TryInvoke(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationInvokePattern>(UIA3PatternIds.Invoke);
            if (pattern == null)
            {
                return false;
            }

            pattern.Invoke();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to toggle the element.
    /// </summary>
    public static bool TryToggle(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationTogglePattern>(UIA3PatternIds.Toggle);
            if (pattern == null)
            {
                return false;
            }

            pattern.Toggle();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the toggle state.
    /// </summary>
    public static string? GetToggleState(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationTogglePattern>(UIA3PatternIds.Toggle);
            if (pattern == null)
            {
                return null;
            }

            return pattern.CurrentToggleState switch
            {
                UIA.ToggleState.ToggleState_Off => "Off",
                UIA.ToggleState.ToggleState_On => "On",
                UIA.ToggleState.ToggleState_Indeterminate => "Indeterminate",
                _ => null
            };
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tries to expand the element.
    /// </summary>
    public static bool TryExpand(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationExpandCollapsePattern>(UIA3PatternIds.ExpandCollapse);
            if (pattern == null)
            {
                return false;
            }

            pattern.Expand();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to collapse the element.
    /// </summary>
    public static bool TryCollapse(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationExpandCollapsePattern>(UIA3PatternIds.ExpandCollapse);
            if (pattern == null)
            {
                return false;
            }

            pattern.Collapse();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to select the element (for selection items).
    /// </summary>
    public static bool TrySelect(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationSelectionItemPattern>(UIA3PatternIds.SelectionItem);
            if (pattern == null)
            {
                return false;
            }

            pattern.Select();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to scroll the element into view.
    /// </summary>
    public static bool TryScrollIntoView(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationScrollItemPattern>(UIA3PatternIds.ScrollItem);
            if (pattern == null)
            {
                return false;
            }

            pattern.ScrollIntoView();
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets text from TextPattern or Value pattern.
    /// </summary>
    public static string? GetText(this UIA.IUIAutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        try
        {
            // Try TextPattern first
            var textPattern = element.GetPattern<UIA.IUIAutomationTextPattern>(UIA3PatternIds.Text);
            if (textPattern != null)
            {
                var range = textPattern.DocumentRange;
                return range?.GetText(-1);
            }

            // Fall back to ValuePattern
            var valuePattern = element.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value);
            if (valuePattern != null)
            {
                return valuePattern.CurrentValue;
            }

            // Fall back to Name
            return element.GetName();
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Compares two elements by runtime ID.
    /// </summary>
    public static bool IsSameElement(this UIA.IUIAutomationElement element, UIA.IUIAutomationElement? other)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (other == null)
        {
            return false;
        }

        try
        {
            var id1 = element.GetRuntimeId();
            var id2 = other.GetRuntimeId();

            if (id1 == null || id2 == null)
            {
                return false;
            }

            return id1.SequenceEqual(id2);
        }
        catch (COMException)
        {
            return false;
        }
    }

    #endregion
}
