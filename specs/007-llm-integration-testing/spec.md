# Feature Specification: LLM-Based Integration Testing Framework

**Feature Branch**: `007-llm-integration-testing`  
**Created**: December 8, 2025  
**Status**: Draft  
**Input**: User description: "LLM-based integration testing framework that uses GitHub Copilot to verify MCP server functionality through real LLM-to-MCP interactions with visual verification via screenshots"

## Overview

Traditional unit tests verify code paths in isolation but cannot validate the actual end-to-end behavior when an LLM interacts with the MCP server. This feature introduces a testing paradigm where GitHub Copilot (the LLM) acts as the test executor, invoking MCP tools and using the screenshot capability to visually verify that actions produced the expected results.

### Problem Statement

Current testing gaps:
1. **Unit tests** verify internal logic but not actual MCP protocol interactions
2. **Integration tests** use mocked or programmatic calls, not real LLM reasoning
3. **No visual verification** - tests cannot confirm that a mouse click actually clicked the right location or a window was actually moved
4. **LLM interpretation gaps** - the LLM may misunderstand tool parameters or responses in ways unit tests don't catch

### Solution Approach

Create a testing framework where:
1. **Test scenarios are defined as natural language prompts** that GitHub Copilot executes
2. **The LLM invokes MCP tools** to perform actions (mouse, keyboard, window, screenshot)
3. **Screenshots are captured before and after** each action for visual diff verification
4. **Results are validated** by comparing actual outcomes against expected states

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Execute Single Tool Verification Test (Priority: P1)

As a developer, I want to run a test that verifies a single MCP tool works correctly when invoked by the LLM, so I can confirm the tool behaves as expected in real-world usage.

**Why this priority**: This is the foundational capability - if single tools cannot be verified, no higher-level testing is possible.

**Independent Test**: Can be fully tested by executing one MCP tool (e.g., "move mouse to 500,500") and verifying via screenshot that the cursor is at the expected location.

**Acceptance Scenarios**:

1. **Given** a test scenario file describing a mouse move action, **When** the test runner executes the scenario, **Then** the LLM invokes the mouse_control tool and a screenshot confirms cursor position
2. **Given** a test scenario for keyboard input, **When** the test runner executes, **Then** the LLM invokes keyboard_control and visible text confirms the input was typed
3. **Given** a test scenario that should fail, **When** the action cannot be completed, **Then** the test reports failure with diagnostic screenshots

---

### User Story 2 - Visual Comparison and Diff Analysis (Priority: P1)

As a developer, I want screenshots taken before and after each action so I can visually verify that the action had the expected effect.

**Why this priority**: Visual verification is the core differentiator from traditional testing - without it, we're just running integration tests.

**Independent Test**: Execute any MCP action, capture before/after screenshots, and produce a visual diff highlighting changes.

**Acceptance Scenarios**:

1. **Given** a window move action, **When** before/after screenshots are taken, **Then** the visual diff shows the window in its new position
2. **Given** a mouse click action on a button, **When** before/after screenshots are compared, **Then** the diff highlights the button state change (hover/pressed/clicked)
3. **Given** no visible change occurred, **When** the diff is analyzed, **Then** the test can detect that expected changes did not happen

---

### User Story 3 - Multi-Step Workflow Testing (Priority: P2)

As a developer, I want to define multi-step test workflows that chain multiple MCP actions together, so I can verify complex user interactions work end-to-end.

**Why this priority**: Real-world usage involves sequences of actions. Single-action tests validate tools work; workflow tests validate they work together.

**Independent Test**: Execute a 3-step workflow (e.g., find window → move window → verify position) and confirm all steps complete successfully with intermediate screenshots.

**Acceptance Scenarios**:

1. **Given** a workflow with 3 sequential actions, **When** the workflow executes, **Then** each action is performed in order with screenshots at each step
2. **Given** a workflow where step 2 depends on step 1's result, **When** step 1 returns window handle, **Then** step 2 uses that handle correctly
3. **Given** a workflow where an intermediate step fails, **When** failure occurs, **Then** remaining steps are skipped and failure is reported with context

