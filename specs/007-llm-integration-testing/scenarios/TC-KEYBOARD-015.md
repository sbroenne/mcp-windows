# Test Case: TC-KEYBOARD-015

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-015 |
| **Category** | KEYBOARD |
| **Priority** | P2 |
| **Target App** | System |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that the keyboard layout can be queried using get_keyboard_layout action.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] System has at least one keyboard layout configured
- [ ] No special requirements

## Steps

### Step 1: Get Keyboard Layout

**MCP Tool**: `keyboard_control`  
**Action**: `get_keyboard_layout`  
**Parameters**: (none required)

### Step 2: Record Response

Save the returned keyboard layout information:
- Layout name (e.g., "English (United States)")
- Layout ID/code (e.g., "00000409")
- Any other metadata returned

### Step 3: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

## Expected Result

The tool returns information about the current keyboard layout.

## Pass Criteria

- [ ] `keyboard_control` get_keyboard_layout action returns success
- [ ] Layout name or identifier is returned
- [ ] Response is well-formed and parseable
- [ ] Information matches system keyboard settings

## Failure Indicators

- No layout information returned
- Error response from tool
- Incorrect layout reported
- Malformed response

## Notes

- This is an information query, not an action
- Useful for determining keyboard layout before typing
- Layout affects special character mapping
- P2 priority as this is informational only
- No visual verification needed (data response only)

## Verification

To verify the returned layout is correct:
1. Open Windows Settings → Time & Language → Language & region
2. Check "Preferred languages" and keyboard settings
3. Compare with tool response

## Sample Response Format

```json
{
  "layoutName": "English (United States)",
  "layoutId": "00000409",
  "inputLanguage": "en-US"
}
```

(Actual format may vary based on implementation)
