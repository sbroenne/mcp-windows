# Test Case: TC-MOUSE-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-009 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that mouse scroll wheel down action scrolls content downward.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with enough text to enable scrolling
- [ ] Notepad window is at or near top (room to scroll down)
- [ ] Cursor is positioned over Notepad's text area

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Scrollable Content

Open Notepad and add multiple lines of text so scrolling is possible:
- At least 50+ lines of text
- Ensure we're at or near the top

### Step 3: Move Cursor Over Notepad

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{notepad_center_x}` (center of Notepad window)
- `y`: `{notepad_center_y}` (center of Notepad window)

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Record which lines are visible at top of text area.

### Step 5: Scroll Down

**MCP Tool**: `mouse_control`  
**Action**: `scroll`  
**Parameters**:
- `direction`: `"down"`
- `amount`: `5` (scroll 5 clicks)

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Content should have scrolled down (later lines now visible)
- Earlier lines should have scrolled off the top

## Expected Result

Text content scrolls downward, revealing later content that was below the visible area.

## Pass Criteria

- [ ] `mouse_control` scroll action returns success
- [ ] Visible content changed between screenshots
- [ ] Content moved downward (later lines visible)
- [ ] Earlier visible lines scrolled off top

## Failure Indicators

- No scrolling occurred (same content visible)
- Content scrolled in wrong direction (up instead of down)
- Error response from tool
- Notepad lost focus during scroll

## Notes

- This test uses 5 clicks for more noticeable scroll
- Scroll amount can be adjusted based on content density
- Works best with numbered lines for easy verification
