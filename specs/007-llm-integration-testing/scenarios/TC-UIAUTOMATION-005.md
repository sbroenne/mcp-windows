# Test Case: TC-UIAUTOMATION-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-005 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Primary |
| **Timeout** | 60 seconds |
| **Tools** | ui_automation, window_management, keyboard_control, screenshot_control |

## Objective

Verify that the `wait_for` action can wait for a dialog to appear after triggering an action.

## Preconditions

- [ ] Notepad is installed (Windows built-in)
- [ ] No modal dialogs blocking the screen
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Launch Notepad with Content

Open Notepad and type some text:

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

### Step 3: Type Content

**MCP Tool**: `ui_automation`  
**Action**: `find_and_type`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `controlType`: `"Edit"`
- `text`: `"Unsaved content"`

**Expected**: Text appears in Notepad.

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 5: Trigger Close (Alt+F4)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**: 
- `key`: `"F4"`
- `modifiers`: `"alt"`

**Note**: This triggers the "Save changes?" dialog because content is unsaved.

### Step 6: Wait for Save Dialog

**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `name`: `"Notepad"` (dialog title may vary)
- `controlType`: `"Window"`
- `timeout_ms`: `5000`

**Alternative Parameters** (if dialog is a pane):
- `name`: `"Save"`
- `controlType`: `"Button"`

**Expected**: Returns when dialog/button appears.

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Save dialog is visible with "Save", "Don't Save", "Cancel" buttons.

### Step 8: Click Don't Save

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `name`: `"Don't Save"` (or `"Don't Save"` with apostrophe)
- `controlType`: `"Button"`

**Expected**: Dialog closes, Notepad closes.

### Step 9: Verify Window Closed

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`

**Expected**: Returns empty or error (window no longer exists).

## Expected Result

The `wait_for` action successfully:
1. Blocks until the save dialog appears
2. Returns the dialog element when found
3. Subsequent click dismisses dialog

## Pass Criteria

- [ ] `wait_for` action returns success within timeout
- [ ] Save dialog is visible in after screenshot
- [ ] Dialog contains expected buttons (Save, Don't Save, Cancel)
- [ ] Clicking "Don't Save" closes both dialog and Notepad

## Failure Indicators

- `wait_for` times out (dialog never detected)
- Error response from `ui_automation` tool
- Wrong dialog detected
- Unable to find "Don't Save" button

## Notes

- Dialog appearance timing varies by system load
- Windows 11 uses modern dialogs, Windows 10 uses classic dialogs
- Button names may have different casing or punctuation
- `wait_for` uses exponential backoff polling internally
- Timeout error includes elapsed time and last check result for debugging
