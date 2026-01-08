# Feature Specification: Comprehensive LLM Tool Coverage Tests

**Feature Branch**: `015-llm-tool-coverage`  
**Created**: 2026-01-05  
**Status**: Draft  
**Input**: User description: "Comprehensive LLM tests with agent-benchmark for every tool and every action, focusing on UI test automation against real Windows applications (Notepad and Paint)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Core UI Interaction Tools Coverage (Priority: P1)

As a developer maintaining mcp-windows, I want LLM tests for all UI interaction tools (`ui_find`, `ui_click`, `ui_type`, `ui_read`) so that I can verify that LLMs correctly use these tools against real applications.

**Why this priority**: These are the primary tools for UI automation. If LLMs cannot correctly use these tools, the entire UI Automation First approach fails.

**Independent Test**: Can be fully tested by launching Notepad and Paint, exercising each UI tool against their controls, and verifying state changes.

**Acceptance Scenarios**:

1. **Given** Notepad is running, **When** the LLM is asked to find all menu items, **Then** it uses `ui_find` with appropriate search criteria and receives a list of menu items.

2. **Given** Paint is running, **When** the LLM is asked to click the Brushes button in the toolbar, **Then** it uses `ui_click` and the Brushes tool is selected.

3. **Given** Notepad is running, **When** the LLM is asked to type "Hello World" in the text area, **Then** it uses `ui_type` or `keyboard_control` and the text appears in the document.

4. **Given** Notepad has text content, **When** the LLM is asked to read the document content, **Then** it uses `ui_read` and correctly returns the text content.

---

### User Story 2 - App and Window Management Tools Coverage (Priority: P1)

As a developer, I want LLM tests for the `app` tool and `window_management` tool so that I can verify LLMs can correctly launch applications, manage windows, and handle window lifecycle operations.

**Why this priority**: The `app` tool is the entry point for all workflows. Window management is essential for multi-window workflows and cleanup.

**Independent Test**: Can be tested by launching Notepad and Paint, performing window operations (minimize, maximize, restore, move, resize, close), and verifying window states.

**Acceptance Scenarios**:

1. **Given** no Notepad is running, **When** the LLM is asked to launch Notepad, **Then** it uses `app` with `programPath=notepad.exe` and receives a valid window handle.

2. **Given** multiple windows are open, **When** the LLM is asked to list all windows, **Then** it uses `window_management(action='list')` and receives window information.

3. **Given** Notepad is running, **When** the LLM is asked to find it by title, **Then** it uses `window_management(action='find', title='...')` containing "Notepad" and receives the handle.

4. **Given** a window handle, **When** the LLM is asked to minimize then restore the window, **Then** it uses `minimize` then `restore` actions successfully.

5. **Given** a window handle, **When** the LLM is asked to move the window to (100, 100), **Then** it uses `window_management(action='move', handle='...', x=100, y=100)` successfully.

6. **Given** a window handle, **When** the LLM is asked to resize the window to 800x600, **Then** it uses `window_management(action='resize', handle='...', width=800, height=600)` successfully.

7. **Given** Notepad with unsaved changes, **When** the LLM is asked to close without saving, **Then** it uses `window_management(action='close', handle='...', discardChanges=true)` successfully.

---

### User Story 3 - Keyboard and Mouse Control Tools Coverage (Priority: P2)

As a developer, I want LLM tests for `keyboard_control` and `mouse_control` tools so that I can verify LLMs can type text, press hotkeys, and click at coordinates when needed.

**Why this priority**: Keyboard input is essential for hotkeys. Mouse control is the fallback for custom controls like Paint's canvas.

**Independent Test**: Can be tested by launching Notepad for keyboard operations and Paint for mouse operations.

**Acceptance Scenarios**:

1. **Given** Notepad is active, **When** the LLM is asked to type "Hello World", **Then** it uses `keyboard_control(action='type', text='Hello World')` successfully.

2. **Given** Notepad has text, **When** the LLM is asked to select all, **Then** it uses `keyboard_control(action='press', key='a', modifiers='ctrl')` successfully.

3. **Given** any window is active, **When** the LLM is asked to get the current mouse position, **Then** it uses `mouse_control(action='get_position')` and receives coordinates.

