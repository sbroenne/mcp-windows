# Test Case: TC-SCREENSHOT-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-004 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | Any |
| **Timeout** | 30 seconds |

## Objective

Verify that a rectangular region of the screen can be captured.

## Preconditions

- [ ] Region coordinates are within screen bounds
- [ ] Content is visible in the target region

## Steps

### Step 1: Define Region

Choose a 400x300 region at position (100, 100):
- regionX: 100
- regionY: 100
- regionWidth: 400
- regionHeight: 300

### Step 2: Take Reference Screenshot

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"primary_screen"`

Save as reference to verify region location.

### Step 3: Capture Region

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `100`
- `regionY`: `100`
- `regionWidth`: `400`
- `regionHeight`: `300`

### Step 4: Verify Region Image

Decode and verify:
- Image dimensions are exactly 400x300
- Content matches the specified region from full screenshot

## Expected Result

A screenshot of exactly the specified 400x300 pixel region is captured.

## Pass Criteria

- [ ] `screenshot_control` capture action returns success
- [ ] Image dimensions are exactly 400x300 pixels
- [ ] Image content matches the expected region
- [ ] Region boundaries are accurate (not shifted)

## Failure Indicators

- Wrong dimensions returned
- Wrong region captured (offset error)
- Full screen captured instead of region
- Region extends beyond screen bounds
- Error response from tool

## Notes

- Region coordinates are in screen pixels
- Coordinates may need adjustment for multi-monitor setups
- Negative coordinates possible for left/top monitors
- P1 priority as this is commonly used for focused captures
