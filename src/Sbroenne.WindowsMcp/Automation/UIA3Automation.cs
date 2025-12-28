using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Provides access to the COM-based UI Automation API (IUIAutomation/UIA3).
/// This API (Windows 7+) provides better support for Chromium/Electron apps
/// compared to the managed System.Windows.Automation API (Windows XP era).
/// </summary>
/// <remarks>
/// Thread-safe singleton. The COM objects are initialized once and reused.
/// All UI Automation operations should use this class for COM access.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class UIA3Automation : IDisposable
{
    private static readonly Lazy<UIA3Automation> LazyInstance = new(() => new UIA3Automation(), LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly UIA.IUIAutomation _automation;
    private readonly UIA.IUIAutomationTreeWalker _controlViewWalker;
    private readonly UIA.IUIAutomationCondition _trueCondition;
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static UIA3Automation Instance => LazyInstance.Value;

    private UIA3Automation()
    {
        // Try CUIAutomation8 (Windows 8+) first, fall back to CUIAutomation (Windows 7+)
        try
        {
            _automation = new UIA.CUIAutomation8();
        }
        catch (COMException)
        {
            _automation = new UIA.CUIAutomation();
        }

        _controlViewWalker = _automation.ControlViewWalker;
        _trueCondition = _automation.CreateTrueCondition();
    }

    /// <summary>
    /// Gets the native IUIAutomation interface.
    /// </summary>
    public UIA.IUIAutomation Automation => _automation;

    /// <summary>
    /// Gets the control view tree walker.
    /// </summary>
    public UIA.IUIAutomationTreeWalker ControlViewWalker => _controlViewWalker;

    /// <summary>
    /// Gets a cached TRUE condition.
    /// </summary>
    public UIA.IUIAutomationCondition TrueCondition => _trueCondition;

    /// <summary>
    /// Gets the desktop root element.
    /// </summary>
    public UIA.IUIAutomationElement RootElement => _automation.GetRootElement();

    /// <summary>
    /// Gets an element from a window handle.
    /// </summary>
    public UIA.IUIAutomationElement? ElementFromHandle(nint hwnd)
    {
        try
        {
            return _automation.ElementFromHandle(hwnd);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the element at a screen point.
    /// </summary>
    public UIA.IUIAutomationElement? ElementFromPoint(int x, int y)
    {
        try
        {
            var point = new UIA.tagPOINT { x = x, y = y };
            return _automation.ElementFromPoint(point);
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the currently focused element.
    /// </summary>
    public UIA.IUIAutomationElement? GetFocusedElement()
    {
        try
        {
            return _automation.GetFocusedElement();
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a property condition.
    /// </summary>
    public UIA.IUIAutomationCondition CreatePropertyCondition(int propertyId, object value)
    {
        return _automation.CreatePropertyCondition(propertyId, value);
    }

    /// <summary>
    /// Creates a property condition with flags (e.g., case-insensitive).
    /// </summary>
    public UIA.IUIAutomationCondition CreatePropertyConditionWithFlags(int propertyId, object value, UIA.PropertyConditionFlags flags)
    {
        return _automation.CreatePropertyConditionEx(propertyId, value, flags);
    }

    /// <summary>
    /// Creates an AND condition.
    /// </summary>
    public UIA.IUIAutomationCondition CreateAndCondition(UIA.IUIAutomationCondition condition1, UIA.IUIAutomationCondition condition2)
    {
        return _automation.CreateAndCondition(condition1, condition2);
    }

    /// <summary>
    /// Creates a cache request for batch property retrieval.
    /// Using a cache request significantly reduces cross-process COM calls
    /// by fetching all needed properties in a single operation.
    /// </summary>
    public UIA.IUIAutomationCacheRequest CreateCacheRequest()
    {
        return _automation.CreateCacheRequest();
    }

    /// <summary>
    /// Creates a pre-configured cache request for tree operations.
    /// Includes all commonly needed properties for element info conversion.
    /// </summary>
    /// <param name="includeChildren">Whether to include TreeScope.Children for subtree operations.</param>
    /// <returns>A configured cache request ready for use with FindAllBuildCache.</returns>
    public UIA.IUIAutomationCacheRequest CreateTreeCacheRequest(bool includeChildren = true)
    {
        var request = _automation.CreateCacheRequest();

        // Add all properties needed for UIElementInfo conversion
        request.AddProperty(UIA3PropertyIds.Name);
        request.AddProperty(UIA3PropertyIds.AutomationId);
        request.AddProperty(UIA3PropertyIds.ControlType);
        request.AddProperty(UIA3PropertyIds.BoundingRectangle);
        request.AddProperty(UIA3PropertyIds.IsEnabled);
        request.AddProperty(UIA3PropertyIds.IsOffscreen);
        request.AddProperty(UIA3PropertyIds.FrameworkId);
        request.AddProperty(UIA3PropertyIds.ClassName);
        request.AddProperty(UIA3PropertyIds.NativeWindowHandle);
        request.AddProperty(UIA3PropertyIds.RuntimeId);

        // Set tree scope - Element for consistent behavior with FindAllBuildCache
        // Note: TreeScope affects what gets cached when retrieving the element,
        // not what FindAllBuildCache returns (that's controlled by its scope parameter)
        request.TreeScope = UIA.TreeScope.TreeScope_Element;

        // Don't set TreeFilter - let FindAllBuildCache use default (control view)
        // The condition passed to FindAllBuildCache controls filtering

        return request;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Release COM objects in reverse order of creation
        SafeRelease(_trueCondition);
        SafeRelease(_controlViewWalker);
        SafeRelease(_automation);
    }

    private static void SafeRelease(object? comObject)
    {
        if (comObject != null)
        {
            try
            {
                Marshal.ReleaseComObject(comObject);
            }
            catch
            {
                // Ignore release errors
            }
        }
    }
}

/// <summary>
/// UI Automation property IDs for UIA3.
/// </summary>
internal static class UIA3PropertyIds
{
    internal const int Name = 30005;
    internal const int AutomationId = 30011;
    internal const int ClassName = 30012;
    internal const int ControlType = 30003;
    internal const int LocalizedControlType = 30004;
    internal const int IsEnabled = 30010;
    internal const int IsOffscreen = 30022;
    internal const int BoundingRectangle = 30001;
    internal const int NativeWindowHandle = 30020;
    internal const int RuntimeId = 30000;
    internal const int FrameworkId = 30024;
    internal const int ProcessId = 30002;
    internal const int HasKeyboardFocus = 30008;
    internal const int IsKeyboardFocusable = 30009;
}

/// <summary>
/// UI Automation control type IDs for UIA3.
/// </summary>
internal static class UIA3ControlTypeIds
{
    internal const int Button = 50000;
    internal const int Calendar = 50001;
    internal const int CheckBox = 50002;
    internal const int ComboBox = 50003;
    internal const int Edit = 50004;
    internal const int Hyperlink = 50005;
    internal const int Image = 50006;
    internal const int ListItem = 50007;
    internal const int List = 50008;
    internal const int Menu = 50009;
    internal const int MenuBar = 50010;
    internal const int MenuItem = 50011;
    internal const int ProgressBar = 50012;
    internal const int RadioButton = 50013;
    internal const int ScrollBar = 50014;
    internal const int Slider = 50015;
    internal const int Spinner = 50016;
    internal const int StatusBar = 50017;
    internal const int Tab = 50018;
    internal const int TabItem = 50019;
    internal const int Text = 50020;
    internal const int ToolBar = 50021;
    internal const int ToolTip = 50022;
    internal const int Tree = 50023;
    internal const int TreeItem = 50024;
    internal const int Custom = 50025;
    internal const int Group = 50026;
    internal const int Thumb = 50027;
    internal const int DataGrid = 50028;
    internal const int DataItem = 50029;
    internal const int Document = 50030;
    internal const int SplitButton = 50031;
    internal const int Window = 50032;
    internal const int Pane = 50033;
    internal const int Header = 50034;
    internal const int HeaderItem = 50035;
    internal const int Table = 50036;
    internal const int TitleBar = 50037;
    internal const int Separator = 50038;
    internal const int SemanticZoom = 50039;
    internal const int AppBar = 50040;

    /// <summary>
    /// Gets the name from a control type ID.
    /// </summary>
    internal static string ToName(int controlTypeId)
    {
        return controlTypeId switch
        {
            Button => "Button",
            Calendar => "Calendar",
            CheckBox => "CheckBox",
            ComboBox => "ComboBox",
            Edit => "Edit",
            Hyperlink => "Hyperlink",
            Image => "Image",
            ListItem => "ListItem",
            List => "List",
            Menu => "Menu",
            MenuBar => "MenuBar",
            MenuItem => "MenuItem",
            ProgressBar => "ProgressBar",
            RadioButton => "RadioButton",
            ScrollBar => "ScrollBar",
            Slider => "Slider",
            Spinner => "Spinner",
            StatusBar => "StatusBar",
            Tab => "Tab",
            TabItem => "TabItem",
            Text => "Text",
            ToolBar => "ToolBar",
            ToolTip => "ToolTip",
            Tree => "Tree",
            TreeItem => "TreeItem",
            Custom => "Custom",
            Group => "Group",
            Thumb => "Thumb",
            DataGrid => "DataGrid",
            DataItem => "DataItem",
            Document => "Document",
            SplitButton => "SplitButton",
            Window => "Window",
            Pane => "Pane",
            Header => "Header",
            HeaderItem => "HeaderItem",
            Table => "Table",
            TitleBar => "TitleBar",
            Separator => "Separator",
            SemanticZoom => "SemanticZoom",
            AppBar => "AppBar",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// UI Automation pattern IDs for UIA3.
/// </summary>
internal static class UIA3PatternIds
{
    internal const int Invoke = 10000;
    internal const int Selection = 10001;
    internal const int Value = 10002;
    internal const int RangeValue = 10003;
    internal const int Scroll = 10004;
    internal const int ExpandCollapse = 10005;
    internal const int Grid = 10006;
    internal const int GridItem = 10007;
    internal const int MultipleView = 10008;
    internal const int Window = 10009;
    internal const int SelectionItem = 10010;
    internal const int Dock = 10011;
    internal const int Table = 10012;
    internal const int TableItem = 10013;
    internal const int Text = 10014;
    internal const int Toggle = 10015;
    internal const int Transform = 10016;
    internal const int ScrollItem = 10017;
    internal const int LegacyIAccessible = 10018;
    internal const int ItemContainer = 10019;
    internal const int VirtualizedItem = 10020;
    internal const int SynchronizedInput = 10021;
    internal const int ObjectModel = 10022;
    internal const int Annotation = 10023;
    internal const int Text2 = 10024;
    internal const int Styles = 10025;
    internal const int Spreadsheet = 10026;
    internal const int SpreadsheetItem = 10027;
    internal const int Transform2 = 10028;
    internal const int TextChild = 10029;
    internal const int Drag = 10030;
    internal const int DropTarget = 10031;
    internal const int TextEdit = 10032;
    internal const int CustomNavigation = 10033;
    internal const int Selection2 = 10034;

    /// <summary>
    /// Gets the pattern name from its ID.
    /// </summary>
    internal static string ToName(int patternId)
    {
        return patternId switch
        {
            Invoke => "Invoke",
            Selection => "Selection",
            Value => "Value",
            RangeValue => "RangeValue",
            Scroll => "Scroll",
            ExpandCollapse => "ExpandCollapse",
            Grid => "Grid",
            GridItem => "GridItem",
            MultipleView => "MultipleView",
            Window => "Window",
            SelectionItem => "SelectionItem",
            Dock => "Dock",
            Table => "Table",
            TableItem => "TableItem",
            Text => "Text",
            Toggle => "Toggle",
            Transform => "Transform",
            ScrollItem => "ScrollItem",
            LegacyIAccessible => "LegacyIAccessible",
            ItemContainer => "ItemContainer",
            VirtualizedItem => "VirtualizedItem",
            SynchronizedInput => "SynchronizedInput",
            ObjectModel => "ObjectModel",
            Annotation => "Annotation",
            Text2 => "Text2",
            Styles => "Styles",
            Spreadsheet => "Spreadsheet",
            SpreadsheetItem => "SpreadsheetItem",
            Transform2 => "Transform2",
            TextChild => "TextChild",
            Drag => "Drag",
            DropTarget => "DropTarget",
            TextEdit => "TextEdit",
            CustomNavigation => "CustomNavigation",
            Selection2 => "Selection2",
            _ => "Unknown"
        };
    }
}
