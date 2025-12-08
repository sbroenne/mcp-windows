# Test Case: TC-WINDOW-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-005 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be activated (brought to foreground) by its handle.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open but NOT in foreground
- [ ] Another window (e.g., Calculator) is in foreground
- [ ] Notepad's window handle is known

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Windows

1. Open Notepad on secondary monitor
2. Open Calculator on secondary monitor
3. Click Calculator to make it foreground (Notepad is now background)

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

**Note**: Calculator should be in foreground, Notepad in background.

### Step 5: Activate Notepad by Handle

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Calculator in foreground
- After: Notepad in foreground (title bar active, window on top)

## Expected Result

Notepad is brought to the foreground and becomes the active window.

## Pass Criteria

- [ ] `window_management` activate action returns success
- [ ] Notepad is now the foreground window
- [ ] Notepad title bar shows active state
- [ ] Notepad is on top of other windows

## Failure Indicators

- Notepad not activated
- Wrong window activated
- Handle not recognized
- Notepad flashes but doesn't gain focus
- Error response from tool

## Notes

- Window handle must be valid and current
- Some windows may resist activation (security feature)
- Activate brings window to foreground and gives it focus
- P0 priority as this is essential for window control