---

### User Story 4 - Test Scenario Definition Format (Priority: P2)

As a developer, I want to write test scenarios in a clear, readable format that describes what to test without implementation details.

**Why this priority**: A good scenario format makes tests maintainable and allows non-experts to contribute test cases.

**Independent Test**: Create a scenario file, validate it parses correctly, and confirm all required fields are present.

**Acceptance Scenarios**:

1. **Given** a scenario file in the defined format, **When** loaded by the test runner, **Then** all fields are parsed and validated
2. **Given** an invalid scenario file, **When** loaded, **Then** clear validation errors are reported
3. **Given** a scenario with variables/placeholders, **When** executed, **Then** dynamic values are substituted correctly

---

### User Story 5 - Test Results Reporting (Priority: P2)

As a developer, I want comprehensive test results including screenshots, diffs, and pass/fail status, so I can understand what happened during the test.

**Why this priority**: Without good reporting, failed tests cannot be diagnosed and the testing framework provides limited value.

**Independent Test**: Run a test, generate a results report, and confirm it contains all expected artifacts (screenshots, logs, status).

**Acceptance Scenarios**:

1. **Given** a completed test run, **When** results are generated, **Then** a structured report includes pass/fail status, duration, and screenshots
2. **Given** a failed test, **When** viewing results, **Then** the report includes the failure reason, expected vs actual, and relevant screenshots
3. **Given** a multi-step workflow, **When** viewing results, **Then** each step has its own status and screenshots in the report

---

### User Story 6 - GitHub Copilot Chat Integration (Priority: P3)

As a developer, I want to trigger tests directly from VS Code using chat commands, so I can run LLM integration tests without leaving my IDE.

**Why this priority**: IDE integration improves developer experience but is not essential for the core testing capability.

**Independent Test**: Type a chat command like `/test-mcp mouse-move`, observe the test executes and results appear in the editor.

**Acceptance Scenarios**:

1. **Given** a chat command `/test-mcp [scenario-name]`, **When** executed, **Then** the specified scenario runs and results display in the chat
2. **Given** a chat command `/test-mcp --all`, **When** executed, **Then** all defined scenarios run and summary results display
3. **Given** test results, **When** viewing in chat, **Then** screenshots are displayed inline or linked for quick access

---

### Edge Cases

- What happens when the MCP server is not running? → Test framework detects connection failure and reports clearly
- What happens when a screenshot cannot be captured (e.g., secure desktop)? → Test logs the limitation and continues if possible, or fails gracefully
- What happens when the LLM interprets a scenario incorrectly? → Scenario format should be unambiguous; framework should detect unexpected tool calls
- What happens when visual diff detects no change but action claims success? → This is flagged as a potential false positive for human review
- What happens during concurrent test execution? → Tests should run sequentially to avoid mouse/keyboard conflicts

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow defining test scenarios in a structured file format (YAML or JSON)
- **FR-002**: System MUST execute scenarios by passing prompts to GitHub Copilot for LLM processing
- **FR-003**: System MUST capture screenshots before and after each MCP tool invocation
- **FR-004**: System MUST compute visual differences between before/after screenshots
- **FR-005**: System MUST generate structured test results with pass/fail status
- **FR-006**: System MUST support single-action and multi-step workflow scenarios
- **FR-007**: System MUST validate scenario files before execution and report errors
- **FR-008**: System MUST handle MCP server connection failures gracefully
- **FR-009**: System MUST store screenshots and results in a predictable directory structure
- **FR-010**: System MUST support timeout configuration for long-running actions
- **FR-011**: System MUST capture diagnostic information when tests fail (logs, screenshots, tool responses)
- **FR-012**: System MUST support running specific scenarios by name or all scenarios in a directory

### Key Entities

