# Test Case: TC-VISUAL-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-VISUAL-001 |
| **Category** | VISUAL |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the LLM can detect a window position change by comparing before/after screenshots.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open in windowed mode
- [ ] Window position is within screen bounds

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

1. Open Notepad on secondary monitor
2. Position at (200, 200) using window_management or manually
3. Size to 600x400

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `before.png`

**LLM Observation**: Note the position of Notepad window (top-left corner near 200, 200).

### Step 4: Move Window

**MCP Tool**: `window_management`  
**Action**: `move`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `x`: `500`
- `y`: `300`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `after.png`

### Step 6: Visual Verification by LLM

Compare the two screenshots and identify:
1. **Has the window moved?** (Yes/No)
2. **Direction of movement**: Right and Down
3. **Approximate distance**: ~300px right, ~100px down
4. **Window size change**: None (same size)

## Expected Result

The LLM correctly identifies that the Notepad window has moved from approximately (200, 200) to (500, 300).

## Pass Criteria

- [ ] LLM correctly identifies window moved (not same position)
- [ ] LLM correctly identifies direction (right and down)
- [ ] LLM correctly notes window size is unchanged
- [ ] LLM describes position change accurately

## Failure Indicators

- LLM reports no change when window moved
- LLM reports wrong direction
- LLM reports size change when none occurred
- LLM cannot identify the Notepad window

## Visual Verification Prompt

When comparing before/after screenshots, use this prompt:

> "Compare these two screenshots. Identify:
> 1. What changed between the before and after images?
> 2. Did the Notepad window move? If so, in what direction?
> 3. Did the window size change?
> 4. Are there any other differences?"

## Notes

- This tests the LLM's visual analysis capability
- Core skill for all visual verification tests
- Window should be clearly identifiable in both screenshots
- Background should remain static for clear comparison