4. **Given** Paint is running with a canvas, **When** the LLM is asked to draw a line from (100,100) to (200,200), **Then** it uses `mouse_control(action='drag', x=100, y=100, endX=200, endY=200)` successfully.

5. **Given** Paint is running, **When** the LLM is asked to click at coordinates (150, 150) on the canvas, **Then** it uses `mouse_control(action='click', x=150, y=150)` successfully.

---

### User Story 4 - Screenshot and File Tools Coverage (Priority: P2)

As a developer, I want LLM tests for `screenshot_control` and `ui_file` tools so that I can verify LLMs can capture annotated screenshots and handle Save As dialogs.

**Why this priority**: Screenshots are critical for visual debugging. File operations are common in automation workflows.

**Independent Test**: Can be tested by capturing screenshots of Notepad and Paint, and handling Save As dialogs in both applications.

**Acceptance Scenarios**:

1. **Given** Notepad is running, **When** the LLM is asked to capture a screenshot, **Then** it uses `screenshot_control(action='capture', windowHandle='...')` and receives image data with annotated elements.

2. **Given** Paint is running, **When** the LLM is asked for a plain screenshot, **Then** it uses `screenshot_control(annotate=false)` successfully.

3. **Given** any application is running, **When** the LLM is asked to list monitors, **Then** it uses `screenshot_control(action='list_monitors')` and receives monitor information.

4. **Given** Notepad with text content, **When** the LLM is asked to save the file to a specific path, **Then** it uses `ui_file` to handle the Save As dialog and the file is saved.

5. **Given** Paint with a drawing, **When** the LLM is asked to save the image as a PNG file, **Then** it uses `ui_file` to handle the Save As dialog and the file is saved.

---

### User Story 5 - Paint Canvas and Tool Operations (Priority: P2)

As a developer, I want LLM tests that verify UI tools and mouse control work correctly with Paint's canvas and toolbar so that I can ensure the tools handle both standard UI elements and custom drawing surfaces.

**Why this priority**: Paint provides a real-world test case for mouse-based interactions (drawing, dragging) and toolbar/ribbon UI that differs from standard form controls.

**Independent Test**: Can be tested by running Paint and exercising toolbar selection, color picking, and canvas drawing operations.

**Acceptance Scenarios**:

1. **Given** Paint is running, **When** the LLM is asked to find all tools in the ribbon, **Then** it uses `ui_find` and successfully locates toolbar elements.

2. **Given** Paint is running, **When** the LLM is asked to select the pencil tool, **Then** it uses `ui_click` on the pencil button and the tool is selected.

3. **Given** Paint is running, **When** the LLM is asked to select the color red, **Then** it uses `ui_click` on the red color in the color palette.

4. **Given** Paint with a tool selected, **When** the LLM is asked to draw a rectangle on the canvas, **Then** it uses `mouse_control(action='drag')` with appropriate coordinates.

5. **Given** Paint is running, **When** the LLM is asked to capture an annotated screenshot, **Then** it uses `screenshot_control` and receives element data including toolbar elements.

---

### User Story 6 - Real-World Workflow Scenarios (Priority: P2)

As a developer, I want LLM tests that validate multi-step real-world workflows similar to those documented in industry evaluations (e.g., 4sysops Windows-MCP article) so that I can demonstrate that mcp-windows can accomplish practical automation tasks that competitors struggle with.

**Reference**: https://4sysops.com/archives/windows-mcp-automating-the-windows-gui-with-ai/

**Why this priority**: Proves practical utility. The referenced article shows common failures in MCP GUI automation - we must pass these.

**Independent Test**: Workflows run against Notepad and Paint - real Windows applications available on all systems.

**Acceptance Scenarios** (adapted from 4sysops article using Notepad and Paint):

1. **Text Editing Workflow (Notepad)**: 
   - **Given** Notepad is running, **When** the LLM is asked to "type a paragraph of text, then format it by selecting all and using keyboard shortcuts", **Then** it successfully uses `keyboard_control` for typing and hotkeys, demonstrating multi-step text editing.

2. **Menu Navigation Workflow (Notepad)**:
   - **Given** Notepad is running, **When** the LLM is asked to "open the Format menu and enable Word Wrap", **Then** it uses `ui_click` to open the menu and select the menu item.

