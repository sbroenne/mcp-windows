using System.Diagnostics;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Helper methods for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    private static UIA.IUIAutomationElement? GetRootElement(nint? windowHandle)
    {
        if (windowHandle.HasValue && windowHandle.Value != IntPtr.Zero)
        {
            return Uia.Automation.ElementFromHandle(windowHandle.Value);
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
                if (nint.TryParse(hwndStr, out var hwnd) && hwnd != IntPtr.Zero)
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

            var info = new UIElementInfo
            {
                ElementId = ElementIdGenerator.GenerateId(element, rootElement),
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
        nint? windowHandle)
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
