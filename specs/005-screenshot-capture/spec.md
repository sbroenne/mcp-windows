# Feature Specification: Screenshot Capture

**Feature Branch**: `005-screenshot-capture`  
**Created**: 2025-12-08  
**Status**: Draft  
**Input**: User description: "Screenshot capture functionality for Windows desktop automation - capture full screen, specific monitors, windows, and regions"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture Entire Screen (Priority: P1)

As an LLM agent automating a Windows desktop, I need to capture a screenshot of the entire primary display so I can analyze the current visual state of the desktop and make informed decisions about next actions.

**Why this priority**: This is the most common screenshot use case. Agents need visual feedback to understand application state, verify UI changes, and debug automation issues. Without this capability, agents are "blind" to visual outcomes.

**Independent Test**: Invoke screenshot capture with no parameters, receive a base64-encoded PNG image, decode it, and verify it contains valid image data matching the current screen resolution.

**Acceptance Scenarios**:

1. **Given** the desktop is visible, **When** the agent requests a full screen capture, **Then** a base64-encoded PNG image of the entire primary monitor is returned
2. **Given** multiple monitors exist, **When** the agent requests a full screen capture without specifying a monitor, **Then** the primary monitor is captured by default
3. **Given** applications are running on screen, **When** the agent captures the screen, **Then** all visible windows and UI elements are included in the image
4. **Given** a capture is requested, **When** the operation completes successfully, **Then** the response includes image dimensions (width, height) and format information

---

### User Story 2 - Capture Specific Monitor (Priority: P2)

As an LLM agent working with a multi-monitor setup, I need to capture a specific monitor so I can analyze content on secondary displays without capturing unnecessary screens.

**Why this priority**: Multi-monitor setups are common in professional environments. Agents need to target specific displays when applications span multiple screens or when different tasks are on different monitors.

**Independent Test**: On a multi-monitor system, invoke capture with monitor index 1 (secondary), verify returned image dimensions match the secondary monitor's resolution.

**Acceptance Scenarios**:

1. **Given** multiple monitors are connected, **When** the agent specifies monitor index 0, **Then** the primary monitor is captured
2. **Given** multiple monitors are connected, **When** the agent specifies monitor index 1, **Then** the secondary monitor is captured
3. **Given** the agent specifies an invalid monitor index, **Then** an appropriate error is returned with the list of available monitors
4. **Given** monitors have different resolutions, **When** capturing each monitor, **Then** each capture reflects the correct resolution

---

### User Story 3 - Capture Specific Window (Priority: P2)

As an LLM agent automating a specific application, I need to capture only that application's window so I can analyze its content without visual noise from other applications.

**Why this priority**: Agents typically work with one application at a time. Capturing only the target window reduces image size, improves analysis accuracy, and avoids exposing unrelated content.

**Independent Test**: Open Notepad, get its window handle, invoke capture with that handle, verify returned image dimensions match the Notepad window size.

**Acceptance Scenarios**:

1. **Given** an application window is visible, **When** the agent captures it by window handle, **Then** only that window's content is captured
2. **Given** a window is partially obscured, **When** the agent captures it, **Then** the captured image shows the window as if it were fully visible (restored rendering)
3. **Given** a window is minimized, **When** the agent attempts to capture it, **Then** an appropriate error is returned indicating the window is not visible
4. **Given** an invalid window handle, **When** the agent attempts to capture, **Then** an appropriate error is returned

---

### User Story 4 - Capture Screen Region (Priority: P3)

As an LLM agent performing targeted visual analysis, I need to capture a specific rectangular region of the screen so I can analyze a particular area without processing the entire screen.

**Why this priority**: Region capture is useful for focused analysis (e.g., capturing just a dialog box or button area) and reduces data transfer/processing when only a portion of the screen is relevant.

**Independent Test**: Invoke capture with coordinates (100, 100, 400, 300), verify returned image is exactly 300x200 pixels.

**Acceptance Scenarios**:

1. **Given** valid coordinates (x, y, width, height), **When** the agent captures that region, **Then** an image of exactly those dimensions is returned
2. **Given** coordinates that extend beyond screen bounds, **When** the agent captures, **Then** only the visible portion within screen bounds is captured
3. **Given** invalid coordinates (negative width/height, zero dimensions), **Then** an appropriate error is returned
4. **Given** coordinates on a specific monitor, **When** the agent captures that region, **Then** the content from that screen area is captured

---

### User Story 5 - List Available Monitors (Priority: P3)

As an LLM agent needing to understand the display configuration, I need to query available monitors so I can make informed decisions about which display to capture.

