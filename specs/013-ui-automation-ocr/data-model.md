# Data Model: Windows UI Automation & OCR

**Feature**: 013-ui-automation-ocr  
**Date**: 2024-12-23  
**Status**: Complete

---

## Entities

### UIElementInfo

Represents a UI Automation element returned to the LLM.

```csharp
/// <summary>
/// Represents a UI element discovered via Windows UI Automation.
/// </summary>
public sealed record UIElementInfo
{
    /// <summary>
    /// Composite identifier for subsequent operations.
    /// Format: "window:{hwnd}|runtime:{id}|path:{treePath}"
    /// </summary>
    public required string ElementId { get; init; }
    
    /// <summary>
    /// Developer-assigned automation ID (may be null).
    /// </summary>
    public string? AutomationId { get; init; }
    
    /// <summary>
    /// Human-readable name from accessibility tree.
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Control type (Button, Edit, Text, List, etc.).
    /// </summary>
    public required string ControlType { get; init; }
    
    /// <summary>
    /// Bounding rectangle in screen coordinates.
    /// </summary>
    public required BoundingRect BoundingRect { get; init; }
    
    /// <summary>
    /// Monitor-relative coordinates for use with mouse_control.
    /// </summary>
    public required MonitorRelativeRect MonitorRelativeRect { get; init; }
    
    /// <summary>
    /// Monitor index containing this element.
    /// </summary>
    public required int MonitorIndex { get; init; }
    
    /// <summary>
    /// Supported UI Automation patterns (Invoke, Toggle, Value, etc.).
    /// </summary>
    public required string[] SupportedPatterns { get; init; }
    
    /// <summary>
    /// Current value for elements with ValuePattern (text fields, etc.).
    /// </summary>
    public string? Value { get; init; }
    
    /// <summary>
    /// Current toggle state for elements with TogglePattern.
    /// </summary>
    public string? ToggleState { get; init; }
    
    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    public required bool IsEnabled { get; init; }
    
    /// <summary>
    /// Whether the element is currently visible on screen.
    /// </summary>
    public required bool IsOffscreen { get; init; }
    
    /// <summary>
    /// Child elements (only populated when hierarchy requested).
    /// </summary>
    public UIElementInfo[]? Children { get; init; }
}
```

### BoundingRect

Screen-coordinate bounding rectangle.

```csharp
/// <summary>
/// Represents a bounding rectangle in screen coordinates.
/// </summary>
public sealed record BoundingRect
{
    /// <summary>Screen X coordinate (left edge).</summary>
    public required int X { get; init; }
    
    /// <summary>Screen Y coordinate (top edge).</summary>
    public required int Y { get; init; }
    
    /// <summary>Width in pixels.</summary>
    public required int Width { get; init; }
    
    /// <summary>Height in pixels.</summary>
    public required int Height { get; init; }
    
    /// <summary>Center X coordinate (for clicking).</summary>
    public int CenterX => X + Width / 2;
    
    /// <summary>Center Y coordinate (for clicking).</summary>
    public int CenterY => Y + Height / 2;
}
```

### MonitorRelativeRect

Monitor-relative coordinates for direct use with mouse_control.

```csharp
/// <summary>
/// Bounding rectangle relative to monitor origin (for mouse_control).
/// </summary>
public sealed record MonitorRelativeRect
{
    /// <summary>X coordinate relative to monitor left edge.</summary>
    public required int X { get; init; }
    
    /// <summary>Y coordinate relative to monitor top edge.</summary>
    public required int Y { get; init; }
    
    /// <summary>Width in pixels.</summary>
    public required int Width { get; init; }
    
    /// <summary>Height in pixels.</summary>
    public required int Height { get; init; }
    
    /// <summary>Center X for clicking.</summary>
    public int CenterX => X + Width / 2;
    
    /// <summary>Center Y for clicking.</summary>
    public int CenterY => Y + Height / 2;
}
```

### ElementQuery

Search criteria for finding UI elements.

```csharp
/// <summary>
/// Represents search criteria for finding UI elements.
/// </summary>
public sealed record ElementQuery
{
    /// <summary>
    /// Element name to search for (partial match supported).
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Control type filter (Button, Edit, Text, List, MenuItem, etc.).
    /// </summary>
    public string? ControlType { get; init; }
    
    /// <summary>
    /// Automation ID for precise matching.
    /// </summary>
    public string? AutomationId { get; init; }
    
    /// <summary>
    /// Parent element ID to search within.
    /// </summary>
    public string? ParentElementId { get; init; }
    
    /// <summary>
    /// Window handle to search within.
    /// </summary>
    public nint? WindowHandle { get; init; }
    
    /// <summary>
    /// Maximum depth to search (0 = immediate children only).
    /// </summary>
    public int? MaxDepth { get; init; }
    
    /// <summary>
    /// Whether to include children in results.
    /// </summary>
    public bool IncludeChildren { get; init; }
}
```