- **Test Scenario**: Definition of what to test - includes description, actions, expected outcomes
- **Test Step**: A single action within a scenario - MCP tool invocation with parameters
- **Screenshot Pair**: Before/after images captured around an action
- **Visual Diff**: Computed difference between two screenshots highlighting changes
- **Test Result**: Outcome of a scenario execution - pass/fail, duration, artifacts
- **Test Report**: Aggregated results from one or more scenario executions

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can execute a single-action test scenario and receive pass/fail results within 30 seconds
- **SC-002**: Visual diffs correctly identify changed regions with 95% accuracy (measured against known test cases)
- **SC-003**: Multi-step workflows with up to 10 actions complete successfully without manual intervention
- **SC-004**: Test results include all required artifacts (screenshots, diffs, logs) in 100% of executions
- **SC-005**: Failed tests provide sufficient diagnostic information for a developer to identify the issue without re-running
- **SC-006**: Scenario format is learnable - new contributors can write valid scenarios within 15 minutes of reading documentation
- **SC-007**: Test execution is deterministic - same scenario produces same outcome on repeated runs (excluding timing-sensitive tests)

## Assumptions

- GitHub Copilot is available and configured in VS Code
- The MCP server (Sbroenne.WindowsMcp) is running and accessible
- The Windows desktop is visible and not on a secure desktop (UAC, lock screen)
- Screenshots can be captured of the active desktop
- Test scenarios target the current Windows session (no remote desktop scenarios)
- Tests run sequentially to avoid input conflicts (mouse/keyboard contention)
- **Tests MUST target the secondary monitor when available** to avoid interference with the developer's VS Code session on the primary monitor; tests detect available monitors at startup and select the secondary monitor if present, falling back to primary only when no secondary exists (per Constitution v2.3.0, Principle XIV)

## Clarifications

### Session 2025-12-08
- Q: How should tests handle multi-monitor setups? → A: Tests explicitly target secondary monitor when available; fallback to primary if no secondary exists
- Q: What target application(s) should tests use? → A: **Notepad + Calculator** - Notepad for keyboard/text input tests (always available, simple text editing); Calculator for button click and UI interaction tests (visual feedback, predictable layout). Both are Windows 11 built-in apps requiring no installation.

---

## Test Cases

This section defines all test cases organized by MCP tool. Each test case includes:
- **ID**: Unique identifier for tracking
- **Description**: What the test verifies
- **Prompt**: Natural language instruction for the LLM
- **Verification**: How to confirm success (visual or response-based)
- **Expected Outcome**: What constitutes a pass

---

### Mouse Control Tests (TC-MOUSE-*)

#### TC-MOUSE-001: Move cursor to absolute position
- **Description**: Verify mouse can be moved to specific screen coordinates
- **Prompt**: "Move the mouse cursor to position x=500, y=300"
- **Verification**: Screenshot shows cursor at or near (500, 300)
- **Expected Outcome**: Cursor visible at target location; tool returns success

#### TC-MOUSE-002: Move cursor to screen corners
- **Description**: Verify mouse can reach screen boundaries
- **Prompt**: "Move the mouse cursor to the top-left corner of the screen (0, 0)"
- **Verification**: Screenshot shows cursor at screen corner
- **Expected Outcome**: Cursor at (0, 0) or nearest accessible position

#### TC-MOUSE-003: Single left click
- **Description**: Verify left mouse click is performed
- **Prompt**: "Click the left mouse button at the current cursor position"
- **Verification**: If over a clickable element, visual state change detected
- **Expected Outcome**: Tool returns success; any interactive element responds

#### TC-MOUSE-004: Move and click combined
- **Description**: Verify move and click can be combined in one action
- **Prompt**: "Move the mouse to position x=400, y=200 and click"
- **Verification**: Screenshot confirms cursor position and click effect
- **Expected Outcome**: Cursor at target; click action completed

#### TC-MOUSE-005: Double-click action
- **Description**: Verify double-click is performed correctly
- **Prompt**: "Double-click the left mouse button at position x=500, y=400"
- **Verification**: Double-click effect visible (e.g., text selection, folder open)
- **Expected Outcome**: Tool returns success; double-click behavior observed

