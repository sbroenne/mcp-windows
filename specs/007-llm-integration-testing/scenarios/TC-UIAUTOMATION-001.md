# Test Case: TC-UIAUTOMATION-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-001 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management |

## Objective

Verify that the `find` action can locate UI elements by name in a Windows application.

## Preconditions

- [ ] Notepad is installed (Windows built-in)
- [ ] No modal dialogs blocking the screen
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Launch Notepad

Open Notepad via Start menu or command line:

```powershell
Start-Process notepad.exe
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

### Step 4: Find Edit Control by Control Type

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `controlType`: `"Edit"`

**Expected**: Returns element with:
- `controlType`: `"Edit"` or `"Document"`
- `boundingRect` with valid coordinates
- `elementId` that can be used for subsequent operations

### Step 5: Find File Menu by Name

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `name`: `"File"`
- `controlType`: `"MenuItem"`

**Expected**: Returns element with:
- `name`: `"File"`
- `controlType`: `"MenuItem"`
- `patterns` array (may include `"Invoke"`, `"ExpandCollapse"`)

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={notepad_handle}`

## Expected Result

The `find` action successfully locates:
1. The Edit/Document control (main text area)
2. The File menu item

Both elements return valid `elementId`, `boundingRect`, and `controlType` properties.

## Pass Criteria

- [ ] `find` action returns success for Edit control
- [ ] `find` action returns success for File menu item
- [ ] Returned elements include `elementId` for subsequent operations
- [ ] Returned elements include `boundingRect` with non-zero dimensions
- [ ] `controlType` matches the search criteria

## Failure Indicators

- Error response from `ui_automation` tool
- Empty results (no elements found)
- Element properties missing or invalid
- `boundingRect` with zero dimensions

## Notes

- This is the foundational UI Automation test - all other UI Automation tests depend on basic find working
- Notepad is used because it's a native Windows app with good UI Automation support
- Element structure may vary slightly between Windows 10 and Windows 11
- Windows 11 Notepad has different control types than classic Notepad
