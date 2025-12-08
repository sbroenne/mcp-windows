# Tasks: LLM-Based Integration Testing Framework

**Input**: Design documents from `/specs/007-llm-integration-testing/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not applicable - this feature IS the testing framework (self-referential)

**Organization**: Tasks are grouped by user story to enable independent implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Documentation**: `specs/007-llm-integration-testing/`
- **Scenarios**: `specs/007-llm-integration-testing/scenarios/`
- **Results**: `specs/007-llm-integration-testing/results/`
- **Templates**: `specs/007-llm-integration-testing/templates/`
- **Scripts** (optional): `scripts/test-llm/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create directory structure and templates

- [X] T001 Create scenarios directory in specs/007-llm-integration-testing/scenarios/
- [X] T002 Create results directory in specs/007-llm-integration-testing/results/
- [X] T003 Create templates directory in specs/007-llm-integration-testing/templates/
- [X] T004 [P] Create scenario template in specs/007-llm-integration-testing/templates/scenario-template.md
- [X] T005 [P] Create result template in specs/007-llm-integration-testing/templates/result-template.md
- [X] T006 [P] Create daily report template in specs/007-llm-integration-testing/templates/report-template.md
- [X] T007 Add .gitignore entry for results/ directory (optional - can commit or ignore results)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core templates and validation that ALL user stories depend on

**⚠️ CRITICAL**: No scenario creation can begin until templates are complete

- [X] T008 Define scenario markdown structure with all required fields in templates/scenario-template.md
- [X] T009 Define result markdown structure matching data-model.md in templates/result-template.md
- [X] T010 Define report aggregation format in templates/report-template.md
- [X] T011 Create monitor detection preamble (reusable snippet for all scenarios) in templates/monitor-preamble.md
- [X] T012 Document scenario naming conventions in templates/README.md

**Checkpoint**: Templates ready - scenario authoring can now begin

---

## Phase 3: User Story 1 - Execute Single Tool Verification Test (Priority: P1) 🎯 MVP

**Goal**: Create scenarios that verify single MCP tools work correctly when invoked by the LLM

**Independent Test**: Execute any single-tool scenario (e.g., TC-MOUSE-001) via Copilot chat and verify pass/fail

### Mouse Control Scenarios (12 scenarios)

- [X] T013 [P] [US1] Create scenario TC-MOUSE-001 in scenarios/TC-MOUSE-001.md (Move cursor to absolute position)
- [X] T014 [P] [US1] Create scenario TC-MOUSE-002 in scenarios/TC-MOUSE-002.md (Move cursor to screen corners)
- [X] T015 [P] [US1] Create scenario TC-MOUSE-003 in scenarios/TC-MOUSE-003.md (Single left click)
- [X] T016 [P] [US1] Create scenario TC-MOUSE-004 in scenarios/TC-MOUSE-004.md (Move and click combined)
- [X] T017 [P] [US1] Create scenario TC-MOUSE-005 in scenarios/TC-MOUSE-005.md (Double-click action)
- [X] T018 [P] [US1] Create scenario TC-MOUSE-006 in scenarios/TC-MOUSE-006.md (Right-click context menu)
- [X] T019 [P] [US1] Create scenario TC-MOUSE-007 in scenarios/TC-MOUSE-007.md (Middle-click action)
- [X] T020 [P] [US1] Create scenario TC-MOUSE-008 in scenarios/TC-MOUSE-008.md (Scroll up)
- [X] T021 [P] [US1] Create scenario TC-MOUSE-009 in scenarios/TC-MOUSE-009.md (Scroll down)
- [X] T022 [P] [US1] Create scenario TC-MOUSE-010 in scenarios/TC-MOUSE-010.md (Horizontal scroll)
- [X] T023 [P] [US1] Create scenario TC-MOUSE-011 in scenarios/TC-MOUSE-011.md (Mouse drag operation)
- [X] T024 [P] [US1] Create scenario TC-MOUSE-012 in scenarios/TC-MOUSE-012.md (Click with modifier key)

### Keyboard Control Scenarios (15 scenarios)

