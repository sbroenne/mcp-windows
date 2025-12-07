# Tasks: Keyboard Control

**Input**: Design documents from `/specs/002-keyboard-control/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Integration tests are included as primary validation (per Constitution I).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, etc.) - only for user story phases

---

## Phase 1: Setup

**Purpose**: Project structure and shared infrastructure changes

- [X] T001 Rename `MouseOperationLock.cs` to `InputOperationLock.cs` and update class name in `src/Sbroenne.WindowsMcp/Services/`
- [X] T002 Update `MouseInputService.cs` to use renamed `InputOperationLock` in `src/Sbroenne.WindowsMcp/Input/MouseInputService.cs`
- [X] T003 [P] Add Win key to `ModifierKey` enum in `src/Sbroenne.WindowsMcp/Models/ModifierKey.cs`
- [X] T004 [P] Create `KeyboardIntegrationTestCollection.cs` test collection definition in `tests/Sbroenne.WindowsMcp.Tests/Integration/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that ALL keyboard user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Native Infrastructure

- [X] T005 [P] Add keyboard constants (KEYEVENTF_UNICODE, KEYEVENTF_KEYUP, KEYEVENTF_EXTENDEDKEY, VK_* codes) to `src/Sbroenne.WindowsMcp/Native/NativeConstants.cs`
- [X] T006 [P] Add GetKeyboardLayout, GetKeyboardLayoutName P/Invoke declarations to `src/Sbroenne.WindowsMcp/Native/NativeMethods.cs`

### Models

- [X] T007 [P] Create `KeyboardAction` enum (Type, Press, KeyDown, KeyUp, Combo, Sequence, ReleaseAll, GetKeyboardLayout) in `src/Sbroenne.WindowsMcp/Models/KeyboardAction.cs`
- [X] T008 [P] Create `KeyboardControlErrorCode` enum in `src/Sbroenne.WindowsMcp/Models/KeyboardControlErrorCode.cs`
- [X] T009 [P] Create `KeyboardLayoutInfo` record in `src/Sbroenne.WindowsMcp/Models/KeyboardLayoutInfo.cs`
- [X] T010 [P] Create `KeySequenceItem` record in `src/Sbroenne.WindowsMcp/Models/KeySequenceItem.cs`
- [X] T011 [P] Create `KeyboardControlRequest` record in `src/Sbroenne.WindowsMcp/Models/KeyboardControlRequest.cs`
- [X] T012 [P] Create `KeyboardControlResult` record in `src/Sbroenne.WindowsMcp/Models/KeyboardControlResult.cs`
- [X] T013 [P] Create `HeldKeyState` record in `src/Sbroenne.WindowsMcp/Models/HeldKeyState.cs`

### Configuration

- [X] T014 [P] Create `KeyboardConfiguration` class with timeout and delay settings in `src/Sbroenne.WindowsMcp/Configuration/KeyboardConfiguration.cs`

### Core Services

- [X] T015 Create `VirtualKeyMapper` static class with key name to VK code mappings in `src/Sbroenne.WindowsMcp/Input/VirtualKeyMapper.cs`
- [X] T016 Create `HeldKeyTracker` service for tracking held keys in `src/Sbroenne.WindowsMcp/Input/HeldKeyTracker.cs`
- [X] T017 [P] Create `IKeyboardInputService` interface in `src/Sbroenne.WindowsMcp/Input/IKeyboardInputService.cs`
- [X] T018 Create `KeyboardOperationLogger` structured logging helper in `src/Sbroenne.WindowsMcp/Logging/KeyboardOperationLogger.cs`

### Unit Tests

- [X] T019 [P] Create `VirtualKeyMapperTests.cs` unit tests for key name parsing in `tests/Sbroenne.WindowsMcp.Tests/Unit/`
- [X] T020 [P] Create `KeyboardConfigurationTests.cs` unit tests for validation in `tests/Sbroenne.WindowsMcp.Tests/Unit/`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Type Text String (Priority: P1) 🎯 MVP

**Goal**: Enable LLM to type text into focused input fields using Unicode input

**Independent Test**: Click on a text field, invoke type action, verify text appears correctly

### Tests for User Story 1

