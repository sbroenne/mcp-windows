# Test Case: TC-ERROR-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-008 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | keyboard_control |
| **Dependencies** | None |

## Objective

Verify that the keyboard_control tool returns an appropriate error when given invalid modifier keys in a combo/shortcut operation.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 2: Attempt Combo with Invalid Modifier

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: `invalidmodifier`

**Expected**: Error response indicating unrecognized modifier.

### Step 3: Attempt Combo with Empty Modifier

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: ``

**Expected**: Either success (just key press) or error about empty modifiers.

### Step 4: Attempt Combo with Mixed Valid/Invalid Modifiers

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: `ctrl,fake,shift`

**Expected**: Error response indicating "fake" is not a valid modifier.

### Step 5: Attempt Combo with Special Characters in Modifier

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: `ctrl+alt` (wrong separator)

**Expected**: Error or misparse (comma should be separator, not plus).

### Step 6: Attempt Combo with Duplicate Modifiers

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `c`
- `modifiers`: `ctrl,ctrl,ctrl`

**Expected**: Either success (duplicates ignored) or error about duplicates.

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Desktop unchanged.

## Expected Result

Invalid modifier specifications return error responses:
- Clear error messages for invalid modifier names
- No unexpected key presses or side effects
- Tool remains functional after each error
- Valid modifiers: ctrl, shift, alt, win

## Pass Criteria

- [ ] Invalid modifier returns error (no crash)
- [ ] Mixed valid/invalid modifiers returns error
- [ ] Wrong separator format handled appropriately
- [ ] Error messages identify the invalid modifier
- [ ] Before/after screenshots show no unexpected changes
- [ ] Tool remains responsive

## Failure Indicators

- Any operation crashes
- Unintended keyboard action triggered
- No error message returned (silent failure)
- Tool hangs
- Partial execution (some modifiers applied)

## Notes

- Valid modifiers are: ctrl, shift, alt, win (or cmd on Mac)
- Modifiers should be comma-separated
- Case should be insensitive (CTRL, Ctrl, ctrl all valid)
- This test validates modifier parsing and validation
- Duplicate modifiers may be silently deduplicated or rejected
