# Test Case: TC-WORKFLOW-005

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-005 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | None (opens Notepad) |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 90 seconds |

## Objective

Verify the complete workflow of opening an application using keyboard shortcuts (Win+R for Run dialog, then typing application name), and verifying the application opens.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is NOT currently open
- [ ] No Run dialog currently open
- [ ] No modal dialogs blocking keyboard input

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Before Screenshot (No Notepad)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Verify**: No Notepad window visible.

### Step 3: Verify Notepad Not Running

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Expected**: Either no matches or empty result.

### Step 4: Open Run Dialog

**MCP Tool**: `keyboard_control`  
**Action**: `combo`  
**Parameters**:
- `key`: `r`
- `modifiers`: `win`

### Step 5: Wait and Screenshot Run Dialog

Wait briefly for Run dialog to appear.

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `step-04-run-dialog.png`

**Verify**: Run dialog is visible with text input field.

### Step 6: Type Application Name

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `notepad`

### Step 7: Screenshot with Typed Text

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=true`  
**Save As**: `step-06-typed.png`

**Verify**: Run dialog shows "notepad" in the input field.

### Step 8: Press Enter to Execute

**MCP Tool**: `keyboard_control`  
**Action**: `press`  
**Parameters**:
- `key`: `enter`

### Step 9: Wait for Application

Wait 2-3 seconds for Notepad to launch.

### Step 10: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 11: Verify Notepad Opened

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Expected**: At least one Notepad window found.

### Step 12: Visual Verification

Compare screenshots:
- "before.png": No Notepad visible
- "step-04-run-dialog.png": Run dialog visible
- "step-06-typed.png": Run dialog with "notepad" text
- "after.png": Notepad window now visible

## Expected Result

The workflow successfully:
1. Opens Run dialog via Win+R
2. Types "notepad" in the dialog
3. Presses Enter to execute
4. Notepad application opens
5. Visual and programmatic verification confirm success

## Pass Criteria

- [ ] Win+R combo opens Run dialog
- [ ] Run dialog accepts typed input
- [ ] Enter key submits the command
- [ ] Notepad window appears within timeout
- [ ] `find` action locates the new Notepad window
- [ ] "After" screenshot shows Notepad visible

## Failure Indicators

- Run dialog doesn't open
- Keyboard input goes to wrong window
- Run command fails (error dialog)
- Notepad doesn't open within timeout
- Notepad opens on wrong monitor
- UAC or security dialog blocks execution

## Notes

- Run dialog typically opens on primary monitor regardless of focus
- Notepad may open on any monitor based on Windows settings
- If Notepad is already running, a new instance will open
- Cleanup: Close Notepad after test to reset state
- Some enterprise environments may block Win+R

## Cleanup

After test completion, close the Notepad instance:

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
