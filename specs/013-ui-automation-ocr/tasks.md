# Implementation Tasks: Windows UI Automation & OCR

**Feature**: 013-ui-automation-ocr  
**Generated**: 2024-12-23  
**Total Tasks**: 54  
**Status**: Ready for Implementation

---

## Overview

Implementation tasks for Windows UI Automation & OCR feature, organized by user story priority for independent, incremental delivery.

### User Story Priority Map

| Story | Priority | Description | Task Count |
|-------|----------|-------------|------------|
| US1 | P1 | Identify UI Elements | 8 |
| US6 | P1 | Combined Workflows | 4 |
| US7 | P1 | Documentation | 4 |
| US8 | P1 | Async UI (Wait/Scroll) | 4 |
| US2 | P1 | Read Text | 3 |
| US3 | P2 | OCR | 6 |
| US4 | P2 | Navigate Hierarchies | 3 |
| US5 | P3 | Invoke Patterns | 3 |

---

## Phase 1: Setup

**Goal**: Initialize project structure and dependencies.

### Tasks

- [X] T001 Create project structure per plan.md with new folders for Automation/ and updates to Capture/
- [X] T002 Add System.Windows.Automation (UIAutomationClient) reference to src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj
- [ ] T003 [P] Add Windows.Media.Ocr WinRT reference for legacy OCR to src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj
- [ ] T004 [P] Add CsWin32 package for native interop (monitor detection, window handles) to src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj

---

## Phase 2: Foundational

**Goal**: Create base models, interfaces, and STA thread infrastructure required by all user stories.

### Tasks

- [X] T005 Create src/Sbroenne.WindowsMcp/Models/BoundingRect.cs with screen coordinate record
- [X] T006 [P] Create src/Sbroenne.WindowsMcp/Models/MonitorRelativeRect.cs with monitor-relative coordinate record
- [X] T007 [P] Create src/Sbroenne.WindowsMcp/Models/UIElementInfo.cs with element representation record
- [X] T008 [P] Create src/Sbroenne.WindowsMcp/Models/UIAutomationResult.cs with result record
- [X] T009 [P] Create src/Sbroenne.WindowsMcp/Models/UIAutomationDiagnostics.cs with diagnostics record
- [X] T010 Create src/Sbroenne.WindowsMcp/Automation/ElementQuery.cs with search criteria record
- [X] T011 Create src/Sbroenne.WindowsMcp/Automation/IUIAutomationService.cs with service interface
- [X] T012 Create src/Sbroenne.WindowsMcp/Automation/UIAutomationThread.cs with dedicated STA thread for UI Automation calls
- [X] T013 Create src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs with MCP tool skeleton (action dispatch only)

---

## Phase 3: User Story 1 - Identify UI Elements [P1]

**Story Goal**: As an LLM agent, I want to discover UI elements by name, type, or automation ID so that I can understand available interaction targets.

**Independent Test Criteria**: 
- Tool returns elements with bounding rectangles for any running application
- Elements include monitor index and monitor-relative coordinates
- Can query foreground window without explicit handle

### Tasks

- [ ] T014 [US1] Implement ElementResolver in src/Sbroenne.WindowsMcp/Automation/ElementResolver.cs for building element queries from conditions
- [X] T015 [US1] Implement monitor coordinate calculation in src/Sbroenne.WindowsMcp/Automation/CoordinateConverter.cs (integrate with existing Capture/MonitorService)
- [X] T016 [P] [US1] Implement element ID generation (window:hwnd|runtime:id|path:treePath) in src/Sbroenne.WindowsMcp/Automation/ElementIdGenerator.cs
- [X] T017 [US1] Implement UIAutomationService.FindElements in src/Sbroenne.WindowsMcp/Automation/UIAutomationService.cs with STA thread dispatch
- [X] T018 [US1] Implement "find" action handler in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T019 [P] [US1] Implement "get_tree" action handler in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T020 [US1] Create tests/Sbroenne.WindowsMcp.Tests/Automation/UIAutomationServiceTests.cs with find/get_tree integration tests
- [X] T021 [US1] Create tests/Sbroenne.WindowsMcp.Tests/Tools/UIAutomationToolTests.cs with action dispatch tests

---

## Phase 4: User Story 6 - Combined Workflows [P1]

**Story Goal**: As an LLM agent, I want single-action operations (find+click, find+type) so that I can minimize round-trips for common interactions.

**Independent Test Criteria**:
- find_and_click locates button and clicks via InvokePattern or coordinates
- find_and_type clears field (if configured) and types text
- find_and_select works with ComboBox elements

### Tasks

- [X] T022 [US6] Implement "find_and_click" action with InvokePattern fallback to coordinate click in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T023 [P] [US6] Implement "find_and_type" action with ValuePattern and keyboard fallback in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T024 [P] [US6] Implement "find_and_select" action with SelectionPattern in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T025 [US6] Add integration tests for combined workflows in tests/Sbroenne.WindowsMcp.Tests/Tools/UIAutomationToolTests.cs

