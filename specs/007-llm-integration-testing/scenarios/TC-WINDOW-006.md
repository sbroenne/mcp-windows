# Test Case: TC-WINDOW-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-006 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be minimized to the taskbar.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and visible (not already minimized)
- [ ] Notepad's window handle is known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor in normal or maximized state.

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

**Note**: Notepad window should be visible.

### Step 5: Minimize Window

**MCP Tool**: `window_management`  
**Action**: `minimize`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad window visible
- After: Notepad window not visible (minimized to taskbar)

## Expected Result

Notepad window is minimized and no longer visible on screen.

## Pass Criteria

- [ ] `window_management` minimize action returns success
- [ ] Notepad window is no longer visible
- [ ] Notepad appears in taskbar
- [ ] Window can be restored later

## Failure Indicators

- Window not minimized
- Window closed instead of minimized
- Minimize action failed
- Handle not recognized
- Error response from tool

## Notes

- Minimized windows go to the taskbar
- Window state changes to "minimized"
- Use restore action to bring window back
- Works on normal and maximized windows
