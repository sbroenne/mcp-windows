# Research: Windows UI Automation & OCR

**Feature**: 013-ui-automation-ocr  
**Date**: 2024-12-23  
**Status**: Complete

---

## Research Tasks

This document resolves all "NEEDS CLARIFICATION" items and captures best practices for implementation.

---

## 1. UI Automation API Selection

### Decision: Use `System.Windows.Automation` (UIAutomationClient.dll)

### Rationale
- Native .NET support in both .NET Framework and .NET 6+
- Full access to Windows accessibility tree
- Direct support for all control patterns (Invoke, Toggle, Value, Scroll, Selection, ExpandCollapse)
- DPI-aware bounding rectangles
- Works with Electron apps via Chromium accessibility bridge

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| `Windows.UI.UIAutomation` (WinRT) | Less mature, requires WinRT interop, no significant advantages |
| `IAccessible` (MSAA) | Legacy API, less information available, deprecated |
| Third-party (FlaUI) | Adds external dependency; constitution requires OSI license verification; System.Windows.Automation is sufficient |

### References
- [UI Automation Overview](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-overview)
- [UI Automation Control Patterns](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-control-patterns-overview)

---

## 2. OCR API Selection

### Decision: Dual-API Strategy with Runtime Detection

### Rationale
1. **Primary (when available)**: `Microsoft.Windows.AI.Imaging.TextRecognizer`
   - NPU-accelerated, more accurate
   - Requires Windows 11 24H2+ with NPU
   - Per-word confidence scores

2. **Fallback**: `Windows.Media.Ocr.OcrEngine`
   - Available on Windows 10 build 1809+
   - CPU-based, still effective for UI text
   - Broader compatibility

### Implementation Strategy
```csharp
// At startup or first OCR request:
if (IsWindows11_24H2OrLater() && HasNpuSupport())
{
    if (await TextRecognizer.GetReadyState() == AIFeatureReadyState.Ready 
        || await TextRecognizer.EnsureReadyAsync())
    {
        // Use NPU-accelerated API
        _ocrProvider = new NpuOcrService();
    }
}
else
{
    // Fall back to legacy API
    _ocrProvider = new LegacyOcrService();
}
```

### Package Identity Workaround
`Windows.Media.Ocr` normally requires MSIX package identity. Workarounds:
1. Use `WinRT.Runtime` NuGet package for desktop apps
2. Access via COM interop with appropriate activation context
3. Self-contained deployment with manifest