#### TC-MOUSE-006: Right-click context menu
- **Description**: Verify right-click opens context menu
- **Prompt**: "Right-click at the current mouse position"
- **Verification**: Context menu appears in screenshot
- **Expected Outcome**: Context menu visible; tool returns success

#### TC-MOUSE-007: Middle-click action
- **Description**: Verify middle mouse button click
- **Prompt**: "Perform a middle mouse button click at position x=600, y=300"
- **Verification**: Tool returns success
- **Expected Outcome**: Middle-click action completed without error

#### TC-MOUSE-008: Scroll up
- **Description**: Verify scroll wheel up action
- **Prompt**: "Scroll the mouse wheel up 3 clicks"
- **Verification**: Scrollable content moves up in before/after comparison
- **Expected Outcome**: Visible content scrolled; tool returns success

#### TC-MOUSE-009: Scroll down
- **Description**: Verify scroll wheel down action
- **Prompt**: "Scroll the mouse wheel down 5 clicks"
- **Verification**: Scrollable content moves down in before/after comparison
- **Expected Outcome**: Visible content scrolled; tool returns success

#### TC-MOUSE-010: Horizontal scroll
- **Description**: Verify horizontal scroll capability
- **Prompt**: "Scroll the mouse wheel left 2 clicks"
- **Verification**: Horizontal scrollable content shifts
- **Expected Outcome**: Tool returns success or graceful "not supported" if unavailable

#### TC-MOUSE-011: Mouse drag operation
- **Description**: Verify click-and-drag functionality
- **Prompt**: "Drag the mouse from position (200, 200) to position (400, 400)"
- **Verification**: If dragging a movable element, position changes
- **Expected Outcome**: Drag action completed; tool returns success

#### TC-MOUSE-012: Click with modifier key (Ctrl+Click)
- **Description**: Verify mouse click with modifier key held
- **Prompt**: "Hold Ctrl and click at position x=300, y=300"
- **Verification**: Ctrl+click behavior observed (e.g., multi-select in file explorer)
- **Expected Outcome**: Modifier key applied; tool returns success

---

### Keyboard Control Tests (TC-KEYBOARD-*)

#### TC-KEYBOARD-001: Type simple text
- **Description**: Verify typing plain text characters
- **Prompt**: "Type the text 'Hello World' using the keyboard"
- **Verification**: Text appears in active text field (Notepad recommended)
- **Expected Outcome**: Exact text "Hello World" visible in input area

#### TC-KEYBOARD-002: Type special characters
- **Description**: Verify typing special characters and symbols
- **Prompt**: "Type the text 'Test@123#$%' using the keyboard"
- **Verification**: Special characters appear correctly
- **Expected Outcome**: All characters typed accurately

#### TC-KEYBOARD-003: Press Enter key
- **Description**: Verify Enter key press
- **Prompt**: "Press the Enter key"
- **Verification**: New line created or form submitted
- **Expected Outcome**: Enter action completed; tool returns success

#### TC-KEYBOARD-004: Press Tab key
- **Description**: Verify Tab key for focus navigation
- **Prompt**: "Press the Tab key"
- **Verification**: Focus moves to next element
- **Expected Outcome**: Focus change visible; tool returns success

#### TC-KEYBOARD-005: Press Escape key
- **Description**: Verify Escape key press
- **Prompt**: "Press the Escape key"
- **Verification**: Modal/menu dismissed if present
- **Expected Outcome**: Escape action completed; tool returns success

#### TC-KEYBOARD-006: Press function key (F1)
- **Description**: Verify function key press
- **Prompt**: "Press the F1 key"
- **Verification**: Help window or associated action triggered
- **Expected Outcome**: F1 action triggered; tool returns success

#### TC-KEYBOARD-007: Keyboard shortcut Ctrl+A (Select All)
- **Description**: Verify multi-key shortcut
- **Prompt**: "Press Ctrl+A to select all"
- **Verification**: All content selected in active window
- **Expected Outcome**: Selection visible; tool returns success

