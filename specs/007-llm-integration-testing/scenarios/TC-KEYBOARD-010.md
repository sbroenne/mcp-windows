# Test Case: TC-KEYBOARD-010

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-010 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | System (Window Switcher) |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard shortcut Alt+Tab switches between windows.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] At least two windows are open (e.g., Notepad and Calculator)
- [ ] One window is in foreground, the other in background

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Multiple Windows

1. Open Notepad on secondary monitor
2. Open Calculator on secondary monitor
3. Activate Notepad (it should be in foreground)

### Step 3: Record Initial Foreground Window

**MCP Tool**: `window_management`  
**Action**: `get_foreground`  
**Purpose**: Confirm Notepad is foreground

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Notepad should be in foreground.

### Step 5: Press Alt+Tab

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"tab"`
- `modifiers`: `"alt"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Verify New Foreground Window

**MCP Tool**: `window_management`  
**Action**: `get_foreground`  
**Purpose**: Confirm Calculator is now foreground

### Step 8: Visual Verification

Compare screenshots:
- Before: Notepad in foreground
- After: Calculator in foreground

## Expected Result

Alt+Tab switches the foreground window from Notepad to Calculator.

## Pass Criteria

- [ ] `keyboard_control` combo action returns success
- [ ] Foreground window changed
- [ ] Previously-background window is now foreground
- [ ] Window switcher appeared and selected next window

## Failure Indicators

- No window switch occurred
- Window switcher appeared but didn't switch
- Wrong window activated
- Alt key stuck after operation
- Error response from tool

## Notes

- Alt+Tab is a system-level shortcut
- Single Alt+Tab switches to the most recent window
- Holding Alt shows window switcher UI
- This tests system shortcut integration
