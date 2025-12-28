using System.Diagnostics;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Framework-aware search strategy for UI tree traversal.
/// Different UI frameworks have varying tree depths and structures,
/// requiring different search approaches for optimal performance.
/// </summary>
public readonly struct FrameworkStrategy
{
    /// <summary>
    /// Gets the detected UI framework name.
    /// </summary>
    public string? FrameworkName { get; init; }

    /// <summary>
    /// Gets the recommended maximum search depth for this framework.
    /// </summary>
    public int RecommendedMaxDepth { get; init; }

    /// <summary>
    /// Gets whether to use post-hoc filtering instead of inline filtering.
    /// True for Electron/Chromium apps where content is deeply nested under non-matching parents.
    /// False for WinForms/WPF where inline filtering is more efficient.
    /// </summary>
    public bool UsePostHocFiltering { get; init; }

    /// <summary>
    /// Creates a strategy for Electron/Chromium apps (deep trees, need post-hoc filtering).
    /// </summary>
    public static FrameworkStrategy Electron => new()
    {
        FrameworkName = "Chromium/Electron",
        RecommendedMaxDepth = 15,
        UsePostHocFiltering = true
    };

    /// <summary>
    /// Creates a strategy for WinForms apps (shallow trees, inline filtering OK).
    /// </summary>
    public static FrameworkStrategy WinForms => new()
    {
        FrameworkName = "WinForms",
        RecommendedMaxDepth = 5,
        UsePostHocFiltering = false
    };

    /// <summary>
    /// Creates a strategy for WPF apps (medium depth trees).
    /// </summary>
    public static FrameworkStrategy Wpf => new()
    {
        FrameworkName = "WPF",
        RecommendedMaxDepth = 10,
        UsePostHocFiltering = false
    };

    /// <summary>
    /// Creates a strategy for Win32 apps (shallow trees).
    /// </summary>
    public static FrameworkStrategy Win32 => new()
    {
        FrameworkName = "Win32",
        RecommendedMaxDepth = 5,
        UsePostHocFiltering = false
    };

    /// <summary>
    /// Creates a strategy for unknown frameworks (default to Electron behavior for safety).
    /// </summary>
    public static FrameworkStrategy Unknown => new()
    {
        FrameworkName = null,
        RecommendedMaxDepth = 15,
        UsePostHocFiltering = true
    };
}

