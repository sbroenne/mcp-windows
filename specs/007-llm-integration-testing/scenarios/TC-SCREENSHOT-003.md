# Test Case: TC-SCREENSHOT-003

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-003 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | Secondary |
| **Timeout** | 30 seconds |

## Objective

Verify that a specific monitor can be captured by its index.

## Preconditions

- [ ] At least two monitors are connected
- [ ] Secondary monitor has visible content
- [ ] Monitor indices are known (from list_monitors)

## Steps

### Step 1: List Monitors to Get Index

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Get the secondary monitor index

### Step 2: Capture Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"monitor"`
- `monitorIndex`: `1` (or secondary monitor index)

### Step 3: Record Response

Save the response:
- Base64-encoded image data
- Image dimensions (should match secondary monitor resolution)

### Step 4: Verify Image Content

Decode and verify:
- Image shows content from secondary monitor only
- Resolution matches secondary monitor
- No content from primary monitor

## Expected Result

A screenshot of only the secondary monitor is captured and returned.

## Pass Criteria

- [ ] `screenshot_control` capture action returns success
- [ ] Image dimensions match secondary monitor resolution
- [ ] Image content shows secondary monitor only
- [ ] Primary monitor content is not included

## Failure Indicators

- Wrong monitor captured
- Both monitors captured
- Invalid monitor index error
- Image dimensions don't match
- Error response from tool

## Notes

- Monitor index 0 is typically primary
- This test requires multi-monitor setup
- If only one monitor, test should fail gracefully
- Essential for secondary monitor testing - P0 priority
