# Tasks: Screenshot Capture

**Input**: Design documents from `/specs/005-screenshot-capture/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup ✅

**Purpose**: Add dependencies and create folder structure

- [X] T001 Add `System.Drawing.Common` NuGet package to src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj
- [X] T002 [P] Create Capture/ folder structure in src/Sbroenne.WindowsMcp/Capture/

---

## Phase 2: Foundational (Blocking Prerequisites) ✅

**Purpose**: Core models, interfaces, and configuration that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Models

- [X] T003 [P] Create ScreenshotAction enum in src/Sbroenne.WindowsMcp/Models/ScreenshotAction.cs
- [X] T004 [P] Create CaptureTarget enum in src/Sbroenne.WindowsMcp/Models/CaptureTarget.cs
- [X] T005 [P] Create ScreenshotErrorCode enum in src/Sbroenne.WindowsMcp/Models/ScreenshotErrorCode.cs
- [X] T006 [P] Create MonitorInfo record in src/Sbroenne.WindowsMcp/Models/MonitorInfo.cs
- [X] T007 [P] Create CaptureRegion record in src/Sbroenne.WindowsMcp/Models/CaptureRegion.cs
- [X] T008 [P] Create ScreenshotControlRequest record in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T009 [P] Create ScreenshotControlResult record in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs

### Configuration

- [X] T010 Create ScreenshotConfiguration class in src/Sbroenne.WindowsMcp/Configuration/ScreenshotConfiguration.cs

### Native Methods

- [X] T011 Add PrintWindow P/Invoke to src/Sbroenne.WindowsMcp/Native/NativeMethods.cs
- [X] T012 Add GetCursorInfo and DrawIcon P/Invoke to src/Sbroenne.WindowsMcp/Native/NativeMethods.cs
- [X] T013 Add IsWindow, IsIconic, IsWindowVisible P/Invoke to src/Sbroenne.WindowsMcp/Native/NativeMethods.cs
- [X] T014 Add CURSORINFO struct to src/Sbroenne.WindowsMcp/Native/NativeStructs.cs

### Service Interfaces

- [X] T015 [P] Create IMonitorService interface in src/Sbroenne.WindowsMcp/Capture/IMonitorService.cs
- [X] T016 [P] Create IScreenshotService interface in src/Sbroenne.WindowsMcp/Capture/IScreenshotService.cs

### Logging

- [X] T017 Create ScreenshotOperationLogger in src/Sbroenne.WindowsMcp/Logging/ScreenshotOperationLogger.cs

### Unit Tests for Configuration

- [X] T018 Create ScreenshotConfigurationTests in tests/Sbroenne.WindowsMcp.Tests/Unit/ScreenshotConfigurationTests.cs
- [X] T019 Create CaptureRegionValidationTests in tests/Sbroenne.WindowsMcp.Tests/Unit/CaptureRegionValidationTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin ✅

---

## Phase 3: User Story 5 - List Monitors (Priority: P3 but needed first) 🔧 ✅

**Goal**: Enumerate available monitors with metadata (needed by US1, US2)

**Independent Test**: Invoke list_monitors action, verify at least one monitor with resolution

**Note**: Implemented first because US1 and US2 depend on monitor enumeration

### Implementation

- [X] T020 [US5] Implement MonitorService in src/Sbroenne.WindowsMcp/Capture/MonitorService.cs
- [X] T021 [US5] Register MonitorService in DI container in src/Sbroenne.WindowsMcp/Program.cs

### Integration Tests

- [X] T022 [US5] Create ScreenshotMonitorListTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotMonitorListTests.cs

**Checkpoint**: Monitor enumeration functional; can now proceed with capture stories ✅

---

## Phase 4: User Story 1 - Capture Entire Screen (Priority: P1) 🎯 MVP ✅

**Goal**: Capture the primary monitor as base64-encoded PNG

**Independent Test**: Invoke screenshot capture with no parameters, receive valid PNG image matching screen resolution

### Core Capture Implementation

- [X] T023 [US1] Implement ScreenshotService with primary screen capture in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T024 [US1] Add PNG encoding to base64 in ScreenshotService
- [X] T025 [US1] Add secure desktop detection to ScreenshotService (reuse ISecureDesktopDetector)
- [X] T026 [US1] Add size limit validation to ScreenshotService

### MCP Tool

- [X] T027 [US1] Create ScreenshotControlTool with capture action in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T028 [US1] Register ScreenshotService and ScreenshotControlTool in DI container in src/Sbroenne.WindowsMcp/Program.cs

### Integration Tests

- [X] T029 [US1] Create ScreenshotFullScreenTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotFullScreenTests.cs
- [X] T030 [US1] Test primary screen capture returns valid PNG dimensions
- [X] T031 [US1] Test response includes width, height, format metadata

**Checkpoint**: MVP complete - full screen capture functional ✅

---

## Phase 5: User Story 2 - Capture Specific Monitor (Priority: P2) ✅

**Goal**: Capture a specific monitor by index (0-based)

**Independent Test**: On multi-monitor system, capture monitor index 1, verify dimensions match secondary display

### Implementation

- [X] T032 [US2] Add monitor capture by index to ScreenshotService
- [X] T033 [US2] Add monitor index validation with helpful error (include available monitors list)
- [X] T034 [US2] Add monitor target handling to ScreenshotControlTool

### Integration Tests

- [X] T035 [US2] Create ScreenshotMonitorTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotMonitorTests.cs
- [X] T036 [US2] Test capture primary monitor by index 0
- [X] T037 [US2] Test invalid monitor index returns error with available monitors

**Checkpoint**: Monitor-specific capture functional ✅

---

## Phase 6: User Story 3 - Capture Specific Window (Priority: P2) ✅

**Goal**: Capture a specific window by handle, even when partially obscured

**Independent Test**: Open Notepad, get window handle, capture window, verify dimensions match window size

### Implementation

- [X] T038 [US3] Add window validation methods (IsWindow, IsIconic) to ScreenshotService
- [X] T039 [US3] Implement PrintWindow capture in ScreenshotService
- [X] T040 [US3] Add fallback to screen region capture if PrintWindow fails
- [X] T041 [US3] Add window target handling to ScreenshotControlTool

### Integration Tests

- [X] T042 [US3] Create ScreenshotWindowTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotWindowTests.cs
- [X] T043 [US3] Test capture valid window returns correct dimensions
- [X] T044 [US3] Test invalid window handle returns InvalidWindowHandle error
- [X] T045 [US3] Test minimized window returns WindowMinimized error

**Checkpoint**: Window capture functional (obscured and visible) ✅

---

## Phase 7: User Story 4 - Capture Screen Region (Priority: P3) ✅

**Goal**: Capture a rectangular region by coordinates

**Independent Test**: Capture region (100, 100, 400, 300), verify image is 400x300 pixels

### Implementation

- [X] T046 [US4] Add region validation (positive dimensions, within limits) to ScreenshotService
- [X] T047 [US4] Implement region capture using CopyFromScreen in ScreenshotService
- [X] T048 [US4] Add region target handling to ScreenshotControlTool

### Integration Tests

- [X] T049 [US4] Create ScreenshotRegionTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotRegionTests.cs
- [X] T050 [US4] Test capture region returns exact dimensions
- [X] T051 [US4] Test invalid region (zero/negative dimensions) returns InvalidRegion error

**Checkpoint**: Region capture functional ✅

---

## Phase 8: Cursor Capture (Cross-cutting Feature) ✅

**Goal**: Optional cursor rendering in captured images (default: off)

**Independent Test**: Capture with include_cursor=true, verify cursor appears in image at correct position

### Implementation

- [X] T052 Implement cursor capture using GetCursorInfo + DrawIcon in ScreenshotService
- [X] T053 Add include_cursor parameter handling to all capture methods
- [X] T054 Add cursor offset calculation for region/window captures

### Integration Tests

- [X] T055 Create ScreenshotCursorTests in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotCursorTests.cs
- [X] T056 Test capture without cursor (default) has no cursor
- [X] T057 Test capture with include_cursor=true includes cursor

**Checkpoint**: Cursor capture functional for all capture types ✅

---

## Phase 9: Polish & Cross-Cutting Concerns ✅

**Purpose**: Error handling refinement, documentation, final validation

- [X] T058 [P] Add structured logging throughout ScreenshotService (no image data logging per Constitution XI)
- [X] T059 [P] Add timeout handling with CancellationToken to capture operations
- [X] T060 Validate error messages match contract error_code enum values
- [X] T061 [P] Run all integration tests and verify 100% pass
- [X] T062 Run quickstart.md validation scenarios manually
- [X] T063 Update README.md with screenshot_control tool documentation

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US5: List Monitors)
                                                     ↓
                              ┌──────────────────────┼──────────────────────┐
                              ↓                      ↓                      ↓
                    Phase 4 (US1: Full Screen)  Phase 5 (US2: Monitor)  Phase 6 (US3: Window)
                              ↓                      ↓                      ↓
                              └──────────────────────┼──────────────────────┘
                                                     ↓
                                         Phase 7 (US4: Region)
                                                     ↓
                                         Phase 8 (Cursor Capture)
                                                     ↓
                                         Phase 9 (Polish)
```

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US5 (List Monitors) | Foundational | - |
| US1 (Full Screen) | US5 | US2, US3 (after core capture) |
| US2 (Monitor) | US1 (core capture) | US3 |
| US3 (Window) | US1 (core capture) | US2 |
| US4 (Region) | US1 (core capture) | - |
| Cursor | US1, US2, US3, US4 | - |

