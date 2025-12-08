# Test Case: TC-MOUSE-010

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-010 |
| **Category** | MOUSE |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that horizontal scroll (left/right) is supported and functions correctly.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Application with horizontal scrollable content is available
- [ ] Cursor positioned over horizontally scrollable area

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Horizontally Scrollable Content

Options:
- Wide spreadsheet in a viewer
- Web page with horizontal content
- Image viewer with zoomed image

### Step 3: Move Cursor Over Scrollable Area

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{content_center_x}` (center of scrollable content)
- `y`: `{content_center_y}` (center of scrollable content)

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 5: Attempt Horizontal Scroll Left

**MCP Tool**: `mouse_control`  
**Action**: `scroll`  
**Parameters**:
- `direction`: `"left"`
- `amount`: `2`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Check Response

Evaluate the tool response:
- If successful: Horizontal scroll is supported
- If error: Document the error message (feature may not be supported)

## Expected Result

Either:
1. Horizontal scroll is performed successfully, OR
2. Tool returns clear error/message indicating horizontal scroll is not supported

## Pass Criteria

- [ ] Tool responds without crashing
- [ ] If supported: Content scrolls horizontally
- [ ] If not supported: Clear error message returned
- [ ] Behavior is consistent and documented

## Failure Indicators

- Tool crashes or hangs
- Ambiguous error message
- Vertical scroll performed instead of horizontal
- No response from tool

## Notes

- Horizontal scroll may not be supported by all systems/mice
- This test validates graceful handling of the feature
- Result should document whether horizontal scroll is available
- P2 priority as this is less commonly used than vertical scroll
