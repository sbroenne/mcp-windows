# Test Case: TC-ERROR-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-007 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | keyboard_control |
| **Dependencies** | None |

## Objective

Verify that the keyboard_control tool returns an appropriate error when given an invalid or unrecognized key name.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 2: Attempt Press with Invalid Key Name

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"invalidkeyname123"`

**Expected**: Error response indicating unrecognized key.

### Step 3: Attempt Press with Empty Key

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `""`

**Expected**: Error response indicating key is required.

### Step 4: Attempt Press with Special Characters

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"@#$%^"`

**Expected**: Error response indicating invalid key name.

### Step 5: Attempt Press with Very Long Key Name

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `"this_is_a_very_long_key_name_that_definitely_does_not_exist_in_any_keyboard_layout"`

**Expected**: Error response indicating unrecognized key.

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

**Verify**: Desktop unchanged.

## Expected Result

All invalid key names return error responses:
- Clear error messages explaining the key is not recognized
- No side effects on the system
- Tool remains functional after each error
- No crashes or unhandled exceptions

## Pass Criteria

- [ ] Invalid key name returns error (no crash)
- [ ] Empty key returns error (no crash)
- [ ] Special characters key returns error (no crash)
- [ ] Very long key name returns error (no crash)
- [ ] Error messages are descriptive
- [ ] Before/after screenshots show no changes
- [ ] Tool remains responsive

## Failure Indicators

- Any operation crashes
- Random key is pressed instead
- No error message returned (silent failure)
- Tool hangs
- System becomes unresponsive

## Notes

- Valid key names include: a-z, 0-9, f1-f24, enter, tab, escape, etc.
- Key names should be case-insensitive
- Error messages should ideally suggest valid key names
- This test validates input validation in the keyboard_control tool
