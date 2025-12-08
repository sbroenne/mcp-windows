# Test Case: TC-WINDOW-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WINDOW-003 |
| **Category** | WINDOW |
| **Priority** | P1 |
| **Target App** | Notepad |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that a window can be found using partial title matching (regex).

## Preconditions

- [ ] Notepad is open
- [ ] Window title contains "Notepad"

## Steps

### Step 1: Setup Notepad

Open Notepad (any title - new or with file open).

### Step 2: Find Window by Partial Title (Regex)

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `".*Notepad.*"`
- `regex`: `true`

### Step 3: Record Response

Save the returned window(s):
- Number of matches
- Window handles
- Full titles

### Step 4: Take Screenshot for Documentation

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `context.png`

## Expected Result

The tool returns all windows whose titles match the regex pattern.

## Pass Criteria

- [ ] `window_management` find action returns success
- [ ] At least one Notepad window is found
- [ ] All returned windows have "Notepad" in title
- [ ] Regex pattern is correctly interpreted

## Failure Indicators

- No windows found despite Notepad being open
- Windows without "Notepad" in title returned
- Regex not supported or incorrectly parsed
- Error response from tool

## Notes

- Regex allows flexible pattern matching
- Common patterns:
  - `.*Notepad.*` - contains "Notepad"
  - `^Untitled.*` - starts with "Untitled"
  - `.*\.txt - Notepad$` - ends with ".txt - Notepad"
- Be careful with regex special characters
- P1 priority for flexible window discovery

## Regex Examples

| Pattern | Matches |
|---------|---------|
| `.*Notepad.*` | Any window with "Notepad" anywhere |
| `^Calculator$` | Exactly "Calculator" |
| `.*\\.txt.*` | Any window with ".txt" in title |
| `Chrome\|Firefox` | Chrome OR Firefox windows |
