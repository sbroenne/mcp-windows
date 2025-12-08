# Test Case: TC-KEYBOARD-011

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-011 |
| **Category** | KEYBOARD |
| **Priority** | P2 |
| **Target App** | System (Desktop) |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard shortcut Win+D shows/hides the desktop.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Multiple windows are open
- [ ] Windows are not already minimized

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Windows

Ensure at least one visible window (e.g., Notepad) is open on the secondary monitor.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Application window(s) should be visible.

### Step 4: Press Win+D (Show Desktop)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"d"`
- `modifiers`: `"win"`

### Step 5: After First Screenshot (Desktop Shown)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after-minimize.png`

### Step 6: Press Win+D Again (Restore Windows)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"d"`
- `modifiers`: `"win"`

### Step 7: After Second Screenshot (Windows Restored)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after-restore.png`

### Step 8: Visual Verification

Compare screenshots:
- Before: Windows visible
- After-minimize: Desktop visible (windows minimized)
- After-restore: Windows visible again

## Expected Result

Win+D toggles between showing desktop and restoring windows.

## Pass Criteria

- [ ] `keyboard_control` combo action returns success both times
- [ ] First Win+D minimizes all windows (shows desktop)
- [ ] Second Win+D restores windows to previous state
- [ ] Windows return to their original positions

## Failure Indicators

- Windows not minimized
- Windows not restored on second press
- Wrong windows restored
- Win key stuck
- Error response from tool

## Notes

- Win+D is a system-level shortcut
- Acts as toggle: show desktop / restore windows
- All windows on all monitors are affected
- P2 priority as this is less commonly automated