### UIAutomationResult

Result from UI Automation tool operations.

```csharp
/// <summary>
/// Result from a UI Automation tool operation.
/// </summary>
public sealed record UIAutomationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public required bool Success { get; init; }
    
    /// <summary>Action that was performed.</summary>
    public required string Action { get; init; }
    
    /// <summary>Single element result (for find, wait_for, invoke, etc.).</summary>
    public UIElementInfo? Element { get; init; }
    
    /// <summary>Multiple element results (for find_all, get_tree).</summary>
    public UIElementInfo[]? Elements { get; init; }
    
    /// <summary>Number of elements found.</summary>
    public int? ElementCount { get; init; }
    
    /// <summary>Text content (for get_text action).</summary>
    public string? Text { get; init; }
    
    /// <summary>Error type if failed.</summary>
    public string? ErrorType { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Diagnostic info for debugging.</summary>
    public UIAutomationDiagnostics? Diagnostics { get; init; }
}
```

### UIAutomationDiagnostics

Diagnostic information for troubleshooting.

```csharp
/// <summary>
/// Diagnostic information for UI Automation operations.
/// </summary>
public sealed record UIAutomationDiagnostics
{
    /// <summary>Operation duration in milliseconds.</summary>
    public required long DurationMs { get; init; }
    
    /// <summary>Window that was searched.</summary>
    public string? WindowTitle { get; init; }
    
    /// <summary>Window handle.</summary>
    public nint? WindowHandle { get; init; }
    
    /// <summary>Query that was used.</summary>
    public ElementQuery? Query { get; init; }
    
    /// <summary>Number of elements scanned.</summary>
    public int? ElementsScanned { get; init; }
    
    /// <summary>Elapsed time before timeout (for wait_for).</summary>
    public long? ElapsedBeforeTimeout { get; init; }
    
    /// <summary>Multiple matches when exactly one expected.</summary>
    public UIElementInfo[]? MultipleMatches { get; init; }
}
```

---

## OCR Entities

### OcrResult

Result from OCR text recognition.

```csharp
/// <summary>
/// Result from OCR text recognition.
/// </summary>
public sealed record OcrResult
{
    /// <summary>Whether OCR succeeded.</summary>
    public required bool Success { get; init; }
    
    /// <summary>Full recognized text (all lines joined).</summary>
    public string? Text { get; init; }
    
    /// <summary>Individual text lines with bounding boxes.</summary>
    public OcrLine[]? Lines { get; init; }
    
    /// <summary>OCR engine used ("Legacy" for Windows.Media.Ocr).</summary>
    public required string Engine { get; init; }
    
    /// <summary>Language used for recognition.</summary>
    public required string Language { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Processing time in milliseconds.</summary>
    public required long DurationMs { get; init; }
}
```

### OcrLine

A line of recognized text.

```csharp
/// <summary>
/// A line of recognized text with bounding box.
/// </summary>
public sealed record OcrLine
{
    /// <summary>Full text of the line.</summary>
    public required string Text { get; init; }
    
    /// <summary>Bounding box of the line.</summary>
    public required BoundingRect BoundingRect { get; init; }
    
    /// <summary>Individual words in the line.</summary>
    public required OcrWord[] Words { get; init; }
}
```

### OcrWord

A single recognized word.

```csharp
/// <summary>
/// A single recognized word with confidence.
/// </summary>
public sealed record OcrWord
{
    /// <summary>Recognized text.</summary>
    public required string Text { get; init; }
    
    /// <summary>Bounding box of the word.</summary>
    public required BoundingRect BoundingRect { get; init; }
    
    /// <summary>Confidence score (0.0 to 1.0).</summary>
    public required double Confidence { get; init; }
}
```

---

## Enumerations

### ControlType (Common Values)

```csharp
/// <summary>
/// Common UI Automation control types.
/// </summary>
public static class ControlTypes
{
    public const string Button = "Button";
    public const string Edit = "Edit";
    public const string Text = "Text";
    public const string List = "List";
    public const string ListItem = "ListItem";
    public const string Tree = "Tree";
    public const string TreeItem = "TreeItem";
    public const string Menu = "Menu";
    public const string MenuItem = "MenuItem";
    public const string ComboBox = "ComboBox";
    public const string CheckBox = "CheckBox";
    public const string RadioButton = "RadioButton";
    public const string Tab = "Tab";
    public const string TabItem = "TabItem";
    public const string Window = "Window";
    public const string Pane = "Pane";
    public const string Document = "Document";
    public const string Hyperlink = "Hyperlink";
    public const string Image = "Image";
    public const string ProgressBar = "ProgressBar";
    public const string Slider = "Slider";
    public const string Spinner = "Spinner";
    public const string StatusBar = "StatusBar";
    public const string ToolBar = "ToolBar";
    public const string ToolTip = "ToolTip";
    public const string Group = "Group";
    public const string ScrollBar = "ScrollBar";
    public const string DataGrid = "DataGrid";
    public const string DataItem = "DataItem";
    public const string Custom = "Custom";
}
```

