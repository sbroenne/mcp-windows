# Test Case: TC-MOUSE-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-003 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a single left mouse click is performed correctly at the current cursor position.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Desktop or neutral area visible (no critical UI elements to accidentally click)
- [ ] Mouse cursor is at a known safe position

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor for safe click area

### Step 2: Move Mouse to Safe Area

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{target_center_x}` (center of secondary monitor)
- `y`: `{target_center_y}` (center of secondary monitor)

**Purpose**: Position cursor in a safe area before clicking

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Perform Left Click

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**: (none required - clicks at current position)

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

Left mouse click is performed at the cursor position. The tool returns success. If clicking on an interactive element, visual feedback may be observed.

## Pass Criteria

- [ ] `mouse_control` click action returns success
- [ ] No error messages in response
- [ ] Cursor remains at same position after click
- [ ] If over clickable element, visual state change observed

## Failure Indicators

- Error response from `mouse_control` tool
- Click performed at wrong location
- System error or unexpected dialog appears
- Click event not registered

## Notes

- This test verifies basic click functionality without requiring a specific target
- Visual feedback depends on what's under the cursor (may be just desktop)
- For click verification on a specific target, see TC-MOUSE-004 (move and click)