---

## Phase 5: User Story 8 - Async UI Operations [P1]

**Story Goal**: As an LLM agent, I want to wait for UI elements to appear and scroll elements into view so that I can handle dynamic content reliably.

**Independent Test Criteria**:
- wait_for returns when element appears within timeout
- wait_for returns timeout error with diagnostics when element never appears
- scroll_into_view brings off-screen elements into view

### Tasks

- [X] T026 [US8] Implement "wait_for" action with exponential backoff polling in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T027 [P] [US8] Implement "scroll_into_view" action with ScrollItemPattern in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T028 [US8] Implement window activation before element interaction in src/Sbroenne.WindowsMcp/Automation/UIAutomationService.cs
- [X] T029 [US8] Add wait_for and scroll tests in tests/Sbroenne.WindowsMcp.Tests/Tools/UIAutomationToolTests.cs

---

## Phase 6: User Story 2 - Read Text [P1]

**Story Goal**: As an LLM agent, I want to read text content from UI elements so that I can verify state and extract data without screenshots.

**Independent Test Criteria**:
- get_text returns ValuePattern value for Edit elements
- get_text returns Name property for Text/Static elements
- get_text aggregates child text when includeChildren=true

### Tasks

- [X] T030 [US2] Implement TextExtractor in src/Sbroenne.WindowsMcp/Automation/TextExtractor.cs with ValuePattern/TextPattern/Name fallback chain
- [X] T031 [US2] Implement "get_text" action in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T032 [US2] Add get_text tests in tests/Sbroenne.WindowsMcp.Tests/Automation/TextExtractorTests.cs

---

## Phase 7: User Story 3 - OCR [P2]

**Story Goal**: As an LLM agent, I want to perform OCR on screen regions when UI Automation fails so that I can read text from canvas/bitmap content.

**Independent Test Criteria**:
- ocr returns recognized text with word bounding boxes
- ocr_element captures element bounds and performs OCR
- ocr_status reports engine availability
- OCR availability logged at startup (FR-044)

### Tasks

- [X] T033 [US3] Create src/Sbroenne.WindowsMcp/Models/OcrResult.cs with OcrResult, OcrLine, OcrWord records
- [X] T034 [US3] Create src/Sbroenne.WindowsMcp/Capture/IOcrService.cs with OCR service interface
- [X] T035 [P] [US3] Implement src/Sbroenne.WindowsMcp/Capture/LegacyOcrService.cs with Windows.Media.Ocr
- [X] ~~T036~~ [US3] NPU OCR not implemented (requires MSIX packaging)
- [X] T037 [US3] Add OCR availability startup logging in src/Sbroenne.WindowsMcp/Program.cs per FR-044
- [X] T038 [US3] Implement "ocr", "ocr_element", "ocr_status" actions in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T039 [US3] Add OCR tests in tests/Sbroenne.WindowsMcp.Tests/Capture/OcrServiceTests.cs

---

## Phase 8: User Story 4 - Navigate Hierarchies [P2]

**Story Goal**: As an LLM agent, I want to explore UI element trees so that I can discover controls in complex nested layouts.

**Independent Test Criteria**:
- get_tree returns nested structure with depth control
- find with parentElementId scopes search to subtree
- Tree includes all element properties for decision-making

### Tasks

- [X] T040 [US4] Implement tree traversal with depth limiting in src/Sbroenne.WindowsMcp/Automation/TreeWalker.cs
- [X] T041 [US4] Enhance "get_tree" action with controlTypes filter in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T042 [US4] Add tree traversal tests in tests/Sbroenne.WindowsMcp.Tests/Automation/TreeWalkerTests.cs

---

## Phase 9: User Story 5 - Invoke Patterns [P3]

**Story Goal**: As an LLM agent, I want to invoke specific UI Automation patterns so that I can handle complex controls like toggles and expandable items.

**Independent Test Criteria**:
- invoke with Toggle pattern changes checkbox state
- invoke with ExpandCollapse pattern opens dropdown
- invoke with RangeValue pattern adjusts sliders
- pattern_not_supported error returned with alternative suggestions

### Tasks

- [X] T043 [US5] Implement PatternInvoker in src/Sbroenne.WindowsMcp/Automation/UIAutomationService.cs with all pattern types (Invoke, Toggle, Expand, Collapse, Value, RangeValue, Scroll)
- [X] T044 [US5] Implement "invoke" and "focus" actions in src/Sbroenne.WindowsMcp/Tools/UIAutomationTool.cs
- [X] T045 [US5] Add pattern invocation tests in tests/Sbroenne.WindowsMcp.Tests/Unit/Automation/UIAutomationServiceTests.cs

---

## Phase 10: User Story 7 - Documentation [P1]

**Story Goal**: As a developer, I want comprehensive documentation so that I can understand how to use the UI Automation tool effectively.

