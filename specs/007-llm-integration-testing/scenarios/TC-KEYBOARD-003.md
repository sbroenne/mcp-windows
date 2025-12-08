# Test Case: TC-KEYBOARD-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-003 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the Enter key can be pressed to create a new line.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Text cursor is positioned in the text area
- [ ] Some text exists on the first line

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Initial Text

Open Notepad and type some initial text:
- Line 1: "First line"

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Only one line of text visible.

### Step 4: Press Enter Key

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"enter"`

### Step 5: Type Second Line

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Second line"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: One line "First line"
- After: Two lines - "First line" and "Second line" on separate lines

## Expected Result

Enter key creates a new line, and "Second line" appears on line 2.

## Pass Criteria

- [ ] `keyboard_control` press action returns success
- [ ] New line is created
- [ ] "Second line" appears below "First line"
- [ ] Proper line break (not just text wrapping)

## Failure Indicators

- No new line created
- Enter key triggered other action (e.g., form submit)
- Text appears on same line
- Error response from tool

## Notes

- Enter key behavior may vary by application
- Notepad uses Enter for new line (not form submission)
- This tests the `press` action with a named key
