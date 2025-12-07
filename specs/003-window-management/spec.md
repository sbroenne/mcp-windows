# Feature Specification: Window Management

**Feature Branch**: `003-window-management`  
**Created**: 2025-12-07  
**Status**: Draft  
**Input**: User description: "Window management feature for Windows MCP - enumerate windows, find windows by title, activate/focus windows, and control window state for desktop automation"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - List Open Windows (Priority: P1)

An LLM needs to discover what windows are currently open on the desktop to understand the current state and identify targets for interaction.

**Why this priority**: Window discovery is the foundation for all window-based automation. Without knowing what windows exist, the LLM cannot make informed decisions about which window to interact with.

**Independent Test**: Can be fully tested by opening several applications and invoking the tool to list windows, verifying all visible windows are returned with their titles and positions. Delivers immediate value for situational awareness.

**Acceptance Scenarios**:

1. **Given** multiple applications are running, **When** the LLM invokes `window_management` with action `list`, **Then** a list of all visible top-level windows is returned with titles, process names, and bounding rectangles.

2. **Given** some windows are minimized, **When** the LLM invokes `list`, **Then** minimized windows are included with a flag indicating their minimized state.

3. **Given** some windows are on different monitors, **When** the LLM invokes `list`, **Then** all windows are returned with correct coordinates for their respective monitors.

4. **Given** a filter parameter is provided, **When** the LLM invokes `list` with `filter: "notepad"`, **Then** only windows with "notepad" in their title or process name are returned (case-insensitive).

---

### User Story 2 - Find Window by Title (Priority: P1)

An LLM needs to locate a specific window by its title (or partial title) to get its position for clicking or to activate it.

**Why this priority**: Finding specific windows is essential for targeted automation. Combined with mouse control, this enables clicking on specific application windows.

**Independent Test**: Can be tested by opening a known application and finding it by title, then verifying the returned coordinates match the actual window position.

**Acceptance Scenarios**:

1. **Given** Notepad is open with a file, **When** the LLM invokes `window_management` with action `find` and `title: "Notepad"`, **Then** the window handle, full title, position, and size are returned.

2. **Given** multiple windows match the search, **When** the LLM invokes `find` with `title: "Chrome"`, **Then** all matching windows are returned, sorted by z-order (topmost first).

3. **Given** no windows match, **When** the LLM invokes `find` with a non-existent title, **Then** an empty result is returned with a clear message (not an error).

4. **Given** a regex pattern is provided, **When** the LLM invokes `find` with `title: ".*\\.txt - Notepad"` and `regex: true`, **Then** regex matching is used.

---

### User Story 3 - Activate/Focus Window (Priority: P1)

An LLM needs to bring a specific window to the foreground and give it keyboard focus so that subsequent keyboard input goes to the correct application.

**Why this priority**: Essential for multi-window workflows. The LLM must be able to switch between applications without relying solely on Alt+Tab sequences.

**Independent Test**: Can be tested by having two windows open, activating the background one, and verifying it comes to the foreground and receives focus.

**Acceptance Scenarios**:

1. **Given** a window is in the background, **When** the LLM invokes `window_management` with action `activate` and the window identifier, **Then** the window is brought to the foreground and receives keyboard focus.

2. **Given** a window is minimized, **When** the LLM invokes `activate`, **Then** the window is restored and brought to the foreground.

3. **Given** the target is an elevated (admin) window, **When** the LLM invokes `activate`, **Then** the tool returns an error explaining that elevated windows cannot be activated due to UIPI restrictions.

4. **Given** activation succeeds, **When** the result is returned, **Then** it includes the window's new position and confirmation that it has foreground status.

---

### User Story 4 - Get Foreground Window Info (Priority: P1)

An LLM needs to know which window currently has focus to understand the current context before sending keyboard input.

**Why this priority**: Context awareness is critical for safe automation. The LLM should verify focus before typing to avoid sending input to the wrong window.

