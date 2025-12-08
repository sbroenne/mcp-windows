# Test Case: TC-SCREENSHOT-001

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-001 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |

## Objective

Verify that a screenshot of the primary screen can be captured.

## Preconditions

- [ ] Primary monitor is available
- [ ] Desktop or applications visible on screen
- [ ] No secure desktop (UAC/lock screen)

## Steps

### Step 1: Capture Primary Screen

**MCP Tool**: `screenshot_control`  
**Action**: `capture` (default)  
**Parameters**:
- `target`: `"primary_screen"`

### Step 2: Record Response

Save the response:
- Base64-encoded image data returned
- Image format (PNG expected)
- Image dimensions
- Any metadata

### Step 3: Verify Image

Decode the base64 image and verify:
- Image is valid PNG
- Dimensions match primary screen resolution
- Content shows expected desktop/applications

## Expected Result

A screenshot of the primary screen is captured and returned as base64 PNG data.

## Pass Criteria

- [ ] `screenshot_control` capture action returns success
- [ ] Base64 image data is returned
- [ ] Image decodes to valid PNG
- [ ] Image dimensions match screen resolution
- [ ] Image content matches visible screen

## Failure Indicators

- No image data returned
- Invalid base64 encoding
- Corrupted image data
- Wrong resolution captured
- Black or blank image
- Error response from tool

## Notes

- This is the most basic screenshot operation - P0 priority
- Primary screen is the default target
- No cursor included by default
- Image is returned as base64, not saved to file
- LLM can analyze the returned image directly
