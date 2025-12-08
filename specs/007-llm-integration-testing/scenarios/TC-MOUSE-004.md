# Test Case: TC-MOUSE-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-004 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | Calculator |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that mouse can move to a position and click in a single combined action, with visual confirmation via Calculator button.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Calculator app is open and positioned on secondary monitor
- [ ] Calculator is in standard mode (not scientific/programmer)

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Open Calculator on Secondary Monitor

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**: `key="r"`, `modifiers="win"`  

Then type "calc" and press Enter to open Calculator.

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**: `title="Calculator"`

Move Calculator to secondary monitor center if needed.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Identify the position of the "7" button in Calculator for clicking.

### Step 4: Move and Click on Button "7"

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**:
- `x`: `{button_7_x}` (coordinate of "7" button)
- `y`: `{button_7_y}` (coordinate of "7" button)

**Note**: The click action with x,y coordinates performs move+click atomically.

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- "Before": Calculator display is empty or shows "0"
- "After": Calculator display shows "7"

## Expected Result

Cursor moves to the "7" button and clicks it. Calculator display updates to show "7".

## Pass Criteria

- [ ] `mouse_control` click with coordinates returns success
- [ ] Calculator display changes from initial state
- [ ] Calculator display shows "7" in after screenshot
- [ ] Cursor is positioned over/near the "7" button

## Failure Indicators

- Click not registered (display doesn't change)
- Wrong button clicked (display shows different number)
- Calculator not found or not responding
- Coordinates outside Calculator window

## Notes

- Calculator button positions vary by window size and DPI
- May need to identify button position dynamically from screenshot
- This test validates move+click as atomic operation
