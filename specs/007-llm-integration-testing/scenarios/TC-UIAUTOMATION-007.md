# Test Case: TC-UIAUTOMATION-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-007 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Calculator |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |

## Objective

Verify that the `get_text` action can read text content from UI elements.

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

### Step 3: Click Some Numbers

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"One"`
- `controlType`: `"Button"`

Then:
**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Two"`
- `controlType`: `"Button"`

Then:
**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Three"`
- `controlType`: `"Button"`

**Expected**: Display shows "123".

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 5: Find Display Element

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `automationId`: `"CalculatorResults"` (Windows 10/11 Calculator)

**Alternative** (if automationId fails):
- `controlType`: `"Text"`
- `name` contains `"123"`

**Expected**: Returns display element with elementId.

### Step 6: Get Text from Display

**MCP Tool**: `ui_automation`  
**Action**: `get_text`  
**Parameters**:
- `elementId`: `{display_element_id}`

**Expected**: Returns text containing "123".

### Step 7: Get Text from Window (All Text)

**MCP Tool**: `ui_automation`  
**Action**: `get_text`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `includeChildren`: `true`

**Expected**: Returns aggregated text from all elements in the window.

### Step 8: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

### Step 9: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={calculator_handle}`

## Expected Result

The `get_text` action successfully:
1. Reads text from specific element (display shows "123")
2. Aggregates text from window when `includeChildren` is true
3. Returns structured text content

## Pass Criteria

- [ ] `get_text` on display element returns "123" (or "Display is 123")
- [ ] `get_text` with `includeChildren` returns button labels
- [ ] Text content matches visible display
- [ ] No errors or element-not-found failures

## Failure Indicators

- Error response from `ui_automation` tool
- Element not found for display
- Text is empty or missing
- Text doesn't match displayed value

## Notes

- Calculator display element has automationId "CalculatorResults" on Windows 10/11
- Text may include prefix like "Display is " depending on accessibility settings
- `get_text` uses ValuePattern first, then Name property, then TextPattern
- Windows 11 Calculator has different control structure than Windows 10