- [X] T025 [P] [US1] Create scenario TC-KEYBOARD-001 in scenarios/TC-KEYBOARD-001.md (Type simple text)
- [X] T026 [P] [US1] Create scenario TC-KEYBOARD-002 in scenarios/TC-KEYBOARD-002.md (Type special characters)
- [X] T027 [P] [US1] Create scenario TC-KEYBOARD-003 in scenarios/TC-KEYBOARD-003.md (Press Enter key)
- [X] T028 [P] [US1] Create scenario TC-KEYBOARD-004 in scenarios/TC-KEYBOARD-004.md (Press Tab key)
- [X] T029 [P] [US1] Create scenario TC-KEYBOARD-005 in scenarios/TC-KEYBOARD-005.md (Press Escape key)
- [X] T030 [P] [US1] Create scenario TC-KEYBOARD-006 in scenarios/TC-KEYBOARD-006.md (Press function key F1)
- [X] T031 [P] [US1] Create scenario TC-KEYBOARD-007 in scenarios/TC-KEYBOARD-007.md (Keyboard shortcut Ctrl+A)
- [X] T032 [P] [US1] Create scenario TC-KEYBOARD-008 in scenarios/TC-KEYBOARD-008.md (Keyboard shortcut Ctrl+C)
- [X] T033 [P] [US1] Create scenario TC-KEYBOARD-009 in scenarios/TC-KEYBOARD-009.md (Keyboard shortcut Ctrl+V)
- [X] T034 [P] [US1] Create scenario TC-KEYBOARD-010 in scenarios/TC-KEYBOARD-010.md (Keyboard shortcut Alt+Tab)
- [X] T035 [P] [US1] Create scenario TC-KEYBOARD-011 in scenarios/TC-KEYBOARD-011.md (Keyboard shortcut Win+D)
- [X] T036 [P] [US1] Create scenario TC-KEYBOARD-012 in scenarios/TC-KEYBOARD-012.md (Hold and release key)
- [X] T037 [P] [US1] Create scenario TC-KEYBOARD-013 in scenarios/TC-KEYBOARD-013.md (Key sequence/combo)
- [X] T038 [P] [US1] Create scenario TC-KEYBOARD-014 in scenarios/TC-KEYBOARD-014.md (Arrow key navigation)
- [X] T039 [P] [US1] Create scenario TC-KEYBOARD-015 in scenarios/TC-KEYBOARD-015.md (Get keyboard layout)

### Window Management Scenarios (14 scenarios)

- [X] T040 [P] [US1] Create scenario TC-WINDOW-001 in scenarios/TC-WINDOW-001.md (List all windows)
- [X] T041 [P] [US1] Create scenario TC-WINDOW-002 in scenarios/TC-WINDOW-002.md (Find window by title)
- [X] T042 [P] [US1] Create scenario TC-WINDOW-003 in scenarios/TC-WINDOW-003.md (Find window by partial title)
- [X] T043 [P] [US1] Create scenario TC-WINDOW-004 in scenarios/TC-WINDOW-004.md (Get foreground window)
- [X] T044 [P] [US1] Create scenario TC-WINDOW-005 in scenarios/TC-WINDOW-005.md (Activate window by handle)
- [X] T045 [P] [US1] Create scenario TC-WINDOW-006 in scenarios/TC-WINDOW-006.md (Minimize window)
- [X] T046 [P] [US1] Create scenario TC-WINDOW-007 in scenarios/TC-WINDOW-007.md (Maximize window)
- [X] T047 [P] [US1] Create scenario TC-WINDOW-008 in scenarios/TC-WINDOW-008.md (Restore window)
- [X] T048 [P] [US1] Create scenario TC-WINDOW-009 in scenarios/TC-WINDOW-009.md (Move window to position)
- [X] T049 [P] [US1] Create scenario TC-WINDOW-010 in scenarios/TC-WINDOW-010.md (Resize window)
- [X] T050 [P] [US1] Create scenario TC-WINDOW-011 in scenarios/TC-WINDOW-011.md (Set window bounds)
- [X] T051 [P] [US1] Create scenario TC-WINDOW-012 in scenarios/TC-WINDOW-012.md (Close window)
- [X] T052 [P] [US1] Create scenario TC-WINDOW-013 in scenarios/TC-WINDOW-013.md (Wait for window to appear)
- [X] T053 [P] [US1] Create scenario TC-WINDOW-014 in scenarios/TC-WINDOW-014.md (Filter windows by process name)

