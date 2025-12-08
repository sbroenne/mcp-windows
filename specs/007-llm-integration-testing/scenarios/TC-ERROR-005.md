# Test Case: TC-ERROR-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-005 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |
| **Tools** | window_management |
| **Dependencies** | None |

## Objective

Verify that the window_management `wait_for` action properly times out and returns an error when waiting for a window that never appears.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No window with the test title exists
- [ ] No modal dialogs blocking

## Steps

### Step 1: Verify Target Window Does Not Exist

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"NonExistentTestWindow12345"`

**Expected**: No matches found (empty result or null).

### Step 2: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 3: Wait for Non-Existent Window

**MCP Tool**: `window_management`  
**Action**: `wait_for`  
**Parameters**:
- `title`: `"NonExistentTestWindow12345"`
- `timeoutMs`: `5000` (5 seconds)

**Record**:
- Start time
- End time
- Response content

**Expected**: Error returned after ~5 seconds indicating timeout.

### Step 4: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 5: Verify Timing

Calculate elapsed time between Step 3 start and response.

**Expected**: Approximately 5 seconds (within tolerance of 0.5 seconds).

## Expected Result

The `wait_for` action:
- Waits for the specified timeout duration
- Returns an error or "not found" response after timeout
- Does not hang indefinitely
- Respects the timeout parameter

## Pass Criteria

- [ ] `find` confirms window doesn't exist initially
- [ ] `wait_for` returns after approximately timeoutMs
- [ ] Error or "not found" response received
- [ ] Elapsed time is close to specified timeout (±500ms)
- [ ] Tool remains responsive after timeout
- [ ] No crash or hang

## Failure Indicators

- Wait returns immediately (timeout not respected)
- Wait hangs beyond specified timeout
- No error message returned
- Tool crashes during wait
- Incorrect error message

## Notes

- Timeout precision may vary based on system load
- Short timeout (5s) used to keep test duration reasonable
- The wait_for action should poll internally
- This test validates both timeout enforcement and error handling
