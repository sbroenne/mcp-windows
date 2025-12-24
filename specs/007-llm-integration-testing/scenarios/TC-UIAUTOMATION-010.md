# Test Case: TC-UIAUTOMATION-010

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-010 |
| **Category** | UIAUTOMATION |
| **Priority** | P2 |
| **Target App** | VS Code (Electron) |
| **Target Monitor** | Primary |
| **Timeout** | 45 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |
| **Dependencies** | VS Code must be installed |

## Objective

Verify that UI Automation works with Electron-based applications (VS Code) via the Chromium accessibility bridge.

## Preconditions

- [ ] VS Code is installed
- [ ] VS Code has accessibility enabled (default)
- [ ] No modal dialogs blocking the screen
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Launch VS Code

Open VS Code:

```powershell
code --new-window
Start-Sleep -Seconds 3
```

Wait for window to fully load.

### Step 2: Find VS Code Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Visual Studio Code"`

**Alternative**:
- `regex`: `true`
- `title`: `".*Visual Studio Code.*"`

**Expected**: Returns window handle for VS Code.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Get UI Tree

**MCP Tool**: `ui_automation`  
**Action**: `get_tree`  
**Parameters**:
- `windowHandle`: `{vscode_handle}`
- `depth`: `3`

**Expected**: Returns UI tree with Electron/Chromium accessibility elements.

### Step 5: Find Activity Bar (Side Panel)

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{vscode_handle}`
- `name`: `"Explorer"` (or `"Files"`)
- `controlType`: `"TreeItem"` (or `"Button"`)

**Alternative**:
- `automationId` containing `"workbench.view.explorer"`

**Expected**: Returns Explorer view element.

### Step 6: Find Command Palette Trigger

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{vscode_handle}`
- `name`: `"Command Palette"`
- `controlType`: `"Button"`

**Note**: May need to search by partial name or different control type.

### Step 7: Open Command Palette via Keyboard

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"P"`
- `modifiers`: `"ctrl,shift"` (Ctrl+Shift+P)

### Step 8: Wait for Command Palette

**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `controlType`: `"ComboBox"` (or `"Edit"`)
- `timeout_ms`: `3000`

**Expected**: Command palette input appears.

### Step 9: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Command palette is visible.

### Step 10: Close Command Palette

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"Escape"`

### Step 11: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={vscode_handle}`

## Expected Result

UI Automation successfully:
1. Discovers elements in Electron/Chromium accessibility tree
2. Finds named elements (Explorer, Command Palette)
3. Waits for dynamic elements (Command Palette popup)
4. Returns valid bounding rectangles for mouse integration

## Pass Criteria

- [ ] `get_tree` returns elements from VS Code
- [ ] Elements include names from ARIA labels
- [ ] `wait_for` detects Command Palette appearance
- [ ] Bounding rectangles are valid and usable
- [ ] Tree depth traversal works in Electron app

## Failure Indicators

- Empty UI tree (accessibility may be disabled)
- Elements missing names or roles
- Wait_for times out
- Error response from `ui_automation` tool

## Notes

- VS Code uses Electron which bridges to Windows UI Automation via Chromium
- Element names come from ARIA labels and roles
- Some elements may have different names than expected
- Tree structure differs from native Windows apps
- Accessibility must be enabled in VS Code (usually default)
- If accessibility seems missing, try launching with: `code --enable-accessibility`
- This test validates FR-016, FR-017, FR-018 (Electron app support)
