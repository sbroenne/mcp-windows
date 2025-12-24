# Feature Specification: All Monitors Screenshot Capture

**Feature Branch**: `008-all-monitors-screenshot`  
**Created**: December 8, 2025  
**Status**: Draft  
**Input**: User description: "Add all_monitors capture target to screenshot_control tool that captures the entire virtual screen spanning all monitors in a single screenshot, enabling proper test verification across multi-monitor setups"

## Overview

The current screenshot_control tool can capture individual monitors or the primary screen, but cannot capture all monitors in a single screenshot. This limitation makes it difficult to verify that automated actions (mouse, keyboard, window operations) happened on the correct monitor during LLM-based integration testing.

### Problem Statement

When running integration tests on multi-monitor setups:
1. Tests target the **secondary monitor** to avoid interfering with VS Code on primary
2. Current screenshot captures only show the target monitor
3. **Cannot verify** that the action happened on the correct monitor vs. accidentally on primary
4. Requires multiple screenshot calls (one per monitor) for complete verification

### Solution Approach

Add a new capture target `all_monitors` that:
1. Captures the entire Windows virtual screen (spanning all monitors)
2. Returns a single stitched image showing all monitors
3. Uses existing `CaptureRegionInternalAsync` with virtual screen coordinates
4. Minimal code change - leverages existing infrastructure

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture All Monitors (Priority: P1)

As an LLM agent running integration tests on a multi-monitor system, I need to capture all monitors in a single screenshot so I can verify that actions happened on the correct monitor.

**Why this priority**: This is the core feature - without it, test verification cannot confirm monitor targeting is correct.

**Independent Test**: On a multi-monitor system, invoke capture with `target="all_monitors"`, verify returned image dimensions match the combined virtual screen size.

**Acceptance Scenarios**:

1. **Given** a 2-monitor setup (e.g., 1920x1080 + 2560x1440 side-by-side), **When** the agent captures with `target="all_monitors"`, **Then** a single image is returned spanning both monitors
2. **Given** monitors arranged vertically or with gaps, **When** the agent captures all monitors, **Then** the virtual screen layout is accurately represented
3. **Given** monitors with different DPI settings, **When** the agent captures all monitors, **Then** each monitor's content is captured at its native resolution
4. **Given** cursor is on secondary monitor, **When** capture includes cursor (`includeCursor=true`), **Then** cursor appears at the correct position in the combined image

---

### User Story 2 - Integration Test Verification (Priority: P1)

As an integration test framework, I need before/after screenshots of all monitors so I can verify that:
- The action happened on the intended monitor (secondary)
- The primary monitor (with VS Code) was NOT affected

**Why this priority**: This is the primary use case driving this feature - proper test verification.

**Independent Test**: Run a mouse click test targeting secondary monitor, capture all monitors before/after, verify click effect appears only on secondary.

**Acceptance Scenarios**:

1. **Given** a test that moves the mouse to secondary monitor, **When** before/after all-monitors screenshots are compared, **Then** the cursor position change is visible on secondary monitor only
2. **Given** a test that opens a context menu on secondary, **When** after screenshot is captured, **Then** context menu is visible on secondary and primary is unchanged
3. **Given** a test that accidentally targets primary monitor, **When** all-monitors screenshot is captured, **Then** the error is detectable (action visible on wrong monitor)

---

### User Story 3 - Virtual Screen Information (Priority: P2)

As an LLM agent, I need to query the virtual screen dimensions so I can understand the combined monitor layout before capturing.

**Why this priority**: Useful for understanding the layout, but agents can also infer this from `list_monitors` response.

**Independent Test**: Invoke `list_monitors`, verify response includes virtual screen bounds in addition to individual monitors.

**Acceptance Scenarios**:

1. **Given** multiple monitors, **When** the agent calls `list_monitors`, **Then** the response includes `virtualScreen` bounds (x, y, width, height)
2. **Given** monitors arranged with negative coordinates (left of primary), **When** the agent queries, **Then** virtual screen x can be negative

---

### Edge Cases

- What happens when only one monitor is connected? → Returns that monitor's screenshot (same as primary_screen)
- What happens when monitors have different resolutions? → Virtual screen captures all at native resolution; gaps appear as black regions
- What happens when the virtual screen is very large (4+ monitors, 8K+)? → Apply MaxPixels limit from configuration; return error if exceeded
- What happens with HDR monitors? → Capture in SDR mode for compatibility (existing behavior)
- What happens during secure desktop (UAC/lock screen)? → Return error (existing behavior)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support `target="all_monitors"` in screenshot_control tool
- **FR-002**: System MUST capture the entire Windows virtual screen spanning all connected monitors
- **FR-003**: System MUST return a single PNG image containing all monitor content
- **FR-004**: System MUST handle monitors with different resolutions and DPI settings
- **FR-005**: System MUST support `includeCursor=true` option for all_monitors capture
- **FR-006**: System MUST apply the existing MaxPixels limit to prevent memory issues with very large captures
- **FR-007**: System MUST return appropriate error when virtual screen exceeds size limits
- **FR-008**: System MUST handle negative virtual screen coordinates (monitors left of primary)
- **FR-009**: System SHOULD include `virtualScreen` bounds in `list_monitors` response

### Key Entities

- **Virtual Screen**: The combined coordinate space spanning all monitors. Has origin (can be negative), width, and height.
- **CaptureTarget.AllMonitors**: New enum value for the capture target.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All-monitors capture completes within 500ms on a typical 2-monitor setup
- **SC-002**: Returned image dimensions match the virtual screen dimensions exactly
- **SC-003**: Integration tests can verify monitor targeting with a single before/after screenshot pair
- **SC-004**: Cursor position is accurately rendered when `includeCursor=true`
- **SC-005**: Existing screenshot_control functionality remains unchanged (backward compatible)

## Assumptions

- Windows provides accurate virtual screen metrics via `GetSystemMetrics`
- Virtual screen coordinates are consistent with individual monitor positions
- Existing `CaptureRegionInternalAsync` can handle virtual screen coordinates including negative values
- The feature targets Windows 11 with standard display drivers

## Technical Notes

The implementation can leverage existing infrastructure with minimal changes:

```csharp
// Windows System Metrics for virtual screen
int x = NativeMethods.GetSystemMetrics(SM_XVIRTUALSCREEN);      // Left edge (can be negative)
int y = NativeMethods.GetSystemMetrics(SM_YVIRTUALSCREEN);      // Top edge
int width = NativeMethods.GetSystemMetrics(SM_CXVIRTUALSCREEN); // Total width
int height = NativeMethods.GetSystemMetrics(SM_CYVIRTUALSCREEN); // Total height

var region = new CaptureRegion(x, y, width, height);
return CaptureRegionInternalAsync(region, includeCursor, cancellationToken);
```

This reuses the existing `CaptureRegionInternalAsync` method, minimizing code changes and risk.