### Screenshot Capture Scenarios (10 scenarios)

- [X] T054 [P] [US1] Create scenario TC-SCREENSHOT-001 in scenarios/TC-SCREENSHOT-001.md (Capture primary screen)
- [X] T055 [P] [US1] Create scenario TC-SCREENSHOT-002 in scenarios/TC-SCREENSHOT-002.md (List available monitors)
- [X] T056 [P] [US1] Create scenario TC-SCREENSHOT-003 in scenarios/TC-SCREENSHOT-003.md (Capture specific monitor by index)
- [X] T057 [P] [US1] Create scenario TC-SCREENSHOT-004 in scenarios/TC-SCREENSHOT-004.md (Capture rectangular region)
- [X] T058 [P] [US1] Create scenario TC-SCREENSHOT-005 in scenarios/TC-SCREENSHOT-005.md (Capture with cursor included)
- [X] T059 [P] [US1] Create scenario TC-SCREENSHOT-006 in scenarios/TC-SCREENSHOT-006.md (Capture window by handle)
- [X] T060 [P] [US1] Create scenario TC-SCREENSHOT-007 in scenarios/TC-SCREENSHOT-007.md (Capture with invalid monitor index)
- [X] T061 [P] [US1] Create scenario TC-SCREENSHOT-008 in scenarios/TC-SCREENSHOT-008.md (Capture region with zero dimensions)
- [X] T062 [P] [US1] Create scenario TC-SCREENSHOT-009 in scenarios/TC-SCREENSHOT-009.md (Capture region extending beyond screen)
- [X] T063 [P] [US1] Create scenario TC-SCREENSHOT-010 in scenarios/TC-SCREENSHOT-010.md (Rapid consecutive captures)

**Checkpoint**: 51 single-tool scenarios complete - User Story 1 is MVP-ready

---

## Phase 4: User Story 2 - Visual Comparison and Diff Analysis (Priority: P1)

**Goal**: Create scenarios that capture before/after screenshots and document visual verification patterns

**Independent Test**: Run any visual verification scenario and confirm LLM describes differences correctly

### Visual Verification Scenarios (5 scenarios)

- [X] T064 [P] [US2] Create scenario TC-VISUAL-001 in scenarios/TC-VISUAL-001.md (Detect window position change)
- [X] T065 [P] [US2] Create scenario TC-VISUAL-002 in scenarios/TC-VISUAL-002.md (Detect text content change)
- [X] T066 [P] [US2] Create scenario TC-VISUAL-003 in scenarios/TC-VISUAL-003.md (Detect no change - negative test)
- [X] T067 [P] [US2] Create scenario TC-VISUAL-004 in scenarios/TC-VISUAL-004.md (Detect button state change)
- [X] T068 [P] [US2] Create scenario TC-VISUAL-005 in scenarios/TC-VISUAL-005.md (Detect window close)

### Visual Verification Documentation

- [X] T069 [US2] Document visual verification patterns in templates/visual-verification-guide.md
- [X] T070 [US2] Add before/after screenshot naming conventions to templates/README.md

**Checkpoint**: Visual comparison capability documented and scenarios ready

---

## Phase 5: User Story 3 - Multi-Step Workflow Testing (Priority: P2)

**Goal**: Create scenarios that chain multiple MCP actions together

**Independent Test**: Run TC-WORKFLOW-001 and verify all steps complete in sequence

### Workflow Scenarios (10 scenarios)

- [X] T071 [P] [US3] Create scenario TC-WORKFLOW-001 in scenarios/TC-WORKFLOW-001.md (Find and activate window)
- [X] T072 [P] [US3] Create scenario TC-WORKFLOW-002 in scenarios/TC-WORKFLOW-002.md (Move window and verify position)
- [X] T073 [P] [US3] Create scenario TC-WORKFLOW-003 in scenarios/TC-WORKFLOW-003.md (Type text in window)
- [X] T074 [P] [US3] Create scenario TC-WORKFLOW-004 in scenarios/TC-WORKFLOW-004.md (Click button and verify state)
- [X] T075 [P] [US3] Create scenario TC-WORKFLOW-005 in scenarios/TC-WORKFLOW-005.md (Open application via keyboard)
- [X] T076 [P] [US3] Create scenario TC-WORKFLOW-006 in scenarios/TC-WORKFLOW-006.md (Resize and screenshot window)
- [X] T077 [P] [US3] Create scenario TC-WORKFLOW-007 in scenarios/TC-WORKFLOW-007.md (Copy-paste workflow)
- [X] T078 [P] [US3] Create scenario TC-WORKFLOW-008 in scenarios/TC-WORKFLOW-008.md (Window cascade manipulation)
- [X] T079 [P] [US3] Create scenario TC-WORKFLOW-009 in scenarios/TC-WORKFLOW-009.md (Drag and drop simulation)
- [X] T080 [P] [US3] Create scenario TC-WORKFLOW-010 in scenarios/TC-WORKFLOW-010.md (Full UI interaction sequence)

