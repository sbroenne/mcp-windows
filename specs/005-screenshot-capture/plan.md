# Implementation Plan: Screenshot Capture

**Branch**: `005-screenshot-capture` | **Date**: 2025-12-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-screenshot-capture/spec.md`

## Summary

Implement a `screenshot_control` MCP tool enabling LLMs to capture screenshots on Windows 11. Supports full screen capture, specific monitor capture, window capture (even when obscured), and region capture. Returns base64-encoded PNG images with metadata. Uses `Graphics.CopyFromScreen` for screen/region capture and `PrintWindow` API for obscured window capture. Optional cursor rendering via `GetCursorInfo` + `DrawIcon`.

## Technical Context

| Aspect | Decision | Notes |
|--------|----------|-------|
| **Language/Version** | C# 12+ | Per Constitution XIII |
| **Primary Dependencies** | System.Drawing.Common (NuGet), MCP SDK, Polly, Serilog | All OSS |
| **Storage** | N/A | In-memory bitmap → base64 (no file I/O) |
| **Testing** | xUnit 2.6+ with native assertions | Integration tests primary |
| **Target Platform** | Windows 11 only | Per Constitution IV |
| **Project Type** | Single project (existing MCP server) | Adding to existing structure |
| **Performance Goals** | Full screen capture <500ms | Per SC-001 |
| **Constraints** | 5s timeout, memory limits for 4K+ displays | Per SC-006, FR-012 |
| **Scale/Scope** | Single-frame capture, max ~8K resolution | No video streaming |

### Key Windows APIs

| API | Purpose | Documentation |
|-----|---------|---------------|
| `Graphics.CopyFromScreen` | Screen/region capture via GDI+ | [MS Docs](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen) |
| `PrintWindow` | Capture obscured windows | [MS Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-printwindow) |
| `Screen.AllScreens` | Monitor enumeration | [MS Docs](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.screen.allscreens) |
| `GetCursorInfo` + `DrawIcon` | Cursor capture (optional) | [MS Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorinfo) |
| `IsWindow` / `IsWindowVisible` | Window validation | [MS Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindow) |
| `GetWindowRect` | Window bounds | [MS Docs](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect) |
| `DwmGetWindowAttribute` | True window bounds (with shadows) | [MS Docs](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmgetwindowattribute) |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | PASS | Integration tests for actual captures; unit tests for coordinate/bounds validation |
| II. Latest Libraries Policy | PASS | Will use latest stable System.Drawing.Common |
| III. MCP Protocol Compliance | PASS | Single `screenshot_control` tool with action parameter |
| IV. Windows 11 Target | PASS | Windows 11 only, uses Screen class and DWM APIs |
| V. Dual Packaging | N/A | Core logic shared; transport isolated |
| VI. Augmentation Not Duplication | PASS | Returns raw image data for LLM vision analysis |
| VII. Windows API Docs First | PASS | All APIs researched with MS Docs citations |
| VIII. Security Best Practices | PASS | Input validation for coordinates, size limits |
| IX. Resilient Error Handling | PASS | Meaningful errors for secure desktop, minimized windows |
| X. Thread-Safe Interaction | PASS | No STA required for GDI+ capture; mutex for serialization |
| XI. Observability | PASS | Structured logging (excluding image data per Constitution XI) |
| XII. Graceful Lifecycle | PASS | Proper bitmap disposal, IDisposable pattern |
| XIII. Modern .NET & C# | PASS | Records, nullable enabled, async pattern |
| XIV. xUnit Testing | PASS | xUnit 2.6+ native assertions, IAsyncLifetime |
| XV. Input Simulation | N/A | Read-only operation, no input simulation |
| XVI. Timing & Synchronization | PASS | 5s timeout, no fixed delays |
| XVII. Coordinate Systems & DPI | PASS | Per-Monitor V2 awareness, actual pixel coordinates |
| XVIII. Elevated Process Handling | PASS | PrintWindow may fail on elevated windows; detect and report |
| XIX. Accessibility | N/A | No UI tree walking |
| XX. Window Activation | N/A | Does not change focus |
| XXI. Modern CLI Architecture | PASS | Integrates with existing Host builder |
| XXII. Open Source Dependencies | PASS | System.Drawing.Common is MIT licensed |

### Gate Evaluation

- [x] All principles PASS or N/A
- [x] No violations requiring justification
- [x] Risk: PrintWindow may fail on some windows (mitigation: fallback to screen region capture)

## Project Structure

### Documentation (this feature)

```text
specs/005-screenshot-capture/
├── plan.md              # This file
├── research.md          # Phase 0 output: API research
├── data-model.md        # Phase 1 output: Entity definitions
├── quickstart.md        # Phase 1 output: Developer guide
├── contracts/           # Phase 1 output: MCP tool schema
│   └── screenshot_control.json
└── tasks.md             # Phase 2 output: Task breakdown
```

### Source Code (additions to existing project)

```text
src/Sbroenne.WindowsMcp/
├── Capture/                          # NEW: Screenshot services
│   ├── IScreenshotService.cs         # Service interface
│   ├── ScreenshotService.cs          # Main capture logic
│   ├── IMonitorService.cs            # Monitor enumeration interface
│   └── MonitorService.cs             # Monitor enumeration
├── Configuration/
│   └── ScreenshotConfiguration.cs    # NEW: Timeout, size limits
├── Logging/
│   └── ScreenshotOperationLogger.cs  # NEW: Structured logging
├── Models/
│   ├── ScreenshotAction.cs           # NEW: Action enum
│   ├── CaptureTarget.cs              # NEW: Target type enum
│   ├── ScreenshotErrorCode.cs        # NEW: Error codes
│   ├── ScreenshotControlRequest.cs   # NEW: Request model
│   ├── ScreenshotControlResult.cs    # NEW: Result model
│   ├── MonitorInfo.cs                # NEW: Monitor metadata
│   └── CaptureRegion.cs              # NEW: Region coordinates
├── Native/
│   └── NativeMethods.cs              # EXTEND: Add PrintWindow, GetCursorInfo
└── Tools/
    └── ScreenshotControlTool.cs      # NEW: MCP tool