3. **Save Dialog Handling Workflow (Notepad)**:
   - **Given** Notepad with text content, **When** the LLM is asked to "save the file as 'test-output.txt' to the Documents folder", **Then** it uses `keyboard_control` for Ctrl+S or `ui_click` for File > Save As, then `ui_file` to complete the save operation.

4. **State Discovery Workflow (Paint)**:
   - **Given** Paint is running, **When** the LLM is asked to "discover all tools available in the toolbar", **Then** it uses `screenshot_control(annotate=true)` or `ui_find` and correctly reports brushes, shapes, colors available.

5. **Keyboard Shortcuts Workflow (Notepad)**:
   - **Given** Notepad is open with text content, **When** the LLM is asked to "select all text, copy it, and paste it at the end", **Then** it uses `keyboard_control` with `Ctrl+A`, `Ctrl+C`, `End`, `Ctrl+V` sequence successfully.

6. **Drawing Workflow (Paint)**:
   - **Given** Paint is running, **When** the LLM is asked to "select the brush tool, choose blue color, and draw a line across the canvas", **Then** it uses `ui_click` for tool/color selection and `mouse_control(drag)` for drawing.

7. **Window Lifecycle Workflow**:
   - **Given** no Notepad is running, **When** the LLM is asked to "launch Notepad, minimize it, wait 2 seconds, restore it, and then close it", **Then** it uses `app` to launch, `window_management` for minimize/restore/close operations in sequence.

8. **Multi-App Workflow (Notepad + Paint)**:
   - **Given** neither app is running, **When** the LLM is asked to "open both Notepad and Paint, switch between them, then close both", **Then** it uses `app` to launch both, `window_management(activate)` to switch focus, and `window_management(close)` for cleanup.

---

### Edge Cases

- What happens when the LLM tries to interact with a non-existent element? (Should receive error, not hallucinate success)
- How does the LLM handle timeout scenarios (element never appears)? (Should receive timeout error with clear message)
- What happens when the LLM provides an invalid window handle? (Should receive InvalidWindowHandle error)
- How does the LLM handle a Save dialog that doesn't appear? (Should timeout gracefully)
- How does the LLM recover when a click misses its target? (Should retry with refined coordinates or automation ID)
- What happens when Paint's canvas coordinates are outside the visible area? (Should handle gracefully)
- How does the LLM handle Notepad's "Save changes?" prompt when closing with unsaved content? (Should use discardChanges or handle dialog)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST have LLM tests for `app` tool with `programPath` parameter verification.
- **FR-002**: System MUST have LLM tests for `ui_find` with search criteria (`name`, `nameContains`, `automationId`, `controlType`).
- **FR-003**: System MUST have LLM tests for `ui_click` verifying button clicks, checkbox toggles, and tab selection.
- **FR-004**: System MUST have LLM tests for `ui_type` verifying text input with `clearFirst` option.
- **FR-005**: System MUST have LLM tests for `ui_read` verifying text extraction from various control types.
- **FR-007**: System MUST have LLM tests for `ui_file` verifying Save As dialog handling.
- **FR-008**: System MUST have LLM tests for `window_management` with key actions (`list`, `find`, `activate`, `minimize`, `maximize`, `restore`, `close`, `move`, `resize`, `wait_for`).
- **FR-009**: System MUST have LLM tests for `keyboard_control` with key actions (`type`, `press` with modifiers).
- **FR-010**: System MUST have LLM tests for `mouse_control` with key actions (`click`, `scroll`, `get_position`).
- **FR-011**: System MUST have LLM tests for `screenshot_control` with `capture` (annotated and plain) and `list_monitors` actions.
- **FR-012**: All tests MUST use only Notepad and Paint — standard Windows applications available on all systems.
- **FR-013**: All tests MUST verify correct tool selection (no hallucinated tools assertion).
- **FR-014**: All tests MUST use flexible assertions (regex, anyOf) to accept valid alternative tool choices and parameter variations.
- **FR-015**: All tests MUST include latency assertions (max 30 seconds per step).

### Prompt Writing Requirements (NON-NEGOTIABLE)