#### TC-KEYBOARD-008: Keyboard shortcut Ctrl+C (Copy)
- **Description**: Verify copy shortcut
- **Prompt**: "Press Ctrl+C to copy selected content"
- **Verification**: Tool returns success (clipboard not directly verifiable)
- **Expected Outcome**: Copy action completed without error

#### TC-KEYBOARD-009: Keyboard shortcut Ctrl+V (Paste)
- **Description**: Verify paste shortcut
- **Prompt**: "Press Ctrl+V to paste"
- **Verification**: Previously copied content appears
- **Expected Outcome**: Pasted content visible; tool returns success

#### TC-KEYBOARD-010: Keyboard shortcut Alt+Tab
- **Description**: Verify window switching shortcut
- **Prompt**: "Press Alt+Tab to switch windows"
- **Verification**: Active window changes
- **Expected Outcome**: Different window in foreground; tool returns success

#### TC-KEYBOARD-011: Keyboard shortcut Win+D (Show Desktop)
- **Description**: Verify Windows key shortcuts
- **Prompt**: "Press Win+D to show the desktop"
- **Verification**: All windows minimized, desktop visible
- **Expected Outcome**: Desktop visible; tool returns success

#### TC-KEYBOARD-012: Hold and release key
- **Description**: Verify key hold and release for gaming/special apps
- **Prompt**: "Hold down the Shift key, then release it"
- **Verification**: Key down and key up events sent
- **Expected Outcome**: Tool returns success for both operations

#### TC-KEYBOARD-013: Key sequence/combo
- **Description**: Verify sequential key presses
- **Prompt**: "Press the keys A, B, C in sequence"
- **Verification**: "ABC" appears in text field
- **Expected Outcome**: Characters typed in order

#### TC-KEYBOARD-014: Arrow key navigation
- **Description**: Verify arrow key functionality
- **Prompt**: "Press the Down Arrow key 3 times"
- **Verification**: Selection/cursor moves down
- **Expected Outcome**: Navigation completed; tool returns success

#### TC-KEYBOARD-015: Get keyboard layout
- **Description**: Verify keyboard layout detection
- **Prompt**: "Get the current keyboard layout"
- **Verification**: Layout information returned
- **Expected Outcome**: Tool returns valid layout identifier

---

### Window Management Tests (TC-WINDOW-*)

#### TC-WINDOW-001: List all windows
- **Description**: Verify window enumeration
- **Prompt**: "List all open windows"
- **Verification**: Response includes multiple window entries with titles
- **Expected Outcome**: Array of windows returned with handles and titles

#### TC-WINDOW-002: Find window by title
- **Description**: Verify window search by title
- **Prompt**: "Find the window with 'Notepad' in the title"
- **Verification**: Window handle returned for Notepad
- **Expected Outcome**: Matching window found; handle returned

#### TC-WINDOW-003: Find window by partial title
- **Description**: Verify partial title matching
- **Prompt**: "Find any window containing 'Code' in the title"
- **Verification**: VS Code window found
- **Expected Outcome**: Window with partial match returned

#### TC-WINDOW-004: Get foreground window
- **Description**: Verify active window detection
- **Prompt**: "Get the currently active foreground window"
- **Verification**: Returns handle of currently focused window
- **Expected Outcome**: Valid window handle and title returned

#### TC-WINDOW-005: Activate window by handle
- **Description**: Verify window activation/focus
- **Prompt**: "Activate the Notepad window" (after finding handle)
- **Verification**: Notepad becomes foreground window in screenshot
- **Expected Outcome**: Target window now in foreground

#### TC-WINDOW-006: Minimize window
- **Description**: Verify window minimize action
- **Prompt**: "Minimize the Notepad window"
- **Verification**: Window no longer visible on screen; in taskbar
- **Expected Outcome**: Window minimized; tool returns success

#### TC-WINDOW-007: Maximize window
- **Description**: Verify window maximize action
- **Prompt**: "Maximize the Notepad window"
- **Verification**: Window fills entire screen (or work area)
- **Expected Outcome**: Window maximized; dimensions match screen