### Parallel Opportunities

**Phase 2 - All models in parallel:**
```
T003, T004, T005, T006, T007, T008, T009 (all [P])
T015, T016 (interfaces [P])
```

**After US1 core capture complete:**
```
US2 (T032-T037) can run parallel with US3 (T038-T045)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US5 (List Monitors)
4. Complete Phase 4: US1 (Full Screen Capture)
5. **STOP and VALIDATE**: Test full screen capture independently
6. Deploy/demo MVP

### Incremental Delivery

1. Setup + Foundational + US5 → Foundation ready
2. Add US1 (Full Screen) → MVP!
3. Add US2 (Monitor) + US3 (Window) in parallel
4. Add US4 (Region)
5. Add Cursor Capture
6. Polish phase

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 63 |
| Phase 1 (Setup) | 2 |
| Phase 2 (Foundational) | 17 |
| Phase 3 (US5 List Monitors) | 3 |
| Phase 4 (US1 Full Screen) | 9 |
| Phase 5 (US2 Monitor) | 6 |
| Phase 6 (US3 Window) | 8 |
| Phase 7 (US4 Region) | 6 |
| Phase 8 (Cursor) | 6 |
| Phase 9 (Polish) | 6 |
| Parallel Opportunities | 12 tasks marked [P] |

---

## Notes

- Tests are included per Constitution Principle I (Test-First Development)
- US5 (List Monitors) implemented before US1/US2 because they depend on monitor enumeration
- Cursor capture is cross-cutting and applies to all capture types
- All native P/Invoke methods are added in Foundational phase for reuse
- Memory limits and secure desktop detection are core infrastructure (Phase 4)
