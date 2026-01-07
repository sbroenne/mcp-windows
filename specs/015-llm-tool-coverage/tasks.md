# Tasks: Comprehensive LLM Tool Coverage Tests

**Input**: Design documents from `/specs/015-llm-tool-coverage/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: This feature IS a test suite - each YAML file is a test artifact. No separate test tasks needed.

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **Checkbox**: Every task starts with `- [ ]`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, etc.) - required for Phases 3+
- **File path**: Include exact file paths in descriptions

## Path Conventions

- **Test files**: `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`
- **Config files**: `tests/Sbroenne.WindowsMcp.LLM.Tests/`

---

## Phase 1: Setup

**Purpose**: Project infrastructure for test output

- [X] T001 Add `output/` to gitignore in `tests/Sbroenne.WindowsMcp.LLM.Tests/.gitignore`
- [X] T002 [P] Create output directory placeholder in `tests/Sbroenne.WindowsMcp.LLM.Tests/output/.gitkeep`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared provider/agent configuration pattern that ALL test files must use

**⚠️ CRITICAL**: All user story YAML files depend on this configuration pattern

- [X] T003 Document shared configuration pattern in `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/_config-template.yaml` with:
  - Both providers (azure-openai-gpt41, azure-openai-gpt5-chat) with Entra ID auth and rate_limits
  - Server config (windows-mcp with stdio, server_delay: 10s)
  - Both agent configs with system_prompt templates and clarification_detection
  - Standard settings (verbose: true, max_iterations: 15, test_delay: 60s)
  - Note: This is a reference template, not a runnable test file

**Checkpoint**: Configuration pattern ready - user story YAML files can now be created in parallel

---

## Phase 3: User Story 1 - Core UI Interaction Tools (Priority: P1) 🎯 MVP

**Goal**: LLM tests for ui_find, ui_click, ui_type, ui_read, ui_wait against Notepad

**Independent Test**: Run `.\Run-LLMTests.ps1 -Scenario notepad-ui-test.yaml` and verify all tests pass on both providers

### Implementation for User Story 1

- [X] T004 [US1] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/notepad-ui-test.yaml` with:
  - Providers: azure-openai-gpt41, azure-openai-gpt5-chat (Entra ID auth, rate_limits per research.md)
  - Agents: gpt41-agent, gpt5-chat-agent with system_prompt and clarification_detection
  - Session 1: "UI Discovery" - ui_find for menus and text area
  - Session 2: "Text Input" - ui_type for document editing
  - Session 3: "Text Reading" - ui_read for content extraction
  - Session 4: "Menu Navigation" - ui_click for File/Edit/Format menus
  - Session 5: "UI Waiting" - ui_wait for element states
  - Include cleanup step at start of each session
  - Plain English prompts only (FR-022-025)
  - Assertions: tool_called, no_hallucinated_tools, max_latency_ms: 30000, no_error_messages

- [ ] T005 [US1] Validate notepad-ui-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario notepad-ui-test.yaml -Build`

**Checkpoint**: User Story 1 complete - 5 UI tools verified against Notepad

---

## Phase 4: User Story 2 - App and Window Management (Priority: P1)

**Goal**: LLM tests for app tool and all 10 window_management actions

**Independent Test**: Run `.\Run-LLMTests.ps1 -Scenario window-management-test.yaml` and verify all 10 actions pass

### Implementation for User Story 2

- [X] T006 [P] [US2] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/window-management-test.yaml` with:
  - Same provider/agent config pattern as US1
  - Session 1: "Launch and Find" - app launch, window_management(find), window_management(list)
  - Session 2: "Window State" - minimize, maximize, restore, activate actions
  - Session 3: "Window Position" - move to (100,100), resize to 800x600
  - Session 4: "Window Lifecycle" - wait_for element, close with discardChanges=true
  - Cover ALL 10 actions: list, find, activate, minimize, maximize, restore, close, move, resize, wait_for
  - Plain English prompts (e.g., "Move the window to the top-left corner")
  - Include cleanup step at session start

