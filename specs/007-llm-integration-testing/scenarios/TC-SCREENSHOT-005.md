# Test Case: TC-SCREENSHOT-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-005 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |

## Objective

Verify that screenshots can include the mouse cursor.

## Preconditions

- [ ] Mouse cursor is visible on screen
- [ ] Cursor is in a known, visible position

## Steps

### Step 1: Move Cursor to Known Position

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `500`
- `y`: `500`

### Step 2: Capture Without Cursor

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"primary_screen"`
- `includeCursor`: `false`

Save as `without_cursor.png`

### Step 3: Capture With Cursor

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"primary_screen"`
- `includeCursor`: `true`

Save as `with_cursor.png`

### Step 4: Visual Comparison

Compare the two screenshots:
- `without_cursor.png` should NOT show cursor at (500, 500)
- `with_cursor.png` SHOULD show cursor at (500, 500)

## Expected Result

When `includeCursor=true`, the mouse cursor is visible in the captured image at its current position.

## Pass Criteria

- [ ] `screenshot_control` capture action returns success both times
- [ ] Screenshot without cursor shows no cursor
- [ ] Screenshot with cursor shows cursor at correct position
- [ ] Cursor appearance matches system cursor

## Failure Indicators

- Cursor always visible (even with includeCursor=false)
- Cursor never visible (even with includeCursor=true)
- Cursor at wrong position
- Cursor appearance incorrect
- Error response from tool

## Notes

- Default behavior may vary (cursor excluded by default in most tools)
- Cursor position should match the coordinates where cursor was moved
- Cursor type (arrow, I-beam, etc.) depends on what's under cursor
- P1 priority for visual verification tests
