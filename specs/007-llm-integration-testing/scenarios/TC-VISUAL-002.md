# Test Case: TC-VISUAL-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-VISUAL-002 |
| **Category** | VISUAL |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the LLM can detect text content changes by comparing before/after screenshots.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with initial text
- [ ] Text is clearly visible in screenshots

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad with Initial Text

1. Open Notepad on secondary monitor
2. Type initial text: "Hello World"
3. Ensure text is visible

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `before.png`

**LLM Observation**: Note the text content ("Hello World").

### Step 4: Add More Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"\nThis is a new line of text!"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `after.png`

### Step 6: Visual Verification by LLM

Compare the two screenshots and identify:
1. **Has the text content changed?** (Yes)
2. **What text was added?** "This is a new line of text!"
3. **Was any text removed?** (No)
4. **What is the complete visible text now?**

## Expected Result

The LLM correctly identifies that new text was added on a second line, and can read both the original and new text.

## Pass Criteria

- [ ] LLM correctly identifies text was added
- [ ] LLM correctly reads the new text content
- [ ] LLM correctly notes original text is preserved
- [ ] LLM can read text from the screenshots

## Failure Indicators

- LLM reports no text change
- LLM cannot read the text content
- LLM reports wrong text content
- LLM reports text was removed when it wasn't

## Visual Verification Prompt

When comparing before/after screenshots, use this prompt:

> "Compare these two screenshots of Notepad. Identify:
> 1. What text is visible in the 'before' screenshot?
> 2. What text is visible in the 'after' screenshot?
> 3. What text was added, modified, or removed?
> 4. Is the text change what we expected?"

## Notes

- Tests LLM's OCR/text reading capability
- Font size and clarity affect readability
- Use clear, large fonts for reliable detection
- Avoid complex formatting that may confuse text reading