### Workflow Documentation

- [X] T081 [US3] Document multi-step execution patterns in templates/workflow-guide.md

**Checkpoint**: Multi-step workflows documented and scenarios ready ✅

---

## Phase 6: User Story 4 - Test Scenario Definition Format (Priority: P2)

**Goal**: Finalize and document the scenario format for contributors

**Independent Test**: A new contributor can write a valid scenario within 15 minutes (SC-006)

- [X] T082 [US4] Finalize scenario template with all sections from data-model.md in templates/scenario-template.md
- [X] T083 [US4] Create contributor guide for writing scenarios in docs/CONTRIBUTING-TESTS.md
- [X] T084 [US4] Add example scenarios with annotations in templates/example-scenario-annotated.md
- [X] T085 [US4] Validate contracts/scenario-schema.json matches template structure

**Checkpoint**: Scenario format fully documented for external contributors ✅

---

## Phase 7: User Story 5 - Test Results Reporting (Priority: P2)

**Goal**: Create result templates and reporting structure

**Independent Test**: Run a test, save results per template, and verify all artifacts present (SC-004)

### Error Handling Scenarios (8 scenarios - needed for reporting edge cases)

- [X] T086 [P] [US5] Create scenario TC-ERROR-001 in scenarios/TC-ERROR-001.md (Invalid mouse coordinates)
- [X] T087 [P] [US5] Create scenario TC-ERROR-002 in scenarios/TC-ERROR-002.md (Window action on invalid handle)
- [X] T088 [P] [US5] Create scenario TC-ERROR-003 in scenarios/TC-ERROR-003.md (Type text with no focused input)
- [X] T089 [P] [US5] Create scenario TC-ERROR-004 in scenarios/TC-ERROR-004.md (Screenshot during secure desktop)
- [X] T090 [P] [US5] Create scenario TC-ERROR-005 in scenarios/TC-ERROR-005.md (Timeout on window wait)
- [X] T091 [P] [US5] Create scenario TC-ERROR-006 in scenarios/TC-ERROR-006.md (Close already-closed window)
- [X] T092 [P] [US5] Create scenario TC-ERROR-007 in scenarios/TC-ERROR-007.md (Invalid key name)
- [X] T093 [P] [US5] Create scenario TC-ERROR-008 in scenarios/TC-ERROR-008.md (Keyboard combo with invalid modifier)

### Reporting Templates and Structure

- [X] T094 [US5] Finalize result template with all fields from data-model.md in templates/result-template.md
- [X] T095 [US5] Finalize report template for daily summaries in templates/report-template.md
- [X] T096 [US5] Document result storage conventions in templates/results-guide.md
- [X] T097 [US5] Create sample result file for reference in results/example/TC-EXAMPLE-001/result.md

**Checkpoint**: Results and reporting fully documented ✅

---

## Phase 8: User Story 6 - GitHub Copilot Chat Integration (Priority: P3)

**Goal**: Provide chat command patterns for executing tests from VS Code

**Independent Test**: User can type test execution prompt and see results in chat

- [X] T098 [US6] Create chat prompt templates in templates/chat-prompts.md
- [X] T099 [US6] Document single test execution pattern in quickstart.md (add section)
- [X] T100 [US6] Document batch test execution pattern in quickstart.md (add section)
- [X] T101 [US6] Document category-based execution pattern in quickstart.md (add section)

**Checkpoint**: Chat integration patterns documented in quickstart.md ✅

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and validation

