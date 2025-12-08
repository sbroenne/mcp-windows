# Test Case: TC-KEYBOARD-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-004 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the Tab key inserts a tab character or navigates between controls.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Text cursor is positioned at beginning of a line

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor with some initial text:
- "Text before tab"

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Press Tab Key

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"tab"`

### Step 5: Type After Tab

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"After tab"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Tab character should create visible indentation
- "After tab" text is indented from the cursor position

## Expected Result

Tab key inserts a tab character, creating visible horizontal spacing.

## Pass Criteria

- [ ] `keyboard_control` press action returns success
- [ ] Tab character inserted (visible indentation)
- [ ] Subsequent text appears after the tab
- [ ] Tab width is consistent with Notepad's tab stop settings

## Failure Indicators

- No tab inserted
- Tab moved focus instead of inserting character
- Spaces inserted instead of tab
- Error response from tool

## Notes

- In Notepad, Tab inserts a tab character
- In dialogs, Tab navigates between controls
- This tests Tab behavior in a text editing context
- Default tab width in Notepad is typically 8 spaces
