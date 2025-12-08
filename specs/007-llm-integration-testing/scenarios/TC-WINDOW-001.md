# Test Case: TC-WINDOW-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-001 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that the window_management tool can list all open windows.

## Preconditions

- [ ] At least two windows are open (e.g., Notepad and Calculator)
- [ ] Windows are not minimized to tray

## Steps

### Step 1: Ensure Test Windows Are Open

Open at least:
- Notepad
- Calculator

### Step 2: List All Windows

**MCP Tool**: `window_management`  
**Action**: `list`  
**Parameters**: (none required for basic list)

### Step 3: Record Response

Save the returned window list:
- Window titles
- Window handles
- Process names
- Window states (minimized, maximized, normal)

### Step 4: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

## Expected Result

The tool returns a list of all open windows with their properties.

## Pass Criteria

- [ ] `window_management` list action returns success
- [ ] Notepad appears in the window list
- [ ] Calculator appears in the window list
- [ ] Each window has a valid handle
- [ ] Each window has a title or process name

## Failure Indicators

- Empty window list returned
- Known windows missing from list
- Invalid or missing handles
- Error response from tool
- Malformed response structure

## Notes

- This is a fundamental window operation - P0 priority
- List may include system windows and background processes
- Hidden windows may or may not be included
- Window handles are process-specific identifiers

## Sample Response Format

```json
{
  "windows": [
    {
      "handle": "12345678",
      "title": "Untitled - Notepad",
      "processName": "notepad.exe",
      "state": "normal"
    },
    {
      "handle": "87654321",
      "title": "Calculator",
      "processName": "Calculator.exe",
      "state": "normal"
    }
  ]
}
```

(Actual format may vary based on implementation)