- [X] T102 [P] Update quickstart.md with references to all new templates
- [X] T103 [P] Update spec.md checklists/requirements.md with implementation status
- [X] T104 [P] Create index of all 74 scenarios in scenarios/README.md
- [X] T105 Run quickstart.md validation - execute sample test via Copilot chat
- [X] T106 Verify all scenario files follow template structure (spot check 10%)
- [X] T107 Update plan.md artifacts table with completion status

**Checkpoint**: All phases complete ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all scenario creation
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - US1 (Phase 3) and US2 (Phase 4) are P1 - complete first
  - US3, US4, US5 (Phases 5-7) are P2 - can run in parallel after P1
  - US6 (Phase 8) is P3 - complete last
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Depends On | Notes |
|-------|----------|------------|-------|
| US1 | P1 | Foundational | MVP - single-tool tests |
| US2 | P1 | Foundational | MVP - visual verification |
| US3 | P2 | US1 | Builds on single-tool patterns |
| US4 | P2 | Foundational | Format documentation only |
| US5 | P2 | Foundational | Reporting templates |
| US6 | P3 | US1, US4 | References execution patterns |

### Parallel Opportunities

Within each user story phase, ALL scenario creation tasks marked [P] can run in parallel because each creates a separate file.

**Example - US1 Mouse Scenarios** (12 files, all parallel):
```
T013 scenarios/TC-MOUSE-001.md
T014 scenarios/TC-MOUSE-002.md
T015 scenarios/TC-MOUSE-003.md
... (all 12 can be created simultaneously)
```

---

## Parallel Example: User Story 1 Mouse Scenarios

```bash
# All 12 mouse scenarios can be created in parallel:
Task T013: scenarios/TC-MOUSE-001.md
Task T014: scenarios/TC-MOUSE-002.md
Task T015: scenarios/TC-MOUSE-003.md
Task T016: scenarios/TC-MOUSE-004.md
Task T017: scenarios/TC-MOUSE-005.md
Task T018: scenarios/TC-MOUSE-006.md
Task T019: scenarios/TC-MOUSE-007.md
Task T020: scenarios/TC-MOUSE-008.md
Task T021: scenarios/TC-MOUSE-009.md
Task T022: scenarios/TC-MOUSE-010.md
Task T023: scenarios/TC-MOUSE-011.md
Task T024: scenarios/TC-MOUSE-012.md
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001-T007)
2. Complete Phase 2: Foundational (T008-T012)
3. Complete Phase 3: User Story 1 - Single Tool Tests (T013-T063)
4. Complete Phase 4: User Story 2 - Visual Verification (T064-T070)
5. **STOP and VALIDATE**: Run several tests via Copilot chat
6. MVP delivered - 56 test scenarios ready for use

### Incremental Delivery

| Increment | Phases | Deliverable | Scenario Count |
|-----------|--------|-------------|----------------|
| MVP | 1-4 | Single-tool + visual tests | 56 |
| +Workflows | 5 | Multi-step scenarios | +10 (66 total) |
| +Format | 6 | Contributor docs | +0 (docs only) |
| +Reporting | 7 | Error scenarios + templates | +8 (74 total) |
| +Chat | 8 | Chat patterns | +0 (docs only) |
| Complete | 9 | Polished, validated | 74 scenarios |

### Task Count Summary

| Phase | Tasks | Parallelizable |
|-------|-------|----------------|
| 1 Setup | 7 | 3 |
| 2 Foundational | 5 | 0 |
| 3 US1 Single Tool | 51 | 51 |
| 4 US2 Visual | 7 | 5 |
| 5 US3 Workflow | 11 | 10 |
| 6 US4 Format | 4 | 0 |
| 7 US5 Reporting | 12 | 8 |
| 8 US6 Chat | 4 | 0 |
| 9 Polish | 6 | 3 |
| **Total** | **107** | **80** |

---

## Notes

- **[P] tasks** = different files, no dependencies - can all run simultaneously
- **[Story] label** maps task to specific user story for traceability
- **No code tasks** - this is a documentation-only feature
- Each scenario file is independent and can be authored in any order within a phase
- Commit after each logical group of scenarios (e.g., all TC-MOUSE-*, all TC-KEYBOARD-*)
- Validate by running actual tests via Copilot chat at each checkpoint
