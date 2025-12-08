# Test Case: TC-VISUAL-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-VISUAL-003 |
| **Category** | VISUAL |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the LLM correctly identifies when NO change has occurred between screenshots (negative test).

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with static content
- [ ] No changes will be made between screenshots

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Static Content

1. Open Notepad on secondary monitor
2. Type some text: "This content will not change"
3. Position window and do not move

### Step 3: First Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `before.png`

### Step 4: NO Action (Intentional)

Wait 1-2 seconds without performing any actions.

### Step 5: Second Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `after.png`

### Step 6: Visual Verification by LLM

Compare the two screenshots and identify:
1. **Has anything changed?** (No)
2. **Is the window in the same position?** (Yes)
3. **Is the text content identical?** (Yes)
4. **Are there any differences at all?** (No significant differences)

## Expected Result

The LLM correctly identifies that the two screenshots are essentially identical with no meaningful changes.

## Pass Criteria

- [ ] LLM correctly reports no significant change
- [ ] LLM confirms window position is same
- [ ] LLM confirms text content is same
- [ ] LLM does NOT hallucinate changes that didn't occur

## Failure Indicators

- LLM reports changes when none occurred (false positive)
- LLM reports window moved when it didn't
- LLM reports text changed when it didn't
- LLM describes non-existent differences

## Visual Verification Prompt

When comparing before/after screenshots, use this prompt:

> "Compare these two screenshots carefully. 
> 1. Are there ANY differences between the before and after images?
> 2. Has the window position changed?
> 3. Has any text changed?
> 4. If you see no changes, confirm that the images appear identical."

## Notes

- This is a critical negative test
- LLM should not hallucinate changes
- Minor rendering differences (antialiasing, etc.) should be ignored
- False positives would undermine test reliability
- Important for establishing LLM baseline accuracy
