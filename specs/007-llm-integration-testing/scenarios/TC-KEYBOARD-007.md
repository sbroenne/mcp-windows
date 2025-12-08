# Test Case: TC-KEYBOARD-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-007 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard shortcut Ctrl+A (Select All) works correctly.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with multiple lines of text
- [ ] Nothing is currently selected
- [ ] Notepad has focus

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Content

Open Notepad and add multiple lines of text:
```
Line 1: Hello World
Line 2: This is a test
Line 3: Multiple lines here
```

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Text visible but not selected (normal text color, no highlight).

### Step 4: Press Ctrl+A

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `"a"`
- `modifiers`: `"ctrl"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- Before: Normal text, no selection
- After: All text highlighted/selected (blue background on text)

## Expected Result

All text in Notepad is selected, shown with selection highlight.

## Pass Criteria

- [ ] `keyboard_control` combo action returns success
- [ ] All text is selected (highlighted)
- [ ] Selection includes all lines
- [ ] No text is left unselected

## Failure Indicators

- No selection visible
- Only partial selection
- Wrong action triggered
- Modifier key not applied
- Error response from tool

## Notes

- Ctrl+A is a universal shortcut for Select All
- This tests the `combo` action with modifier keys
- Selected text typically shows with inverted colors or highlight
- P0 priority as this is a fundamental keyboard shortcut