**Independent Test**: Can be tested by focusing different windows and verifying the tool returns correct information each time.

**Acceptance Scenarios**:

1. **Given** any window has focus, **When** the LLM invokes `window_management` with action `get_foreground`, **Then** the current foreground window's title, process name, handle, and position are returned.

2. **Given** the desktop has focus (no window), **When** the LLM invokes `get_foreground`, **Then** a special result indicates the desktop or explorer shell is focused.

3. **Given** a secure desktop is active (UAC, lock screen), **When** the LLM invokes `get_foreground`, **Then** a clear error indicates the secure desktop is active.

---

### User Story 5 - Control Window State (Priority: P2)

An LLM needs to minimize, maximize, restore, or close windows to manage the desktop environment.

**Why this priority**: Important for workspace organization but not as critical as discovery and activation for basic automation flows.

**Independent Test**: Can be tested by maximizing/minimizing a window and verifying its state changes.

**Acceptance Scenarios**:

1. **Given** a normal window, **When** the LLM invokes `window_management` with action `minimize` and the window identifier, **Then** the window is minimized to the taskbar.

2. **Given** a normal window, **When** the LLM invokes `maximize`, **Then** the window fills the screen.

3. **Given** a minimized or maximized window, **When** the LLM invokes `restore`, **Then** the window returns to its normal size and position.

4. **Given** any window, **When** the LLM invokes `close`, **Then** a close message is sent to the window (WM_CLOSE).

5. **Given** the window has unsaved changes, **When** `close` is invoked, **Then** the application's save prompt appears (the tool does not force-close).

---

### User Story 6 - Move and Resize Window (Priority: P2)

An LLM needs to position and size windows to arrange the workspace or ensure a window is visible on a specific monitor.

**Why this priority**: Useful for workspace organization and multi-monitor workflows, but not essential for basic automation.

**Independent Test**: Can be tested by moving a window to specific coordinates and verifying its new position.

**Acceptance Scenarios**:

1. **Given** any window, **When** the LLM invokes `window_management` with action `move` and coordinates, **Then** the window moves to the specified position.

2. **Given** any window, **When** the LLM invokes `resize` with width and height, **Then** the window is resized.

3. **Given** coordinates would place the window off-screen, **When** `move` is invoked, **Then** the tool warns but allows the move (some applications rely on off-screen positioning).

4. **Given** a combined move and resize is needed, **When** the LLM invokes `set_bounds` with x, y, width, height, **Then** both are applied atomically.

---

### User Story 7 - Wait for Window (Priority: P2)

An LLM needs to wait for a window to appear (e.g., after launching an application) before interacting with it.

**Why this priority**: Important for reliable automation workflows where timing matters, but polling with find can work as a fallback.

**Independent Test**: Can be tested by starting an application and waiting for its window to appear.

**Acceptance Scenarios**:

1. **Given** an application is being launched, **When** the LLM invokes `window_management` with action `wait_for` and `title: "Notepad"`, **Then** the tool blocks until a matching window appears or timeout occurs.

2. **Given** a timeout of 10 seconds, **When** no matching window appears within 10 seconds, **Then** a timeout error is returned.

3. **Given** the window already exists, **When** `wait_for` is invoked, **Then** it returns immediately with the window info.

---

### Edge Cases

- What happens when a window closes during an operation?
  - The tool returns an error indicating the window is no longer valid.

- How are invisible windows handled?
  - By default, only visible windows are listed. An `include_hidden` flag can include invisible windows.

- How are child windows and dialogs handled?
  - Top-level windows only by default. An `include_children` flag can include child windows/dialogs.

- What happens with windows from elevated processes?
  - They are listed with their titles but marked as elevated. Activation and input operations return errors due to UIPI.

- How are windows identified between operations?
  - Windows are identified by handle (HWND). Handles may become invalid if the window closes.

- What happens with fullscreen exclusive mode applications (games)?
  - The tool reports the window but operations may fail. Results include appropriate warnings.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tool MUST enumerate top-level visible windows using `EnumWindows` API.
