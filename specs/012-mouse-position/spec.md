# Feature Specification: Mouse Position Awareness for LLM Usability

**Feature Branch**: `012-mouse-position`  
**Created**: December 11, 2025  
**Status**: Draft  
**Input**: User description: "Improve mouse control for LLM usability by adding get_position action and monitor awareness"  
**Extends**: [001-mouse-control](../001-mouse-control/spec.md)

## Problem Statement

LLMs using the `mouse_control` MCP tool struggle to position the mouse accurately because the `monitorIndex` parameter is optional (defaults to 0). This creates a mismatch:

1. LLM takes a screenshot of monitor 1
2. LLM finds a button at coordinates (500, 300) in that image
3. LLM calls `mouse_control(action: click, x: 500, y: 300)` — **forgetting monitorIndex**
4. Click happens on monitor 0 at (500, 300) — **wrong monitor!**

The fix is simple: **make `monitorIndex` a required parameter** so the LLM must be explicit about which monitor it's targeting.

## Evolution from 001-mouse-control

The original [001-mouse-control spec](../001-mouse-control/spec.md) was designed from a **human developer's perspective**—assuming the caller would "just know" where to click. It focused on:

| 001 Spec Focus | Assumption |
|----------------|------------|
| Accurate cursor positioning | Caller knows the target coordinates |
| Multi-monitor support | Caller understands monitor layout |
| DPI awareness | Caller handles scaling calculations |
| Click/drag/scroll operations | Caller will position cursor first |

**What 001 got right:**
- Robust Windows API usage (`SendInput`, not deprecated `mouse_event`)
- Proper multi-monitor coordinate handling with normalization
- Excellent error handling for elevated processes, secure desktop, UIPI
- Modifier key management without stuck keys
- Comprehensive action support (move, click, double_click, right_click, middle_click, drag, scroll)
- **Monitor-relative coordinates already work correctly!**

**What 001 didn't anticipate:**
The optional `monitorIndex` with a default of 0 is a footgun for LLMs:

| Human Developer | LLM Caller |
|-----------------|------------|
| Remembers which monitor they're working on | May forget to specify monitorIndex |
| Mentally tracks context | Each tool call is stateless |
| Catches mistakes by seeing cursor move | Cannot see the screen |

**The simple fix:**
Instead of adding new actions like `get_position` and `move_relative`, we simply **require the LLM to be explicit** about which monitor it's targeting. The coordinate translation already works correctly.

## Simplified LLM Workflow

```
┌──────────────────────────────────────────────────────────────┐
│                    LLM UI Automation Flow                     │
├──────────────────────────────────────────────────────────────┤
│  1. screenshot_control(target: monitor, monitorIndex: 1)     │
│     → Get image of monitor 1                                  │
│                                                               │
│  2. Analyze image, find button at (500, 300)                 │
│     → Coordinates are already monitor-relative!               │
│                                                               │
│  3. mouse_control(action: click, x: 500, y: 300,             │
│                   monitorIndex: 1)  ← REQUIRED                │
│     → Click at correct position on correct monitor            │
└──────────────────────────────────────────────────────────────┘
```

This is much simpler than adding `get_position` or `move_relative` because:
- Screenshot coordinates **are already monitor-relative** (0,0 is top-left of image)
- The coordinate translation **already works** in the existing implementation
- We just need to **force explicit monitor targeting**

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Explicit Monitor Targeting (Priority: P1)

As an LLM performing UI automation, I need to explicitly specify which monitor I'm targeting so that coordinates from my screenshot analysis map correctly to mouse positions.

**Why this priority**: This is the core fix - eliminating the silent failure mode where the LLM clicks on the wrong monitor.

**Independent Test**: Can be tested by calling any mouse action with coordinates but without monitorIndex and verifying an error is returned, and by calling actions without coordinates to confirm they still work at the current cursor position.

**Acceptance Scenarios**:

1. **Given** an LLM calls mouse_control with action "click" and x=500, y=300, **When** monitorIndex is not provided, **Then** the tool returns an error indicating monitorIndex is required.

2. **Given** an LLM takes a screenshot of monitor 1 and finds a button at (500, 300), **When** it calls mouse_control with x=500, y=300, monitorIndex=1, **Then** the click occurs at the correct position on monitor 1.

3. **Given** a single-monitor setup, **When** an LLM calls mouse_control with action "click" and x=500, y=300 and monitorIndex=0, **Then** the operation succeeds.

4. **Given** an LLM specifies an invalid monitorIndex (e.g., 5 on a 2-monitor system), **When** the tool is called with coordinates, **Then** a clear error is returned listing valid monitor indices.

5. **Given** an LLM calls mouse_control with action "click" and no coordinates (x,y omitted), **When** monitorIndex is also omitted, **Then** the click occurs at the current cursor position without error.