- [X] T021 [P] [US1] Create `KeyboardTypeTests.cs` integration test class with basic ASCII typing test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T022 [P] [US1] Add Unicode character typing test (accented chars, CJK, emoji) to `KeyboardTypeTests.cs`
- [X] T023 [P] [US1] Add special character typing test (!@#$%^&*) to `KeyboardTypeTests.cs`
- [X] T024 [P] [US1] Add newline handling test (\n → Enter) to `KeyboardTypeTests.cs`
- [X] T025 [P] [US1] Add long text chunking test (1000+ chars) to `KeyboardTypeTests.cs`

### Implementation for User Story 1

- [X] T026 [US1] Implement `KeyboardInputService` with type action (Unicode input via KEYEVENTF_UNICODE) in `src/Sbroenne.WindowsMcp/Input/KeyboardInputService.cs`
- [X] T027 [US1] Add surrogate pair handling for emoji in `KeyboardInputService.TypeTextAsync`
- [X] T028 [US1] Add text chunking logic (1000 char segments with delays) in `KeyboardInputService.TypeTextAsync`
- [X] T029 [US1] Add elevated window detection before type operation in `KeyboardInputService`
- [X] T030 [US1] Add secure desktop detection before type operation in `KeyboardInputService`
- [X] T031 [US1] Create `KeyboardControlTool` MCP tool class with type action handler in `src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs`
- [X] T032 [US1] Register keyboard services in DI container in `src/Sbroenne.WindowsMcp/Program.cs`

**Checkpoint**: User Story 1 complete - can type any text via `type` action ✅

---

## Phase 4: User Story 2 - Press Single Key (Priority: P1)

**Goal**: Enable LLM to press individual keys (Enter, Tab, Escape, arrows, F-keys, Copilot)

**Independent Test**: Press Tab to move focus, Enter to submit form, verify navigation works

### Tests for User Story 2

- [X] T033 [P] [US2] Create `KeyboardPressTests.cs` integration test class with Enter key test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T034 [P] [US2] Add Tab key test to `KeyboardPressTests.cs`
- [X] T035 [P] [US2] Add Escape key test to `KeyboardPressTests.cs`
- [X] T036 [P] [US2] Add arrow keys test to `KeyboardPressTests.cs`
- [X] T037 [P] [US2] Add F-key test (F1-F12) to `KeyboardPressTests.cs`
- [X] T038 [P] [US2] Add Copilot key test to `KeyboardPressTests.cs`
- [X] T039 [P] [US2] Add media key tests (volumemute, playpause) to `KeyboardPressTests.cs`

### Implementation for User Story 2

- [X] T040 [US2] Add press action implementation (single key via VK code) to `KeyboardInputService.PressKeyAsync`
- [X] T041 [US2] Add extended key flag handling for navigation keys in `KeyboardInputService`
- [X] T042 [US2] Add repeat parameter support to `KeyboardInputService.PressKeyAsync`
- [X] T043 [US2] Add press action handler to `KeyboardControlTool`

**Checkpoint**: User Story 2 complete - can press any single key ✅

---

## Phase 5: User Story 3 - Key Combination with Modifiers (Priority: P1)

**Goal**: Enable LLM to perform keyboard shortcuts (Ctrl+C, Alt+Tab, Win+E)

**Independent Test**: Select text, invoke Ctrl+C, then Ctrl+V, verify clipboard works

### Tests for User Story 3

- [X] T044 [P] [US3] Create `KeyboardModifierTests.cs` integration test class with Ctrl+key test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T045 [P] [US3] Add Shift+key test to `KeyboardModifierTests.cs`
- [X] T046 [P] [US3] Add Alt+Tab test to `KeyboardModifierTests.cs`
- [X] T047 [P] [US3] Add Ctrl+Shift+key test to `KeyboardModifierTests.cs`
- [X] T048 [P] [US3] Add Win+key test to `KeyboardModifierTests.cs`
- [X] T049 [P] [US3] Add modifier cleanup test (no stuck keys) to `KeyboardModifierTests.cs`

### Implementation for User Story 3

- [X] T050 [US3] Add modifier key handling to `KeyboardInputService` using `ModifierKeyManager`
- [X] T051 [US3] Add Win key support to `ModifierKeyManager` in `src/Sbroenne.WindowsMcp/Input/ModifierKeyManager.cs`
- [X] T052 [US3] Add combo action handler to `KeyboardControlTool` (alias for press with modifiers)
- [X] T053 [US3] Add modifier cleanup in finally block to prevent stuck keys in `KeyboardInputService`

**Checkpoint**: User Story 3 complete - can perform keyboard shortcuts with modifiers ✅

---

## Phase 6: User Story 4 - Press and Hold Key (Priority: P2)

**Goal**: Enable LLM to hold keys for duration (Shift for selection, arrow for continuous movement)

**Independent Test**: Hold Shift, verify key state, release, verify no stuck keys

### Tests for User Story 4

- [X] T054 [P] [US4] Create `KeyboardHoldReleaseTests.cs` integration test class with key_down test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T055 [P] [US4] Add key_up test to `KeyboardHoldReleaseTests.cs`
- [X] T056 [P] [US4] Add release_all test to `KeyboardHoldReleaseTests.cs`
- [X] T057 [P] [US4] Add held key tracking test to `KeyboardHoldReleaseTests.cs`
- [X] T058 [P] [US4] Add key_up for non-held key error test to `KeyboardHoldReleaseTests.cs`

### Implementation for User Story 4

- [X] T059 [US4] Add key_down action implementation (press without release) to `KeyboardInputService.KeyDownAsync`
- [X] T060 [US4] Add key_up action implementation (release previously held key) to `KeyboardInputService.KeyUpAsync`
- [X] T061 [US4] Integrate `HeldKeyTracker` with `KeyboardInputService` for state tracking
- [X] T062 [US4] Add release_all action implementation to `KeyboardInputService.ReleaseAllKeysAsync`
- [X] T063 [US4] Add key_down, key_up, release_all action handlers to `KeyboardControlTool`
- [X] T064 [US4] Add held key cleanup on tool disposal in `KeyboardControlTool`

**Checkpoint**: User Story 4 complete - can hold and release keys with tracking ✅

---

## Phase 7: User Story 5 - Key Sequence (Priority: P2)

**Goal**: Enable LLM to press sequence of keys with timing (macros, multi-key commands)

**Independent Test**: Execute sequence of keys with delays, verify order and timing

### Tests for User Story 5

- [X] T065 [P] [US5] Create `KeyboardSequenceTests.cs` integration test class with basic sequence test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T066 [P] [US5] Add sequence with per-key modifiers test to `KeyboardSequenceTests.cs`
- [X] T067 [P] [US5] Add sequence with custom delay test to `KeyboardSequenceTests.cs`
- [X] T068 [P] [US5] Add sequence error handling test (rollback on failure) to `KeyboardSequenceTests.cs`

### Implementation for User Story 5

- [X] T069 [US5] Add sequence action implementation to `KeyboardInputService.ExecuteSequenceAsync`
- [X] T070 [US5] Add per-key modifier support in sequence execution
- [X] T071 [US5] Add configurable inter-key delay support in sequence execution
- [X] T072 [US5] Add sequence action handler to `KeyboardControlTool`

**Checkpoint**: User Story 5 complete - can execute key sequences with timing ✅

---

## Phase 8: User Story 6 - Get Keyboard Layout (Priority: P2)

**Goal**: Enable LLM to query the current keyboard layout for context

**Independent Test**: Query layout, verify language tag and display name returned

### Tests for User Story 6

- [X] T073 [P] [US6] Create `KeyboardLayoutTests.cs` integration test class with layout query test in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- [X] T074 [P] [US6] Add layout info structure validation test to `KeyboardLayoutTests.cs`

### Implementation for User Story 6

- [X] T075 [US6] Add get_keyboard_layout action implementation to `KeyboardInputService.GetKeyboardLayoutAsync`
- [X] T076 [US6] Add layout language tag parsing (BCP-47 format) in `KeyboardInputService`
- [X] T077 [US6] Add get_keyboard_layout action handler to `KeyboardControlTool`

**Checkpoint**: User Story 6 complete - can query keyboard layout information ✅

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and cleanup

- [X] T078 [P] Update README.md with keyboard control documentation
- [X] T079 [P] Add keyboard control examples to quickstart.md validation
- [X] T080 Run full test suite and verify all tests pass
- [X] T081 Run quickstart.md validation scenarios manually
- [X] T082 Review structured logging output for all operations
- [X] T083 Final code review and cleanup

**Checkpoint**: Phase 9 complete - feature ready for release ✅

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────────┐
                                     │
Phase 2: Foundational ───────────────┼─→ BLOCKS ALL USER STORIES
                                     │
         ┌───────────────────────────┘
         │
         ├─→ Phase 3: US1 - Type Text (P1) 🎯 MVP
         │
         ├─→ Phase 4: US2 - Press Key (P1)
         │
         ├─→ Phase 5: US3 - Modifiers (P1)
         │
         ├─→ Phase 6: US4 - Hold/Release (P2)
         │
         ├─→ Phase 7: US5 - Sequence (P2)
         │
         └─→ Phase 8: US6 - Layout Query (P2)
                                     │
Phase 9: Polish ─────────────────────┘
```

### User Story Dependencies

| Story | Depends On | Can Start After |
|-------|------------|-----------------|
| US1 - Type Text | Foundational | Phase 2 complete |
| US2 - Press Key | Foundational | Phase 2 complete |
| US3 - Modifiers | Foundational + US2 partial | T040-T041 (press implementation) |
| US4 - Hold/Release | Foundational | Phase 2 complete |
| US5 - Sequence | Foundational + US2 partial | T040-T041 (press implementation) |
| US6 - Layout Query | Foundational | Phase 2 complete |

### Parallel Opportunities by Phase

**Phase 1 - Setup**:
- T003, T004 can run in parallel (different files)

**Phase 2 - Foundational**:
- T005, T006 can run in parallel (different native files)
- T007-T014 can all run in parallel (independent model files)
- T015-T018 have dependencies: T015 → T016 → T17 → T18
- T019, T020 can run in parallel (different test files)

**Phase 3+ - User Stories**:
- All test tasks within a story marked [P] can run in parallel
- Different user stories (after foundational) can be worked in parallel by separate developers

---

## Parallel Example: Phase 2 Foundational

```bash
# Launch all model tasks in parallel:
T007: Create KeyboardAction enum
T008: Create KeyboardControlErrorCode enum
T009: Create KeyboardLayoutInfo record
T010: Create KeySequenceItem record
T011: Create KeyboardControlRequest record
T012: Create KeyboardControlResult record
T013: Create HeldKeyState record
T014: Create KeyboardConfiguration class

# Then sequential core services:
T015: VirtualKeyMapper (needed by others)
T016: HeldKeyTracker (uses models)
T017: IKeyboardInputService interface
T018: KeyboardOperationLogger
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T020)
3. Complete Phase 3: User Story 1 - Type Text (T021-T032)
4. **STOP and VALIDATE**: Test type action independently
5. Deploy/demo if ready - LLMs can now type text!

