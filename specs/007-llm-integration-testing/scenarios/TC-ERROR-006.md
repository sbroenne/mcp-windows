# Test Case: TC-ERROR-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-006 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |
| **Tools** | window_management |
| **Dependencies** | None |

## Objective

Verify that attempting to close an already-closed window returns an appropriate error without crashing.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is NOT currently open
- [ ] No modal dialogs blocking

## Steps

### Step 1: Open Notepad

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `r`
- `modifiers`: `win`

Then:

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `notepad`

Then:

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `enter`

Wait for Notepad to appear.

### Step 2: Find Notepad and Get Handle

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: `notepad_handle`

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Close Notepad (First Close)

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

**Expected**: Success - window closed.

### Step 5: Intermediate Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-04-closed.png`

**Verify**: Notepad is no longer visible.

### Step 6: Attempt to Close Again (Second Close)

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"{notepad_handle}"` (same handle as before)

**Expected**: Error indicating window no longer exists.

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

- First close operation succeeds
- Second close operation returns an error (window not found, invalid handle, etc.)
- No crash or hang occurs
- Clear error message provided

## Pass Criteria

- [ ] Notepad opens successfully
- [ ] Handle is obtained from `find`
- [ ] First `close` succeeds
- [ ] Screenshot confirms window is closed
- [ ] Second `close` returns error (not success)
- [ ] Error message indicates window doesn't exist
- [ ] Tool remains responsive

## Failure Indicators

- Second close reports success (should be error)
- Tool crashes on second close
- Silent failure (no error message)
- Handle reused for different window
- Unexpected window closed

## Notes

- Window handles become invalid after window closes
- Windows may reuse handle values for new windows
- The error should be clear about why close failed
- This pattern tests the stale handle scenario
