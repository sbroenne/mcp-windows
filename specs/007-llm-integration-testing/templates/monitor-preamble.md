# Monitor Detection Preamble

This is a reusable snippet that MUST be included at the start of every test scenario execution. It ensures tests run on the secondary monitor to avoid interfering with VS Code.

---

## Standard Preamble (Copy to Step 1 of every scenario)

```markdown
### Step 1: Detect and Target Secondary Monitor

**Purpose**: Identify available monitors and target secondary monitor for test execution.

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`

**Expected Response**:
```json
{
  "monitors": [
    { "index": 0, "isPrimary": true, "bounds": { "x": 0, "y": 0, "width": 2560, "height": 1440 } },
    { "index": 1, "isPrimary": false, "bounds": { "x": 2560, "y": 0, "width": 1920, "height": 1080 } }
  ]
}
```

**Logic**:
1. If 2+ monitors available: Use the first non-primary monitor (typically index 1)
2. If only 1 monitor: Mark test as using primary (acceptable fallback), log warning
3. Calculate center point: `centerX = bounds.x + (bounds.width / 2)`, `centerY = bounds.y + (bounds.height / 2)`

**Variables Set**:
- `targetMonitorIndex`: Index to use for screenshots (0 or 1)
- `targetBounds`: The bounds object for the target monitor
- `targetCenterX`: X coordinate of monitor center
- `targetCenterY`: Y coordinate of monitor center
```

---

## Usage Example

When executing a test, the LLM should:

1. **First**, invoke `screenshot_control` with `action="list_monitors"`
2. **Parse** the response to identify the secondary monitor
3. **Calculate** coordinates within that monitor's bounds
4. **Use** `monitorIndex` parameter for all subsequent screenshot calls
5. **Offset** mouse coordinates to target the secondary monitor

---

## Coordinate Calculation

Windows uses a virtual screen coordinate system where:
- Primary monitor typically starts at (0, 0)
- Secondary monitor starts at primary's right edge (e.g., x=2560 for a 2560-wide primary)

### Example: Targeting center of secondary monitor

```
Primary: 2560x1440 at (0, 0)
Secondary: 1920x1080 at (2560, 0)

Secondary center:
  X = 2560 + (1920 / 2) = 2560 + 960 = 3520
  Y = 0 + (1080 / 2) = 540
  
Target: (3520, 540)
```

---

## Fallback Behavior

If no secondary monitor is available:

1. Test is NOT automatically failed
2. Test proceeds on primary monitor with a **BLOCKED** warning
3. Result file should note: "Executed on primary monitor (no secondary available)"
4. Some tests may be marked BLOCKED if they specifically require isolation from VS Code

---

## Why Secondary Monitor?

Per Constitution v2.3.0, Principle XIV:

> Integration tests that interact with the Windows desktop (mouse, keyboard, screenshots) MUST use the secondary monitor when available to avoid interference with active VS Code session and developer workflow on the primary monitor.

This prevents:
- Accidental clicks on VS Code UI elements
- Screenshots capturing VS Code instead of test content
- Keyboard input going to the wrong window
- Test actions disrupting active development work

---

## Multi-Monitor Screenshot Best Practices

### 1. Always Check Window's Actual Monitor

After any window operation, verify the window's `monitor_index` in the response:

```json
{
  "window": {
    "handle": "12345",
    "monitor_index": 1,  // <-- Check this!
    "bounds": { "x": 2600, "y": 100, "width": 800, "height": 600 }
  }
}
```

### 2. Capture Screenshots from the Window's Monitor

Don't hardcode the monitor index. Use the `monitor_index` from the window response:

```markdown
**Correct**: Capture screenshot using `monitorIndex` = window's `monitor_index`
**Incorrect**: Always capture `monitorIndex` = 0 regardless of window location
```

### 3. Detect Cross-Monitor Straddling

A window may unexpectedly span multiple monitors. Calculate:
- `windowEndX = bounds.x + bounds.width`
- Compare against monitor boundaries

If a window on Monitor 1 (starting at x=2560) has `bounds.x = 2000`, it's straddling the primary monitor!

### 4. Capture Both Monitors for Position Tests

When testing window move/resize operations, consider capturing both monitors:

```markdown
1. Capture primary monitor screenshot
2. Capture secondary monitor screenshot
3. Verify window appears on expected monitor and ONLY that monitor
```

This catches bugs where windows are repositioned to the wrong monitor or span multiple monitors.
