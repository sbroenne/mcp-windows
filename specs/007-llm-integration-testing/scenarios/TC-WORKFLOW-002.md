# Test Case: TC-WORKFLOW-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-002 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify the complete workflow of moving a window to a new position and visually verifying the position change through before/after screenshots.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and visible on the secondary monitor
- [ ] Notepad is not maximized (must be in restored/normal state)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find Notepad Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: Store the returned handle and current bounds (x, y, width, height).

### Step 3: Before Screenshot (Initial Position)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Observe and document Notepad's current position in the screenshot.

### Step 4: Calculate Target Position

Using secondary monitor bounds from Step 1:
- Calculate a new position that is visibly different from current position
- Suggested: Move to bottom-right quadrant of secondary monitor
- Example: `x = monitor_x + monitor_width - 600`, `y = monitor_y + monitor_height - 500`

### Step 5: Move Window to New Position

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `x`: `{calculated_x}`
- `y`: `{calculated_y}`

### Step 6: After Screenshot (New Position)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- "before.png": Notepad at original position
- "after.png": Notepad at new position (bottom-right quadrant)

Verify the window has clearly moved to a different location.

### Step 8: Verify Final Bounds

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Verify**: Returned bounds (x, y) match the target position.

## Expected Result

The workflow successfully:
1. Finds the Notepad window
2. Moves it to the calculated target position
3. Visual comparison confirms the position change
4. Final bounds query confirms the move

## Pass Criteria

- [ ] `find` action returns Notepad with valid handle
- [ ] `move` action returns success
- [ ] "Before" screenshot shows Notepad at original position
- [ ] "After" screenshot shows Notepad at new position
- [ ] Visual comparison confirms visible position change
- [ ] Final `find` returns bounds matching target position (within tolerance)
- [ ] Window size (width, height) unchanged after move

## Failure Indicators

- Notepad not found
- Move action fails or returns error
- Window appears in same position in before/after screenshots
- Window bounds don't match target position
- Window size changed unexpectedly
- Window moved to wrong monitor

## Notes

- Window move uses absolute screen coordinates
- For multi-monitor setups, ensure target coordinates are within secondary monitor bounds
- Maximized windows cannot be moved; must be restored first
- The window remains at the same size; only position changes
- Consider monitor DPI scaling when calculating positions