tests/Sbroenne.WindowsMcp.Tests/
├── Integration/
│   ├── ScreenshotFullScreenTests.cs      # NEW: US1 tests
│   ├── ScreenshotMonitorTests.cs         # NEW: US2 tests
│   ├── ScreenshotWindowTests.cs          # NEW: US3 tests
│   ├── ScreenshotRegionTests.cs          # NEW: US4 tests
│   ├── ScreenshotMonitorListTests.cs     # NEW: US5 tests
│   └── ScreenshotCursorTests.cs          # NEW: Cursor capture tests
└── Unit/
    ├── CaptureRegionValidationTests.cs   # NEW: Bounds validation
    └── ScreenshotConfigurationTests.cs   # NEW: Config tests
```

**Structure Decision**: Follows existing project patterns with dedicated `Capture/` folder for screenshot services, consistent with `Input/`, `Window/`, and `Clipboard/` folders.

## Complexity Tracking

| Metric | Count |
|--------|-------|
| New entities | 8 (ScreenshotAction, CaptureTarget, ScreenshotErrorCode, ScreenshotControlRequest, ScreenshotControlResult, MonitorInfo, CaptureRegion, ScreenshotConfiguration) |
| MCP tools | 1 (screenshot_control with 6 actions) |
| External integrations | 0 |
| Windows APIs | 8+ (CopyFromScreen, PrintWindow, Screen.AllScreens, GetCursorInfo, DrawIcon, IsWindow, GetWindowRect, DwmGetWindowAttribute) |
| NuGet additions | 1 (System.Drawing.Common) |
| Estimated story points | 8 |

---

## Phase 0: Outline & Research ✅

### Prerequisites
- [x] Spec complete and approved
- [x] Constitution reviewed (v2.1.0)

### Research Tasks

| ID | Topic | Status | Output |
|----|-------|--------|--------|
| R1 | Graphics.CopyFromScreen for screen capture | ✅ Complete | → research.md §1 |
| R2 | PrintWindow API for obscured window capture | ✅ Complete | → research.md §2 |
| R3 | Screen.AllScreens for monitor enumeration | ✅ Complete | → research.md §3 |
| R4 | Cursor capture via GetCursorInfo + DrawIcon | ✅ Complete | → research.md §4 |
| R5 | PNG encoding via System.Drawing.Imaging | ✅ Complete | → research.md §5 |
| R6 | Base64 encoding performance considerations | ✅ Complete | → research.md §6 |
| R7 | DPI awareness and actual vs logical coordinates | ✅ Complete | → research.md §7 |
| R8 | Secure desktop detection (reuse existing) | ✅ Complete | → research.md §8 |
| R9 | Memory management for large bitmaps | ✅ Complete | → research.md §9 |
| R10 | Window validation (IsWindow, IsIconic, IsWindowVisible) | ✅ Complete | → research.md §10 |

### Deliverables
- [x] `research.md` — All API patterns documented with MS Docs citations

---

## Phase 1: Design & Contracts ✅

### Prerequisites
- [x] `research.md` complete

### Design Tasks

| ID | Artifact | Depends On | Status |
|----|----------|------------|--------|
| D1 | Entity extraction → data-model.md | R1-R10 | ✅ Complete |
| D2 | MCP tool schema → contracts/screenshot_control.json | D1 | ✅ Complete |
| D3 | Developer quickstart → quickstart.md | D1, D2 | ✅ Complete |
| D4 | Update copilot-instructions.md with screenshot context | D1 | ✅ Complete |

### Deliverables
- [x] `data-model.md` — Entity definitions, validation rules
- [x] `contracts/screenshot_control.json` — MCP tool JSON schema
- [x] `quickstart.md` — Usage examples for each action

---

## Phase 2: Task Generation ✅

### Prerequisites
- [x] Phase 1 complete
- [x] All designs validated

### Task Breakdown

| Phase | Purpose | Tasks |
|-------|---------|-------|
| Setup | Add System.Drawing.Common, create folders | 2 |
| Foundational | Models, interfaces, configuration | 17 |
| US5: List Monitors | Monitor enumeration (needed by US1/US2) | 3 |
| US1: Full Screen | Capture primary monitor (MVP) | 9 |
| US2: Specific Monitor | Capture by index | 6 |
| US3: Window Capture | PrintWindow integration | 8 |
| US4: Region Capture | Coordinate-based capture | 6 |
| Cursor Capture | Optional cursor rendering | 6 |
| Polish | Documentation, final tests | 6 |

**Total**: 63 tasks

### Deliverables
- [x] `tasks.md` — Full task breakdown with parallel opportunities

---

## Constitution Check (Post-Design) ✅

*Completed after Phase 1 design. All principles validated.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ PASS | data-model.md defines validation rules testable via unit tests |
| II. Latest Libraries Policy | ✅ PASS | System.Drawing.Common latest stable per research |
| III. MCP Protocol Compliance | ✅ PASS | Contract defined with proper snake_case, examples |
| VII. Windows API Docs First | ✅ PASS | All 10 research items complete with MS Docs citations |
| XIII. Modern .NET & C# | ✅ PASS | Records defined for immutable models |
| XXII. Open Source Dependencies | ✅ PASS | System.Drawing.Common MIT license confirmed |

---

## Next Steps

1. ~~Execute Phase 0 research tasks → `research.md`~~ ✅ Complete
2. ~~Complete Phase 1 design artifacts → `data-model.md`, `contracts/`, `quickstart.md`~~ ✅ Complete
3. ~~Generate Phase 2 task breakdown → `tasks.md`~~ ✅ Complete
4. Begin implementation (start with Phase 1: Setup)
