# Test Case: TC-WORKFLOW-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-008 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Multiple (Notepad, Calculator) |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 120 seconds |

## Objective

Verify the workflow of manipulating multiple windows in a cascade arrangement: opening two applications, positioning them in a cascaded layout, and verifying both are visible.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open on secondary monitor
- [ ] Calculator is open on secondary monitor
- [ ] Both windows are in restored (not maximized) state
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

**Record**: `monitor_x`, `monitor_y`, `monitor_width`, `monitor_height`

### Step 2: Find Both Windows

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: `notepad_handle`

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Calculator"`
- `regex`: `true`

**Record**: `calculator_handle`

### Step 3: Before Screenshot (Current State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Set Window Sizes (Same Size for Cascade)

**MCP Tool**: `window_management`  
**Action**: `resize`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `width`: `600`
- `height`: `400`

**MCP Tool**: `window_management`  
**Action**: `resize`  
**Parameters**:
- `handle`: `"{calculator_handle}"`
- `width`: `600`
- `height`: `400`

### Step 5: Position Notepad (Back Window)

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `x`: `{monitor_x + 50}`
- `y`: `{monitor_y + 50}`

### Step 6: Position Calculator (Front Window, Offset)

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"{calculator_handle}"`
- `x`: `{monitor_x + 100}`
- `y`: `{monitor_y + 100}`

### Step 7: Bring Calculator to Front

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{calculator_handle}"`

### Step 8: Intermediate Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-07-cascade.png`

**Verify**: Both windows visible in cascade, Calculator in front.

### Step 9: Bring Notepad to Front

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 10: After Screenshot (Notepad Front)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 11: Visual Verification

Compare screenshots:
- "before.png": Initial state (random positions)
- "step-07-cascade.png": Cascaded with Calculator front
- "after.png": Cascaded with Notepad front

Both windows should be visible in cascade layout throughout.

## Expected Result

The workflow successfully:
1. Finds both Notepad and Calculator windows
2. Resizes both to same dimensions (600x400)
3. Positions them in cascade (offset by 50 pixels)
4. Toggles which window is in front
5. Both windows remain visible in cascade arrangement

## Pass Criteria

- [ ] Both windows found with valid handles
- [ ] Both windows resized to 600x400
- [ ] Cascade positioning shows offset (both visible)
- [ ] Activate correctly brings target window to front
- [ ] All screenshots show both windows visible
- [ ] Z-order changes correctly between steps

## Failure Indicators

- One or both windows not found
- Resize fails for either window
- Windows overlap completely (not cascaded)
- Activate doesn't change Z-order
- One window moves off-screen
- Windows on wrong monitor

## Notes

- Cascade offset of 50 pixels ensures title bars are visible
- Both windows should be the same size for clean cascade
- This pattern is useful for comparing content side-by-side
- Window activation determines which is "on top"
- Some windows resist being moved (floating tool windows)