#### TC-WINDOW-008: Restore window
- **Description**: Verify window restore from minimized/maximized
- **Prompt**: "Restore the Notepad window to normal size"
- **Verification**: Window returns to previous size and position
- **Expected Outcome**: Window restored; tool returns success

#### TC-WINDOW-009: Move window to position
- **Description**: Verify window repositioning
- **Prompt**: "Move the Notepad window to position x=100, y=100"
- **Verification**: Window top-left corner at (100, 100) in screenshot
- **Expected Outcome**: Window at new position; tool returns success

#### TC-WINDOW-010: Resize window
- **Description**: Verify window resizing
- **Prompt**: "Resize the Notepad window to 800 pixels wide and 600 pixels tall"
- **Verification**: Window dimensions match target in screenshot
- **Expected Outcome**: Window resized to 800x600

#### TC-WINDOW-011: Set window bounds (move + resize)
- **Description**: Verify combined move and resize
- **Prompt**: "Set the Notepad window bounds to x=50, y=50, width=640, height=480"
- **Verification**: Window at position with specified dimensions
- **Expected Outcome**: Window matches all specified bounds

#### TC-WINDOW-012: Close window
- **Description**: Verify window close action
- **Prompt**: "Close the Notepad window"
- **Verification**: Window no longer appears in window list or screen
- **Expected Outcome**: Window closed; no longer in list

#### TC-WINDOW-013: Wait for window to appear
- **Description**: Verify window wait functionality
- **Prompt**: "Wait for a window with 'Calculator' in the title to appear (timeout 10 seconds)"
- **Verification**: Returns when Calculator opens or timeout expires
- **Expected Outcome**: Window found or timeout reported appropriately

#### TC-WINDOW-014: Filter windows by process name
- **Description**: Verify filtering by process
- **Prompt**: "List all windows belonging to the 'explorer.exe' process"
- **Verification**: Only Explorer windows returned
- **Expected Outcome**: Filtered list of windows

---

### Screenshot Capture Tests (TC-SCREENSHOT-*)

#### TC-SCREENSHOT-001: Capture primary screen
- **Description**: Verify full primary screen capture
- **Prompt**: "Take a screenshot of the primary screen"
- **Verification**: Image returned with correct dimensions
- **Expected Outcome**: Valid PNG image matching primary monitor resolution

#### TC-SCREENSHOT-002: List available monitors
- **Description**: Verify monitor enumeration
- **Prompt**: "List all available monitors"
- **Verification**: Response includes monitor details (index, resolution, position)
- **Expected Outcome**: Array of monitors with dimensions

#### TC-SCREENSHOT-003: Capture specific monitor by index
- **Description**: Verify per-monitor capture
- **Prompt**: "Take a screenshot of monitor index 0"
- **Verification**: Image returned for specified monitor
- **Expected Outcome**: Valid image matching monitor 0 resolution

#### TC-SCREENSHOT-004: Capture rectangular region
- **Description**: Verify region capture
- **Prompt**: "Take a screenshot of the region from (100, 100) with width 400 and height 300"
- **Verification**: Image is exactly 400x300 pixels
- **Expected Outcome**: 400x300 PNG image of specified region

#### TC-SCREENSHOT-005: Capture with cursor included
- **Description**: Verify cursor rendering in screenshot
- **Prompt**: "Take a screenshot of the primary screen and include the mouse cursor"
- **Verification**: Cursor visible in captured image
- **Expected Outcome**: Screenshot contains cursor overlay

#### TC-SCREENSHOT-006: Capture window by handle
- **Description**: Verify window-specific capture
- **Prompt**: "Take a screenshot of the Notepad window" (after finding handle)
- **Verification**: Image shows only Notepad window content
- **Expected Outcome**: Image dimensions match window size

