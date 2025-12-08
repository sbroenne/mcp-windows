# Test Case: TC-KEYBOARD-013

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-013 |
| **Category** | KEYBOARD |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that a sequence of key presses can be executed in order.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Text area is empty or has known content

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor with focus on text area.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 4: Execute Key Sequence

**MCP Tool**: `keyboard_control`  
**Action**: `sequence`  
**Parameters**:
- `sequence`: `[{"key":"h"},{"key":"e"},{"key":"l"},{"key":"l"},{"key":"o"}]`
- `interKeyDelayMs`: `50`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Text should show: "hello"
- Each key pressed in sequence
- Correct order preserved

## Expected Result

The key sequence is executed in order, producing "hello" in Notepad.

## Pass Criteria

- [ ] `keyboard_control` sequence action returns success
- [ ] All keys in sequence are pressed
- [ ] Keys are pressed in correct order
- [ ] Inter-key delay is respected (no dropped keys)

## Failure Indicators

- Missing characters
- Characters out of order
- Keys pressed too fast (dropped inputs)
- Wrong characters produced
- Error response from tool

## Notes

- Sequence action is useful for complex input patterns
- interKeyDelayMs controls timing between keys
- Differs from `type` action which handles string conversion
- Can include special keys in sequence (enter, tab, etc.)
- P2 priority as this is an advanced feature
