# Test Case: TC-WORKFLOW-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-009 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 90 seconds |

## Objective

Verify the workflow of simulating a drag-and-drop operation by selecting text in Notepad and dragging it to a new location within the document.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open on secondary monitor with multiple lines of text
- [ ] Example content: "Line 1\nLine 2\nLine 3"
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find and Activate Notepad

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: Handle and bounds.

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

### Step 3: Ensure Content Exists

Type test content if Notepad is empty:

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Line One\nLine Two\nLine Three"`

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Document shows three lines of text.

### Step 5: Select Text for Drag (Line Two)

Use keyboard to position and select:

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `home`
- `modifiers`: `ctrl`

(Cursor at start of document)

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `down`

(Cursor on Line Two)

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `end`
- `modifiers`: `shift`

(Line Two selected)

### Step 6: Selection Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-05-selected.png`

**Verify**: "Line Two" is highlighted.

### Step 7: Calculate Drag Coordinates

Based on Notepad window position and text layout:
- `startX`, `startY`: Center of selected text
- `endX`, `endY`: Position after Line Three

### Step 8: Perform Drag Operation

**MCP Tool**: `mouse_control`  
**Action**: `drag`  
**Parameters**:
- `startX`: `{start_x}`
- `startY`: `{start_y}`
- `endX`: `{end_x}`
- `endY`: `{end_y}`
- `button`: `left`

### Step 9: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 10: Visual Verification

Compare screenshots:
- "before.png": Lines in order (One, Two, Three)
- "step-05-selected.png": Line Two highlighted
- "after.png": Line order changed (One, Three, Two) OR text moved

## Expected Result

The workflow successfully:
1. Prepares text content in Notepad
2. Selects a line of text
3. Drags the selection to a new position
4. Text is moved to the new location

## Pass Criteria

- [ ] Text content created in Notepad
- [ ] Text selection visible (highlighted)
- [ ] Drag operation completes without error
- [ ] Text position changed after drag
- [ ] "After" screenshot shows text in new location
- [ ] No text lost during operation

## Failure Indicators

- Selection not visible
- Drag operation fails
- Text not moved (copy instead of move)
- Text deleted but not placed
- Drop location incorrect
- Application becomes unresponsive

## Notes

- Text drag-and-drop in Notepad moves text by default
- Holding Ctrl during drag copies instead of moves
- Drag coordinates must be precise for text selection
- Alternative: Use clipboard (cut/paste) for more reliable text movement
- Some applications don't support text drag-and-drop
