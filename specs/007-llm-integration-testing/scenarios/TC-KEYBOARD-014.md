# Test Case: TC-KEYBOARD-014

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-014 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that arrow keys (up, down, left, right) work for navigation.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with multiple lines of text
- [ ] Cursor is in the middle of the text
- [ ] Notepad has focus

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Multi-line Text

Create text with multiple lines:
```
Line 1: AAAA
Line 2: BBBB
Line 3: CCCC
```
Place cursor at the start of "BBBB" (line 2).

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Press Right Arrow (3 times)

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"right"`
- `repeat`: `3`

(Cursor moves 3 characters right)

### Step 5: Press Down Arrow

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"down"`

(Cursor moves to line 3)

### Step 6: Type Marker

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"X"`

(Insert "X" at cursor position to mark where we navigated)

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 8: Visual Verification

Line 3 should show: "CCCXC" or "CCXCC" (depending on exact cursor position)
- X inserted after arrow navigation

## Expected Result

Arrow keys navigate the cursor within the text. The inserted "X" confirms the cursor moved to the expected position.

## Pass Criteria

- [ ] `keyboard_control` press action returns success
- [ ] Right arrow moves cursor right
- [ ] Down arrow moves cursor to next line
- [ ] Repeat parameter works correctly
- [ ] Cursor position is as expected

## Failure Indicators

- Cursor didn't move
- Moved in wrong direction
- Repeat count not honored
- X inserted at wrong position
- Error response from tool

## Notes

- Arrow keys: up, down, left, right
- `repeat` parameter allows multiple presses
- Navigation behavior may vary at text boundaries
- Home/End keys also available for line navigation
