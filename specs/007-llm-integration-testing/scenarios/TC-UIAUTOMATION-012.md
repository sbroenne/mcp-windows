# Test Case: TC-UIAUTOMATION-012

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-012 |
| **Category** | UIAUTOMATION |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, window_management, screenshot_control |

## Objective

Verify that UI Automation correctly calculates monitor-relative coordinates for multi-monitor setups.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is installed (Windows built-in)
- [ ] User has standard (non-elevated) permissions

## Steps

### Step 1: Detect Monitors

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`

**Expected**: Returns array of monitors with bounds.

### Step 2: Launch Notepad on Secondary Monitor

```powershell
Start-Process notepad.exe
Start-Sleep -Seconds 1
```

### Step 3: Move Notepad to Secondary Monitor

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`

Then:
**MCP Tool**: `window_management`  
**Action**: `move_to_monitor`  
**Parameters**:
- `handle`: `{notepad_handle}`
- `monitorIndex`: `{secondary_monitor_index}`

### Step 4: Before Screenshot (All Monitors)

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="all_monitors"`, `includeCursor=true`  
**Save As**: `before.png`

### Step 5: Find Element on Secondary Monitor

**MCP Tool**: `ui_automation`  
**Action**: `find`  
**Parameters**:
- `windowHandle`: `{notepad_handle}`
- `name`: `"File"`
- `controlType`: `"MenuItem"`

**Expected**: Returns element with:
- `boundingRect`: Absolute screen coordinates
- `monitorIndex`: Index of secondary monitor
- `monitorRelativeRect`: Coordinates relative to secondary monitor

### Step 6: Verify Coordinate Mapping

Verify that:
1. `boundingRect.x` matches secondary monitor offset + local position
2. `monitorRelativeRect.x` is positive and within monitor width
3. `monitorIndex` matches the secondary monitor

### Step 7: Click Using Returned Coordinates

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**:
- `x`: `{element.monitorRelativeRect.x + width/2}`
- `y`: `{element.monitorRelativeRect.y + height/2}`
- `monitorIndex`: `{element.monitorIndex}`

**Expected**: Click lands on the File menu.

### Step 8: After Screenshot (All Monitors)

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="all_monitors"`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: File menu is open.

### Step 9: Close Menu

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"Escape"`

### Step 10: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={notepad_handle}`

## Expected Result

UI Automation correctly:
1. Returns absolute screen coordinates in `boundingRect`
2. Returns monitor-relative coordinates in `monitorRelativeRect`
3. Identifies correct `monitorIndex` for the element
4. Coordinates work seamlessly with `mouse_control`

## Pass Criteria

- [ ] Element found on secondary monitor
- [ ] `monitorIndex` matches secondary monitor
- [ ] `monitorRelativeRect` is within secondary monitor bounds
- [ ] Click using coordinates opens File menu
- [ ] Coordinates integrate correctly with mouse_control

## Failure Indicators

- Wrong monitor index returned
- Coordinates off by monitor offset
- Click lands on wrong location
- Error calculating coordinates

## Notes

- Multi-monitor coordinate handling is critical for real-world automation
- `boundingRect` uses Windows virtual screen coordinates (can be negative)
- `monitorRelativeRect` is always positive, relative to monitor top-left
- If secondary monitor unavailable, test validates primary monitor handling
- This test validates FR-014, FR-015 (multi-monitor coordinate integration)
