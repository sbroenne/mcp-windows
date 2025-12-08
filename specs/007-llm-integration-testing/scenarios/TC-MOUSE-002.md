# Test Case: TC-MOUSE-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-002 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the mouse cursor can be moved to all four corners of the screen, testing boundary coordinates.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No applications with always-on-top windows blocking corners
- [ ] Taskbar auto-hide enabled or position known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Get secondary monitor bounds to calculate corner coordinates

**Store**:
- `minX`: bounds.x (e.g., 2560)
- `minY`: bounds.y (e.g., 0)
- `maxX`: bounds.x + bounds.width - 1 (e.g., 4479)
- `maxY`: bounds.y + bounds.height - 1 (e.g., 1079)

### Step 2: Move to Top-Left Corner

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{minX}` 
- `y`: `{minY}`

**Screenshot**: Capture to verify cursor at top-left

### Step 3: Move to Top-Right Corner

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{maxX}`
- `y`: `{minY}`

**Screenshot**: Capture to verify cursor at top-right

### Step 4: Move to Bottom-Right Corner

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{maxX}`
- `y`: `{maxY}`

**Screenshot**: Capture to verify cursor at bottom-right

### Step 5: Move to Bottom-Left Corner

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{minX}`
- `y`: `{maxY}`

**Screenshot**: Capture to verify cursor at bottom-left

### Step 6: Final Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

Mouse cursor successfully moves to all four corners of the secondary monitor. Each corner position is reachable without error.

## Pass Criteria

- [ ] All four corner movements complete without error
- [ ] Cursor reaches top-left corner (visible at screen edge)
- [ ] Cursor reaches top-right corner
- [ ] Cursor reaches bottom-right corner
- [ ] Cursor reaches bottom-left corner
- [ ] Final screenshot shows cursor at bottom-left

## Failure Indicators

- Error when moving to any corner
- Cursor "clamped" before reaching edge
- Cursor moves to wrong monitor
- Any move operation times out

## Notes

- Screen edges may have 1-pixel buffer in some Windows configurations
- If taskbar is visible, bottom corners may be partially obstructed
- This test validates that boundary coordinates are handled correctly