- **FR-022**: All test prompts MUST be written in plain English as a real user would type them.
- **FR-023**: Test prompts MUST NOT contain tool names, parameter names, or API syntax.
- **FR-024**: Test prompts MUST NOT include implementation hints or technical guidance.
- **FR-025**: If an LLM fails to select the correct tool, the fix MUST be in tool descriptions or system prompts, NOT in the test prompt.

**Rationale** (from `.github/testing.instructions.md`):
> Test scenarios represent what REAL USERS would say. The test USER prompts should be:
> - Natural language a real user would type
> - Free of implementation details (tool names, parameter names, exact syntax)
> - The "specification" of what the LLM should be able to handle

**Good prompt examples**:
- "Find all buttons in the window"
- "Click the Submit button"
- "Type 'testuser' in the username field"
- "Minimize the window, wait 2 seconds, then restore it"

**Bad prompt examples** (NEVER use these):
- ❌ "Use ui_find with controlType=Button"
- ❌ "Call window_management with action='minimize'"
- ❌ "Use keyboard_control to type the text"
- **FR-016**: System MUST have LLM tests for multi-step form completion workflows (combining `ui_type`, `ui_click`, checkbox/radio/combobox interactions).
- **FR-017**: System MUST have LLM tests for tab navigation workflows (switching between tabs, reading content from different tabs).
- **FR-018**: System MUST have LLM tests for keyboard shortcut sequences (Ctrl+A, Ctrl+C, Ctrl+V, etc.).
- **FR-019**: System MUST have LLM tests for scroll-and-find workflows (scrolling to locate elements, then interacting).
- **FR-020**: System MUST have LLM tests for window lifecycle workflows (launch → minimize → restore → close sequence).
- **FR-021**: All multi-step workflow tests MUST validate that LLM maintains context across steps within a session.

### Key Entities

- **Notepad (notepad.exe)**: Windows built-in text editor. Key UI elements: Document text area, Menu bar (File, Edit, Format, View, Help), Status bar. Supports: Text input, keyboard shortcuts (Ctrl+S, Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+Z), Save As dialog, Word Wrap toggle, Font dialog.
- **Paint (mspaint.exe)**: Windows built-in image editor. Key UI elements: Canvas area, Ribbon toolbar with Home/View tabs, Tools group (Pencil, Brush, Fill, Text, Eraser, Color Picker), Shapes group, Colors palette, Size controls. Supports: Mouse drawing/dragging, tool selection, color selection, Save As dialog with format options (PNG, JPEG, BMP, GIF).
- **Test Session**: A group of related test steps that share conversation context in agent-benchmark.
- **Assertion Types**: `tool_called`, `tool_param_equals`, `tool_result_matches_json`, `no_hallucinated_tools`, `max_latency_ms`, `output_not_contains`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tools (10 tools) have at least one LLM test covering basic functionality.
- **SC-002**: All 10 key `window_management` actions have LLM test coverage (`list`, `find`, `activate`, `minimize`, `maximize`, `restore`, `close`, `move`, `resize`, `wait_for`).
- **SC-003**: All 5 UI tools (`ui_find`, `ui_click`, `ui_type`, `ui_read`, `ui_file`) have LLM test coverage.
- **SC-004**: All LLM tests pass with 100% success rate on all configured providers (Azure OpenAI GPT-4.1 and GPT-5.2-chat).
- **SC-005**: Each test step completes within 30 seconds.
- **SC-006**: Zero hallucinated tools across all test sessions.
- **SC-007**: Tests are reproducible — same results when run multiple times (allowing for minor LLM variation).
- **SC-008**: All 8 real-world workflow scenarios (Notepad and Paint workflows) pass successfully.
- **SC-009**: Multi-step workflows complete end-to-end without human intervention.
- **SC-010**: Tests work on any Windows 10/11 system without requiring custom test harness installation.

---

## Test File Organization

Tests will be organized in YAML files under `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`:

| File | Coverage |
|------|----------|
| `notepad-ui-test.yaml` | Text input, menu navigation, keyboard shortcuts against Notepad |
| `paint-ui-test.yaml` | Tool selection, color picking, canvas operations against Paint |
| `window-management-test.yaml` | All window_management actions (using Notepad) |
| `keyboard-mouse-test.yaml` | keyboard_control (Notepad) and mouse_control (Paint) actions |
| `screenshot-test.yaml` | screenshot_control actions (both apps) |
| `file-dialog-test.yaml` | Save As dialog handling in both Notepad and Paint |
| `real-world-workflows-test.yaml` | Multi-step workflow scenarios using Notepad and Paint |

