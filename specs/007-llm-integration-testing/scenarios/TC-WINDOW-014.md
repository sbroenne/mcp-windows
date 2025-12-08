# Test Case: TC-WINDOW-014

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-014 |
| **Category** | WINDOW |
| **Priority** | P2 |
| **Target App** | Multiple |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that windows can be filtered by process name.

## Preconditions

- [ ] Multiple applications are open
- [ ] At least one application has multiple windows (optional)
- [ ] Process names are known

## Steps

### Step 1: Setup Multiple Applications

Open:
- Notepad (process: notepad.exe)
- Calculator (process: Calculator.exe or calc.exe)
- Optional: Another Notepad instance

### Step 2: List Windows with Process Filter

**MCP Tool**: `window_management`  
**Action**: `list`  
**Parameters**:
- `filter`: `"notepad"` (filter by process name)

### Step 3: Record Filtered Results

Save the returned windows:
- Count of windows returned
- All should be Notepad processes
- No Calculator windows should appear

### Step 4: List Windows with Different Filter

**MCP Tool**: `window_management`  
**Action**: `list`  
**Parameters**:
- `filter`: `"Calculator"`

### Step 5: Record Second Filter Results

Save the returned windows:
- Should only include Calculator
- No Notepad windows

### Step 6: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

## Expected Result

The filter parameter restricts the window list to matching process names.

## Pass Criteria

- [ ] `window_management` list action returns success
- [ ] "notepad" filter returns only Notepad windows
- [ ] "Calculator" filter returns only Calculator windows
- [ ] Filter is case-insensitive (implementation dependent)

## Failure Indicators

- Filter not applied (all windows returned)
- Wrong windows returned
- No windows returned despite matching apps being open
- Error response from tool

## Notes

- Filter matches process name, not window title
- Useful for finding all windows of an application
- Multiple windows from same process are all returned
- P2 priority as this is a convenience feature

## Process Name Examples

| Application | Process Name |
|-------------|--------------|
| Notepad | notepad.exe |
| Calculator | Calculator.exe |
| File Explorer | explorer.exe |
| VS Code | Code.exe |
| Chrome | chrome.exe |
| Edge | msedge.exe |
