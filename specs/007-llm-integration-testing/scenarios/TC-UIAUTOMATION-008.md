# Test Case: TC-UIAUTOMATION-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-008 |
| **Category** | UIAUTOMATION |
| **Priority** | P2 |
| **Target App** | Notepad (Font Dialog) |
| **Target Monitor** | Primary |
| **Timeout** | 45 seconds |
| **Tools** | ui_automation, window_management, keyboard_control, screenshot_control |

## Objective

Verify that the `invoke` action with Toggle pattern can change the state of a checkbox.

## Preconditions

- [ ] Notepad is installed (Windows built-in)
- [ ] No modal dialogs blocking the screen
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Launch Notepad

Open Notepad via command line:

```powershell
Start-Process notepad.exe
Start-Sleep -Seconds 1
```

### Step 2: Find Notepad Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`

**Expected**: Returns window handle for Notepad.

### Step 3: Open Font Dialog

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"o"`
- `modifiers`: `"alt"` (Alt+O for Format menu)

Wait, then:
**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"f"` (Font option)

**Alternative**: Use UI Automation
**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `name`: `"Format"`
- `controlType`: `"MenuItem"`

Then:
**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `name`: `"Font..."`
- `controlType`: `"MenuItem"`
- `timeout_ms`: `2000`

Then click Font...

### Step 4: Wait for Font Dialog

**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `name`: `"Font"`
- `controlType`: `"Window"`
- `timeout_ms`: `5000`

**Expected**: Font dialog appears.

### Step 5: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 6: Find Strikeout Checkbox

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `name`: `"Strikeout"` (or `"Strikethrough"`)
- `controlType`: `"CheckBox"`

**Expected**: Returns checkbox element with Toggle pattern.

### Step 7: Get Initial State

**MCP Tool**: `ui_automation`  
**Action**: `get_text`  
**Parameters**:
- `elementId`: `{checkbox_element_id}`

**Expected**: Returns current toggle state (unchecked/checked).

### Step 8: Toggle Checkbox

**MCP Tool**: `ui_automation`  
**Action**: `invoke`  
**Parameters**:
- `elementId`: `{checkbox_element_id}`
- `pattern`: `"Toggle"`

**Expected**: Checkbox state changes.

### Step 9: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Checkbox visual state has changed.

### Step 10: Visual Verification

Compare before and after screenshots:
- Before: Checkbox unchecked (empty)
- After: Checkbox checked (with checkmark)

### Step 11: Cleanup - Cancel Dialog

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `name`: `"Cancel"`
- `controlType`: `"Button"`

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={notepad_handle}`

## Expected Result

The `invoke` action with Toggle pattern successfully:
1. Locates the checkbox element
2. Toggles the checkbox state
3. Visual confirmation shows state change

## Pass Criteria

- [ ] Checkbox element found with Toggle pattern
- [ ] `invoke` with Toggle pattern returns success
- [ ] Checkbox visual state changes in after screenshot
- [ ] Dialog can be cancelled without error

## Failure Indicators

- Toggle pattern not supported on element
- Error response from `ui_automation` tool
- Checkbox state did not change
- Dialog failed to open

## Notes

- Toggle pattern is the UI Automation standard for checkboxes
- Some apps may not support Toggle pattern (fallback to click)
- The Font dialog is used because it has checkboxes with good accessibility
- Windows 10 and 11 have slightly different Font dialogs
- Other patterns to test: Invoke (buttons), ExpandCollapse (dropdowns), Value (sliders)
