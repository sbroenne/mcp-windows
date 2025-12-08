# Test Case: TC-WINDOW-011

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-011 |
| **Category** | WINDOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window's position and size can be set together using set_bounds.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open in normal (not maximized) state
- [ ] Notepad's window handle is known
- [ ] Target bounds are within screen

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

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

**Note**: Record Notepad's current position and size.

### Step 5: Set Window Bounds

**MCP Tool**: `window_management`  
**Action**: `set_bounds`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `x`: `50`
- `y`: `50`
- `width`: `600`
- `height`: `400`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad at original position/size
- After: Notepad at (50, 50) with size 600x400

## Expected Result

Notepad window is positioned at (50, 50) and sized to 600x400 in a single operation.

## Pass Criteria

- [ ] `window_management` set_bounds action returns success
- [ ] Window position changed to (50, 50)
- [ ] Window size changed to 600x400
- [ ] Both changes applied atomically

## Failure Indicators

- Position not changed
- Size not changed
- Only one of position/size changed
- Window snapped to different position
- Error response from tool

## Notes

- set_bounds combines move and resize in one operation
- More efficient than separate move + resize calls
- Avoids intermediate window states
- P2 priority as this is a convenience method
