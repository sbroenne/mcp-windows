# Test Case: TC-KEYBOARD-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-002 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that special characters can be typed correctly using the keyboard_control tool.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Text cursor is positioned in the text area
- [ ] Text area is empty or has known content

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor and ensure it has focus.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Type Special Characters

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"!@#$%^&*()_+-=[]{}|;':\",./<>?"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- All special characters should be visible in Notepad
- Character order should match the input string

## Expected Result

All special characters are typed correctly and visible in Notepad.

## Pass Criteria

- [ ] `keyboard_control` type action returns success
- [ ] All special characters appear correctly
- [ ] Character order matches input
- [ ] No missing or substituted characters

## Failure Indicators

- Missing characters
- Incorrect characters (e.g., wrong shift-key mapping)
- Characters appear in wrong order
- Some characters trigger shortcuts instead of typing
- Error response from tool

## Notes

- Special characters require Shift key combinations
- This tests the keyboard mapping for non-alphanumeric keys
- Keyboard layout affects character mapping (assumes US layout)
- Some characters may behave differently on international keyboards
