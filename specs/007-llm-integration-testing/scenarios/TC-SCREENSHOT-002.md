# Test Case: TC-SCREENSHOT-002

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-SCREENSHOT-002 |
| **Category** | SCREENSHOT |
| **Priority** | P1 |
| **Target App** | System |
| **Target Monitor** | All |
| **Timeout** | 30 seconds |

## Objective

Verify that available monitors can be listed with their properties.

## Preconditions

- [ ] At least one monitor is connected
- [ ] Monitors are properly configured in Windows

## Steps

### Step 1: List Monitors

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Parameters**: (none required)

### Step 2: Record Response

Save the returned monitor information:
- Number of monitors
- For each monitor:
  - Index (0-based)
  - Resolution (width x height)
  - Position (x, y)
  - Primary flag
  - Device name (optional)

### Step 3: Verify Against System

Compare with Windows Display Settings:
- Number of monitors matches
- Resolutions match
- Primary monitor correctly identified

## Expected Result

A list of all connected monitors with their properties is returned.

## Pass Criteria

- [ ] `screenshot_control` list_monitors action returns success
- [ ] At least one monitor is returned
- [ ] Primary monitor is identified
- [ ] Each monitor has resolution and position
- [ ] Monitor indices are sequential (0, 1, 2...)

## Failure Indicators

- No monitors returned
- Missing monitor properties
- Incorrect resolution reported
- Primary monitor not identified
- Error response from tool

## Notes

- This is essential for multi-monitor support - P0 priority
- Monitor index is used in other screenshot operations
- Position values can be negative for left/top monitors
- Virtual desktop may have different bounds than physical

## Sample Response Format

```json
{
  "monitors": [
    {
      "index": 0,
      "width": 1920,
      "height": 1080,
      "x": 0,
      "y": 0,
      "isPrimary": true,
      "deviceName": "\\\\.\\DISPLAY1"
    },
    {
      "index": 1,
      "width": 2560,
      "height": 1440,
      "x": 1920,
      "y": 0,
      "isPrimary": false,
      "deviceName": "\\\\.\\DISPLAY2"
    }
  ]
}
```

(Actual format may vary based on implementation)