- [ ] T007 [US2] Validate window-management-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario window-management-test.yaml`

**Checkpoint**: User Story 2 complete - All 10 window_management actions verified

---

## Phase 5: User Story 3 - Keyboard and Mouse Control (Priority: P2)

**Goal**: LLM tests for keyboard_control and mouse_control tools

**Independent Test**: Run `.\Run-LLMTests.ps1 -Scenario keyboard-mouse-test.yaml` and verify all tests pass

### Implementation for User Story 3

- [X] T008 [P] [US3] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/keyboard-mouse-test.yaml` with:
  - Same provider/agent config pattern
  - Session 1: "Typing" - keyboard_control(type) with text in Notepad
  - Session 2: "Hotkeys" - keyboard_control(press) with Ctrl+A, Ctrl+C, Ctrl+V sequences
  - Session 3: "Mouse Click" - mouse_control(click) at coordinates in Paint canvas
  - Session 4: "Mouse Drag" - mouse_control(drag) for drawing a line in Paint
  - Session 5: "Mouse Position" - mouse_control(get_position) to read current position
  - Use Notepad for keyboard, Paint for mouse tests
  - Use anyOf assertions for flexible tool acceptance (keyboard_control OR ui_type)
  - Plain English prompts (e.g., "Type 'Hello World'", "Select all the text")

- [ ] T009 [US3] Validate keyboard-mouse-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario keyboard-mouse-test.yaml`

**Checkpoint**: User Story 3 complete - Keyboard and mouse controls verified

---

## Phase 6: User Story 4 - Screenshot and File Tools (Priority: P2)

**Goal**: LLM tests for screenshot_control and ui_file tools

**Independent Test**: Run both `screenshot-test.yaml` and `file-dialog-test.yaml` and verify all tests pass

### Implementation for User Story 4

- [X] T010 [P] [US4] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/screenshot-test.yaml` with:
  - Same provider/agent config pattern
  - Session 1: "Annotated Screenshot" - screenshot_control(capture, annotate=true) of Notepad
  - Session 2: "Plain Screenshot" - screenshot_control(capture, annotate=false) of Paint
  - Session 3: "Monitor List" - screenshot_control(list_monitors) to enumerate displays
  - Session 4: "Window Screenshot" - capture specific window by handle
  - Verify annotated screenshots return element data
  - Plain English prompts (e.g., "Take a screenshot of Notepad with element labels")

- [X] T011 [P] [US4] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/file-dialog-test.yaml` with:
  - Same provider/agent config pattern
  - Session 1: "Notepad Save" - save text file via ui_file with unique filename using {{now format="epoch"}}
  - Session 2: "Paint Save" - save image as PNG via ui_file
  - Use {{TEST_RESULTS_PATH}} environment variable for output folder
  - Plain English prompts (e.g., "Save the document as test.txt in the output folder")

- [ ] T012 [US4] Validate screenshot-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario screenshot-test.yaml`
- [ ] T013 [US4] Validate file-dialog-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario file-dialog-test.yaml`

**Checkpoint**: User Story 4 complete - Screenshot and file dialog tools verified

---

## Phase 7: User Story 5 - Paint Canvas and Tool Operations (Priority: P2)

**Goal**: LLM tests for Paint ribbon UI and canvas drawing operations

**Independent Test**: Run `.\Run-LLMTests.ps1 -Scenario paint-ui-test.yaml` and verify all tests pass

### Implementation for User Story 5

- [X] T014 [P] [US5] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/paint-ui-test.yaml` with:
  - Same provider/agent config pattern
  - Session 1: "Tool Discovery" - ui_find for ribbon toolbar elements (pencil, brush, shapes)
  - Session 2: "Tool Selection" - ui_click for pencil, brush, shape buttons
  - Session 3: "Color Selection" - ui_click for color palette (red, blue, green)
  - Session 4: "Canvas Drawing" - mouse_control(drag) for drawing (LLM discovers coordinates autonomously)
  - Session 5: "State Discovery" - screenshot_control(annotate=true) to verify element discovery
  - Verify tool calls only, not visual output (per clarification)
  - Plain English prompts (e.g., "Find all tools in the toolbar", "Draw a diagonal line")

