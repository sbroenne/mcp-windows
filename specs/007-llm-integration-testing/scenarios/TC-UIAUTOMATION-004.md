# Test Case: TC-UIAUTOMATION-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-004 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |

## Objective

Verify that the `find_and_type` action can locate a text field and type text into it in a single operation.

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

Wait for window to appear.

### Step 2: Find Notepad Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"` (or `"Untitled - Notepad"`)

**Expected**: Returns window handle for Notepad.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: Notepad text area is empty.

### Step 4: Find and Type Text

**MCP Tool**: `ui_automation`  
**Action**: `find_and_type`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `controlType`: `"Edit"` (or `"Document"` for Windows 11)
- `text`: `"Hello from UI Automation!"`

**Expected**: Returns success with typed element info.

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Notepad shows "Hello from UI Automation!" in the text area.

### Step 6: Visual Verification

Compare before and after screenshots:
- Before: Empty text area
- After: Text "Hello from UI Automation!" visible

### Step 7: Cleanup - Close Without Saving

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**: `key="w"`, `modifiers="alt"` (Alt+F4)

Then click "Don't Save" if prompted.

## Expected Result

The `find_and_type` action successfully:
1. Locates the Edit/Document control
2. Focuses the control
3. Types the specified text
4. Text appears in Notepad

## Pass Criteria

- [ ] `find_and_type` action returns success
- [ ] Text "Hello from UI Automation!" appears in Notepad
- [ ] All characters typed correctly (no missing/garbled characters)
- [ ] Text cursor is at end of typed text

## Failure Indicators

- Error response from `ui_automation` tool
- Element not found for Edit control
- No text appeared in Notepad
- Text is garbled or incomplete
- Wrong characters typed

## Notes

- Windows 11 Notepad uses "Document" controlType, Windows 10 uses "Edit"
- `find_and_type` attempts ValuePattern first, falls back to keyboard simulation
- This test validates the combined find+focus+type workflow
- No `clearFirst` parameter used - assumes empty field
