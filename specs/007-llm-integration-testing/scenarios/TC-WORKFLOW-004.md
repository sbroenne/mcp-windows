# Test Case: TC-WORKFLOW-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-004 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Calculator |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify the complete workflow of finding an application window, clicking a button within it, and verifying the state change through visual comparison.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Windows Calculator is open on secondary monitor
- [ ] Calculator is in Standard mode (not Scientific, Programmer, etc.)
- [ ] Calculator display is clear (shows "0")
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find Calculator Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Calculator"`
- `regex`: `true`

**Record**: Store the returned handle and bounds.

### Step 3: Activate Calculator Window

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{calculator_handle}"`

### Step 4: Before Screenshot (Initial State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Calculator display should show "0" or clear state.

### Step 5: Calculate Button Position

Using Calculator window bounds from Step 2:
- Calculate approximate position of the "7" button
- Button positions are relative to window; estimate based on window size
- Example: `x = window_x + window_width * 0.15`, `y = window_y + window_height * 0.55`

### Step 6: Move Mouse to Button

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{button_x}`
- `y`: `{button_y}`

### Step 7: Pre-Click Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-06-pre-click.png`

**Verify**: Mouse cursor is positioned over the "7" button.

### Step 8: Click Button

**MCP Tool**: `mouse_control`  
**Action**: `click`

### Step 9: After Screenshot (State Changed)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 10: Visual Verification

Compare screenshots:
- "before.png": Calculator display shows "0"
- "step-06-pre-click.png": Cursor over "7" button
- "after.png": Calculator display shows "7"

## Expected Result

The workflow successfully:
1. Finds and activates Calculator
2. Moves mouse to the "7" button
3. Clicks the button
4. Calculator display updates to show "7"

## Pass Criteria

- [ ] `find` action returns Calculator with valid handle
- [ ] `activate` action returns success
- [ ] `move` action positions cursor over target button
- [ ] `click` action returns success
- [ ] "Before" screenshot shows Calculator display as "0"
- [ ] "After" screenshot shows Calculator display as "7"
- [ ] Button click registered (display changed)

## Failure Indicators

- Calculator not found
- Mouse moved to wrong location
- Click didn't register (display unchanged)
- Wrong button clicked (display shows different number)
- Calculator not responding
- Modal dialog appeared

## Notes

- Calculator button positions vary based on window size and Calculator mode
- Windows 11 Calculator has a modern UI; button positions may differ from classic Calculator
- If Standard mode not available, Scientific mode can be used with adjusted positions
- Consider clicking "C" (Clear) first to ensure clean state
- Mouse cursor visibility in screenshot helps verify positioning