#### TC-SCREENSHOT-007: Capture with invalid monitor index
- **Description**: Verify error handling for bad monitor index
- **Prompt**: "Take a screenshot of monitor index 999"
- **Verification**: Error returned with available monitors listed
- **Expected Outcome**: Graceful error with helpful message

#### TC-SCREENSHOT-008: Capture region with zero dimensions
- **Description**: Verify validation of region parameters
- **Prompt**: "Take a screenshot of a region with width 0 and height 100"
- **Verification**: Validation error returned
- **Expected Outcome**: Error indicating invalid region dimensions

#### TC-SCREENSHOT-009: Capture region extending beyond screen
- **Description**: Verify handling of out-of-bounds regions
- **Prompt**: "Take a screenshot of region from (0, 0) with width 99999 and height 99999"
- **Verification**: Either clipped to screen bounds or error returned
- **Expected Outcome**: Graceful handling with valid image or clear error

#### TC-SCREENSHOT-010: Rapid consecutive captures
- **Description**: Verify performance under repeated capture
- **Prompt**: "Take 3 screenshots of the primary screen in succession"
- **Verification**: All 3 images returned successfully
- **Expected Outcome**: Consistent results across all captures

---

### Multi-Tool Workflow Tests (TC-WORKFLOW-*)

#### TC-WORKFLOW-001: Find and activate window
- **Description**: Multi-step: find window, then activate it
- **Prompt**: "Find the Notepad window and bring it to the foreground"
- **Verification**: Notepad becomes active window
- **Expected Outcome**: Window found and activated in sequence

#### TC-WORKFLOW-002: Move window and verify position
- **Description**: Multi-step: move window, screenshot, verify
- **Prompt**: "Move the Notepad window to (200, 200) and take a screenshot to verify"
- **Verification**: Screenshot shows window at target position
- **Expected Outcome**: Window moved; screenshot confirms position

#### TC-WORKFLOW-003: Type text in window
- **Description**: Multi-step: activate window, type text
- **Prompt**: "Activate Notepad, then type 'Integration test successful'"
- **Verification**: Text visible in Notepad in final screenshot
- **Expected Outcome**: Text appears in Notepad

#### TC-WORKFLOW-004: Click button and verify state
- **Description**: Multi-step: move, click, verify change
- **Prompt**: "Move mouse to (300, 300), click, and take before/after screenshots"
- **Verification**: Visual diff shows click effect
- **Expected Outcome**: Screenshots captured; diff generated

#### TC-WORKFLOW-005: Open application via keyboard
- **Description**: Multi-step: Win key, type, enter
- **Prompt**: "Press the Windows key, type 'notepad', and press Enter"
- **Verification**: Notepad application opens
- **Expected Outcome**: New Notepad window appears

#### TC-WORKFLOW-006: Resize and screenshot window
- **Description**: Multi-step: find, resize, capture
- **Prompt**: "Find Notepad, resize it to 640x480, and take a screenshot of just that window"
- **Verification**: Window screenshot is 640x480
- **Expected Outcome**: Window resized and captured correctly

#### TC-WORKFLOW-007: Copy-paste workflow
- **Description**: Multi-step: type, select all, copy, paste
- **Prompt**: "In Notepad, type 'Test', press Ctrl+A, press Ctrl+C, press End, then press Ctrl+V"
- **Verification**: "TestTest" visible in Notepad
- **Expected Outcome**: Text duplicated via copy-paste

#### TC-WORKFLOW-008: Window cascade manipulation
- **Description**: Multi-step: minimize all, restore one
- **Prompt**: "Minimize all windows with Win+D, then activate and maximize Notepad"
- **Verification**: Only Notepad visible, maximized
- **Expected Outcome**: Desktop shown, then Notepad restored and maximized

#### TC-WORKFLOW-009: Drag and drop simulation
- **Description**: Multi-step: locate, drag, drop
- **Prompt**: "Click at (100, 100), hold, drag to (300, 300), and release"
- **Verification**: If dragging a file/item, position changes
- **Expected Outcome**: Drag operation completed

