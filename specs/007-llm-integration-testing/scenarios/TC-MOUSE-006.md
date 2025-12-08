# Test Case: TC-MOUSE-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-006 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that right-click opens a context menu at the cursor position.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Desktop or Explorer window visible for right-click context menu
- [ ] No existing context menus or popups open

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Move to Desktop Area

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{target_center_x}` (center of secondary monitor)
- `y`: `{target_center_y}` (center of secondary monitor)

Ensure we're over the desktop, not an application window.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: No context menu visible in before screenshot.

### Step 4: Perform Right-Click

**MCP Tool**: `mouse_control`  
**Action**: `right_click`  
**Parameters**: (none - clicks at current position)

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- "Before": No context menu visible
- "After": Context menu visible near cursor position

### Step 7: Cleanup - Dismiss Context Menu

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**: `key="escape"`

**Purpose**: Close the context menu to clean up.

## Expected Result

Right-click produces a context menu at the cursor position. The context menu is visible in the "after" screenshot.

## Pass Criteria

- [ ] `mouse_control` right_click action returns success
- [ ] Context menu appears in after screenshot
- [ ] Context menu is positioned near cursor
- [ ] Menu contains expected items (View, Sort by, etc. for desktop)

## Failure Indicators

- No context menu appears
- Context menu appears at wrong position
- Error response from tool
- Different menu appears (application-specific instead of desktop)

## Notes

- Context menu content varies by right-click target
- Desktop right-click shows Windows desktop context menu
- Test cleanup dismisses menu with Escape key
