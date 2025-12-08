# Test Case: TC-MOUSE-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-005 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that double-click action is performed correctly, triggering double-click behavior.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] A folder or text file is visible on the desktop for double-click testing
- [ ] No modal dialogs blocking interaction

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Ensure Test Target Exists

For this test, we need something that responds to double-click:
- A desktop icon on secondary monitor, OR
- A text file in Notepad (to select word), OR
- Any folder window

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Position and Double-Click

**MCP Tool**: `mouse_control`  
**Action**: `double_click`  
**Parameters**:
- `x`: `{target_x}` (position of clickable item)
- `y`: `{target_y}` (position of clickable item)

### Step 5: Wait for Response

Allow 1-2 seconds for double-click action to complete (e.g., application launch, selection).

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots for double-click effect:
- If desktop icon: Application window should open
- If text in Notepad: Word under cursor should be selected
- If folder: Folder should open in Explorer

## Expected Result

Double-click action is performed, triggering the expected double-click behavior for the target element.

## Pass Criteria

- [ ] `mouse_control` double_click action returns success
- [ ] Visual evidence of double-click effect in after screenshot
- [ ] Action completed within timeout
- [ ] No error dialogs appeared

## Failure Indicators

- Only single-click registered (no double-click effect)
- Error response from tool
- Wrong element double-clicked
- Double-click too slow (registered as two single clicks)

## Notes

- Double-click timing is system-dependent
- The MCP tool handles double-click timing internally
- Test can use any available double-click target on secondary monitor
