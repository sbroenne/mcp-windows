# Test Case: TC-SCREENSHOT-008

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-008 |
| **Category** | SCREENSHOT |
| **Priority** | P2 |
| **Target App** | System |
| **Target Monitor** | Any |
| **Timeout** | 30 seconds |

## Objective

Verify that attempting to capture a region with zero or negative dimensions returns an appropriate error.

## Preconditions

- [ ] Screen is available for capture
- [ ] Various invalid dimension scenarios prepared

## Steps

### Step 1: Attempt Capture with Zero Width

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `100`
- `regionY`: `100`
- `regionWidth`: `0`
- `regionHeight`: `300`

Record error response.

### Step 2: Attempt Capture with Zero Height

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `100`
- `regionY`: `100`
- `regionWidth`: `400`
- `regionHeight`: `0`

Record error response.

### Step 3: Attempt Capture with Negative Dimensions

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"region"`
- `regionX`: `100`
- `regionY`: `100`
- `regionWidth`: `-100`
- `regionHeight`: `-100`

Record error response.

## Expected Result

All attempts return clear errors indicating invalid dimensions.

## Pass Criteria

- [ ] Tool does NOT crash or hang for any case
- [ ] Each attempt returns an error response
- [ ] Error messages clearly indicate dimension problem
- [ ] No partial or corrupted images returned
- [ ] Consistent error handling across all cases

## Failure Indicators

- Tool crashes on invalid input
- Empty or 1x1 image returned
- No error (silent failure)
- Different behavior for zero vs negative
- Unclear error messages

## Notes

- Tests input validation for region capture
- Zero and negative dimensions are both invalid
- P2 priority as this is edge case handling
- Important for robustness of LLM interactions
