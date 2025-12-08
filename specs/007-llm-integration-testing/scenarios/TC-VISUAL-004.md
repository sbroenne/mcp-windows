# Test Case: TC-VISUAL-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-VISUAL-004 |
| **Category** | VISUAL |
| **Priority** | P1 |
| **Target App** | Calculator |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the LLM can detect UI state changes (button press, display update) in Calculator.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Calculator is open on secondary monitor
- [ ] Calculator display is clear/shows initial state

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Calculator

1. Open Calculator on secondary monitor
2. Clear any previous calculations (press C or clear)
3. Ensure display shows "0" or is empty

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `before.png`

**LLM Observation**: Note the Calculator display value (should be 0 or empty).

### Step 4: Click a Number Button

**MCP Tool**: `mouse_control`  
**Action**: `click`  
**Parameters**:
- `x`: `{button_5_x}` (coordinates of the "5" button)
- `y`: `{button_5_y}`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `after.png`

### Step 6: Visual Verification by LLM

Compare the two screenshots and identify:
1. **Has the Calculator display changed?** (Yes)
2. **What was the display before?** (0 or empty)
3. **What is the display now?** (5)
4. **Which button appears to have been pressed?** (5)

## Expected Result

The LLM correctly identifies that the Calculator display changed from 0 to 5 after clicking the "5" button.

## Pass Criteria

- [ ] LLM correctly identifies display changed
- [ ] LLM correctly reads the new display value (5)
- [ ] LLM recognizes the change corresponds to button click
- [ ] LLM can identify Calculator UI elements

## Failure Indicators

- LLM reports no display change
- LLM reads wrong display value
- LLM cannot identify Calculator UI
- LLM cannot read the numeric display

## Visual Verification Prompt

When comparing before/after screenshots, use this prompt:

> "Compare these two screenshots of Calculator.
> 1. What value is shown in the Calculator display in the 'before' image?
> 2. What value is shown in the Calculator display in the 'after' image?
> 3. Did clicking a button change the display?
> 4. Is the change consistent with pressing the '5' button?"

## Notes

- Tests LLM's ability to read numeric displays
- Calculator UI varies by Windows version
- Windows 11 Calculator has different layouts (Standard, Scientific, etc.)
- Ensure using Standard calculator for predictable layout
