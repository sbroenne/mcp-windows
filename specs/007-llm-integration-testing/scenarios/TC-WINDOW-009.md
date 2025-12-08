# Test Case: TC-WINDOW-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-009 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be moved to a specific screen position.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open in normal (not maximized) state
- [ ] Notepad's window handle is known
- [ ] Target position is within screen bounds

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds and calculate target position

### Step 2: Setup Notepad

Open Notepad on secondary monitor in windowed mode.

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

**Note**: Record Notepad's current position.

### Step 5: Move Window

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `x`: `100` (or calculated position on secondary monitor)
- `y`: `100`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad at original position
- After: Notepad at new position (100, 100 relative to monitor)

## Expected Result

Notepad window moves to the specified coordinates.

## Pass Criteria

- [ ] `window_management` move action returns success
- [ ] Window position changed
- [ ] Window is at or near specified coordinates
- [ ] Window size remains unchanged

## Failure Indicators

- Window didn't move
- Window moved to wrong position
- Window size changed during move
- Window snapped to edge/corner instead of exact position
- Error response from tool

## Notes

- Coordinates may be absolute (screen) or relative (monitor)
- Windows may snap to screen edges in Windows 11
- Maximized windows cannot be moved (must restore first)
- Position refers to top-left corner of window
