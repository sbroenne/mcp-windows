# Tasks: Screenshot LLM Optimization

**Input**: Design documents from `/specs/011-screenshot-llm-optimization/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Create new files and prepare project structure

- [X] T001 [P] Create ImageFormat enum in src/Sbroenne.WindowsMcp/Models/ImageFormat.cs
- [X] T002 [P] Create OutputMode enum in src/Sbroenne.WindowsMcp/Models/OutputMode.cs
- [X] T003 [P] Create ProcessedImage record in src/Sbroenne.WindowsMcp/Capture/ProcessedImage.cs
- [X] T004 [P] Create IImageProcessor interface in src/Sbroenne.WindowsMcp/Capture/IImageProcessor.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core ImageProcessor service that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Implement ImageProcessor.CalculateScaledDimensions() in src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs
- [X] T006 Implement ImageProcessor.ScaleBitmap() with HighQualityBicubic in src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs
- [X] T007 Implement ImageProcessor.EncodeToJpeg() with quality parameter in src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs
- [X] T008 Implement ImageProcessor.EncodeToPng() in src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs
- [X] T009 Implement ImageProcessor.Process() orchestration method in src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs
- [X] T010 Add default constants to src/Sbroenne.WindowsMcp/Configuration/ScreenshotConfiguration.cs (DefaultImageFormat, DefaultQuality, DefaultMaxWidth)

**Checkpoint**: ImageProcessor ready - user story implementation can begin

---

## Phase 3: User Story 1 - LLM-Optimized Default Format (Priority: P1) 🎯 MVP

**Goal**: Screenshots default to JPEG format (quality 85) instead of PNG

**Independent Test**: Request capture with no parameters → verify JPEG format returned, file size under 500KB

### Implementation for User Story 1

- [X] T011 [US1] Add ImageFormat property to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T012 [US1] Add Quality property (1-100, default 85) to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T013 [US1] Add imageFormat parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T014 [US1] Add quality parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T015 [US1] Update ScreenshotControlResult.Format to use dynamic format in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs
- [X] T016 [US1] Add FileSizeBytes property to ScreenshotControlResult in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs
- [X] T017 [US1] Integrate ImageProcessor encoding into ScreenshotService.CaptureScreenAsync() in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T018 [US1] Integrate ImageProcessor encoding into ScreenshotService.CaptureWindowAsync() in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T019 [US1] Integrate ImageProcessor encoding into ScreenshotService.CaptureRegionAsync() in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T020 [US1] Add input validation for Quality (1-100) and ImageFormat in ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T021 [US1] Add integration test for default JPEG capture in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T022 [US1] Add integration test for explicit PNG capture in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T023 [US1] Add integration test for custom quality parameter in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T023a [US1] Add integration test for invalid imageFormat error response in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T023b [US1] Add integration test verifying quality parameter ignored for PNG format in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs

**Checkpoint**: Default JPEG capture works - MVP complete

---

## Phase 4: User Story 2 - Auto-Scaling for LLM Vision Models (Priority: P1)

**Goal**: Screenshots auto-scale to 1568px width by default

**Independent Test**: On 4K display, request capture → verify width is 1568px, original dimensions in metadata

### Implementation for User Story 2

- [X] T024 [US2] Add MaxWidth property (default 1568) to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T025 [US2] Add MaxHeight property (default 0) to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T026 [US2] Add maxWidth parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T027 [US2] Add maxHeight parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T028 [US2] Add OriginalWidth property to ScreenshotControlResult in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs
- [X] T029 [US2] Add OriginalHeight property to ScreenshotControlResult in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs
- [X] T030 [US2] Integrate ImageProcessor scaling into ScreenshotService capture methods in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T031 [US2] Add input validation for MaxWidth/MaxHeight (non-negative) in ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T032 [US2] Add integration test for default auto-scaling in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T033 [US2] Add integration test for maxWidth: 0 (disabled) in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T034 [US2] Add integration test for both maxWidth and maxHeight constraints in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T035 [US2] Add integration test verifying no upscaling occurs in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs

**Checkpoint**: Auto-scaling works - LLM optimization defaults complete

---

## Phase 5: User Story 3 - File Output Mode (Priority: P2)

**Goal**: Support saving screenshots to file instead of inline base64

**Independent Test**: Request capture with outputMode: "file" → verify file path returned, file exists on disk

### Implementation for User Story 3

- [X] T036 [US3] Add OutputMode property to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T037 [US3] Add OutputPath property to ScreenshotControlRequest in src/Sbroenne.WindowsMcp/Models/ScreenshotControlRequest.cs
- [X] T038 [US3] Add outputMode parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T039 [US3] Add outputPath parameter to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T040 [US3] Add FilePath property to ScreenshotControlResult in src/Sbroenne.WindowsMcp/Models/ScreenshotControlResult.cs
- [X] T041 [US3] Implement GenerateTempFilePath() helper in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T042 [US3] Implement file output logic in ScreenshotService capture methods in src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs
- [X] T043 [US3] Add input validation for OutputPath (directory exists, writable) in ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T044 [US3] Add integration test for file output to temp directory in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T045 [US3] Add integration test for file output with custom outputPath in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T046 [US3] Add integration test for invalid outputPath error handling in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs

**Checkpoint**: File output mode works - all output options complete

---

## Phase 6: User Story 4 - Combined Optimizations (Priority: P2)

**Goal**: Verify all optimizations work together seamlessly

**Independent Test**: Default capture on 4K display → verify JPEG format, 1568px width, quality 85, under 300KB

### Implementation for User Story 4

- [X] T047 [US4] Add integration test for combined defaults (JPEG + scaling + inline) in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T048 [US4] Add integration test for combined file output (JPEG + scaling + file) in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T049 [US4] Add integration test for backward compatibility (PNG + no scaling) in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs
- [X] T050 [US4] Add integration test for all capture targets (screen, window, region) with optimizations in tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotLlmOptimizationTests.cs

**Checkpoint**: All optimizations verified working together

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and cleanup

- [X] T051 [P] Update XML documentation on all new parameters in ScreenshotControlTool for MCP auto-description
- [X] T052 [P] Update README.md with LLM optimization parameter documentation
- [X] T053 Run quickstart.md validation steps to verify implementation
- [X] T054 Run full test suite and verify all existing tests still pass

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)         → No dependencies - create new files
       ↓
Phase 2 (Foundational)  → Depends on Phase 1 - BLOCKS all user stories
       ↓
Phase 3 (US1) ─┬─────── → Can start after Phase 2
Phase 4 (US2) ─┤        → Can start after Phase 2 (parallel with US1)
               ↓
Phase 5 (US3) ─────────→ Can start after Phase 2 (parallel with US1/US2)
               ↓
Phase 6 (US4) ─────────→ Depends on US1, US2, US3 completion
       ↓
Phase 7 (Polish)        → After all stories complete
```

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|-------|------------|---------------------|
| US1 (JPEG format) | Phase 2 | US2, US3 |
| US2 (Auto-scaling) | Phase 2 | US1, US3 |
| US3 (File output) | Phase 2 | US1, US2 |
| US4 (Combined) | US1, US2, US3 | None |

