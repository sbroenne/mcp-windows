# Test Case: TC-KEYBOARD-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-005 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the Escape key is pressed correctly and can dismiss dialogs or cancel operations.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with unsaved changes
- [ ] A dialog is open (e.g., "Save changes?" dialog)

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Dialog Scenario

1. Open Notepad on secondary monitor
2. Type some text (to create unsaved changes)
3. Press Alt+F4 or click X to trigger the "Do you want to save?" dialog

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Save dialog should be visible.

### Step 4: Press Escape Key

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"escape"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 6: Visual Verification

Compare screenshots:
- Before: Save dialog visible
- After: Dialog dismissed, back to Notepad with unsaved text

## Expected Result

Escape key dismisses the dialog and cancels the close operation. Notepad remains open with the unsaved text.

## Pass Criteria

- [ ] `keyboard_control` press action returns success
- [ ] Dialog is dismissed
- [ ] Notepad window remains open
- [ ] Unsaved text is preserved

## Failure Indicators

- Dialog not dismissed
- Wrong button activated (Save or Don't Save)
- Application closed unexpectedly
- Error response from tool

## Notes

- Escape is commonly used to cancel/dismiss dialogs
- Behavior may vary by dialog type
- This tests the `press` action with the Escape key
- Alternative: Test in Find/Replace dialog
