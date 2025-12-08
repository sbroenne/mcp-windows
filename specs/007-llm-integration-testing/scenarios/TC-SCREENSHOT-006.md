# Test Case: TC-SCREENSHOT-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-006 |
| **Category** | SCREENSHOT |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a specific window can be captured by its handle.

## Preconditions

- [ ] Notepad is open
- [ ] Notepad's window handle is known
- [ ] Notepad window is visible (not minimized)

## Steps

### Step 1: Setup Notepad

Open Notepad with some visible content.

### Step 2: Get Notepad's Handle

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Record the window handle.

### Step 3: Capture Window by Handle

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"window"`
- `windowHandle`: `{notepad_handle}`

### Step 4: Verify Window Capture

Decode and verify:
- Image shows only the Notepad window
- Window chrome (title bar, borders) included
- Desktop behind window is NOT included
- Other windows are NOT included

## Expected Result

A screenshot of only the Notepad window is captured, regardless of what's behind it.

## Pass Criteria

- [ ] `screenshot_control` capture action returns success
- [ ] Image contains only the Notepad window
- [ ] Window dimensions match Notepad's size
- [ ] No other windows or desktop visible
- [ ] Window chrome is included

## Failure Indicators

- Wrong window captured
- Desktop visible around/behind window
- Other windows appear in capture
- Minimized window produces empty/small image
- Invalid handle error
- Error response from tool

## Notes

- Window capture isolates a specific window
- Useful for documentation and focused testing
- May not work with minimized windows
- Handle must be valid and current
- P2 priority as region capture is often sufficient
