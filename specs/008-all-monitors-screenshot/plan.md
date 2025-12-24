# Implementation Plan: All Monitors Screenshot Capture

**Branch**: `008-all-monitors-screenshot` | **Date**: 2025-12-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-all-monitors-screenshot/spec.md`

## Summary

Add a new `all_monitors` capture target to the screenshot_control tool that captures the entire Windows virtual screen spanning all connected monitors in a single screenshot. This enables proper test verification for LLM-based integration tests running on multi-monitor setups.

## Technical Context

**Language/Version**: C# 12+ / .NET 8.0 LTS  
**Primary Dependencies**: Windows GDI+ (System.Drawing), existing ScreenshotService infrastructure  
**Storage**: N/A (returns base64-encoded PNG via MCP)  
**Testing**: xUnit 2.6+ integration tests on multi-monitor systems  
**Target Platform**: Windows 11 with multi-monitor support  
**Project Type**: Single project (existing MCP server)  
**Performance Goals**: < 500ms capture time on typical 2-monitor setup (SC-001)  
**Constraints**: MaxPixels limit applies to prevent memory issues with large virtual screens  
**Scale/Scope**: Adds 1 new enum value, ~20 lines of new code, reuses existing infrastructure

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ PASS | Integration tests will be written first |
| VI. Augmentation, Not Duplication | ✅ PASS | Raw screenshot data for LLM analysis |
| VII. Windows API Documentation-First | ✅ PASS | Uses documented GetSystemMetrics API |
| VIII. Security Best Practices | ✅ N/A | No new security surface |
| XIII. Modern .NET & C# Best Practices | ✅ PASS | Uses existing patterns |
| XIV. xUnit Testing Best Practices | ✅ PASS | Integration tests with secondary monitor |
| XVII. Coordinate Systems & DPI | ✅ PASS | Virtual screen handles all DPI scenarios |
| XXII. Open Source Dependencies | ✅ PASS | No new dependencies |

**All gates pass.** No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/008-all-monitors-screenshot/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/
│   └── requirements.md  # Specification checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/Sbroenne.WindowsMcp/
├── Models/
│   └── CaptureTarget.cs           # ADD: AllMonitors = 4
├── Capture/
│   └── ScreenshotService.cs       # ADD: CaptureAllMonitorsAsync method
├── Tools/
│   └── ScreenshotControlTool.cs   # UPDATE: Add "all_monitors" target parsing
└── Input/
    └── CoordinateNormalizer.cs    # EXISTING: GetVirtualScreenBounds() already exists

tests/Sbroenne.WindowsMcp.Tests/
└── Integration/
    └── ScreenshotAllMonitorsTests.cs  # NEW: Integration tests for all_monitors
```

**Structure Decision**: Minimal changes to existing structure. The `CoordinateNormalizer.GetVirtualScreenBounds()` method already exists and provides the virtual screen dimensions needed.

## Complexity Tracking

> **No violations requiring justification.** This feature adds minimal complexity by reusing existing infrastructure.

---

## Post-Design Constitution Check

*Re-evaluation after Phase 1 design completion*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| I. Test-First Development | ✅ PASS | Tests defined before implementation |
| VI. Augmentation, Not Duplication | ✅ PASS | Returns raw screenshot for LLM analysis |
| VII. Windows API Documentation-First | ✅ PASS | GetSystemMetrics is well-documented |
| XIII. Modern .NET & C# Best Practices | ✅ PASS | Follows existing async patterns |
| XIV. xUnit Testing Best Practices | ✅ PASS | Uses [Collection("WindowsDesktop")] |
| XVII. Coordinate Systems & DPI | ✅ PASS | Virtual screen handles negative coords |
| XXII. Open Source Dependencies | ✅ PASS | Zero new dependencies |

**All gates pass post-design. Proceed to Phase 2 (tasks.md).**

---

## Artifacts Generated

| Phase | Artifact | Path | Status |
|-------|----------|------|--------|
| 0 | Research | [research.md](research.md) | ✅ Complete |
| 1 | Data Model | [data-model.md](data-model.md) | ✅ Complete |
| 1 | Quickstart Guide | [quickstart.md](quickstart.md) | ✅ Complete |
| 1 | Agent Context | `.github/agents/copilot-instructions.md` | ✅ Complete |
| 2 | Tasks | [tasks.md](tasks.md) | ✅ Complete |
| 3 | Implementation | Source code | ✅ Complete |
