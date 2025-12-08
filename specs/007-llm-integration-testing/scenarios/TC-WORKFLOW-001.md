# Test Case: TC-WORKFLOW-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-001 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify the complete workflow of finding a window by title and activating it to the foreground using multiple MCP tool calls in sequence.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with a known title
- [ ] Another window is currently in foreground (Notepad is in background)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Before Screenshot (Initial State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: Notepad is visible but NOT in the foreground (another window is active).

### Step 3: Find Window by Title

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: Store the returned handle for use in Step 4.

### Step 4: Intermediate Screenshot (After Find)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-03-after-find.png`

**Verify**: State should be unchanged (find doesn't affect window).

### Step 5: Activate Window by Handle

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot (Final State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- "before.png": Notepad visible but in background
- "step-03-after-find.png": Same as before (no change)
- "after.png": Notepad now in foreground with active title bar

## Expected Result

The workflow successfully:
1. Finds the Notepad window by partial title match
2. Retrieves a valid window handle
3. Activates the window, bringing it to the foreground

## Pass Criteria

- [ ] `list_monitors` returns at least 1 monitor (secondary preferred)
- [ ] `find` action returns success with at least one match
- [ ] `find` returns a valid window handle
- [ ] `activate` action returns success
- [ ] "After" screenshot shows Notepad in foreground
- [ ] Notepad title bar shows active/focused state
- [ ] All three screenshots captured successfully

## Failure Indicators

- No monitors detected
- Notepad window not found
- Invalid or null handle returned
- Activate action fails
- Notepad still in background after activation
- Any tool returns an error response

## Notes

- This is a multi-step workflow that tests tool chaining
- The intermediate screenshot verifies that `find` is non-destructive
- If Notepad is already in foreground, activate should still succeed (no-op)
- Window handles are session-specific and may change between tests
- Total execution time should be under 60 seconds

## Workflow Dependencies

This scenario establishes the **Find-and-Activate** pattern used by:
- TC-WORKFLOW-003 (Type text in window)
- TC-WORKFLOW-004 (Click button and verify state)
- TC-WORKFLOW-006 (Resize and screenshot window)
