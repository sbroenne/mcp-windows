# Test Case: TC-WORKFLOW-010

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-010 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Calculator |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 120 seconds |

## Objective

Verify a complete UI interaction sequence: perform a multi-step calculation in Calculator (7 + 3 = 10), with visual verification at each step.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Windows Calculator is open on secondary monitor
- [ ] Calculator is in Standard mode
- [ ] Calculator display is clear (shows "0")
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find and Activate Calculator

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Calculator"`
- `regex`: `true`

**Record**: Handle and bounds.

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{calculator_handle}"`

### Step 3: Before Screenshot (Clear State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: Display shows "0".

### Step 4: Press "7" Using Keyboard

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `7`

### Step 5: After "7" Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-04-seven.png`

**Verify**: Display shows "7".

### Step 6: Press "+" Using Keyboard

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `+`

(Note: May need to use `combo` with shift+= on some keyboards)

### Step 7: After "+" Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-06-plus.png`

**Verify**: Display still shows "7" (or shows operation indicator).

### Step 8: Press "3" Using Keyboard

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `3`

### Step 9: After "3" Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-08-three.png`

**Verify**: Display shows "3".

### Step 10: Press "=" Using Keyboard

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `enter`

(Note: Enter key triggers equals in Calculator)

### Step 11: After Screenshot (Result)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Display shows "10".

### Step 12: Visual Verification

Compare all screenshots:
- "before.png": Display shows "0"
- "step-04-seven.png": Display shows "7"
- "step-06-plus.png": Shows operation in progress
- "step-08-three.png": Display shows "3"
- "after.png": Display shows "10" (7 + 3 = 10)

## Expected Result

The workflow successfully:
1. Inputs "7"
2. Inputs "+" operator
3. Inputs "3"
4. Presses Enter/Equals
5. Result "10" is displayed
6. All intermediate states captured

## Pass Criteria

- [ ] Calculator found and activated
- [ ] "7" key input shows "7" on display
- [ ] "+" operator accepted
- [ ] "3" key input shows "3" on display
- [ ] Enter/Equals shows result
- [ ] Final display shows "10"
- [ ] All 5 screenshots captured successfully

## Failure Indicators

- Calculator not found
- Key input not registered
- Wrong number displayed
- Calculation result incorrect
- Calculator in wrong mode
- Keyboard input goes to wrong application

## Notes

- Calculator responds to numpad and regular number keys
- "+" requires Shift+= on US keyboard layout
- Enter key acts as equals in Calculator
- Some Calculator modes show expression history
- Clear (C or Escape) can reset between tests

## Cleanup

Reset Calculator for next test:

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `escape`

This clears the Calculator display.
