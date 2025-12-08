# Test Case: TC-MOUSE-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-008 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that mouse scroll wheel up action scrolls content upward.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with enough text to enable scrolling
- [ ] Notepad window is scrolled down (not at top)
- [ ] Cursor is positioned over Notepad's text area

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Scrollable Content

Open Notepad and add multiple lines of text so scrolling is possible:
- At least 50+ lines of text
- Scroll down so we're not at the top

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

### Step 5: Scroll Up

**MCP Tool**: `mouse_control`  
**Action**: `scroll`  
**Parameters**:
- `direction`: `"up"`
- `amount`: `3` (scroll 3 clicks)

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Content should have scrolled up (earlier lines now visible)
- Line numbers at top should be lower than before

## Expected Result

Text content scrolls upward, revealing earlier content that was above the visible area.

## Pass Criteria

- [ ] `mouse_control` scroll action returns success
- [ ] Visible content changed between screenshots
- [ ] Content moved upward (earlier lines visible)
- [ ] Scroll position indicator (if visible) shows upward movement

## Failure Indicators

- No scrolling occurred (same content visible)
- Content scrolled in wrong direction
- Error response from tool
- Notepad lost focus during scroll

## Notes

- Scroll amount is measured in "clicks" or notches
- Actual scroll distance depends on application and system settings
- Test requires pre-positioned content for reliable verification
