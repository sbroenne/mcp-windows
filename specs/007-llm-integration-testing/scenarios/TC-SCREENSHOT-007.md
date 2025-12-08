# Test Case: TC-SCREENSHOT-007

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-007 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | Any |
| **Timeout** | 30 seconds |

## Objective

Verify that attempting to capture an invalid monitor index returns an appropriate error.

## Preconditions

- [ ] Monitor configuration is known
- [ ] Invalid index value is determined (e.g., 99)

## Steps

### Step 1: List Available Monitors

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Determine valid monitor indices

Record the highest valid index (e.g., 1 for a 2-monitor setup).

### Step 2: Attempt Capture with Invalid Index

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"monitor"`
- `monitorIndex`: `99` (or any index beyond available monitors)

### Step 3: Record Error Response

Save the error response:
- Error type/code
- Error message
- Any additional details

## Expected Result

The tool returns a clear error indicating the monitor index is invalid.

## Pass Criteria

- [ ] Tool does NOT crash or hang
- [ ] Error response is returned
- [ ] Error message clearly indicates invalid monitor index
- [ ] No partial or corrupted image returned

## Failure Indicators

- Tool crashes
- Tool hangs indefinitely
- No error returned (silent failure)
- Incorrect error message (misleading)
- Primary screen captured as fallback (undocumented)
- Random/undefined behavior

## Notes

- This is an error handling test
- Valid behavior: Clear error with helpful message
- Should NOT fall back to primary without indication
- P1 priority for robust error handling

## Expected Error Response

```json
{
  "error": true,
  "code": "INVALID_MONITOR_INDEX",
  "message": "Monitor index 99 is not valid. Available indices: 0, 1"
}
```

(Actual format may vary based on implementation)