## Assumptions

- Notepad (notepad.exe) and Paint (mspaint.exe) are available on all Windows 10/11 systems by default.
- No custom test harness installation or build steps are required — tests use built-in Windows applications.
- Azure OpenAI endpoints are configured via environment variables (`AZURE_OPENAI_ENDPOINT`).
- Tests run on Windows 10/11 with English locale (for `ui_file` and `discardChanges` features, menu item names).
- The test machine has at least one monitor for screenshot tests.
- Paint's ribbon UI may vary slightly between Windows versions; tests should use flexible element matching.
- Test artifacts (saved files from Notepad/Paint) are written to unique timestamped folders under `tests/Sbroenne.WindowsMcp.LLM.Tests/output/` (e.g., `output/2026-01-07_143022/`), kept for debugging, and gitignored.

## Clarifications

### Session 2026-01-05

- Q: How should the WinForms test harness be launched for LLM tests? → A: ~~Build and launch via `dotnet run` from a dedicated launcher project~~ **SUPERSEDED**: Use Notepad and Paint instead
- Q: How should LLM tests handle non-determinism and flakiness? → A: Retry individual test steps up to 2 times on failure
- Q: Should tests pass on multiple providers or just one? → A: Pass on all configured providers
- Q: Should each test session start with a clean state? → A: Yes, close all test app windows (Notepad, Paint) before each session
- Q: How strict should parameter assertions be? → A: Use flexible assertions (regex, anyOf) to accept valid alternatives

### Session 2026-01-07

- Q: Why switch from test harnesses to Notepad and Paint? → A: Eliminates need to build/maintain custom test harnesses; uses real Windows apps available on all systems; provides more realistic test scenarios
- Q: What about Electron app testing? → A: Deferred to future work; Notepad and Paint cover Win32 UI automation comprehensively
- Q: Where should test artifacts (saved files) be written and how cleaned up? → A: Use unique timestamped folders per run, keep for debugging (manual cleanup), gitignore the output folder
- Q: How should tests discover canvas coordinates for Paint drawing? → A: Let the LLM decide autonomously (screenshot, ui_find, or other); tests should not prescribe the discovery method
- Q: What should happen when Azure OpenAI rate limits are hit? → A: Configure rate limiting in agent-benchmark to prevent hitting limits (built-in support)
- Q: Should tests verify visual result of Paint drawing? → A: Verify tool calls only (assert mouse_control called with drag action); no pixel verification
- Q: Minimum providers that must pass for success? → A: All configured providers must pass (100% across GPT-4.1 and GPT-5.2-chat)

### Session 2026-01-05 (Update)

- Added **User Story 6 - Real-World Workflow Scenarios** based on 4sysops article patterns
- Source: https://4sysops.com/archives/windows-mcp-automating-the-windows-gui-with-ai/
- Rationale: The article documents common failures in Windows MCP GUI automation; we must demonstrate mcp-windows passes these real-world scenarios
- Added 7 acceptance scenarios covering: form completion, tab navigation, dialog handling, state discovery, keyboard shortcuts, scroll-and-find, window lifecycle
- Added FR-016 through FR-021 for new workflow requirements
- Added SC-008 and SC-009 for workflow success criteria
- Added `real-world-workflows-test.yaml` to test file organization

### Session 2026-01-05 (Plain English Prompts)

- Added **FR-022 through FR-025**: Plain English prompt requirements (NON-NEGOTIABLE)
- Source: `.github/testing.instructions.md` - "NEVER modify test scenario USER prompts to include implementation hints"
- Key principle: Test prompts must be natural language that real users would type, NOT technical instructions with tool names
- Acceptance scenarios in this spec describe **what to assert** (tool called), not **what to prompt**
- Example interpretation:
  - Spec says: "When the LLM is asked to click Submit, Then it uses ui_click"
  - Prompt should be: "Click the Submit button" (plain English)
  - Assertion should verify: tool_called=ui_click (technical validation)
- If tests fail, fix tool descriptions or system prompts — NEVER add hints to test prompts