### References
- [Windows.Media.Ocr Namespace](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr)
- [Windows AI Text Recognition](https://learn.microsoft.com/en-us/windows/ai/apis/text-recognition)

---

## 3. STA Thread Requirements for UI Automation

### Decision: Use Existing STA Thread Pattern from Codebase

### Rationale
The constitution (Principle X) mandates: "UI Automation requires STA; dedicate an STA thread for all UI Automation operations."

The codebase already has patterns for STA operations (window management). The UI Automation service will:
1. Create a dedicated STA thread at service startup
2. Marshal all UI Automation calls to this thread via `Channel<T>` or `TaskCompletionSource`
3. Never block the STA thread on async operations

### Pattern
```csharp
public class UIAutomationService : IUIAutomationService, IDisposable
{
    private readonly Thread _staThread;
    private readonly BlockingCollection<Func<object>> _workQueue;
    
    public UIAutomationService()
    {
        _workQueue = new BlockingCollection<Func<object>>();
        _staThread = new Thread(StaThreadProc) { IsBackground = true };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
    }
    
    private void StaThreadProc()
    {
        foreach (var work in _workQueue.GetConsumingEnumerable())
        {
            work();
        }
    }
}
```

---

## 4. Element Identification Strategy

### Decision: Composite Element ID with Fallback Chain

### Rationale
UI elements need stable identifiers for subsequent operations (invoke, scroll_into_view). However:
- `AutomationId` is often empty
- `RuntimeId` changes between sessions
- `Name` is not unique

### Strategy
Return a composite identifier:
```json
{
  "elementId": "window:0x1234|path:3.2.5|name:Install|type:Button",
  "automationId": "installButton",
  "runtimeId": [42, 1234567],
  "name": "Install",
  "controlType": "Button",
  "boundingRect": { "x": 480, "y": 330, "width": 80, "height": 28 }
}
```

For subsequent operations, attempt resolution in order:
1. `RuntimeId` (fastest, may be stale)
2. `AutomationId` if present (stable)
3. Path from root (recreate tree walk)
4. Re-search by name + type (slowest, most reliable)

---

## 5. Coordinate System Integration

### Decision: Return Screen Coordinates Matching mouse_control

### Rationale
The constitution (Principle XVII) requires consistent coordinate systems. UI Automation's `BoundingRectangle` returns screen coordinates (same as `GetCursorPos`).

### Mapping
| Source | Coordinate System | Notes |
|--------|-------------------|-------|
| `AutomationElement.BoundingRectangle` | Screen (physical pixels) | DPI-aware via Per-Monitor V2 |
| `mouse_control` coordinates | Monitor-relative | Converted from screen at tool level |
| Return to LLM | Screen coordinates | LLM converts to monitor-relative for mouse_control |

### Implementation
```csharp
// UI Automation returns screen coordinates
var rect = element.Current.BoundingRectangle;

// Convert to monitor-relative for LLM (same as screenshot_control)
var monitor = _monitorService.GetMonitorFromPoint((int)rect.X, (int)rect.Y);
var monitorRelativeX = (int)rect.X - monitor.Bounds.Left;
var monitorRelativeY = (int)rect.Y - monitor.Bounds.Top;
```

---

## 6. Electron App Accessibility

### Decision: Standard UI Automation Queries Work via Chromium Bridge

### Rationale
Electron apps (VS Code, Teams, Slack) use Chromium, which implements Windows UI Automation via its accessibility layer:
- ARIA roles map to UIA control types
- `aria-label` becomes `Name`
- Web content is exposed as native UI elements

### Considerations
1. Accessibility must be enabled (default in most Electron apps)
2. Dynamic content may have delayed appearance in tree (use `wait_for`)
3. Some controls may have generic names - use hierarchy context

### Testing Strategy
Integration tests will use VS Code (available on all dev machines) as the primary Electron test target.

---

## 7. wait_for Implementation

### Decision: Polling with Exponential Backoff

### Rationale
The constitution (Principle XVI) prohibits fixed delays. Instead:
1. Poll immediately
2. If not found, wait with exponential backoff (50ms → 100ms → 200ms → 400ms)
3. Cap at 1 second intervals
4. Stop at timeout

### Pattern
```csharp
public async Task<UIElementInfo?> WaitForAsync(
    ElementQuery query, 
    int timeoutMs = 5000,
    CancellationToken ct = default)
{
    var sw = Stopwatch.StartNew();
    var delay = 50;
    
    while (sw.ElapsedMilliseconds < timeoutMs)
    {
        var element = FindElement(query);
        if (element != null) return element;
        
        await Task.Delay(delay, ct);
        delay = Math.Min(delay * 2, 1000);
    }
    
    return null; // Timeout - caller will convert to error
}
```

---

## 8. Virtualized List Handling

### Decision: Scroll-and-Search with ScrollPattern

### Rationale
Virtualized lists (VS Code file explorer, data grids) only have visible items in the accessibility tree. To find off-screen items:

1. Search current tree
2. If not found and parent has `ScrollPattern`, scroll down
3. Re-search
4. Repeat until found or scroll exhausted (can't scroll further)

### Pattern
```csharp
public async Task<UIElementInfo?> FindWithScrollAsync(
    AutomationElement parent,
    Condition condition,
    CancellationToken ct = default)
{
    var scrollPattern = parent.TryGetPattern<ScrollPattern>();
    
    // First search without scrolling
    var element = parent.FindFirst(TreeScope.Descendants, condition);
    if (element != null) return ToInfo(element);
    
    if (scrollPattern == null) return null; // Can't scroll
    
    // Scroll to top first
    scrollPattern.SetScrollPercent(ScrollPattern.NoScroll, 0);
    
    while (scrollPattern.Current.VerticalScrollPercent < 100)
    {
        ct.ThrowIfCancellationRequested();
        
        // Scroll down one page
        scrollPattern.ScrollVertical(ScrollAmount.LargeIncrement);
        await Task.Delay(100, ct); // Allow UI to update
        
        element = parent.FindFirst(TreeScope.Descendants, condition);
        if (element != null) return ToInfo(element);
    }
    
    return null; // Not found after full scroll
}
```

---

## 9. Error Response Strategy

### Decision: Structured Errors with Actionable Context

### Rationale
The constitution (Principle IX) requires: "Return meaningful MCP error responses with actionable context."

### Error Categories

| Error Type | When | Response Content |
|------------|------|------------------|
| `element_not_found` | Find returns empty | Query criteria, window info, suggestion to verify window is open |
| `timeout` | wait_for expires | Elapsed time, search criteria, last partial matches |
| `multiple_matches` | >1 element matches | List of matches with locations, suggestion to refine query |
| `pattern_not_supported` | Element lacks pattern | Element info, available patterns, fallback suggestion |
| `element_stale` | Cached element gone | Original element info, suggestion to re-query |
| `elevated_target` | UIPI blocks access | Window info, elevation status, workaround guidance |

---

## 10. Documentation Structure

### Decision: Dedicated Pages with Workflow Examples

### GitHub Pages Structure
```
gh-pages/
├── ui-automation.md      # Full UI Automation documentation
│   ├── Actions           # find, invoke, wait_for, scroll_into_view, etc.
│   ├── Parameters        # All parameters with types and defaults
│   ├── Return Values     # Response structure
│   ├── Workflows         # Copy from spec's LLM Agent Workflow section
│   ├── Error Handling    # Error types and recovery
│   └── Limitations       # UIPI, virtualized lists, Electron quirks
├── ocr.md                # OCR documentation
│   ├── Actions           # recognize, get_status
│   ├── Languages         # Supported OCR languages
│   └── API Selection     # Legacy vs NPU-accelerated
└── features.md           # Update with new features
```

### VS Code Extension Updates
- `README.md`: Add UI Automation & OCR section with 3 workflow examples
- `package.json`: Update `mcp.tools` with new tool descriptions

---

## Summary

All research items resolved. Key decisions:

| Topic | Decision |
|-------|----------|
| UI Automation API | `System.Windows.Automation` |
| OCR API | Dual: NPU-accelerated primary, legacy fallback |
| Threading | Dedicated STA thread for UI Automation |
| Element IDs | Composite with fallback resolution chain |
| Coordinates | Screen coordinates, LLM converts to monitor-relative |
| wait_for | Polling with exponential backoff |
| Virtualized lists | Scroll-and-search via ScrollPattern |
| Errors | Structured with actionable context |

**Ready for Phase 1: Data Model & Contracts**
