# Test Case: TC-WINDOW-013

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-013 |
| **Category** | WINDOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the tool can wait for a window to appear within a timeout period.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is NOT currently open
- [ ] Ability to launch Notepad during test

## Steps

### Step 1: Ensure Notepad is Closed

Close all instances of Notepad before starting.

### Step 2: Verify Notepad Not Found

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Should return no results.

### Step 3: Start Wait and Launch Notepad

Start the wait_for action, then launch Notepad:

**MCP Tool**: `window_management`  
**Action**: `wait_for`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`
- `timeoutMs`: `10000` (10 seconds)

While waiting, launch Notepad (manually or via script).

### Step 4: Record Wait Result

The tool should return successfully when Notepad appears.
Record:
- Time to find window
- Window handle returned
- Success/failure status

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

The wait_for action returns successfully after Notepad appears, before the timeout expires.

## Pass Criteria

- [ ] `window_management` wait_for action returns success
- [ ] Window handle is returned
- [ ] Action completed before timeout
- [ ] Notepad window is confirmed visible

## Failure Indicators

- Timeout expired before window found
- Window found but handle not returned
- False positive (wrong window detected)
- Error response from tool

## Notes

- wait_for polls for window appearance
- Useful for waiting for application launch
- Timeout prevents infinite waiting
- Can use regex for flexible matching
- P2 priority as this is an advanced synchronization feature

## Timing Considerations

- Notepad typically launches in < 2 seconds
- 10 second timeout provides margin
- If test times out, Notepad may have failed to launch
- Consider launching Notepad before calling wait_for for simpler test
