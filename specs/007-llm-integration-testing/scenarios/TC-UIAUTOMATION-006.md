# Test Case: TC-UIAUTOMATION-006

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-UIAUTOMATION-006 |
| **Category** | UIAUTOMATION |
| **Priority** | P2 |
| **Target App** | None (screen region) |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |
| **Tools** | ui_automation, screenshot_control |

## Objective

Verify that the `ocr` action can perform optical character recognition on a screen region.

## Preconditions

- [ ] Windows 11 (OCR API requirement)
- [ ] OCR language pack installed (English by default)
- [ ] Screen contains visible text in a known location

## Steps

### Step 1: Launch Notepad with Text

Open Notepad and type known text:

```powershell
Start-Process notepad.exe
Start-Sleep -Seconds 1
```

### Step 2: Type Known OCR Text

**MCP Tool**: `ui_automation`  
**Action**: `find_and_type`  
**Parameters**:
- `controlType`: `"Edit"`
- `text`: `"OCR Test 12345 ABCDE"`

**Note**: Using mixed case and numbers to test OCR accuracy.

### Step 3: Get Notepad Window Bounds

**MCP Tool**: `window_management`  
**Action**: `find`  
**Parameters**:
- `title`: `"Notepad"`

**Expected**: Returns window info including bounds.

### Step 4: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `before.png`

**Verify**: Text "OCR Test 12345 ABCDE" is visible.

### Step 5: Perform OCR on Region

**MCP Tool**: `ui_automation`  
**Action**: `ocr`  
**Parameters**:
- `x`: `{window_left + 10}`
- `y`: `{window_top + 50}`
- `width`: `{window_width - 20}`
- `height`: `200`
- `language`: `"en-US"`

**Expected**: Returns OCR result with:
- `text`: Contains "OCR Test 12345 ABCDE"
- `lines`: Array of line objects
- `words`: Array of word objects with bounding boxes

### Step 6: Check OCR Status

**MCP Tool**: `ui_automation`  
**Action**: `ocr_status`

**Expected**: Returns status with:
- `available`: `true`
- `defaultEngine`: `"Legacy"` (Windows.Media.Ocr)
- `availableLanguages`: Array including `"en-US"`

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**: `target="primary_screen"`, `includeCursor=false`  
**Save As**: `after.png`

### Step 8: Cleanup

**MCP Tool**: `window_management`  
**Action**: `close`  
**Parameters**: `handle={notepad_handle}`

Click "Don't Save" if prompted.

## Expected Result

The `ocr` action successfully:
1. Captures the specified screen region
2. Recognizes text in the region
3. Returns structured results with word bounding boxes
4. Matches the known input text

## Pass Criteria

- [ ] `ocr` action returns success
- [ ] Recognized text contains "OCR Test 12345 ABCDE" (case-insensitive)
- [ ] Result includes word-level bounding boxes
- [ ] `ocr_status` shows OCR is available
- [ ] At least one language (en-US) is available

## Failure Indicators

- Error response from `ui_automation` tool
- OCR returns empty text
- Text recognition is completely wrong
- OCR status shows unavailable
- Bounding boxes are missing or invalid

## Notes

- OCR uses Windows.Media.Ocr (legacy API) on all Windows 10+ systems
- NPU-accelerated OCR is not implemented (requires MSIX packaging)
- Accuracy depends on font size, contrast, and image quality
- Small text or unusual fonts may have lower accuracy
- Confidence scores are returned but may be -1 for legacy API
