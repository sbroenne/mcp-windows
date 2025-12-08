# Test Case: TC-ERROR-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-001 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | mouse_control |
| **Dependencies** | None |

## Objective

Verify that the mouse_control tool handles invalid coordinates gracefully by returning an appropriate error message when given coordinates outside the valid screen area.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect Monitor Bounds

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify valid coordinate ranges

**Record**: Maximum x and y values from combined monitor bounds.

### Step 2: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Document**: Current cursor position.

### Step 3: Attempt Move to Negative Coordinates

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `-10000`
- `y`: `-10000`

**Expected**: Error response or cursor constrained to valid area.

### Step 4: After Screenshot (Negative Test)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-03-negative.png`

### Step 5: Attempt Move to Extreme Positive Coordinates

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `99999`
- `y`: `99999`

**Expected**: Error response or cursor constrained to valid area.

### Step 6: After Screenshot (Positive Test)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

## Expected Result

The tool handles invalid coordinates gracefully:
- Either returns an error message explaining the coordinates are out of bounds
- Or constrains the cursor to the nearest valid screen edge
- No crash or unhandled exception occurs

## Pass Criteria

- [ ] Tool does not crash or hang
- [ ] Either error message returned OR cursor moved to screen boundary
- [ ] Cursor remains visible and controllable after invalid coordinate attempts
- [ ] No system-level errors or exceptions

## Failure Indicators

- Tool crashes or becomes unresponsive
- Cursor disappears or becomes stuck
- Unhandled exception thrown
- System becomes unstable

## Notes

- Windows typically clamps cursor position to valid screen bounds
- Virtual screen coordinates may extend beyond visible monitors
- Some remote desktop environments handle coordinates differently
- This test validates error handling, not successful movement
