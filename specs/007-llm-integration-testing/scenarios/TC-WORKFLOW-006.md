# Test Case: TC-WORKFLOW-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-WORKFLOW-006 |
| **Category** | WORKFLOW |
| **Priority** | P2 |
| **Target App** | Notepad |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 60 seconds |

## Objective

Verify the complete workflow of resizing a window to specific dimensions and taking a screenshot to verify the resize operation.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Notepad is open on secondary monitor
- [ ] Notepad is not maximized (must be in restored/normal state)
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

**Record**: Store the returned handle and current bounds (width, height).

### Step 3: Before Screenshot (Original Size)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

**Note**: Document Notepad's current size visually.

### Step 4: Define Target Dimensions

Choose target dimensions that are:
- Noticeably different from current size
- Within monitor bounds
- Reasonable for Notepad (e.g., 800x600 or 1024x768)

### Step 5: Resize Window

**MCP Tool**: `window_management`  
**Action**: `resize`  
**Parameters**:
- `handle`: `"{notepad_handle}"`
- `width`: `800`
- `height`: `600`

### Step 6: After Screenshot (Resized)

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 7: Verify New Dimensions

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`
- `regex`: `true`

**Verify**: Returned bounds show width=800, height=600 (or close to it).

### Step 8: Visual Verification

Compare screenshots:
- "before.png": Notepad at original size
- "after.png": Notepad at 800x600

Verify the window size change is visible.

## Expected Result

The workflow successfully:
1. Finds Notepad window and records original size
2. Resizes window to 800x600
3. Visual comparison confirms size change
4. Programmatic verification confirms new dimensions

## Pass Criteria

- [ ] `find` action returns Notepad with valid handle
- [ ] `resize` action returns success
- [ ] "Before" and "After" screenshots show visible size difference
- [ ] Final `find` returns width close to 800
- [ ] Final `find` returns height close to 600
- [ ] Window position (x, y) unchanged after resize

## Failure Indicators

- Notepad not found
- Resize action fails or returns error
- Window size unchanged in screenshots
- Window moved during resize
- Final dimensions don't match target (beyond tolerance)
- Window extended beyond monitor bounds

## Notes

- Window dimensions include frame/border (may add ~10-20 pixels)
- Actual dimensions may differ slightly due to DPI scaling
- Windows has minimum window sizes; very small dimensions may be adjusted
- Maximized windows must be restored before resizing
- Some applications have fixed aspect ratios or size constraints
