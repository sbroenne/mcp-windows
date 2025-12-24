# Test Case: TC-UIAUTOMATION-011

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-011 |
| **Category** | UIAUTOMATION |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation |

## Objective

Verify that error handling works correctly for invalid element IDs and stale elements.

## Preconditions

- [ ] No specific application required
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Find with Invalid Window Handle

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `"999999999"` (invalid handle)
- `name`: `"Test"`

**Expected Error**:
- `success`: `false`
- `errorType`: `"window_not_found"`
- Error message indicates window handle is invalid

### Step 2: Get Text with Invalid Element ID

**MCP Tool**: `ui_automation`  
**Action**: `get_text`  
**Parameters**:
- `elementId`: `"invalid:element:id:12345"`

**Expected Error**:
- `success`: `false`
- `errorType`: `"element_not_found"`
- Error message indicates element ID is invalid

### Step 3: Invoke Pattern on Invalid Element

**MCP Tool**: `ui_automation`  
**Action**: `invoke`  
**Parameters**:
- `elementId`: `"stale:element:id"`
- `pattern`: `"Invoke"`

**Expected Error**:
- `success`: `false`
- `errorType`: `"element_not_found"` or `"element_stale"`

### Step 4: Wait For with Zero Timeout

**MCP Tool**: `ui_automation`  
**Action**: `wait_for`  
**Parameters**:
- `name`: `"NonExistentElement12345"`
- `controlType`: `"Button"`
- `timeout_ms`: `100` (very short timeout)

**Expected Error**:
- `success`: `false`
- `errorType`: `"timeout"`
- Error message includes elapsed time

### Step 5: Find with Conflicting Parameters

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- (no windowHandle, no search criteria)

**Expected Error**:
- `success`: `false`
- `errorType`: `"invalid_parameter"`
- Error message indicates missing parameters

### Step 6: OCR with Invalid Coordinates

**MCP Tool**: `ui_automation`  
**Action**: `ocr`  
**Parameters**:
- `x`: `-1000`
- `y`: `-1000`
- `width`: `100`
- `height`: `100`

**Expected Error**:
- `success`: `false`
- `errorType`: `"invalid_parameter"` or `"ocr_failed"`
- Error message indicates coordinates are out of bounds

## Expected Result

All error cases return:
1. `success: false`
2. Appropriate `errorType` from the defined set
3. Descriptive error message
4. No crashes or exceptions

## Pass Criteria

- [ ] Invalid window handle returns `window_not_found`
- [ ] Invalid element ID returns `element_not_found`
- [ ] Timeout returns `timeout` with elapsed time
- [ ] Missing parameters return `invalid_parameter`
- [ ] All error responses are well-structured JSON

## Failure Indicators

- Tool crashes or throws unhandled exception
- Generic error without specific errorType
- Missing error message
- Success returned for invalid input

## Notes

- Error types defined in contract:
  - `window_not_found`
  - `element_not_found`
  - `element_stale`
  - `pattern_not_supported`
  - `timeout`
  - `invalid_parameter`
  - `ocr_failed`
  - `elevated_target`
  - `focus_failed`
  - `click_failed`
  - `type_failed`
- Good error handling is essential for LLM agents to recover
- Error messages should be actionable (suggest what to fix)
