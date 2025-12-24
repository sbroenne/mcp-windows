# Test Case: TC-UIAUTOMATION-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-002 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Calculator |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management |

## Objective

Verify that the `get_tree` action returns a hierarchical tree of UI elements for a window.

## Preconditions

- [ ] Calculator is installed (Windows built-in)
- [ ] No modal dialogs blocking the screen
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Launch Calculator

Open Calculator via command line:

```powershell
Start-Process calc.exe
Start-Sleep -Seconds 2
```

Wait for window to appear.

### Step 2: Find Calculator Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Calculator"`

**Expected**: Returns window handle for Calculator.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Get UI Tree

**MCP Tool**: `ui_automation`  
**Action**: `get_tree`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `depth`: `3`

**Expected**: Returns nested element tree with:
- Root element (Calculator window)
- Child elements (panels, buttons, display)
- Element properties (name, controlType, boundingRect)

### Step 5: Get Tree with Control Type Filter

**MCP Tool**: `ui_automation`  
**Action**: `get_tree`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `controlTypes`: `["Button"]`
- `depth`: `5`

**Expected**: Returns only Button elements (number buttons, operation buttons).

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={calculator_handle}`

## Expected Result

The `get_tree` action returns:
1. A hierarchical tree structure with parent-child relationships
2. Elements include name, controlType, and boundingRect
3. Filtered tree contains only Button elements when controlTypes filter applied

## Pass Criteria

- [ ] `get_tree` returns success
- [ ] Tree has nested structure (children property populated)
- [ ] Root element has controlType "Window" or "Pane"
- [ ] Filtered tree contains only Button elements
- [ ] Elements include valid boundingRect coordinates
- [ ] Number buttons (0-9) are discoverable in the tree

## Failure Indicators

- Error response from `ui_automation` tool
- Flat structure (no children)
- Missing element properties
- Filter not applied (non-Button elements in filtered result)

## Notes

- Calculator is used because it has a rich, button-heavy UI
- Windows 10/11 Calculator uses different element structures
- `depth` parameter controls tree traversal depth (1 = children only, 2 = grandchildren, etc.)
- Large trees may be truncated for performance
