# Test Case: TC-ERROR-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-003 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | keyboard_control |
| **Dependencies** | None |

## Objective

Verify that the keyboard_control tool handles typing when no text input is focused, documenting the behavior (whether it types to active window or is ignored).

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No application with text input is focused
- [ ] Desktop or non-text-input window is in foreground
- [ ] No modal dialogs blocking

## Steps

### Step 1: Ensure Desktop Focus

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `d`
- `modifiers`: `win`

**Purpose**: Show desktop, ensuring no text input field is focused.

### Step 2: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: Desktop is showing with no active text input fields.

### Step 3: Attempt to Type Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Test text with no focused input"`

**Record**: Whether tool returns success, error, or warning.

### Step 4: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 5: Visual Comparison

Compare before.png and after.png:
- Document any visible changes
- Note if text appeared anywhere
- Verify no unexpected side effects

### Step 6: Restore Windows

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `d`
- `modifiers`: `win`

**Purpose**: Restore windows to previous state.

## Expected Result

One of the following behaviors (document which occurs):
1. Tool returns success but text goes nowhere (keyboard events sent but no receiver)
2. Tool returns warning that no text input is focused
3. Text triggers system actions (some keys may activate Start menu or other shortcuts)
4. Tool returns error indicating no valid target

## Pass Criteria

- [ ] Tool does not crash
- [ ] Behavior is documented (what happens when no input focused)
- [ ] Tool returns some response (success, warning, or error)
- [ ] System remains stable after attempt
- [ ] No unexpected windows or dialogs opened

## Failure Indicators

- Tool crashes or hangs
- System becomes unresponsive
- Unintended application launched
- Security-sensitive action triggered
- Data loss occurs

## Notes

- Windows sends keyboard events to the focused window regardless of input fields
- Some key combinations may trigger system shortcuts
- The Start menu may appear if text contains Windows key combinations
- This is a behavioral documentation test, not strictly pass/fail
- Some desktop environments may have search that captures keyboard input
