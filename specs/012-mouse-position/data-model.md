# Phase 1 Data Model: Mouse Position Awareness for LLM Usability

**Date**: December 11, 2025  
**Feature**: 012-mouse-position  
**Status**: Complete

## Domain Model

### Entities

#### MouseControlRequest
Represents a single mouse control operation request.

| Field | Type | Nullable | Validation |
|-------|------|----------|-----------|
| action | string (enum) | ✗ | Required; one of: move, click, double_click, right_click, middle_click, drag, scroll, get_position |
| x | int | ✓ | If provided with y, requires monitorIndex |
| y | int | ✓ | If provided with x, requires monitorIndex |
| endX | int | ✓ | Drag only; if provided, requires endY and monitorIndex |
| endY | int | ✓ | Drag only; if provided, requires endX and monitorIndex |
| monitorIndex | int | ✓ | **Conditional**: Required if x/y or endX/endY are provided; must be valid (0 <= monitorIndex < monitorCount) |
| direction | string | ✓ | Scroll only; one of: up, down, left, right |
| amount | int | ✓ | Scroll only; default 1 |
| modifiers | string[] | ✓ | Click actions only; array of: ctrl, shift, alt |
| button | string | ✓ | Drag only; one of: left, right, middle; default left |

**Validation Rules**:
1. If `action` is "move": x and y REQUIRED, monitorIndex REQUIRED
2. If `action` is "click", "double_click", "right_click", "middle_click": 
   - If x and y provided → monitorIndex REQUIRED
   - If x and y omitted → monitorIndex OPTIONAL (operate at current cursor)
3. If `action` is "drag": startX, startY, endX, endY REQUIRED, monitorIndex REQUIRED
4. If `action` is "scroll": direction REQUIRED, monitorIndex OPTIONAL (if x/y provided, monitorIndex REQUIRED)
5. If `action` is "get_position": no parameters required

#### MouseControlResponse
Represents the result of a mouse control operation.

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| success | bool | ✗ | Operation succeeded (true) or failed (false) |
| final_position | { x: int, y: int } | ✗ | Final cursor position (absolute screen coordinates or monitor-relative depending on context) |
| monitorIndex | int | ✓ | **NEW**: Monitor where cursor ended up (populated on success) |
| monitorWidth | int | ✓ | **NEW**: Width of the target monitor (populated on success) |
| monitorHeight | int | ✓ | **NEW**: Height of the target monitor (populated on success) |
| window_title | string | ✓ | Title of window under cursor at end of operation |
| error | string | ✓ | Error message (populated on failure) |
| error_code | string | ✓ | Machine-readable error code (populated on failure) |
| error_details | object | ✓ | Additional error context (e.g., valid_bounds, valid_indices) |

**Success Example**:
```json
{
  "success": true,
  "final_position": { "x": 500, "y": 300 },
  "monitorIndex": 1,
  "monitorWidth": 2560,
  "monitorHeight": 1440,
  "window_title": "Visual Studio Code"
}
```

**Failure Example** (missing monitorIndex):
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "monitorIndex is required when using x/y coordinates",
  "error_code": "missing_required_parameter",
  "error_details": {
    "valid_indices": [0, 1]
  }
}
```

**Failure Example** (invalid monitorIndex):
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "Invalid monitorIndex: 5",
  "error_code": "invalid_coordinates",
  "error_details": {
    "valid_indices": [0, 1, 2],
    "provided_index": 5
  }
}
```

