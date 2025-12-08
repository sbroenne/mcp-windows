# Test Case: TC-WORKFLOW-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-007 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 90 seconds |

## Objective

Verify the complete copy-paste workflow: type text, select all, copy to clipboard, paste, and verify the text is duplicated.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with empty document on secondary monitor
- [ ] Notepad has focus
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find and Activate Notepad

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Then:

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 3: Before Screenshot (Empty)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: Notepad is empty or clear.

### Step 4: Type Original Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Test Line"`

### Step 5: After Type Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-04-typed.png`

**Verify**: "Test Line" is visible in Notepad.

### Step 6: Select All (Ctrl+A)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `a`
- `modifiers`: `ctrl`

### Step 7: After Select Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-06-selected.png`

**Verify**: Text appears highlighted/selected.

### Step 8: Copy (Ctrl+C)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: `ctrl`

### Step 9: Move to End of Document

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `end`

Then:

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `enter`

### Step 10: Paste (Ctrl+V)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `v`
- `modifiers`: `ctrl`

### Step 11: After Screenshot (Pasted)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 12: Visual Verification

Compare screenshots:
- "before.png": Empty Notepad
- "step-04-typed.png": "Test Line" visible
- "step-06-selected.png": Text highlighted
- "after.png": Two lines of "Test Line" (original + pasted)

## Expected Result

The workflow successfully:
1. Types original text
2. Selects all text (Ctrl+A)
3. Copies to clipboard (Ctrl+C)
4. Adds new line
5. Pastes text (Ctrl+V)
6. Verification shows duplicated text

## Pass Criteria

- [ ] Text is typed successfully
- [ ] Ctrl+A selects the text (visible highlight)
- [ ] Ctrl+C copies without error
- [ ] Ctrl+V pastes the text
- [ ] "After" screenshot shows text appearing twice
- [ ] Both lines contain identical text

## Failure Indicators

- Text not typed
- Selection not visible
- Copy fails (nothing to paste)
- Paste fails or pastes wrong content
- Only one line of text visible at end
- Clipboard content incorrect

## Notes

- This test exercises the full clipboard workflow
- Clipboard state persists after test; may affect subsequent tests
- Windows clipboard history (Win+V) may be enabled
- Some enterprise environments restrict clipboard operations
- Consider clearing clipboard before test for isolation