**Independent Test Criteria**:
- GitHub Pages include ui-automation.md with examples
- VS Code extension README documents ui_automation tool
- Features page lists UI Automation capabilities

### Tasks

- [X] T046 [US7] Create gh-pages/ui-automation.md with comprehensive usage guide and examples
- [X] T047 [P] [US7] Update gh-pages/features.md with UI Automation & OCR feature section
- [X] T048 [P] [US7] Update vscode-extension/README.md with ui_automation tool documentation
- [X] T049 [US7] Update vscode-extension/package.json with ui_automation description

---

## Phase 11: Polish & Cross-Cutting

**Goal**: Final integration, edge cases, and cleanup.

### Tasks

- [X] T050 Implement error handling with all error types from contract in src/Sbroenne.WindowsMcp/Automation/UIAutomationService.cs
- [X] T051 [P] Add elevated process detection and elevated_target error handling in src/Sbroenne.WindowsMcp/Automation/UIAutomationService.cs
- [X] T052 [P] Add stale element detection and element_stale error handling in src/Sbroenne.WindowsMcp/Automation/ElementIdGenerator.cs
- [X] T053 Run full integration test suite on Windows 11 with secondary monitor configuration (75 tests passing)
- [X] T054 Update specs/013-ui-automation-ocr/checklists/ with implementation verification checklist

---

## Dependencies

### Story Completion Order

```
Phase 1 (Setup) ─► Phase 2 (Foundational) ─┬─► US1 (Identify) ─┬─► US6 (Workflows)
                                           │                   │
                                           │                   ├─► US8 (Async)
                                           │                   │
                                           │                   ├─► US2 (Text)
                                           │                   │
                                           │                   └─► US7 (Docs)
                                           │
                                           └─► US3 (OCR) ──────► can start after Phase 2
                                           │
                                           └─► US4 (Hierarchy) ► depends on US1 get_tree
                                           │
                                           └─► US5 (Patterns) ─► depends on US1 find
```

### Task Dependencies

| Task | Depends On | Rationale |
|------|------------|-----------|
| T014-T021 (US1) | T005-T013 | Foundational models and STA thread required |
| T022-T025 (US6) | T017, T018 | Requires find capability from US1 |
| T026-T029 (US8) | T017, T018 | Requires find capability from US1 |
| T030-T032 (US2) | T017, T018 | Requires find capability from US1 |
| T033-T039 (US3) | T005-T006 | Only needs BoundingRect models, independent of UI Automation |
| T040-T042 (US4) | T019 | Enhances get_tree from US1 |
| T043-T045 (US5) | T017, T018 | Requires find and element caching from US1 |
| T046-T049 (US7) | None | Documentation can start anytime |
| T050-T054 (Polish) | All US phases | Final integration |

---

## Parallel Execution Opportunities

### Phase 2 Parallelism
Tasks T005-T009 (models) can all run in parallel.

### Per-Story Parallelism

**US1**: T016, T019 can run parallel to T017-T018 (different files)

**US6**: T023, T024 can run parallel (different actions in same file)

**US3**: T035 implements LegacyOcrService (NPU not implemented)

**US7**: T047, T048 can run parallel (different files)

### Cross-Story Parallelism

Once Phase 2 complete:
- US1, US3, US7 can start in parallel (no dependencies)
- US6, US8, US2 can start once US1 T018 complete

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

**Recommended MVP**: Complete through Phase 3 (US1 - Identify UI Elements)

This provides:
- Working `find` action to locate elements by name/type/automationId
- Working `get_tree` action to explore UI structure
- Bounding rectangles with monitor-relative coordinates for mouse_control integration
- Sufficient capability for LLM to begin UI automation tasks

### Incremental Delivery Order

1. **MVP (US1)**: find, get_tree → LLM can discover elements
2. **Add US6**: find_and_click, find_and_type, find_and_select → Common workflows
3. **Add US8**: wait_for, scroll_into_view → Dynamic UI handling
4. **Add US2**: get_text → Read element content
5. **Add US3**: ocr, ocr_element, ocr_status → Canvas/bitmap text extraction
6. **Add US4**: Enhanced tree traversal → Complex UI navigation
7. **Add US5**: invoke patterns → Toggle, expand/collapse, sliders
8. **Add US7**: Documentation → User-facing docs

### Feature Flags

No runtime feature flags required. OCR uses Windows.Media.Ocr (legacy engine) on all supported Windows versions.

---

## Validation Checklist

After implementation, verify:

- [ ] All 54 tasks completed
- [ ] All actions from contract implemented (13 actions)
- [ ] All error types handled (11 error types)
- [ ] OCR availability logged at startup (FR-044)
- [ ] Integration tests pass on Windows 11
- [ ] Secondary monitor coordinate calculation verified
- [ ] Electron app (VS Code) automation verified
- [ ] Documentation published to GitHub Pages
- [ ] VS Code extension updated