#### TC-WORKFLOW-010: Full UI interaction sequence
- **Description**: Complex workflow testing multiple tools
- **Prompt**: "Open Calculator (Win key, type 'calc', Enter), wait for it to appear, click the '7' button, click '+', click '3', click '=', and take a screenshot"
- **Verification**: Calculator shows result "10"
- **Expected Outcome**: Correct calculation displayed

---

### Error Handling Tests (TC-ERROR-*)

#### TC-ERROR-001: Invalid mouse coordinates (negative)
- **Description**: Verify handling of negative coordinates
- **Prompt**: "Move mouse to position x=-100, y=-100"
- **Verification**: Error returned or position clamped to valid range
- **Expected Outcome**: Graceful handling with clear feedback

#### TC-ERROR-002: Window action on invalid handle
- **Description**: Verify handling of stale window handle
- **Prompt**: "Activate window with handle 999999999"
- **Verification**: Error returned indicating invalid handle
- **Expected Outcome**: Clear error message

#### TC-ERROR-003: Type text with no focused input
- **Description**: Verify keyboard input without text field
- **Prompt**: "Type 'test' with no window focused"
- **Verification**: Action completes or warns about no input target
- **Expected Outcome**: Keystrokes sent; behavior depends on context

#### TC-ERROR-004: Screenshot during secure desktop
- **Description**: Verify handling when screenshot is blocked
- **Prompt**: "Take a screenshot" (during UAC prompt)
- **Verification**: Error returned indicating secure desktop
- **Expected Outcome**: Clear error with explanation

#### TC-ERROR-005: Timeout on window wait
- **Description**: Verify timeout behavior
- **Prompt**: "Wait for a window titled 'NonExistentApp12345' with 3 second timeout"
- **Verification**: Timeout error returned after 3 seconds
- **Expected Outcome**: Timeout reported with elapsed time

#### TC-ERROR-006: Close already-closed window
- **Description**: Verify handling of duplicate close
- **Prompt**: "Close the window with handle [previously closed handle]"
- **Verification**: Error or success (idempotent)
- **Expected Outcome**: Graceful handling

#### TC-ERROR-007: Invalid key name
- **Description**: Verify handling of unrecognized key
- **Prompt**: "Press the 'InvalidKeyName' key"
- **Verification**: Error returned with supported keys hint
- **Expected Outcome**: Clear validation error

#### TC-ERROR-008: Keyboard combo with invalid modifier
- **Description**: Verify handling of bad modifier key
- **Prompt**: "Press Ctrl+Shift+InvalidMod+A"
- **Verification**: Error or partial execution with warning
- **Expected Outcome**: Clear error indicating invalid modifier

---

### Visual Verification Tests (TC-VISUAL-*)

#### TC-VISUAL-001: Detect window position change
- **Description**: Verify visual diff detects window movement
- **Prompt**: "Take screenshot, move Notepad window 100px right, take screenshot"
- **Verification**: Diff highlights old and new window positions
- **Expected Outcome**: Visual diff shows position change

#### TC-VISUAL-002: Detect text content change
- **Description**: Verify visual diff detects text changes
- **Prompt**: "Take screenshot of Notepad, type 'New Text', take screenshot"
- **Verification**: Diff highlights new text area
- **Expected Outcome**: Text region marked as changed

#### TC-VISUAL-003: Detect no change (negative test)
- **Description**: Verify detection when nothing changes
- **Prompt**: "Take two screenshots with no actions between"
- **Verification**: Diff shows no significant changes
- **Expected Outcome**: Diff indicates images are effectively identical

#### TC-VISUAL-004: Detect button state change
- **Description**: Verify detection of UI state changes
- **Prompt**: "Screenshot, hover over a button, screenshot"
- **Verification**: Diff highlights button hover state
- **Expected Outcome**: Button area marked as changed

#### TC-VISUAL-005: Detect window close
- **Description**: Verify detection of window disappearance
- **Prompt**: "Screenshot, close Notepad, screenshot"
- **Verification**: Diff shows window region changed significantly
- **Expected Outcome**: Large change region where window was