**Why this priority**: Before capturing specific monitors, agents need to discover what monitors are available and their properties (resolution, position, primary status).

**Independent Test**: Invoke list monitors action, verify response includes at least one monitor with resolution and position information.

**Acceptance Scenarios**:

1. **Given** a single monitor system, **When** the agent lists monitors, **Then** one monitor entry is returned with its resolution
2. **Given** a multi-monitor system, **When** the agent lists monitors, **Then** all monitors are listed with their index, resolution, position, and primary status
3. **Given** monitors are arranged in a specific layout, **When** the agent lists monitors, **Then** the position data reflects the actual arrangement

---

### Edge Cases

- What happens when the screen is locked (secure desktop)? → Return error indicating secure desktop is active
- What happens during UAC prompts? → Return error as secure desktop blocks capture
- How does the system handle very high-resolution displays (4K+)? → Return full resolution image; consider optional scaling parameter
- What happens with HDR displays? → Capture in SDR mode for compatibility
- How are scaled displays handled (125%, 150% DPI)? → Return actual pixel dimensions, not logical dimensions
- What happens if capture fails due to GPU driver issues? → Return appropriate error with fallback suggestion

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture the entire primary monitor when no parameters are specified
- **FR-002**: System MUST return screenshots as base64-encoded PNG data
- **FR-003**: System MUST include image metadata (width, height, format) in the response
- **FR-004**: System MUST support capturing specific monitors by index (0-based)
- **FR-005**: System MUST support capturing specific windows by window handle
- **FR-006**: System MUST support capturing rectangular screen regions by coordinates
- **FR-007**: System MUST return a list of available monitors with resolution, position, and primary status
- **FR-008**: System MUST handle DPI-scaled displays correctly, returning actual pixel dimensions
- **FR-009**: System MUST return appropriate errors when capture is not possible (secure desktop, minimized window, invalid handle)
- **FR-010**: System MUST handle multi-monitor setups with monitors at different resolutions
- **FR-011**: System MUST capture window content even when partially obscured (using PrintWindow or similar)
- **FR-012**: System MUST limit maximum capture dimensions to prevent memory exhaustion
- **FR-013**: System MUST support an optional parameter to include the mouse cursor in captures (default: off)

### Key Entities

- **Monitor**: Represents a display device with index, resolution (width × height), position (x, y), and primary status
- **CaptureTarget**: The subject of the capture - can be a full screen, specific monitor, window, or region
- **ScreenshotResult**: The captured image data with base64 content, dimensions, format, and capture metadata
- **Region**: A rectangular area defined by x, y, width, and height in screen coordinates

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can capture a full screen screenshot in under 500 milliseconds
- **SC-002**: Captured images accurately represent the current screen state with no visual artifacts
- **SC-003**: Multi-monitor capture correctly identifies and captures the specified display
- **SC-004**: Window captures include the complete window content regardless of occlusion
- **SC-005**: Region captures return images with exact pixel dimensions requested (within screen bounds)
- **SC-006**: All capture operations complete within 5 seconds or return a timeout error
- **SC-007**: System provides clear error messages when capture fails due to security restrictions
- **SC-008**: 95% of capture requests succeed on first attempt under normal operating conditions

## Assumptions

1. **Primary Monitor Default**: When no monitor is specified, the primary monitor is captured
2. **PNG Format**: All captures are returned as PNG for lossless quality and broad compatibility
3. **Base64 Encoding**: Binary image data is base64-encoded for safe JSON transport
4. **Coordinate System**: Screen coordinates use the Windows virtual screen coordinate system (can include negative values for left/above-primary monitors)
5. **Window Handle Format**: Window handles are passed as integer values (native HWND)
6. **Cursor Capture Optional**: Mouse cursor is excluded by default but can be included via optional `include_cursor` parameter
7. **Synchronous Operation**: Capture operations are blocking (complete before returning)
8. **Memory Management**: Large captures (8K displays, multiple monitors) are handled without memory exhaustion via streaming or size limits
9. **No Video Capture**: This feature is for single-frame screenshots, not video recording

## Out of Scope

- Video recording or screen capture streaming
- OCR or image analysis (consumers can do this with the returned image)
- Image editing or manipulation
- Clipboard integration (saving screenshot to clipboard)
- File saving (consumers can decode base64 and save if needed)
- Annotation or highlighting
- Delayed/timed captures
- Sound capture

## Dependencies

- **Window Management Feature (003)**: For resolving window handles and window state detection
- **Mouse Control Feature (001)**: Coordinate system understanding for region capture
