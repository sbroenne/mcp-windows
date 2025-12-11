# Implementation Plan: Mouse Position Awareness for LLM Usability

**Branch**: `012-mouse-position` | **Date**: December 11, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-mouse-position/spec.md`

## Summary

**Primary Requirement**: Make `monitorIndex` a required parameter when x/y coordinates are provided to the `mouse_control` tool, eliminating silent failures where LLMs click on the wrong monitor.

**Technical Approach**: 
- Modify `MouseControlTool.ExecuteAsync()` to enforce `monitorIndex` validation rules:
  - If x/y coordinates are provided → `monitorIndex` MUST be present, else return error with list of valid indices
  - If no coordinates → allow actions at current cursor position without `monitorIndex`
- Extend response payload (via `MouseControlResult`) to include `monitorIndex`, `monitorWidth`, `monitorHeight`, `error_code`, `error_details`
- Add `get_position` action (P3 priority) to return current cursor position and monitor context
- All success responses include monitor dimensions for LLM awareness and validation
- All error responses include machine-readable error_code and contextual error_details

**Phase 1 Deliverables**: ✅ COMPLETE
- ✅ [research.md](research.md) — Decision rationale, validation logic, implementation approach
- ✅ [data-model.md](data-model.md) — Request/response entities, validation rules, state transitions
- ✅ [quickstart.md](quickstart.md) — Developer walkthrough with code examples and test patterns
- ✅ [contracts/mouse-control.md](contracts/mouse-control.md) — JSON schemas and example payloads

## Technical Context

**Language/Version**: C# 12+, .NET 8.0 LTS
**Primary Dependencies**: MCP C# SDK, System.Windows.Automation (existing)
**Storage**: N/A (stateless tool)
**Testing**: xUnit 2.6+ with integration tests on Windows 11 desktop
**Target Platform**: Windows 11 (architecture-independent, .NET 8.0 runtime required)
**Project Type**: Single C# service (standalone + VS Code extension, shared core)
**Performance Goals**: Tool calls complete within 5 seconds (configurable timeout via `MCP_MOUSE_TIMEOUT_MS` / VS Code setting)
**Constraints**: 
- Coordinate validation must happen before any input is sent (fail-fast)
- Monitor index validation must list valid indices in error messages for LLM clarity
- Responses always include monitor context (index + dimensions) on success
**Scale/Scope**: 
- Modifies existing `MouseControlTool` in `src/Sbroenne.WindowsMcp/Tools/`
- Extends response model `MouseControlResult` in `src/Sbroenne.WindowsMcp/Models/`
- Adds input validation and monitor querying logic
- Impacts all mouse action handlers (move, click, double_click, right_click, middle_click, drag, scroll)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **All Constitution Principles Satisfied:**

| Principle | Check | Notes |
|-----------|-------|-------|
| I. Test-First Development | ✅ PASS | Integration tests on Windows 11 desktop required for all handlers; xUnit fixtures for multi-monitor setup |
| II. Latest Libraries Policy | ✅ PASS | Modifies existing tool; uses System.Automation (built-in), no new external dependencies |
| III. MCP Protocol Compliance | ✅ PASS | Extends `[Description]` attributes on parameters, uses structured output in response model, tool is semantic actuator |
| IV. Windows 11 Target Platform | ✅ PASS | Uses `System.Windows.Automation`, `GetSystemMetrics` for monitor detection; no COM/deprecated APIs |
| V. Dual Packaging Architecture | ✅ PASS | Changes to core `MouseControlTool` shared by both standalone and VS Code extension |
| VI. Augmentation, Not Duplication | ✅ PASS | Tool is a "dumb actuator"—validates and reports monitor context; LLM decides what to click |
| VII. Windows API Documentation-First | ✅ PASS | Uses `GetSystemMetrics` (SM_XVIRTUALSCREEN, etc.) documented in Microsoft Docs |
| VIII. Security Best Practices | ✅ PASS | Input validation on `monitorIndex`; inherits elevated process detection from 001 |
| IX. Resilient Error Handling | ✅ PASS | Returns actionable errors: missing `monitorIndex`, invalid index (lists valid ones), out-of-bounds coordinates |
| X. Thread-Safe Windows Interaction | ✅ PASS | Uses existing STA automation thread infrastructure; no new threading |
| XI. Observability & Diagnostics | ✅ PASS | Logs tool invocation with correlation ID and outcome (existing structured logging) |
| XII. Graceful Lifecycle Management | ✅ PASS | Stateless tool; no new lifecycle concerns |
| XIII. Modern .NET & C# Best Practices | ✅ PASS | Uses latest C# 12 patterns, nullable reference types, async/await, constructor injection |
| XIV. xUnit Testing Best Practices | ✅ PASS | Integration tests with multi-monitor fixtures; coordinate generation via `TestMonitorHelper` |
| XV. Input Simulation Best Practices | ✅ PASS | No new input simulation; modifies validation layer only |
| XVI. Timing & Synchronization | ✅ PASS | No new timing changes; uses existing 5-second tool timeout |
| XVII. Coordinate Systems & DPI Awareness | ✅ PASS | Enforces monitor-relative coordinates; leverages existing `CoordinateNormalizer` |
| XVIII. Elevated Process Handling | ✅ PASS | Inherits elevated process detection from 001; validates *before* sending input |
| XIX. Accessibility & Inclusive Design | ✅ PASS | No UI automation tree changes; respects existing accessibility handling |
| XX. Window Activation & Focus Management | ✅ PASS | No changes to focus; tool operates at cursor position |
| XXI. Modern .NET CLI Application Architecture | ✅ PASS | No CLI changes; modifies tool handler logic only |
| XXII. Open Source Dependencies Only | ✅ PASS | No new dependencies; uses only System.* and MCP SDK |

**Gate Status**: ✅ **PASS** — No principle violations; feature is safe to proceed.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Sbroenne.WindowsMcp/
│   ├── Tools/
│   │   └── MouseControlTool.cs          # [MODIFIED] Enforce monitorIndex validation
│   ├── Models/
│   │   ├── MouseControlResult.cs        # [MODIFIED] Add monitorIndex, monitorWidth, monitorHeight
│   │   ├── MouseControlErrorCode.cs     # [UNCHANGED] Inherits from 001
│   │   └── [other existing models]
│   ├── Input/
│   │   └── MouseInputService.cs         # [UNCHANGED] Coordinate handling still works
│   └── [other existing services]

tests/
├── Sbroenne.WindowsMcp.Tests/
│   ├── Tools/
│   │   └── MouseControlToolTests.cs     # [MODIFIED] Add monitorIndex validation tests
│   ├── Fixtures/
│   │   └── MultiMonitorFixture.cs       # [NEW] Provides test monitor setup
│   └── [other existing tests]
```

**Structure Decision**: Single project modification. The existing `MouseControlTool` and `MouseInputService` handle all coordinate and monitor logic; this feature adds validation rules and response enrichment without restructuring.

## Complexity Tracking

No Constitution violations; no complexity justifications needed.