- **FR-002**: The tool MUST return window information including: handle, title, class name, process name, process ID, bounding rectangle, and window state (normal, minimized, maximized).
- **FR-003**: The tool MUST support the `list` action to enumerate all matching windows.
- **FR-004**: The tool MUST support the `find` action to search for windows by title (substring or regex).
- **FR-005**: The tool MUST support the `activate` action to bring a window to the foreground using `SetForegroundWindow` and related APIs.
- **FR-006**: The tool MUST support the `get_foreground` action to return the current foreground window info.
- **FR-007**: The tool MUST support the `minimize`, `maximize`, `restore`, and `close` actions for window state control.
- **FR-008**: The tool MUST support the `move`, `resize`, and `set_bounds` actions for window positioning.
- **FR-009**: The tool MUST support the `wait_for` action to wait for a window to appear with configurable timeout.
- **FR-010**: The `list` and `find` actions MUST support optional `filter` parameter for title/process name filtering.
- **FR-011**: The tool MUST detect when the target is an elevated process window and return a clear error for operations that would fail due to UIPI.
- **FR-012**: The tool MUST handle secure desktop scenarios (UAC, lock screen) by returning a clear error.
- **FR-013**: Window titles MUST be matched case-insensitively by default.
- **FR-014**: The tool MUST return the monitor information (which monitor a window is on) when returning window positions.
- **FR-015**: The tool MUST log all operations with correlation ID, action, window identifiers, and outcome as structured JSON to stderr.
- **FR-016**: The `wait_for` action MUST accept a `timeout_ms` parameter (default: 30000ms).
- **FR-017**: The tool MUST handle windows that change title dynamically (e.g., browser tabs) by querying the current title at operation time.
- **FR-018**: The `close` action MUST send WM_CLOSE and NOT forcibly terminate the process.
- **FR-019**: The tool MUST support identifying windows by handle (numeric) or by title search.

### Key Entities

- **WindowInfo**: Complete information about a window (handle, title, class, process, bounds, state, monitor)
- **WindowState**: Window state enumeration (normal, minimized, maximized, hidden)
- **WindowAction**: The action to perform (list, find, activate, get_foreground, minimize, maximize, restore, close, move, resize, set_bounds, wait_for)
- **WindowBounds**: Position and size (x, y, width, height)
- **WindowFilter**: Filter criteria for searching (title substring, regex, process name)
- **WindowManagementResult**: Success status, window info, error details if failed
- **WindowManagementErrorCode**: Standardized error codes for programmatic handling

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All window enumeration operations complete successfully on a standard Windows 11 desktop.
- **SC-002**: Window listing returns all visible top-level windows within 500ms.
- **SC-003**: Window activation successfully brings the target window to the foreground 95% of the time (Windows focus stealing prevention may occasionally block).
- **SC-004**: Window positions and sizes are reported accurately to within 1 pixel.
- **SC-005**: Elevated process windows are correctly detected and appropriate errors returned.
- **SC-006**: Multi-monitor setups are correctly handled with accurate per-monitor coordinates.
- **SC-007**: The `wait_for` action correctly waits for windows to appear and returns within 100ms of window creation.
- **SC-008**: All window state changes (minimize, maximize, restore) complete successfully.
- **SC-009**: Window move and resize operations position windows accurately to specified coordinates.
- **SC-010**: All operations return within 5 seconds (default timeout configurable).

## Assumptions

- The MCP Server runs at standard (non-elevated) integrity level by default.
- The Windows 11 desktop is accessible (not locked, no UAC dialogs active).
- Windows are managed by the Desktop Window Manager (DWM) in the default compositor mode.
- Window handles (HWND) remain valid for the duration of an operation but may become invalid between operations.
- Applications respond normally to window messages (WM_CLOSE, etc.).
- The tool operates on the current user's desktop session only; remote desktops or other sessions are not supported.
