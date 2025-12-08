# Test Case: TC-WINDOW-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-004 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that the currently focused (foreground) window can be retrieved.

## Preconditions

- [ ] At least one window is open and has focus
- [ ] No dialogs blocking foreground detection

## Steps

### Step 1: Setup Known Foreground Window

1. Open Notepad
2. Click on Notepad to ensure it has focus

### Step 2: Get Foreground Window

**MCP Tool**: `window_management`  
**Action**: `get_foreground`  
**Parameters**: (none required)

### Step 3: Record Response

Save the returned window information:
- Window handle
- Window title
- Process name

### Step 4: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

### Step 5: Visual Verification

Compare the returned window title with what's visible as the active window.

## Expected Result

The tool returns information about the window that currently has focus.

## Pass Criteria

- [ ] `window_management` get_foreground action returns success
- [ ] Returned window matches the visible foreground window
- [ ] Valid handle is returned
- [ ] Title matches expected window

## Failure Indicators

- No window returned
- Wrong window reported as foreground
- Handle doesn't correspond to visible foreground
- Error response from tool

## Notes

- Foreground window is the window with keyboard focus
- Only one window can be foreground at a time
- Desktop may be reported if no application is focused
- P0 priority as this is essential for context detection

## Edge Cases

- Desktop focused (no application window)
- Dialog or popup is foreground
- Full-screen application
- Multiple monitors with different focus states
