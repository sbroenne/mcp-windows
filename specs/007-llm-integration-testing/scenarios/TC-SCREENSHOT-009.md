# Test Case: TC-SCREENSHOT-009

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-009 |
| **Category** | SCREENSHOT |
| **Priority** | P2 |
| **Target App** | System |
| **Target Monitor** | Primary |
| **Timeout** | 30 seconds |

## Objective

Verify behavior when capturing a region that extends beyond screen boundaries.

## Preconditions

- [ ] Screen resolution is known
- [ ] Region is intentionally positioned to extend beyond screen

## Steps

### Step 1: Get Screen Resolution

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Get primary screen dimensions (e.g., 1920x1080)

### Step 2: Attempt Capture Extending Beyond Right Edge

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `1800` (near right edge)
- `regionY`: `100`
- `regionWidth`: `400` (extends 280 pixels beyond edge)
- `regionHeight`: `300`

### Step 3: Record Response

Document behavior:
- Error returned?
- Image returned? If so, what dimensions?
- Is out-of-bounds area black/transparent?
- Is region clipped to screen bounds?

### Step 4: Attempt Fully Off-Screen Capture

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `5000` (completely off screen)
- `regionY`: `5000`
- `regionWidth`: `400`
- `regionHeight`: `300`

Record response.

## Expected Result

The tool either:
1. Returns an error for out-of-bounds regions, OR
2. Clips the region to screen bounds (documented behavior), OR
3. Returns image with black/transparent areas for out-of-bounds portions

## Pass Criteria

- [ ] Tool does NOT crash
- [ ] Behavior is consistent and predictable
- [ ] If image returned, dimensions are sensible
- [ ] Completely off-screen region is handled appropriately
- [ ] Behavior is documented/understandable

## Failure Indicators

- Tool crashes
- Undefined or random behavior
- Garbage data in image
- No response (hang)
- Inconsistent behavior

## Notes

- This tests boundary condition handling
- Different implementations may behave differently
- Key is consistent, predictable behavior
- P2 priority as this is edge case handling
- Document the actual behavior for future reference
