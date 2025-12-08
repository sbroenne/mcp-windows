# Test Case: TC-WORKFLOW-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-003 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify the complete workflow of finding a window, activating it, and typing text into it, with visual verification that the text appears.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open with an empty or known document
- [ ] Notepad window is on the secondary monitor
- [ ] No modal dialogs blocking

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Find Notepad Window

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Record**: Store the returned handle.

### Step 3: Before Screenshot (Initial State)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Document what text (if any) is visible in Notepad.

### Step 4: Activate Notepad Window

**MCP Tool**: `window_management`  
**Action**: `activate`  
**Parameters**:
- `handle`: `"{notepad_handle}"`

**Purpose**: Ensure Notepad has focus before typing.

### Step 5: Post-Activate Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `step-04-activated.png`

**Verify**: Notepad is now in foreground with active title bar.

### Step 6: Type Test Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"Hello from LLM Integration Test!"`

### Step 7: After Screenshot (Text Typed)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 8: Visual Verification

Compare screenshots:
- "before.png": Notepad empty or with original content
- "step-04-activated.png": Notepad active/focused
- "after.png": Notepad showing "Hello from LLM Integration Test!"

## Expected Result

The workflow successfully:
1. Finds Notepad window
2. Activates it to receive keyboard input
3. Types the test string
4. Visual verification shows text in Notepad

## Pass Criteria

- [ ] `find` action returns Notepad with valid handle
- [ ] `activate` action returns success
- [ ] `type` action returns success
- [ ] "After" screenshot shows typed text visible in Notepad
- [ ] Text matches expected string exactly
- [ ] Notepad title may change to indicate unsaved changes (asterisk)

## Failure Indicators

- Notepad not found
- Activate action fails
- Type action fails or returns error
- Text not visible in after screenshot
- Text appears in wrong application
- Partial text typed (missing characters)
- Special characters not typed correctly

## Notes

- This workflow depends on the Find-and-Activate pattern from TC-WORKFLOW-001
- Ensure Notepad is in text editing mode (not in menu or dialog)
- The typed text will cause Notepad to show unsaved changes indicator (*)
- Consider clearing existing text first for cleaner verification
- For non-ASCII characters, ensure keyboard layout supports them
