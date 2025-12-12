# Tasks: Mouse Position Awareness for LLM Usability

**Input**: Design documents from `/specs/012-mouse-position/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm scope and artifacts before code changes

- [ ] T001 Validate feature scope against plan and spec in specs/012-mouse-position/plan.md and specs/012-mouse-position/spec.md
- [ ] T002 [P] Confirm supporting design artifacts are present (research.md, data-model.md, contracts/) in specs/012-mouse-position/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared test scaffolding for all user stories

- [ ] T003 Create multi-monitor test fixture in tests/Sbroenne.WindowsMcp.Tests/Fixtures/MultiMonitorFixture.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Explicit Monitor Targeting (Priority: P1) 🎯 MVP

**Goal**: Require monitorIndex when coordinates are provided; allow coordinate-less actions without monitorIndex

**Independent Test**: Coordinate-based calls without monitorIndex fail with actionable error; coordinate-less calls still succeed at current cursor

### Tests for User Story 1

- [ ] T004 [P] [US1] Add failing test for missing monitorIndex with coordinates in tests/Sbroenne.WindowsMcp.Tests/Tools/MouseControlToolTests.cs
- [ ] T005 [P] [US1] Add failing test for invalid monitorIndex listing valid indices in tests/Sbroenne.WindowsMcp.Tests/Tools/MouseControlToolTests.cs
- [ ] T006 [P] [US1] Add failing test for coordinates out of bounds returning valid_bounds in tests/Sbroenne.WindowsMcp.Tests/Tools/MouseControlToolTests.cs

### Implementation for User Story 1

- [ ] T007 [US1] Enforce monitorIndex requirement when coordinates are present in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [ ] T008 [US1] Validate monitorIndex range and coordinate bounds per monitor in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [ ] T009 [US1] Preserve coordinate-less actions (click/scroll without x/y) without requiring monitorIndex in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs

**Checkpoint**: User Story 1 independently testable

---

## Phase 4: User Story 2 - Monitor Info in Responses (Priority: P2)

**Goal**: Include monitor index and dimensions in all success responses; provide error_code/error_details on failures

**Independent Test**: Successful actions return monitorIndex/monitorWidth/monitorHeight; errors return structured error_code and details

### Tests for User Story 2

- [ ] T010 [P] [US2] Add test asserting success responses include monitorIndex and dimensions in tests/Sbroenne.WindowsMcp.Tests/Tools/MouseControlToolTests.cs

### Implementation for User Story 2

- [ ] T011 [US2] Extend response model with monitorIndex, monitorWidth, monitorHeight, error_code, error_details in src/Sbroenne.WindowsMcp/Models/MouseControlResult.cs
- [ ] T012 [US2] Populate monitor context on success and structured error details on failure in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs

**Checkpoint**: User Story 2 independently testable

---

## Phase 5: User Story 3 - Query Current Position (Priority: P3)

**Goal**: Provide get_position action returning cursor coordinates and monitor context

**Independent Test**: get_position returns current cursor x/y and monitorIndex with dimensions

### Tests for User Story 3

- [ ] T013 [P] [US3] Add test for get_position returning monitorIndex and dimensions in tests/Sbroenne.WindowsMcp.Tests/Tools/MouseControlToolTests.cs

### Implementation for User Story 3

- [ ] T014 [US3] Implement get_position action in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [ ] T015 [US3] Update contracts to document get_position response fields in specs/012-mouse-position/contracts/mouse-control.md

**Checkpoint**: User Story 3 independently testable

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Final docs and consistency sweep

- [ ] T016 [P] Refresh quickstart examples to match implemented behavior in specs/012-mouse-position/quickstart.md
- [ ] T017 [P] Ensure data-model and plan reflect final API fields in specs/012-mouse-position/data-model.md and specs/012-mouse-position/plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1) → Foundational (Phase 2) → User Stories (Phases 3-5) → Polish (Final)

### User Story Completion Order (by priority)

- US1 (P1) → US2 (P2) → US3 (P3)

### Parallel Opportunities

- [P] tasks within each phase can run concurrently (e.g., T002, T004-T006, T010, T013, T016-T017)
- After Phase 2, separate contributors can work on different user stories in parallel

## Implementation Strategy

- MVP = Complete User Story 1 (coordinate validation) after foundational fixture, then validate
- Incremental: Ship US1 → add monitor context (US2) → add get_position (US3)
- Tests-first per story: add failing tests before implementation tasks