/// <summary>
/// Helper methods for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    private static UIA.IUIAutomationElement? GetRootElement(string? windowHandle)
    {
        if (WindowHandleParser.TryParse(windowHandle, out var parsedHandle) && parsedHandle != IntPtr.Zero)
        {
            return Uia.Automation.ElementFromHandle(parsedHandle);
        }

        var foregroundWindow = GetForegroundWindowHandle();
        if (foregroundWindow != IntPtr.Zero)
        {
            return Uia.Automation.ElementFromHandle(foregroundWindow);
        }

        return null;
    }

    private static UIA.IUIAutomationElement? GetRootElementFromElementId(string? elementId)
    {
        if (string.IsNullOrEmpty(elementId))
        {
            return null;
        }

        // Try to extract window handle from element ID
        // Format: "window:{hwnd}|runtime:{id}|path:{treePath}"
        if (elementId.StartsWith("window:", StringComparison.Ordinal))
        {
            var endIdx = elementId.IndexOf('|');
            if (endIdx > 7)
            {
                var hwndStr = elementId.Substring(7, endIdx - 7);
                if (WindowHandleParser.TryParse(hwndStr, out var hwnd) && hwnd != IntPtr.Zero)
                {
                    return Uia.Automation.ElementFromHandle(hwnd);
                }
            }
        }

        return null;
    }

    private static UIA.IUIAutomationCondition BuildCondition(ElementQuery query)
    {
        var conditions = new List<UIA.IUIAutomationCondition>();

        if (!string.IsNullOrEmpty(query.Name))
        {
            conditions.Add(Uia.CreatePropertyConditionWithFlags(
                UIA3PropertyIds.Name,
                query.Name,
                UIA.PropertyConditionFlags.PropertyConditionFlags_IgnoreCase));
        }

        if (!string.IsNullOrEmpty(query.AutomationId))
        {
            conditions.Add(Uia.CreatePropertyCondition(UIA3PropertyIds.AutomationId, query.AutomationId));
        }

        if (!string.IsNullOrEmpty(query.ControlType))
        {
            var controlTypeId = GetControlTypeId(query.ControlType);
            if (controlTypeId > 0)
            {
                conditions.Add(Uia.CreatePropertyCondition(UIA3PropertyIds.ControlType, controlTypeId));
            }
        }

        if (conditions.Count == 0)
        {
            return Uia.TrueCondition;
        }

        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        // Combine with AND
        var combined = conditions[0];
        for (var i = 1; i < conditions.Count; i++)
        {
            combined = Uia.Automation.CreateAndCondition(combined, conditions[i]);
        }

        return combined;
    }

    private static int GetControlTypeId(string controlTypeName)
    {
        return controlTypeName.ToUpperInvariant() switch
        {
            "BUTTON" => UIA3ControlTypeIds.Button,
            "CALENDAR" => UIA3ControlTypeIds.Calendar,
            "CHECKBOX" => UIA3ControlTypeIds.CheckBox,
            "COMBOBOX" => UIA3ControlTypeIds.ComboBox,
            "CUSTOM" => UIA3ControlTypeIds.Custom,
            "DATAGRID" => UIA3ControlTypeIds.DataGrid,
            "DATAITEM" => UIA3ControlTypeIds.DataItem,
            "DOCUMENT" => UIA3ControlTypeIds.Document,
            "EDIT" => UIA3ControlTypeIds.Edit,
            "GROUP" => UIA3ControlTypeIds.Group,
            "HEADER" => UIA3ControlTypeIds.Header,
            "HEADERITEM" => UIA3ControlTypeIds.HeaderItem,
            "HYPERLINK" => UIA3ControlTypeIds.Hyperlink,
            "IMAGE" => UIA3ControlTypeIds.Image,
            "LIST" => UIA3ControlTypeIds.List,
            "LISTITEM" => UIA3ControlTypeIds.ListItem,
            "MENU" => UIA3ControlTypeIds.Menu,
            "MENUBAR" => UIA3ControlTypeIds.MenuBar,
            "MENUITEM" => UIA3ControlTypeIds.MenuItem,
            "PANE" => UIA3ControlTypeIds.Pane,
            "PROGRESSBAR" => UIA3ControlTypeIds.ProgressBar,
            "RADIOBUTTON" => UIA3ControlTypeIds.RadioButton,
            "SCROLLBAR" => UIA3ControlTypeIds.ScrollBar,
            "SEPARATOR" => UIA3ControlTypeIds.Separator,
            "SLIDER" => UIA3ControlTypeIds.Slider,
            "SPINNER" => UIA3ControlTypeIds.Spinner,
            "SPLITBUTTON" => UIA3ControlTypeIds.SplitButton,
            "STATUSBAR" => UIA3ControlTypeIds.StatusBar,
            "TAB" => UIA3ControlTypeIds.Tab,
            "TABITEM" => UIA3ControlTypeIds.TabItem,
            "TABLE" => UIA3ControlTypeIds.Table,
            "TEXT" => UIA3ControlTypeIds.Text,
            "THUMB" => UIA3ControlTypeIds.Thumb,
            "TITLEBAR" => UIA3ControlTypeIds.TitleBar,
            "TOOLBAR" => UIA3ControlTypeIds.ToolBar,
            "TOOLTIP" => UIA3ControlTypeIds.ToolTip,
            "TREE" => UIA3ControlTypeIds.Tree,
            "TREEITEM" => UIA3ControlTypeIds.TreeItem,
            "WINDOW" => UIA3ControlTypeIds.Window,
            _ => 0
        };
    }

    internal static UIElementInfo? ConvertToElementInfo(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        CoordinateConverter coordinateConverter,
        UIElementInfo[]? children = null)
    {
        try
        {
            var rect = element.CurrentBoundingRectangle;
            var boundingRect = new BoundingRect
            {
                X = rect.left,
                Y = rect.top,
                Width = rect.right - rect.left,
                Height = rect.bottom - rect.top
            };

            // Get monitor-relative rect and clickable point
            var (monitorRelativeRect, monitorIndex) = coordinateConverter.ToMonitorRelative(boundingRect);

            // Use fast element ID using RuntimeId (resolution primarily uses RuntimeId anyway)
            var elementId = GenerateFastElementIdFromCurrent(element, rootElement);

            var info = new UIElementInfo
            {
                ElementId = elementId,
                Name = element.GetName(),
                AutomationId = element.GetAutomationId(),
                ControlType = element.GetControlTypeName(),
                BoundingRect = boundingRect,
                MonitorRelativeRect = monitorRelativeRect,
                MonitorIndex = monitorIndex,
                ClickablePoint = ClickablePoint.FromCenter(monitorRelativeRect, monitorIndex),
                SupportedPatterns = element.GetSupportedPatternNames(),
                Value = element.TryGetValue(),
                ToggleState = element.GetToggleState(),
                IsEnabled = element.CurrentIsEnabled != 0,
                IsOffscreen = element.CurrentIsOffscreen != 0,
                Children = children
            };

            return info;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a UIA element to UIElementInfo using cached properties.
    /// This is significantly faster than ConvertToElementInfo as it uses
    /// pre-fetched property values instead of making individual COM calls.
    /// </summary>
    /// <param name="element">The element with cached properties.</param>
    /// <param name="rootElement">The root element for generating element IDs.</param>
    /// <param name="coordinateConverter">Coordinate converter for monitor-relative positions.</param>
    /// <param name="children">Optional child elements.</param>
    /// <param name="skipPatterns">If true, skips pattern detection (expensive) for tree operations.</param>
    /// <returns>The element info, or null if conversion fails.</returns>
    internal static UIElementInfo? ConvertToElementInfoFromCache(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        CoordinateConverter coordinateConverter,
        UIElementInfo[]? children = null,
        bool skipPatterns = true)
    {
        try
        {
            var rect = element.CachedBoundingRectangle;
            var boundingRect = new BoundingRect
            {
                X = rect.left,
                Y = rect.top,
                Width = rect.right - rect.left,
                Height = rect.bottom - rect.top
            };

            // Get monitor-relative rect and clickable point
            var (monitorRelativeRect, monitorIndex) = coordinateConverter.ToMonitorRelative(boundingRect);

            // Use cached element ID generation - much faster than full ElementIdGenerator
            // Just use RuntimeId since it's already cached and uniquely identifies the element
            var elementId = GenerateFastElementId(element, rootElement);

            var info = new UIElementInfo
            {
                ElementId = elementId,
                Name = element.GetCachedName(),
                AutomationId = element.GetCachedAutomationId(),
                ControlType = element.GetCachedControlTypeName(),
                BoundingRect = boundingRect,
                MonitorRelativeRect = monitorRelativeRect,
                MonitorIndex = monitorIndex,
                ClickablePoint = ClickablePoint.FromCenter(monitorRelativeRect, monitorIndex),
                // Skip expensive pattern detection for tree operations - patterns are only
                // needed when performing actions, not for discovery/navigation
                SupportedPatterns = skipPatterns ? [] : element.GetSupportedPatternNames(),
                Value = skipPatterns ? null : element.TryGetValue(),
                ToggleState = skipPatterns ? null : element.GetToggleState(),
                IsEnabled = element.GetCachedIsEnabled(),
                IsOffscreen = element.GetCachedIsOffscreen(),
                Children = children
            };

            return info;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a fast element ID using Current properties (not cached).
    /// This avoids the expensive tree path calculation of ElementIdGenerator.
    /// Format: "window:{hwnd}|runtime:{id}|path:fast"
    /// </summary>
    private static string GenerateFastElementIdFromCurrent(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
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

            // Simplified format - no tree path (expensive to calculate)
            // Resolution primarily uses RuntimeId anyway
            return $"window:{windowHandle}|runtime:{runtimeIdStr}|path:fast";
        }
        catch
        {
            return "window:0|runtime:0|path:error";
        }
    }

    /// <summary>
    /// Generates a fast element ID using only cached properties.
    /// This avoids the expensive tree path calculation of ElementIdGenerator.
    /// Format: "window:{hwnd}|runtime:{id}"
    /// </summary>
    private static string GenerateFastElementId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement)
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

    private UIElementInfo[]? GetChildren(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement, int maxChildren = 100)
    {
        var children = new List<UIElementInfo>();
        var walker = Uia.ControlViewWalker;
        var child = walker.GetFirstChildElement(element);
        var count = 0;

        while (child != null && count < maxChildren)
        {
            try
            {
                var childInfo = ConvertToElementInfo(child, rootElement, _coordinateConverter);
                if (childInfo != null)
                {
                    children.Add(childInfo);
                }

                count++;
                child = walker.GetNextSiblingElement(child);
            }
            catch
            {
                break;
            }
        }

        return children.Count > 0 ? [.. children] : null;
    }

    private static UIAutomationDiagnostics CreateDiagnostics(Stopwatch stopwatch)
    {
        return new UIAutomationDiagnostics
        {
            DurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    private static UIAutomationDiagnostics CreateDiagnostics(Stopwatch stopwatch, ElementQuery? query)
    {
        return new UIAutomationDiagnostics
        {
            DurationMs = stopwatch.ElapsedMilliseconds,
            Query = query
        };
    }

    private static UIAutomationDiagnostics CreateDiagnosticsWithContext(
        Stopwatch stopwatch,
        UIA.IUIAutomationElement rootElement,
        ElementQuery? query,
        int elementsScanned,
        string? windowTitle,
        string? windowHandle)
    {
        return new UIAutomationDiagnostics
        {
            DurationMs = stopwatch.ElapsedMilliseconds,
            Query = query,
            ElementsScanned = elementsScanned,
            WindowTitle = windowTitle,
            WindowHandle = windowHandle,
            DetectedFramework = DetectFramework(rootElement)
        };
    }

    /// <summary>
    /// Detects the UI framework of the given element.
    /// </summary>
    private static string? DetectFramework(UIA.IUIAutomationElement element)
    {
        try
        {
            var frameworkId = element.GetFrameworkId();
            var className = element.GetClassName();

            // Check for Chromium-based apps (Electron, Chrome, Edge, etc.)
            if (className?.StartsWith("Chrome", StringComparison.OrdinalIgnoreCase) == true ||
                frameworkId == "Chrome")
            {
                return "Chromium/Electron";
            }

            // If we have Win32 framework but Chrome class, it's Chromium
            if (frameworkId == "Win32" && className != null)
            {
                // Also check children for Chrome framework
                var walker = UIA3Automation.Instance.ControlViewWalker;
                var child = walker.GetFirstChildElement(element);
                while (child != null)
                {
                    try
                    {
                        var childClassName = child.GetClassName();
                        var childFramework = child.GetFrameworkId();
                        if (childClassName?.StartsWith("Chrome", StringComparison.OrdinalIgnoreCase) == true ||
                            childFramework == "Chrome")
                        {
                            return "Chromium/Electron";
                        }
                        child = walker.GetNextSiblingElement(child);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            return frameworkId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the appropriate search strategy for the detected UI framework.
    /// </summary>
    /// <param name="element">The root element to detect framework from.</param>
    /// <returns>A framework strategy with optimal search parameters.</returns>
    internal static FrameworkStrategy GetFrameworkStrategy(UIA.IUIAutomationElement element)
    {
        var framework = DetectFramework(element);

        return framework switch
        {
            "Chromium/Electron" => FrameworkStrategy.Electron,
            "WinForm" or "WindowsForms" => FrameworkStrategy.WinForms,
            "WPF" => FrameworkStrategy.Wpf,
            "Win32" => FrameworkStrategy.Win32,
            "XAML" or "UWP" or "WinUI" => FrameworkStrategy.Wpf, // Use WPF-like strategy for modern XAML
            "Qt" => FrameworkStrategy.Win32, // Qt uses Win32-like shallow trees
            _ => FrameworkStrategy.Unknown // Default to Electron approach for safety
        };
    }

    private UIAutomationResult CheckElevatedTarget(UIA.IUIAutomationElement element)
    {
        try
        {
            var rect = element.CurrentBoundingRectangle;
            var centerX = rect.left + (rect.right - rect.left) / 2;
            var centerY = rect.top + (rect.bottom - rect.top) / 2;

            if (_elevationDetector.IsTargetElevated(centerX, centerY))
            {
                LogElevatedTargetWarning(_logger, centerX, centerY);
                return UIAutomationResult.CreateFailure(
                    "",
                    UIAutomationErrorType.ElevatedTarget,
                    "Target window is running elevated. Restart VS Code as Administrator to interact with it.",
                    null);
            }
        }
        catch
        {
            // Best effort
        }

        return new UIAutomationResult
        {
            Success = true,
            Action = ""
        };
    }
}
