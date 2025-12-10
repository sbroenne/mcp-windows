# Data Model: Code Quality & MCP SDK Migration

**Feature**: 010-code-quality  
**Date**: 2025-12-10

## Overview

This feature is primarily a migration/enhancement of existing code. The data model focuses on:
1. Existing result types that will be used for structured output
2. New resource types for MCP Resources
3. Semantic annotations on tool attributes

## Existing Result Types (Used for Structured Output)

These types already exist and are well-designed. They will be exposed via `UseStructuredContent = true`.

### MouseControlResult

**Location**: `src/Sbroenne.WindowsMcp/Models/MouseControlResult.cs`

```text
MouseControlResult (record)
├── Success: bool (required)
├── FinalPosition: FinalPosition (required)
│   ├── X: int
│   └── Y: int
├── WindowTitle: string? (optional)
├── Error: string? (optional, when Success=false)
├── ErrorCode: MouseControlErrorCode (internal)
└── ErrorDetails: Dictionary<string, object>? (optional)
```

### KeyboardControlResult

**Location**: `src/Sbroenne.WindowsMcp/Models/KeyboardControlResult.cs`

```text
KeyboardControlResult (record)
├── Success: bool (required)
├── Action: string (required)
├── KeysPressed: string[]? (optional)
├── TextTyped: string? (optional)
├── Error: string? (optional)
├── ErrorCode: KeyboardControlErrorCode (internal)
└── KeyboardLayout: string? (optional)
```

### WindowManagementResult

**Location**: `src/Sbroenne.WindowsMcp/Models/WindowManagementResult.cs`

```text
WindowManagementResult (record)
├── Success: bool (required)
├── Action: string (required)
├── Windows: WindowInfo[]? (for list/find actions)
│   ├── Handle: string
│   ├── Title: string
│   ├── ProcessName: string
│   ├── ProcessId: int
│   ├── Bounds: WindowBounds
│   │   ├── Left: int
│   │   ├── Top: int
│   │   ├── Width: int
│   │   └── Height: int
│   ├── State: WindowState
│   └── IsVisible: bool
├── Window: WindowInfo? (for single window actions)
├── Error: string? (optional)
└── ErrorCode: WindowManagementErrorCode (internal)
```

### ScreenshotControlResult

**Location**: `src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs`

```text
ScreenshotControlResult (record)
├── Success: bool (required)
├── FilePath: string? (when saved to file)
├── Base64Data: string? (when returned as data)
├── Width: int (captured image width)
├── Height: int (captured image height)
├── Format: string (png, jpg, etc.)
├── Target: string (primary_screen, monitor, window, region)
├── MonitorInfo: MonitorInfo? (when targeting monitor)
├── Error: string? (optional)
└── ErrorCode: ScreenshotErrorCode (internal)
```

## New Types for MCP Resources

### SystemResources Class

**Location**: `src/Sbroenne.WindowsMcp/Resources/SystemResources.cs` (NEW)

```text
SystemResources (class with [McpServerResourceType])
├── GetMonitors() → MonitorInfo[]
│   Resource: system://monitors
│   Returns: List of all connected monitors
│
└── GetKeyboardLayout() → KeyboardLayoutInfo
    Resource: system://keyboard/layout
    Returns: Current keyboard layout info
```

### MonitorInfo (Existing)

**Location**: `src/Sbroenne.WindowsMcp/Models/MonitorInfo.cs`

```text
MonitorInfo (record)
├── Index: int (0-based monitor index)
├── Name: string (device name)
├── IsPrimary: bool
├── X: int (left edge in virtual screen coords)
├── Y: int (top edge in virtual screen coords)
├── Width: int
├── Height: int
├── WorkAreaX: int
├── WorkAreaY: int
├── WorkAreaWidth: int
├── WorkAreaHeight: int
├── ScaleFactor: double (DPI scale, e.g., 1.25 for 125%)
└── DpiX: int
```

### KeyboardLayoutInfo (Existing)

**Location**: `src/Sbroenne.WindowsMcp/Models/KeyboardLayoutInfo.cs`

```text
KeyboardLayoutInfo (record)
├── LayoutId: string (hex string like "00000409")
├── LanguageTag: string (BCP-47 like "en-US")
├── LocaleName: string (display name like "English (United States)")
└── KeyboardName: string (keyboard name like "US")
```

## Tool Semantic Annotations

### McpServerTool Attribute Properties

| Tool | Name | Title | ReadOnly | Destructive | OpenWorld |
|------|------|-------|----------|-------------|-----------|
| MouseControlTool | mouse_control | "Mouse Control" | false | true | true |
| KeyboardControlTool | keyboard_control | "Keyboard Control" | false | true | true |
| WindowManagementTool | window_management | "Window Management" | false | true | true |
| ScreenshotControlTool | screenshot_control | "Screenshot Capture" | true | false | true |

### Attribute Meanings

- **ReadOnly**: Tool only reads system state, no side effects
- **Destructive**: Tool modifies system state (clicks, typing, window changes)
- **OpenWorld**: Tool interacts with external Windows system (all 4 tools)

## Completions Data

### Mouse Control Completions

| Parameter | Valid Values |
|-----------|--------------|
| `action` | move, click, double_click, right_click, middle_click, drag, scroll |
| `direction` | up, down, left, right |
| `button` | left, right, middle |

### Keyboard Control Completions

| Parameter | Valid Values |
|-----------|--------------|
| `action` | type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout |
| `key` | enter, tab, escape, backspace, delete, space, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, up, down, left, right, home, end, pageup, pagedown, insert, ctrl, shift, alt, win, copilot |

### Window Management Completions

| Parameter | Valid Values |
|-----------|--------------|
| `action` | list, find, activate, get_foreground, minimize, maximize, restore, close, move, resize, set_bounds, wait_for |

## Relationships

```text
Program.cs
    ├── WithTools<MouseControlTool>()
    ├── WithTools<KeyboardControlTool>()
    ├── WithTools<WindowManagementTool>()
    ├── WithTools<ScreenshotControlTool>()
    ├── WithResources<SystemResources>()        # NEW
    └── WithCompleteHandler(...)                # NEW

SystemResources
    ├── IMonitorService (injected)
    └── IKeyboardInputService (injected, for layout)
```

## Migration Notes

1. **Return Types**: Tool methods currently return `string` (JSON-serialized). Change to return typed result objects directly.

2. **Partial Methods**: Add `partial` keyword to tool classes and methods. SDK source generator will run.

3. **XML Documentation**: Existing XML docs on tool methods are sufficient. Add any missing `<param>` tags.

4. **No Schema Changes**: Existing result types don't need modification - they're already well-structured records with proper JSON attributes.