---

### User Story 2 - Monitor Information in Responses (Priority: P2)

As an LLM performing UI automation, I need confirmation of which monitor the cursor ended up on so I can verify my operation succeeded.

**Why this priority**: Provides feedback for the LLM to confirm actions worked as expected.

**Independent Test**: Can be tested by performing any mouse action and verifying the response includes the target monitor index and dimensions.

**Acceptance Scenarios**:

1. **Given** a mouse_control action completes successfully, **When** the response is returned, **Then** it includes the monitorIndex where the cursor is now located.

2. **Given** a successful mouse action on monitor 1, **When** the response is returned, **Then** it includes the monitor's dimensions (width and height).

3. **Given** a user with a 1920×1080 monitor at index 0, **When** an LLM clicks at (100, 100, monitorIndex=0), **Then** the response includes monitorIndex=0, monitorWidth=1920, monitorHeight=1080.

---

### User Story 3 - Query Current Position (Priority: P3)

As an LLM performing UI automation, I may need to know where the cursor currently is before deciding where to move it.

**Why this priority**: Nice-to-have for advanced scenarios, but not critical since the primary workflow uses screenshot→click.

**Independent Test**: Can be tested by calling get_position and verifying valid coordinates and monitor index are returned.

**Acceptance Scenarios**:

1. **Given** the cursor is at any position, **When** an LLM calls mouse_control with action "get_position", **Then** the response includes x, y coordinates relative to the current monitor and the monitorIndex.

2. **Given** the cursor is on monitor 1 at position (500, 300), **When** get_position is called, **Then** the response shows x=500, y=300, monitorIndex=1.

---

### Edge Cases

- What happens when an LLM specifies coordinates outside the target monitor's bounds?
  - Return error with valid coordinate range for that monitor.
- What happens if a monitor is disconnected between screenshot and click?
  - Return error indicating the monitor index is no longer valid.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `monitorIndex` parameter MUST be required for all mouse_control actions that use coordinates (move, click, double_click, right_click, middle_click, drag start/end, scroll with position).
- **FR-002**: When coordinates are provided and `monitorIndex` is not provided, the tool MUST return a clear error message indicating that monitorIndex is required when using x/y coordinates.
- **FR-003**: When an invalid `monitorIndex` is provided together with coordinates, the tool MUST return an error listing valid monitor indices (e.g., "Invalid monitorIndex: 5. Valid indices: 0, 1, 2").
- **FR-004**: When no coordinates are provided, actions MUST operate at the current cursor position and MUST NOT require `monitorIndex`.
- **FR-005**: All successful mouse_control responses MUST include the `monitorIndex` where the cursor ended up.
- **FR-006**: All successful mouse_control responses MUST include the target monitor's dimensions (`monitorWidth`, `monitorHeight`).
- **FR-007**: System MUST support a new "get_position" action that returns current cursor position (x, y relative to monitor, plus monitorIndex).
- **FR-008**: The get_position action MUST NOT require monitorIndex as input (it reports where the cursor already is).
- **FR-009**: Coordinates MUST continue to be interpreted as relative to the specified monitor's origin (0,0 = top-left of that monitor).

### Key Entities

- **Monitor Index**: Required identifier (0-based) specifying which monitor coordinates are relative to.
- **Monitor Dimensions**: Width and height of a monitor, returned in responses for LLM awareness.
- **Cursor Position**: Current x,y coordinates relative to a specific monitor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of mouse operations with coordinates require explicit monitorIndex specification.
- **SC-002**: LLMs successfully click on the correct monitor when using screenshot coordinates + matching monitorIndex.
- **SC-003**: Clear error messages are returned when monitorIndex is missing or invalid.
- **SC-004**: All responses include monitor dimensions for LLM awareness.
- **SC-005**: Existing screenshot→click workflow works correctly when LLM specifies matching monitor indices.

## Assumptions

- LLMs will use `screenshot_control` with a specific monitorIndex before attempting mouse operations.
- Coordinates from screenshot analysis (0,0 at top-left of image) directly map to monitor-relative coordinates.
- This feature introduces a **breaking change**: all existing calls that provide x/y coordinates without `monitorIndex` will fail with an error. No deprecation period or warning phase—the requirement is enforced immediately.

## Out of Scope

- `move_relative` action (unnecessary if LLM uses screenshot coordinates)
- OCR or visual element detection (LLMs use screenshot tool for that)
- Automatic coordinate calculation from UI element names

## Clarifications

### Session 2025-12-11

- Q: Should the breaking change (requiring `monitorIndex` when x/y coordinates are used) be rolled out immediately or phased with deprecation warnings? → A: Immediate hard break. The requirement is enforced immediately with no transition period.
