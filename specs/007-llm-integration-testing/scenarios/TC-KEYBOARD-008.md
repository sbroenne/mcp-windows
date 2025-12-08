# Test Case: TC-KEYBOARD-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-008 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard shortcut Ctrl+C (Copy) copies selected text to clipboard.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with text
- [ ] Text is selected (using Ctrl+A or manual selection)
- [ ] Notepad has focus

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Selected Text

1. Open Notepad and type: "Text to copy"
2. Select all text with Ctrl+A

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Text should be selected (highlighted).

### Step 4: Press Ctrl+C

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"c"`
- `modifiers`: `"ctrl"`

### Step 5: Verify Copy by Pasting Elsewhere

1. Press End key to deselect and move cursor to end
2. Press Enter to create new line
3. Press Ctrl+V to paste

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: Single line of selected text
- After: Two lines - original and pasted copy

## Expected Result

Text is copied to clipboard and can be pasted, resulting in duplicate text.

## Pass Criteria

- [ ] `keyboard_control` combo action returns success
- [ ] Text is copied to clipboard (verified by paste)
- [ ] Pasted text matches original
- [ ] Original text is preserved (not cut)

## Failure Indicators

- Nothing pasted (clipboard empty)
- Different text pasted
- Original text was removed (Ctrl+X instead of Ctrl+C)
- Error response from tool

## Notes

- Ctrl+C copies without removing original
- Verification requires paste operation (Ctrl+V)
- This tests clipboard integration
- P0 priority as copy/paste is fundamental
