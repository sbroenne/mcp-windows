# Test Case: TC-ERROR-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-002 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | window_management |
| **Dependencies** | None |

## Objective

Verify that window_management tools handle invalid window handles gracefully by returning appropriate error messages without crashing.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 2: Attempt Activate with Invalid Handle

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"999999999"` (invalid/non-existent handle)

**Expected**: Error response indicating invalid handle.

### Step 3: Attempt Minimize with Invalid Handle

**MCP Tool**: `window_management`  
**Action**: `minimize`  
**Parameters**:
- `handle`: `"0"` (null handle)

**Expected**: Error response indicating invalid handle.

### Step 4: Attempt Move with Invalid Handle

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"-1"` (invalid handle)
- `x`: `100`
- `y`: `100`

**Expected**: Error response indicating invalid handle.

### Step 5: Attempt Resize with Invalid Handle

**MCP Tool**: `window_management`  
**Action**: `resize`  
**Parameters**:
- `handle`: `"abc123"` (non-numeric handle)
- `width`: `800`
- `height`: `600`

**Expected**: Error response indicating invalid handle format.

### Step 6: Attempt Close with Invalid Handle

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"9999999999999"` (very large number)

**Expected**: Error response indicating invalid handle.

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Desktop state unchanged (no windows affected).

## Expected Result

All operations with invalid handles return error responses:
- Clear error messages explaining the handle is invalid
- No windows affected on the desktop
- Tool remains functional after each error
- No crashes or unhandled exceptions

## Pass Criteria

- [ ] `activate` with invalid handle returns error (no crash)
- [ ] `minimize` with invalid handle returns error (no crash)
- [ ] `move` with invalid handle returns error (no crash)
- [ ] `resize` with invalid handle returns error (no crash)
- [ ] `close` with invalid handle returns error (no crash)
- [ ] Before/after screenshots show no desktop changes
- [ ] Tool remains responsive after all attempts

## Failure Indicators

- Any operation crashes or hangs
- A random window is affected
- Unhandled exception thrown
- Tool becomes unresponsive
- No error message returned (silent failure)

## Notes

- Window handles are typically large positive integers
- Handles become invalid when windows are closed
- Zero (0) is typically an invalid handle
- Error messages should be descriptive for debugging
- This test validates error handling across all window actions
