# Test Case: TC-MOUSE-011

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-011 |
| **Category** | MOUSE |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that mouse drag operation (click-hold-move-release) works correctly.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] A draggable element is available (desktop icon, window, etc.)
- [ ] Target drag destination is clear

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Identify Drag Source and Destination

For this test:
- **Source**: A desktop icon or file on secondary monitor
- **Destination**: 200 pixels to the right

Calculate:
- `startX`: Source element center X
- `startY`: Source element center Y
- `endX`: startX + 200
- `endY`: startY (same Y for horizontal drag)

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Record position of the drag target element.

### Step 4: Perform Drag Operation

**MCP Tool**: `mouse_control`  
**Action**: `drag`  
**Parameters**:
- `startX`: `{source_x}`
- `startY`: `{source_y}`
- `endX`: `{destination_x}`
- `endY`: `{destination_y}`
- `button`: `"left"` (default)

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- Element should have moved from source to destination
- Element is now at or near the end coordinates

## Expected Result

The dragged element moves from the start position to the end position. Visual comparison confirms the position change.

## Pass Criteria

- [ ] `mouse_control` drag action returns success
- [ ] Element position changed in after screenshot
- [ ] Element is at or near the destination coordinates
- [ ] No residual selection or drag artifacts

## Failure Indicators

- Element didn't move
- Element moved to wrong position
- Drag started but didn't complete
- Error response from tool
- Element was copied instead of moved

## Notes

- Drag behavior depends on the element being dragged
- Desktop icons may snap to grid after drag
- File drag may trigger copy instead of move depending on context
- This test validates the drag mechanic, not the target behavior
