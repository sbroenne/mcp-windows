# Phase 0 Research: Mouse Position Awareness for LLM Usability

**Date**: December 11, 2025  
**Feature**: 012-mouse-position  
**Status**: Complete

## Overview

This research documents the API design patterns, implementation approach, and validation strategies for requiring explicit `monitorIndex` in the `mouse_control` tool.

---

## Decision: monitorIndex as Required Parameter

**Decision**: Make `monitorIndex` required when x/y coordinates are provided; allow omission for coordinate-less actions (click at current cursor position).

**Rationale**:
- Screenshot coordinates are **inherently monitor-relative** (0,0 = top-left of captured monitor's image).
- When an LLM uses `screenshot_control(monitorIndex: Z)`, it receives coordinates in that monitor's frame.
- If the LLM forgets to pass the matching `monitorIndex` to `mouse_control`, it silently clicks on **monitor 0 instead**, causing failures.
- Requiring explicit `monitorIndex` with coordinates **forces correctness by design**—the contract is unambiguous.

**Alternatives Considered & Rejected**:
1. **Add `move_relative` action** (original spec):
   - ❌ Unnecessary complexity; LLM can calculate absolute coordinates from screenshot.
   - ❌ Doesn't solve the root problem: forgetting which monitor was captured.
2. **Deprecation period for missing `monitorIndex`**:
   - ❌ Defers clarity; LLMs benefit from immediate, strict contracts.
   - ✅ We chose immediate enforcement (hardbreak).
3. **Optional with smart defaults** (e.g., detect monitor from screenshot metadata):
   - ❌ Requires passing screenshot object to mouse tool (breaks tool independence).
   - ❌ Adds coupling between tools; violates "dumb actuator" principle.

**Chosen Approach**: Immediate hard requirement.

---

## API Contract Changes

### Current Behavior (001-mouse-control)

```
mouse_control(action="click", x=500, y=300)
↓
Uses default monitorIndex=0 (silent failure if user meant monitor 1!)
```

### New Behavior (012-mouse-position)

```
mouse_control(action="click", x=500, y=300)
↓
Returns ERROR: "monitorIndex is required when using x/y coordinates. Specify monitorIndex."

mouse_control(action="click", x=500, y=300, monitorIndex=1)
↓
Clicks at (500, 300) on monitor 1 ✓

mouse_control(action="click")
↓
Clicks at current cursor position (monitorIndex not needed) ✓
```

---

## Validation Logic

### When monitorIndex is Required

✅ **Coordinates provided** → monitorIndex REQUIRED:
- `move(x=?, y=?, monitorIndex=?)`
- `click(x=?, y=?, monitorIndex=?)`
- `double_click(x=?, y=?, monitorIndex=?)`
- `right_click(x=?, y=?, monitorIndex=?)`
- `middle_click(x=?, y=?, monitorIndex=?)`
- `drag(startX=?, startY=?, endX=?, endY=?, monitorIndex=?)`
- `scroll(x=?, y=?, direction=?, monitorIndex=?)`

### When monitorIndex is Optional (Not Needed)

✅ **No coordinates provided** → monitorIndex OPTIONAL (ignored):
- `click()` → click at current cursor position
- `scroll(direction=down)` → scroll at current cursor position
- `get_position()` → return current position (monitorIndex is output, not input)

### Error Messages

| Scenario | Error Code | Message | Error Details |
|----------|------------|---------|----------------|
| x/y provided, monitorIndex missing | `missing_required_parameter` | "monitorIndex is required when using x/y coordinates" | Valid indices: [0, 1, 2] |
| Invalid monitorIndex | `invalid_coordinates` | "Invalid monitorIndex: 5" | Valid indices: [0, 1, 2] |
| Coordinates out of monitor bounds | `coordinates_out_of_bounds` | "Coordinates (2000, 300) out of bounds for monitor 1" | Monitor 1 bounds: { left: 1920, top: 0, width: 2560, height: 1440 } |

---

## Response Enrichment

### What Gets Added to All Success Responses

Every successful `mouse_control` response includes:
- `monitorIndex` (where cursor ended up)
- `monitorWidth`, `monitorHeight` (dimensions of that monitor)

**Example**:
```json
{
  "success": true,
  "final_position": { "x": 500, "y": 300 },
  "monitorIndex": 1,
  "monitorWidth": 2560,
  "monitorHeight": 1440,
  "window_title": "Visual Studio Code"
}
```

**Why**: Provides feedback to LLM; allows validation ("I clicked on monitor 1 ✓" vs. "I ended up on monitor 0 ✗").

---

## Implementation Approach

### Files to Modify

1. **MouseControlTool.cs** (`src/Sbroenne.WindowsMcp/Tools/`)
   - Add `monitorIndex` parameter to method signature (currently optional, needs to become conditional-required)
   - Add validation logic: if x/y provided, require monitorIndex
   - Pass monitorIndex to all action handlers
   - Enrich responses with monitor dimensions

2. **MouseControlResult.cs** (`src/Sbroenne.WindowsMcp/Models/`)
   - Add `monitorIndex` field
   - Add `monitorWidth` field
   - Add `monitorHeight` field
   - Update JSON serialization

3. **MouseControlTool.cs** handlers (HandleMoveAsync, HandleClickAsync, etc.)
   - Use provided monitorIndex instead of defaulting to 0
   - Validate monitorIndex early in each handler

### Files to Create (Tests)

4. **MouseControlToolTests.cs** additions or new test class
   - Test: monitorIndex required when x/y provided
   - Test: monitorIndex not required when x/y omitted
   - Test: invalid monitorIndex returns clear error
   - Test: coordinates clamped to monitor bounds
   - Test: response includes monitor dimensions
   - Multi-monitor fixtures: use secondary monitor for test clicks (if available)

---

## Coordinate System & Monitor Layout

### Key Facts

- **Monitor 0** is primary (typically top-left origin: 0,0)
- **Secondary monitors** can have negative coordinates (e.g., monitor to the left of primary)
- **Coordinate system** is always monitor-relative: (0,0) is top-left of specified monitor
- **Existing normalization** (0-65535 range for SendInput) already handles multi-monitor geometry correctly; no changes needed there

### Validation Example

On a 2-monitor setup (monitor 0: 1920×1080, monitor 1: 2560×1440):

```
LLM: "Click at (2200, 300, monitorIndex=1)"
→ Check: 2200 < 2560? ✓ | 300 < 1440? ✓
→ Send to SendInput (with coordinate translation)
→ ✓ Click succeeds on monitor 1
```

---

## Breaking Change & Rollout

**Status**: Immediate hard break (Option A).

**Impact**:
- Existing code that calls `mouse_control(x=500, y=300)` without monitorIndex will fail immediately.
- **Mitigation**: Update all call sites to include `monitorIndex`. For single-monitor setups, use `monitorIndex=0`. For multi-monitor, require caller to specify.

**Affected Integrations**:
- Any MCP client using `mouse_control` with coordinates (LLMs, automation scripts, etc.)
- VS Code extension bundled with this server
- Standalone server users

**Messaging**:
- Clear error messages in responses: "monitorIndex is required when using x/y coordinates."
- Release notes: "Breaking change: monitorIndex now required for coordinate-based mouse actions."

---

## Testing Strategy

### Integration Tests

- **Single Monitor Setup**: Test all actions with `monitorIndex=0`
- **Multi-Monitor Setup** (if available):
  - Test clicks on secondary monitor
  - Test validation with invalid indices
  - Test coordinates outside monitor bounds
  - Prefer **secondary monitor for test clicks** to avoid interfering with developer's VS Code session

### Test Fixtures

- `MultiMonitorFixture`: Provides monitor enumeration, detects secondary monitor, supplies test coordinates
- Pattern: Tests use `TestMonitorHelper.GetTestMonitorIndex()` to target secondary monitor if available, else primary

### Unit Tests (if applicable)

- Validation logic: missing monitorIndex, invalid index, out-of-bounds coordinates

---

## Implementation Checklist

- [ ] Modify `MouseControlTool` to enforce monitorIndex validation
- [ ] Update `MouseControlResult` with monitor fields
- [ ] Add parameter descriptions to enforce LLM clarity
- [ ] Write integration tests on Windows 11 (multi-monitor preferred)
- [ ] Update VS Code extension to pass monitorIndex
- [ ] Update standalone server documentation
- [ ] Release notes: breaking change notice
