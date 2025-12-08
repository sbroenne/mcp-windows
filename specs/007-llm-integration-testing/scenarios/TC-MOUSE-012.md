# Test Case: TC-MOUSE-012

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-012 |
| **Category** | MOUSE |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that mouse click with modifier key (Ctrl+Click) is performed correctly.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Target that responds to Ctrl+Click is available (e.g., file explorer for multi-select)
- [ ] No existing selection that would interfere

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Test Environment

Open File Explorer on secondary monitor with multiple files visible.
This allows testing Ctrl+Click for multi-selection.

### Step 3: Select First Item

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**:
- `x`: `{file1_x}` (first file position)
- `y`: `{file1_y}` (first file position)

This selects the first file normally.

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: One file should be selected (highlighted).

### Step 5: Ctrl+Click Second Item

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**:
- `x`: `{file2_x}` (second file position)
- `y`: `{file2_y}` (second file position)
- `modifiers`: `"ctrl"`

### Step 6: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Visual Verification

Compare screenshots:
- Before: One file selected
- After: Two files selected (both highlighted)

## Expected Result

Ctrl+Click adds the second file to the selection without deselecting the first. Both files are highlighted in the after screenshot.

## Pass Criteria

- [ ] `mouse_control` click with modifier returns success
- [ ] First file remains selected after Ctrl+Click
- [ ] Second file is added to selection
- [ ] Both files highlighted in after screenshot

## Failure Indicators

- First file deselected (normal click instead of Ctrl+Click)
- Modifier not applied
- Only one file selected in final state
- Error response from tool

## Notes

- Modifier can be: ctrl, shift, alt
- Ctrl+Click is commonly used for multi-selection
- Shift+Click typically extends selection
- Test validates modifier key integration with click action