### Incremental Delivery

| Increment | Stories Included | Capabilities |
|-----------|-----------------|--------------|
| MVP | US1 only | Type text into any field |
| +1 | US1 + US2 | Type text + press navigation keys |
| +2 | US1 + US2 + US3 | Full keyboard shortcuts (Ctrl+C, etc.) |
| +3 | All P1 + US4 | Hold keys for extended operations |
| +4 | All P1 + P2 | Full keyboard control suite |

### Sequential Execution (Single Developer)

1. Setup → Foundational → US1 → US2 → US3 → US4 → US5 → US6 → Polish

### Parallel Execution (Team)

After Foundational phase:
- Developer A: US1 (Type Text)
- Developer B: US2 + US3 (Press + Modifiers)
- Developer C: US4 (Hold/Release)

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 83 |
| Setup Tasks | 4 |
| Foundational Tasks | 16 |
| User Story 1 Tasks | 12 |
| User Story 2 Tasks | 11 |
| User Story 3 Tasks | 10 |
| User Story 4 Tasks | 11 |
| User Story 5 Tasks | 8 |
| User Story 6 Tasks | 5 |
| Polish Tasks | 6 |
| Parallel Opportunities | 47 tasks marked [P] |

### Independent Test Criteria per Story

| Story | Test Criteria |
|-------|---------------|
| US1 | Type "Hello World" into Notepad → verify text appears |
| US2 | Press Tab in form → verify focus moves |
| US3 | Select text + Ctrl+C + Ctrl+V → verify paste works |
| US4 | key_down Shift + key_up Shift → verify no stuck keys |
| US5 | Execute ["h","i","enter"] sequence → verify output |
| US6 | Query layout → verify languageTag and displayName returned |

### MVP Scope

**Recommended MVP**: User Story 1 (Type Text) only
- Tasks: T001-T032 (32 tasks)
- Delivers: LLMs can type any text into any field
- Immediate value for form filling, chat, document editing
