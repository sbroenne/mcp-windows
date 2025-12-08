# Test Case: TC-WINDOW-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-008 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a minimized or maximized window can be restored to normal state.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and either minimized or maximized
- [ ] Notepad's window handle is known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad in Minimized State

1. Open Notepad on secondary monitor
2. Minimize it using window controls or previous test

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

**Note**: Notepad should not be visible (minimized).

### Step 5: Restore Window

**MCP Tool**: `window_management`  
**Action**: `restore`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Notepad not visible (minimized)
- After: Notepad visible in normal windowed state

## Expected Result

Notepad window is restored from minimized state to its previous normal size and position.

## Pass Criteria

- [ ] `window_management` restore action returns success
- [ ] Notepad window is visible again
- [ ] Window returns to previous size (before minimize)
- [ ] Window returns to previous position

## Failure Indicators

- Window not restored
- Window appears at wrong size or position
- Window appears maximized instead of normal
- Handle not recognized
- Error response from tool

## Notes

- Restore from minimized: Window appears at previous size/position
- Restore from maximized: Window returns to normal size
- If already normal, restore may be a no-op
- This is the inverse of minimize/maximize
