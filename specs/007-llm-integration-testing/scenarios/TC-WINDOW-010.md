# Test Case: TC-WINDOW-010

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-010 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be resized to specific dimensions.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open in normal (not maximized) state
- [ ] Notepad's window handle is known
- [ ] Window is resizable

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

**Note**: Record Notepad's current size visually.

### Step 5: Resize Window

**MCP Tool**: `window_management`  
**Action**: `resize`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `width`: `800`
- `height`: `600`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad at original size
- After: Notepad at 800x600 pixels

## Expected Result

Notepad window is resized to 800x600 pixels.

## Pass Criteria

- [ ] `window_management` resize action returns success
- [ ] Window dimensions changed
- [ ] Window is at or near specified dimensions
- [ ] Window position remains unchanged (or minimal change)

## Failure Indicators

- Window size unchanged
- Wrong dimensions applied
- Window moved unexpectedly during resize
- Minimum size constraints prevent resize
- Error response from tool

## Notes

- Some windows have minimum size constraints
- Maximized windows cannot be resized directly
- Dimensions include window chrome (title bar, borders)
- Client area (content) is smaller than total dimensions