### Within Each Phase

- Tasks marked [P] can run in parallel
- Sequential tasks depend on previous task completion within same story
- All T001-T004 (Setup) can run in parallel
- T005-T009 (Foundational) must be sequential (each builds on previous)

---

## Parallel Execution Examples

### Phase 1: All Setup Tasks
```bash
# Launch all simultaneously:
T001: Create ImageFormat enum
T002: Create OutputMode enum
T003: Create ProcessedImage record
T004: Create IImageProcessor interface
```

### Phase 3-5: User Stories 1, 2, 3
```bash
# Once Phase 2 complete, launch US1, US2, US3 in parallel:
Developer A: T011-T023 (US1 - JPEG format)
Developer B: T024-T035 (US2 - Auto-scaling)
Developer C: T036-T046 (US3 - File output)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T010)
3. Complete Phase 3: US1 - JPEG Format (T011-T023)
4. Complete Phase 4: US2 - Auto-Scaling (T024-T035)
5. **STOP and VALIDATE**: Test default capture returns JPEG at 1568px
6. Deploy/demo if ready

### Full Implementation

7. Complete Phase 5: US3 - File Output (T036-T046)
8. Complete Phase 6: US4 - Combined Tests (T047-T050)
9. Complete Phase 7: Polish (T051-T054)

---

## Summary

| Phase | Tasks | Parallel Opportunities |
|-------|-------|----------------------|
| Setup | T001-T004 (4) | All 4 parallel |
| Foundational | T005-T010 (6) | Sequential |
| US1 (P1) | T011-T023 (13) | T011-T012 parallel, T013-T014 parallel |
| US2 (P1) | T024-T035 (12) | T024-T025 parallel, T026-T027 parallel |
| US3 (P2) | T036-T046 (11) | T036-T037 parallel, T038-T039 parallel |
| US4 (P2) | T047-T050 (4) | All 4 parallel |
| Polish | T051-T054 (4) | T051-T052 parallel |

**Total Tasks**: 56
**Per User Story**: US1=15, US2=12, US3=11, US4=4
**Parallel Opportunities**: Setup phase, US1-US3 phases can run concurrently
**Independent Test Criteria**: Each user story has specific verification test
**MVP Scope**: Phase 1 + 2 + US1 + US2 (32 tasks)
