# Test Case: TC-UIAUTOMATION-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-003 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Calculator |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |

## Objective

Verify that the `find_and_click` action can locate a button by name and click it in a single operation.

## Preconditions

- [ ] Calculator is installed (Windows built-in)
- [ ] No modal dialogs blocking the screen
- [ ] Calculator is in Standard mode (not Scientific)
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

**Verify**: Calculator display shows "0" or empty.

### Step 4: Click Button "5"

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Five"`
- `controlType`: `"Button"`

**Expected**: Returns success with clicked element info.

### Step 5: Click Button "+"

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Plus"`
- `controlType`: `"Button"`

**Expected**: Returns success with clicked element info.

### Step 6: Click Button "3"

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Three"`
- `controlType`: `"Button"`

**Expected**: Returns success with clicked element info.

### Step 7: Click Button "="

**MCP Tool**: `ui_automation`  
**Action**: `find_and_click`  
**Parameters**:
- `windowHandle`: `{calculator_handle}`
- `name`: `"Equals"`
- `controlType`: `"Button"`

**Expected**: Returns success with clicked element info.

### Step 8: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Calculator display shows "8" (5 + 3 = 8).

### Step 9: Visual Verification

Compare before and after screenshots:
- Before: Display shows "0" or empty
- After: Display shows "8"

### Step 10: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={calculator_handle}`

## Expected Result

The `find_and_click` action successfully:
1. Locates each button by name
2. Clicks the button (via InvokePattern or coordinate click)
3. Calculator computes 5 + 3 = 8

## Pass Criteria

- [ ] All `find_and_click` actions return success
- [ ] Calculator display shows "8" in after screenshot
- [ ] Each click triggered the expected button action
- [ ] No errors or element-not-found failures

## Failure Indicators

- Error response from `ui_automation` tool
- Element not found for button name
- Calculator display shows wrong result
- Button click had no effect

## Notes

- Button names may vary by locale (English: "Five", "Plus", "Three", "Equals")
- Windows 11 Calculator has different button naming than Windows 10
- `find_and_click` uses InvokePattern if available, falls back to coordinate click
- This test validates the combined find+click workflow reduces round-trips
