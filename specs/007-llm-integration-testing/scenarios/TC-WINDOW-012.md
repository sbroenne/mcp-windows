# Test Case: TC-WINDOW-012

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-012 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be closed programmatically.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with NO unsaved changes
- [ ] Notepad's window handle is known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open a fresh Notepad on secondary monitor.
**Important**: Do NOT type anything (no unsaved changes).

### Step 3: Get Notepad's Handle

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Record the returned handle.

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Notepad should be visible.

### Step 5: Close Window

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Verify Window Closed

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Should return no results (or fewer results if other Notepads open).

### Step 8: Visual Verification

Compare screenshots:
- Before: Notepad window visible
- After: Notepad window gone

## Expected Result

Notepad window is closed and no longer appears in the window list.

## Pass Criteria

- [ ] `window_management` close action returns success
- [ ] Notepad window is closed
- [ ] Window no longer appears in window list
- [ ] No "save changes" dialog appeared (clean close)

## Failure Indicators

- Window not closed
- Save dialog blocked the close (had unsaved changes)
- Handle not recognized
- Application crashed instead of clean close
- Error response from tool

## Notes

- Close sends WM_CLOSE message to window
- Applications with unsaved changes may show confirmation dialog
- **IMPORTANT**: Use clean Notepad (no unsaved text) for reliable test
- Close is different from "kill process" - allows graceful shutdown
- P1 priority as this is essential for cleanup
