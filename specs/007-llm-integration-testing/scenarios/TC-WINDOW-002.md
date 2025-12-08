# Test Case: TC-WINDOW-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-002 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be found by its exact title.

## Preconditions

- [ ] Notepad is open with a known title
- [ ] Title is unique (no other windows with same title)

## Steps

### Step 1: Setup Notepad with Known Title

Open Notepad. The default title is "Untitled - Notepad" (or localized equivalent).
For a more unique title, save a file with a specific name.

### Step 2: Find Window by Exact Title

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Untitled - Notepad"`

### Step 3: Record Response

Save the returned window information:
- Window handle
- Confirmed title
- Process information

### Step 4: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

## Expected Result

The tool returns the window matching the exact title provided.

## Pass Criteria

- [ ] `window_management` find action returns success
- [ ] Window with matching title is found
- [ ] Valid handle is returned
- [ ] Only one match (or expected number of matches)

## Failure Indicators

- No window found
- Wrong window returned
- Multiple unexpected matches
- Error response from tool
- Invalid handle returned

## Notes

- Exact title match is case-sensitive (implementation dependent)
- Window title may include file name if document is open
- Use `regex=true` for partial matching (see TC-WINDOW-003)
- P0 priority as this is essential for window targeting

## Variations

Test with different title patterns:
- "Untitled - Notepad" (default new document)
- "filename.txt - Notepad" (saved file)
- Localized titles for non-English Windows