- [ ] T015 [US5] Validate paint-ui-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario paint-ui-test.yaml`

**Checkpoint**: User Story 5 complete - Paint ribbon and canvas operations verified

---

## Phase 8: User Story 6 - Real-World Workflow Scenarios (Priority: P2)

**Goal**: Multi-step workflow tests matching 4sysops article patterns and spec acceptance scenarios

**Independent Test**: Run `.\Run-LLMTests.ps1 -Scenario real-world-workflows-test.yaml` and verify all 8 workflows complete end-to-end

### Implementation for User Story 6

- [X] T016 [P] [US6] Create `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/real-world-workflows-test.yaml` with:
  - Same provider/agent config pattern
  - Session 1: "Text Editing Workflow" - type paragraph, select all, copy, paste at end (Notepad)
  - Session 2: "Menu Navigation Workflow" - open Format menu, enable Word Wrap (Notepad)
  - Session 3: "Save Dialog Workflow" - type text, save as test file with unique name (Notepad)
  - Session 4: "State Discovery Workflow" - discover all toolbar tools via screenshot or ui_find (Paint)
  - Session 5: "Keyboard Shortcuts Workflow" - Ctrl+A, Ctrl+C, End, Ctrl+V sequence (Notepad)
  - Session 6: "Drawing Workflow" - select brush, choose blue, draw line across canvas (Paint)
  - Session 7: "Window Lifecycle Workflow" - launch, minimize, wait, restore, close (Notepad)
  - Session 8: "Multi-App Workflow" - open Notepad and Paint, switch between, close both
  - Each workflow is multi-step within a single session
  - Plain English prompts only (e.g., "Type a paragraph, then select all and copy it")
  - Assertions verify end-to-end completion (output_regex, tool_called, no_error_messages)

- [ ] T017 [US6] Validate real-world-workflows-test.yaml passes with `.\Run-LLMTests.ps1 -Scenario real-world-workflows-test.yaml`

**Checkpoint**: User Story 6 complete - All 8 real-world workflows verified end-to-end

---

## Phase 9: Polish & Documentation

**Purpose**: Update documentation and perform final validation across all providers

- [X] T018 [P] Update `tests/Sbroenne.WindowsMcp.LLM.Tests/README.md` with:
  - New test files in Project Structure section
  - Updated Test Scenarios table with all 7 YAML files
  - Remove references to WinForms/Electron test harnesses
  - Add Notepad and Paint as test applications

- [ ] T019 Run full test suite with GPT-4.1: `.\Run-LLMTests.ps1 -Model gpt-4.1` and verify 100% pass rate
- [ ] T020 Run full test suite with GPT-5.2-chat: `.\Run-LLMTests.ps1 -Model gpt-5.2-chat` and verify 100% pass rate
- [ ] T021 Validate quickstart.md commands work correctly by following the guide end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - establishes config pattern for all YAML files
- **User Stories (Phases 3-8)**: All depend on Foundational completion
  - P1 stories (US1, US2) should complete first for MVP
  - P2 stories (US3-US6) can proceed in parallel after Phase 2
- **Polish (Phase 9)**: Depends on all user story YAML files being validated

### User Story Dependencies

| Story | Priority | Can Start After | Dependencies on Other Stories |
|-------|----------|-----------------|-------------------------------|
| US1 (Core UI) | P1 | Phase 2 | None - **MVP candidate** |
| US2 (App/Window) | P1 | Phase 2 | None - **MVP candidate** |
| US3 (Keyboard/Mouse) | P2 | Phase 2 | None |
| US4 (Screenshot/File) | P2 | Phase 2 | None |
| US5 (Paint Canvas) | P2 | Phase 2 | None |
| US6 (Workflows) | P2 | Phase 2 | None (validates all tools) |

### Parallel Opportunities per Phase

```text
Phase 1: Setup
  T001 (gitignore) ─┬─ T002 [P] (output/.gitkeep)
                    └─ Can run in parallel

Phase 2: Foundational
  T003 (config template) ─── Sequential, must complete before any YAML files

Phases 3-8: User Stories (ALL can run in parallel after Phase 2!)
  ┌─ T004 [US1] notepad-ui-test.yaml
  ├─ T006 [P] [US2] window-management-test.yaml
  ├─ T008 [P] [US3] keyboard-mouse-test.yaml
  ├─ T010 [P] [US4] screenshot-test.yaml
  ├─ T011 [P] [US4] file-dialog-test.yaml
  ├─ T014 [P] [US5] paint-ui-test.yaml
  └─ T016 [P] [US6] real-world-workflows-test.yaml

Phase 9: Polish
  T018 [P] (README) ─┬─ T019, T020, T021 (validation)
                     └─ README can run in parallel with validation
```

---

## Parallel Example: All YAML Files After Phase 2

```bash
# After Phase 2 completes, ALL 7 YAML files can be created in parallel:

# Assign to different agents/developers:
Agent 1: T004 [US1] notepad-ui-test.yaml
Agent 2: T006 [US2] window-management-test.yaml  
Agent 3: T008 [US3] keyboard-mouse-test.yaml
Agent 4: T010 [US4] screenshot-test.yaml + T011 file-dialog-test.yaml
Agent 5: T014 [US5] paint-ui-test.yaml
Agent 6: T016 [US6] real-world-workflows-test.yaml