**Failure Example** (coordinates out of bounds):
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "Coordinates (2700, 100) out of bounds for monitor 1",
  "error_code": "coordinates_out_of_bounds",
  "error_details": {
    "valid_bounds": {
      "left": 1920,
      "top": 0,
      "right": 1920 + 2560,
      "bottom": 1440
    },
    "provided_coordinates": { "x": 2700, "y": 100 }
  }
}
```

### Key Types & Enumerations

#### MouseAction
Enum of valid actions:
- `move` — Move cursor to coordinates
- `click` — Left-click
- `double_click` — Double left-click
- `right_click` — Right-click
- `middle_click` — Middle-click
- `drag` — Drag from start to end coordinates
- `scroll` — Scroll in specified direction
- `get_position` — Query current cursor position (P3 priority)

#### ScrollDirection
Enum of scroll directions:
- `up`
- `down`
- `left`
- `right`

#### MouseButton
Enum for drag operations:
- `left` (default)
- `right`
- `middle`

#### ModifierKey
Set of modifier keys:
- `ctrl`
- `shift`
- `alt`

#### MouseControlErrorCode
Machine-readable error codes:
- `invalid_action` — Unknown action
- `invalid_coordinates` — Coordinates validation failed
- `coordinates_out_of_bounds` — Coordinates outside all monitor bounds
- `missing_required_parameter` — monitorIndex required but not provided
- `invalid_scroll_direction` — Unknown scroll direction
- `elevated_process_target` — Target is elevated (admin) process
- `secure_desktop_active` — UAC or lock screen active
- `input_blocked` — Input blocked by system
- `send_input_failed` — Windows SendInput API failed
- `operation_timeout` — Operation exceeded timeout
- `window_lost_during_drag` — Window closed during drag
- `unexpected_error` — Unexpected error (details in error message)

### Relationships

```
MouseControlRequest
  ├─ action: MouseAction
  ├─ coordinates: { x, y } → requires monitorIndex
  ├─ monitorIndex → validates against Monitor registry
  └─ [optional] modifiers: ModifierKey[]

Monitor (from system registry)
  ├─ index: int (0-based)
  ├─ bounds: { left, top, width, height }
  ├─ x, y, width, height
  └─ dpiScale: float (for future use)

MouseControlResponse
  ├─ success: bool
  ├─ final_position: { x, y } (monitor-relative or absolute, depending on context)
  ├─ monitorIndex: int (where cursor is now)
  ├─ error: string (if success=false)
  └─ error_details: object (validation context)
```

### State Transitions

```
LLM sends MouseControlRequest
  ↓
[Validation Phase]
  ├─ Is action valid? → No: return invalid_action error
  ├─ Does action require coordinates? 
  │   ├─ Yes: Are x/y provided?
  │   │   ├─ No: return missing_required_parameter error
  │   │   └─ Yes: Is monitorIndex provided?
  │   │       ├─ No: return missing_required_parameter error
  │   │       └─ Yes: proceed to coordinate validation
  │   └─ No: proceed to operation
  └─ Are coordinates within monitor bounds?
      ├─ No: return coordinates_out_of_bounds error
      └─ Yes: proceed to operation
  ↓
[Execution Phase]
  ├─ Move cursor (if needed)
  ├─ Perform action (click, drag, scroll, etc.)
  └─ Query final position + monitor
  ↓
[Response Phase]
  ├─ Success: return final_position + monitorIndex + monitorWidth/Height
  └─ Failure: return error code + error_details

```

### Validation Rules

#### monitorIndex Validation
- Must be integer
- Must be >= 0 and < (number of monitors in system)
- Must be accompanied by coordinates (if x/y provided, monitorIndex REQUIRED)
- Error response includes list of valid indices

#### Coordinate Validation
- Must be integers
- If monitorIndex is provided, coordinates must be within that monitor's bounds
- Bounds: `monitorIndex.left <= x < monitorIndex.right` and `monitorIndex.top <= y < monitorIndex.bottom`
- Error response includes valid bounds and provided coordinates

#### Action-Specific Validation
- `move`: x, y, monitorIndex required
- `click`/`double_click`/`right_click`/`middle_click`: x, y optional (if provided, monitorIndex required)
- `drag`: startX, startY, endX, endY, monitorIndex required
- `scroll`: direction required, x/y optional (if provided, monitorIndex required)
- `get_position`: no parameters required
