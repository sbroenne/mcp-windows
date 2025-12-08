# Test Case: TC-KEYBOARD-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-009 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard shortcut Ctrl+V (Paste) pastes text from clipboard.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Clipboard contains text (copied previously)
- [ ] Cursor is in position to paste

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Prepare Clipboard Content

First, put known text on clipboard:
1. Type "Clipboard content" in Notepad
2. Select all (Ctrl+A)
3. Copy (Ctrl+C)
4. Clear the text (Delete or Ctrl+A then Delete)

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Text area should be empty.

### Step 4: Press Ctrl+V

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"v"`
- `modifiers`: `"ctrl"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- Before: Empty text area
- After: "Clipboard content" visible

## Expected Result

Clipboard content is pasted into Notepad at the cursor position.

## Pass Criteria

- [ ] `keyboard_control` combo action returns success
- [ ] Clipboard text appears in Notepad
- [ ] Text is inserted at cursor position
- [ ] Text matches what was copied

## Failure Indicators

- No text pasted
- Wrong text pasted
- Text pasted in wrong position
- Paste triggered other action
- Error response from tool

## Notes

- Ctrl+V pastes from system clipboard
- Content must be on clipboard before paste
- This tests clipboard read operation
- P0 priority as copy/paste is fundamental
