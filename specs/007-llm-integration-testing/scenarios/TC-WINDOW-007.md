# Test Case: TC-WINDOW-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-007 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be maximized to fill the screen.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open in normal (not maximized) state
- [ ] Notepad's window handle is known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor in normal (windowed) state.
Ensure it's not already maximized.

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

**Note**: Notepad should be in windowed mode (not filling screen).

### Step 5: Maximize Window

**MCP Tool**: `window_management`  
**Action**: `maximize`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad in windowed mode (desktop visible around edges)
- After: Notepad filling entire screen (no desktop visible)

## Expected Result

Notepad window expands to fill the entire screen/monitor.

## Pass Criteria

- [ ] `window_management` maximize action returns success
- [ ] Notepad window fills the screen
- [ ] Window title bar shows maximize state (restore icon visible)
- [ ] Desktop is no longer visible behind Notepad

## Failure Indicators

- Window not maximized
- Window size unchanged
- Maximized to wrong monitor
- Handle not recognized
- Error response from tool

## Notes

- Maximized window fills one monitor (not all monitors)
- Maximize button in title bar changes to restore button
- Works on normal and minimized windows
- Doesn't work on already-maximized windows (no-op)
