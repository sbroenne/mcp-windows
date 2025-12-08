# Test Case: TC-VISUAL-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-VISUAL-005 |
| **Category** | VISUAL |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify that the LLM can detect when a window has been closed (no longer visible).

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open and visible
- [ ] Notepad has no unsaved changes (for clean close)

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Setup Notepad

1. Open Notepad on secondary monitor
2. Ensure it's visible and in focus
3. No unsaved changes (so it closes without dialog)

### Step 3: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `before.png`

**LLM Observation**: Note Notepad window is visible on screen.

### Step 4: Get Notepad Handle and Close

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

Then:

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 5: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=false`  
**Save As**: `after.png`

### Step 6: Visual Verification by LLM

Compare the two screenshots and identify:
1. **Is Notepad visible in the 'before' screenshot?** (Yes)
2. **Is Notepad visible in the 'after' screenshot?** (No)
3. **What is visible instead of Notepad?** (Desktop or other windows)
4. **Was the window closed successfully?** (Yes)

## Expected Result

The LLM correctly identifies that the Notepad window was visible before and is no longer visible after the close action.

## Pass Criteria

- [ ] LLM correctly identifies Notepad in before screenshot
- [ ] LLM correctly identifies Notepad is NOT in after screenshot
- [ ] LLM describes what's visible in place of Notepad
- [ ] LLM confirms window was closed

## Failure Indicators

- LLM reports Notepad still visible when it's not
- LLM cannot identify Notepad in before screenshot
- LLM reports wrong application was closed
- LLM misidentifies the window state

## Visual Verification Prompt

When comparing before/after screenshots, use this prompt:

> "Compare these two screenshots.
> 1. Is there a Notepad window visible in the 'before' screenshot?
> 2. Is there a Notepad window visible in the 'after' screenshot?
> 3. If Notepad is no longer visible, what is shown in its place?
> 4. Confirm whether the Notepad window was successfully closed."

## Notes

- Tests LLM's ability to detect window presence/absence
- Clear visual difference between window present and not
- Important for verifying cleanup operations
- May see desktop, other apps, or empty space after close
