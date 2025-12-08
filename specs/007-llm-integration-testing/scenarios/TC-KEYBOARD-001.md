# Test Case: TC-KEYBOARD-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-001 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that simple text can be typed into an application using the keyboard_control tool.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and has focus
- [ ] Text cursor is positioned in the text area
- [ ] Text area is empty or has known content

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

Open Notepad on secondary monitor and ensure it has focus.
The text area should be empty for clean verification.

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Text area should be empty.

### Step 4: Type Simple Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Hello World"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- Before: Empty text area
- After: Text "Hello World" visible in Notepad

## Expected Result

The text "Hello World" is typed into Notepad and visible in the after screenshot.

## Pass Criteria

- [ ] `keyboard_control` type action returns success
- [ ] Text "Hello World" appears in Notepad
- [ ] All characters typed correctly (no missing/extra characters)
- [ ] Text cursor is at end of typed text

## Failure Indicators

- No text appeared in Notepad
- Text is garbled or incomplete
- Wrong characters typed
- Notepad lost focus during typing
- Error response from tool

## Notes

- This is a fundamental keyboard test - P0 priority
- Tests basic Latin characters and space
- No special characters or Unicode in this test
- Notepad should have focus before typing
