# Test Case: TC-MOUSE-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-001 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the mouse cursor can be moved to specific absolute coordinates on the secondary monitor.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No modal dialogs blocking the target area
- [ ] Mouse is not currently being held by another application

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

**Expected**: Returns array of monitors with bounds. Use first non-primary monitor.

### Step 2: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: 
- `target`: `"monitor"`
- `monitorIndex`: `{secondary_index}`
- `includeCursor`: `true`

**Save As**: `before.png`

### Step 3: Move Mouse to Target Position

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{target_center_x}` (center of secondary monitor)
- `y`: `{target_center_y}` (center of secondary monitor)

**Example** (for secondary at 2560,0 with 1920x1080):
- `x`: `3520` (2560 + 960)
- `y`: `540` (0 + 540)

### Step 4: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: 
- `target`: `"monitor"`
- `monitorIndex`: `{secondary_index}`
- `includeCursor`: `true`

**Save As**: `after.png`

### Step 5: Visual Verification

Compare before and after screenshots:
- Cursor should be visible at or near the target coordinates
- Cursor position should have changed from before screenshot

## Expected Result

Mouse cursor is positioned at the center of the secondary monitor. The cursor is visible in the "after" screenshot at the expected location.

## Pass Criteria

- [ ] `list_monitors` returns at least 1 monitor
- [ ] `mouse_control` move action returns success
- [ ] "After" screenshot shows cursor at target location
- [ ] Cursor position differs from "before" screenshot (unless already at target)

## Failure Indicators

- Error response from `mouse_control` tool
- Cursor not visible in "after" screenshot
- Cursor at wrong position in "after" screenshot
- Screenshot capture fails

## Notes

- This is the foundational mouse test - all other mouse tests depend on basic movement working
- Coordinates are absolute screen coordinates in the Windows virtual screen space
- Secondary monitor coordinates extend beyond primary monitor bounds
