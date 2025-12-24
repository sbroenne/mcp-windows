# Test Case: TC-UIAUTOMATION-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-009 |
| **Category** | UIAUTOMATION |
| **Priority** | P2 |
| **Target App** | Calculator |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |

## Objective

Verify that the `scroll_into_view` action can scroll elements into the visible viewport.

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

### Step 2: Find Calculator Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Calculator"`

**Expected**: Returns window handle for Calculator.

### Step 3: Switch to Scientific Mode

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Open Navigation"` (hamburger menu)
- `controlType`: `"Button"`

Wait, then:
**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `name`: `"Scientific"`
- `timeout_ms`: `2000`

Then click:
**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `name`: `"Scientific"`
- `controlType`: `"ListItem"`

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 5: Find Element That May Need Scrolling

In Scientific mode, some functions may be in a scrollable panel.

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Hyperbolic sine"` (or another function that may be scrolled)
- `controlType`: `"Button"`

**Note**: If element is visible, this test validates scroll_into_view doesn't break visible elements.

### Step 6: Scroll Into View

**MCP Tool**: `ui_automation`  
**Action**: `scroll_into_view`  
**Parameters**:
- `elementId`: `{function_element_id}`

**Expected**: Returns success. If element was off-screen, it's now visible.

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

### Step 8: Verify Element is Clickable

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `elementId`: `{function_element_id}`

**Expected**: Element is clickable after scroll_into_view.

### Step 9: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={calculator_handle}`

## Expected Result

The `scroll_into_view` action successfully:
1. Identifies the parent scrollable container
2. Scrolls the element into the visible viewport
3. Element becomes interactable after scrolling

## Pass Criteria

- [ ] `scroll_into_view` action returns success
- [ ] Element is visible in after screenshot
- [ ] Element can be clicked after scroll
- [ ] No errors from scroll operation

## Failure Indicators

- ScrollItemPattern not supported
- Error response from `ui_automation` tool
- Element still not visible after scroll
- Unable to click element after scroll

## Notes

- `scroll_into_view` uses ScrollItemPattern on the element if available
- Falls back to ScrollPattern on parent container
- Some apps use virtualized lists where off-screen items don't exist
- For virtualized lists, scroll-and-search pattern is needed
- Calculator Scientific mode may have all buttons visible without scrolling
