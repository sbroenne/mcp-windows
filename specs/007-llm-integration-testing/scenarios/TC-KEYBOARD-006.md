# Test Case: TC-KEYBOARD-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-006 |
| **Category** | KEYBOARD |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that function key F1 can be pressed to trigger help.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] No dialogs or overlays blocking

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

**Note**: Just Notepad visible, no help window.

### Step 4: Press F1 Key

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"f1"`

### Step 5: Wait for Help Response

Allow time for help window/browser to open (1-2 seconds).

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Only Notepad
- After: Help content visible (browser window or help dialog)

## Expected Result

F1 triggers the help function. In Windows 11 Notepad, this typically opens Bing search for Notepad help in a browser.

## Pass Criteria

- [ ] `keyboard_control` press action returns success
- [ ] Help action is triggered
- [ ] New window or browser tab opens with help content
- [ ] Original Notepad window remains open

## Failure Indicators

- No response to F1
- Wrong application received the keypress
- Error response from tool
- F1 triggered different action

## Notes

- F1 behavior varies by application
- Windows 11 Notepad opens Bing search for help
- Classic Notepad may open Windows Help
- This tests function key mapping
- P2 priority as function keys are less commonly automated