# Or execute sequentially by priority:
Priority 1 (MVP): T004, T006 (Core UI + Window Management)
Priority 2: T008, T010, T011, T014, T016 (Remaining tools + Workflows)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001, T002) - .gitignore and output folder
2. Complete Phase 2: Foundational (T003) - Config template
3. Complete Phase 3: User Story 1 (T004, T005) - notepad-ui-test.yaml
4. Complete Phase 4: User Story 2 (T006, T007) - window-management-test.yaml
5. **STOP and VALIDATE**: All 11 tools have basic coverage (app, window_management, 5 UI tools, keyboard, mouse, screenshot, ui_file)
6. Verify 100% pass rate on both GPT-4.1 and GPT-5.2-chat

**MVP delivers**: 7 tasks, ~50% of test coverage, validates core tool functionality

### Full Delivery (All User Stories)

1. Complete MVP (Phases 1-4)
2. Add Phase 5: US3 (T008, T009) - keyboard-mouse-test.yaml
3. Add Phase 6: US4 (T010-T013) - screenshot-test.yaml + file-dialog-test.yaml
4. Add Phase 7: US5 (T014, T015) - paint-ui-test.yaml
5. Add Phase 8: US6 (T016, T017) - real-world-workflows-test.yaml
6. Complete Phase 9: Polish (T018-T021) - README update + final validation

**Full delivery**: 21 tasks, 100% test coverage, 8 real-world workflows validated

---

## Task Summary

| Phase | Description | Tasks | Story Labels |
|-------|-------------|-------|--------------|
| Phase 1 | Setup | T001-T002 | - |
| Phase 2 | Foundational | T003 | - |
| Phase 3 | Core UI Tools (P1) | T004-T005 | [US1] |
| Phase 4 | App/Window (P1) | T006-T007 | [US2] |
| Phase 5 | Keyboard/Mouse (P2) | T008-T009 | [US3] |
| Phase 6 | Screenshot/File (P2) | T010-T013 | [US4] |
| Phase 7 | Paint Canvas (P2) | T014-T015 | [US5] |
| Phase 8 | Workflows (P2) | T016-T017 | [US6] |
| Phase 9 | Polish | T018-T021 | - |

**Total Tasks**: 21  
**Tasks per User Story**: US1=2, US2=2, US3=2, US4=4, US5=2, US6=2  
**Parallel Opportunities**: 7 YAML files can be created simultaneously after Phase 2  
**Independent Test Criteria**: Each user story has its own validation task  
**Suggested MVP Scope**: Phases 1-4 (7 tasks) - Core UI + Window Management

---

## Success Criteria Verification

| ID | Criterion | Verified By |
|----|-----------|-------------|
| SC-001 | 100% tool coverage (11 tools) | T004 (5 UI), T006 (app, window_mgmt), T008 (keyboard, mouse), T010 (screenshot), T011 (ui_file) |
| SC-002 | All 10 window_management actions | T006 - window-management-test.yaml |
| SC-003 | All 6 UI tools covered | T004 (ui_find, ui_click, ui_type, ui_read, ui_wait) + T011 (ui_file) |
| SC-004 | All providers pass | T019 (GPT-4.1) + T020 (GPT-5.2-chat) |
| SC-005 | <30s per step | max_latency_ms: 30000 assertion in all YAML files |
| SC-006 | Zero hallucinated tools | no_hallucinated_tools assertion in all YAML files |
| SC-007 | Reproducible | T019, T020 - run multiple times |
| SC-008 | 8 workflows pass | T016 - real-world-workflows-test.yaml |
| SC-009 | End-to-end complete | T016, T017 - multi-step sessions work |
| SC-010 | No custom harness | All tests use only notepad.exe and mspaint.exe |

---

## Notes

- All prompts MUST use plain English per FR-022-025 (no tool names or technical syntax)
- Each test file follows provider/agent config pattern from research.md Section 5
- Use `anyOf` assertions for flexible tool acceptance (e.g., keyboard_control OR ui_type)
- Include cleanup step at session start to close any existing Notepad/Paint windows
- Verify tool calls only, not visual output (per clarification)
- Use `{{now format="epoch"}}` for unique filenames in file save tests
- All YAML files must include both GPT-4.1 and GPT-5.2-chat agents