### PatternType (Supported Patterns)

```csharp
/// <summary>
/// Supported UI Automation patterns for interaction.
/// </summary>
public static class PatternTypes
{
    public const string Invoke = "Invoke";           // Click/activate
    public const string Toggle = "Toggle";           // Checkbox/toggle
    public const string Value = "Value";             // Get/set text
    public const string Selection = "Selection";    // Multi-select
    public const string SelectionItem = "SelectionItem"; // Single select
    public const string ExpandCollapse = "ExpandCollapse"; // Tree/dropdown
    public const string Scroll = "Scroll";           // Scrollable container
    public const string ScrollItem = "ScrollItem";   // Scroll into view
    public const string Text = "Text";               // Rich text
    public const string RangeValue = "RangeValue";   // Slider/spinner
    public const string Window = "Window";           // Window controls
    public const string Transform = "Transform";    // Move/resize
}
```

### UIAutomationErrorType

```csharp
/// <summary>
/// Error types for UI Automation operations.
/// </summary>
public static class UIAutomationErrorType
{
    public const string ElementNotFound = "element_not_found";
    public const string Timeout = "timeout";
    public const string MultipleMatches = "multiple_matches";
    public const string PatternNotSupported = "pattern_not_supported";
    public const string ElementStale = "element_stale";
    public const string ElevatedTarget = "elevated_target";
    public const string InvalidParameter = "invalid_parameter";
    public const string ScrollExhausted = "scroll_exhausted";
    public const string WindowNotFound = "window_not_found";
    public const string InternalError = "internal_error";
}
```

---

## State Transitions

### Element Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                      Element Lifecycle                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐              │
│  │ Unknown  │─────►│ Queried  │─────►│  Active  │              │
│  └──────────┘ find └──────────┘ cache└──────────┘              │
│                         │                 │                     │
│                         │                 │ invoke/             │
│                         │                 │ toggle/             │
│                         │                 │ scroll              │
│                         │                 ▼                     │
│                         │            ┌──────────┐              │
│                         │            │ Modified │              │
│                         │            └──────────┘              │
│                         │                 │                     │
│                         │     disappears  │                     │
│                         ▼                 ▼                     │
│                    ┌──────────┐                                │
│                    │  Stale   │ ◄── Element no longer valid    │
│                    └──────────┘                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Operation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    find_and_click Flow                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐              │
│  │  Query   │─────►│  Found   │─────►│ Activate │              │
│  └──────────┘      └──────────┘      │  Window  │              │
│       │                  │           └──────────┘              │
│       │ not found        │ multiple       │                     │
│       ▼                  ▼                ▼                     │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐              │
│  │  Error:  │      │  Error:  │      │  Invoke  │──► Success   │
│  │ NotFound │      │ Multiple │      │ Pattern  │              │
│  └──────────┘      └──────────┘      └──────────┘              │
│                                           │                     │
│                              no pattern   │                     │
│                                           ▼                     │
│                                      ┌──────────┐              │
│                                      │  Click   │──► Success   │
│                                      │  Center  │              │
│                                      └──────────┘              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Validation Rules

### ElementQuery Validation

| Field | Rule |
|-------|------|
| Name | At least one of: Name, ControlType, or AutomationId must be specified |
| ControlType | Must be valid control type string (see ControlTypes) |
| WindowHandle | If provided, must be valid window handle (> 0) |
| MaxDepth | If provided, must be >= 0 |

### Timeout Validation

| Parameter | Default | Min | Max |
|-----------|---------|-----|-----|
| timeout_ms | 5000 | 100 | 60000 |
| retry_interval_ms | 50 (start) | 10 | 1000 |

---

## Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                     Entity Relationships                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ElementQuery ─────────────────► UIElementInfo (0..*)           │
│       │                              │                          │
│       │ optional                     │ 1                        │
│       ▼                              ▼                          │
│  WindowHandle               BoundingRect + MonitorRelativeRect  │
│                                      │                          │
│                                      │ 1                        │
│                                      ▼                          │
│                               UIAutomationResult                │
│                                      │                          │
│                                      │ 0..1                     │
│                                      ▼                          │
│                           UIAutomationDiagnostics               │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  OcrResult ────────────────────► OcrLine (0..*)                 │
│                                      │                          │
│                                      │ 1..*                     │
│                                      ▼                          │
│                                  OcrWord                        │
│                                      │                          │
│                                      │ 1                        │
│                                      ▼                          │
│                               BoundingRect                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
