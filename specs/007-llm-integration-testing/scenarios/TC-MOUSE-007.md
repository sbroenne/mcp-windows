# Test Case: TC-MOUSE-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-007 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that middle mouse button click is performed correctly.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Test area visible (desktop or application that responds to middle-click)
- [ ] Mouse has middle button capability

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Move to Target Position

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{target_center_x}` (center of secondary monitor)
- `y`: `{target_center_y}` (center of secondary monitor)

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Perform Middle Click

**MCP Tool**: `mouse_control`  
**Action**: `middle_click`  
**Parameters**: (none - clicks at current position)

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

Middle mouse button click is performed without error. The tool returns success.

## Pass Criteria

- [ ] `mouse_control` middle_click action returns success
- [ ] No error messages in response
- [ ] Action completes within timeout
- [ ] If over scrollable content, scroll cursor may appear

## Failure Indicators

- Error response from `mouse_control` tool
- Middle-click action not supported error
- Unexpected behavior (e.g., application crash)

## Notes

- Middle-click behavior varies by context:
  - Browser: Opens link in new tab
  - Text editor: May paste clipboard
  - Scrollable area: Enters scroll mode
  - Desktop: Usually no visible effect
- This test validates the action completes without error
- Visual verification depends on context
