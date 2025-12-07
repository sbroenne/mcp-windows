# Implementation Plan: Keyboard Control

**Branch**: `002-keyboard-control` | **Date**: 2025-12-07 | **Spec**: [spec.md](spec.md)
**Status**: Phase 1 Complete - Ready for Task Generation

---

## Phase Completion Status

| Phase | Status | Output |
|-------|--------|--------|
| Phase 0: Research | ✅ Complete | [research.md](research.md) |
| Phase 1: Design | ✅ Complete | [data-model.md](data-model.md), [quickstart.md](quickstart.md), [contracts/](contracts/) |
| Phase 2: Tasks | ✅ Complete | [tasks.md](tasks.md) |

## Summary

Implement comprehensive keyboard input simulation for Windows MCP, enabling LLMs to type text, press keys, perform keyboard shortcuts, and manage key state. The implementation uses the Windows `SendInput` API with Unicode input support for layout-independent text typing, following the same architectural patterns established by the mouse control feature.

## Technical Context

**Language/Version**: C# 12+ (.NET 8.0 LTS)  
**Primary Dependencies**: Microsoft.Extensions.Logging, Serilog, MCP SDK, System.CommandLine  
**Storage**: N/A (stateless operations, held keys tracked in memory only)  
**Testing**: xUnit 2.6+ with native assertions, NSubstitute for mocking  
**Target Platform**: Windows 11 only (Per-Monitor V2 DPI aware)  
**Project Type**: Single project with MCP tools  
**Performance Goals**: Type 1,000 characters in under 2 seconds with no dropped characters  
**Constraints**: 5 second default timeout, serialize with mouse operations via shared mutex  
**Scale/Scope**: Single MCP tool with 7 actions (type, press, key_down, key_up, sequence, release_all, get_keyboard_layout)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ PASS | Integration tests will be primary; unit tests for pure logic (key name parsing, virtual key mapping) |
| II. Latest Libraries Policy | ✅ PASS | Will use latest stable NuGet packages |
| III. MCP Protocol Compliance | ✅ PASS | KeyboardControlTool will use `[McpServerToolType]` and `[McpServerTool]` attributes |
| IV. Windows 11 Target | ✅ PASS | Uses SendInput API, no legacy compatibility |
| V. Dual Packaging | ✅ PASS | Core logic in shared services, transport isolated |
| VI. Augmentation, Not Duplication | ✅ PASS | Tool sends input only; no decision-making logic |
| VII. Windows API Documentation-First | ✅ PASS | Research phase will cite Microsoft Docs for SendInput, KEYEVENTF_UNICODE |
| VIII. Security Best Practices | ✅ PASS | Input validation on all parameters, elevated process detection |
| IX. Resilient Error Handling | ✅ PASS | Meaningful errors with context, modifier cleanup in finally blocks |
| X. Thread-Safe Windows Interaction | ✅ PASS | Shared mutex with mouse control for serialization |
| XI. Observability | ✅ PASS | Structured JSON logging to stderr with correlation IDs |
| XII. Graceful Lifecycle | ✅ PASS | release_all action for cleanup, held key tracking |
| XIII. Modern .NET Best Practices | ✅ PASS | Records, nullable, async, DI |
| XIV. xUnit Testing Best Practices | ✅ PASS | Native xUnit assertions, [Collection] for desktop tests |
| XV. Input Simulation Best Practices | ✅ PASS | SendInput only, modifier tracking, cleanup on failure |
| XVI. Timing & Synchronization | ✅ PASS | Configurable delays, no fixed sleeps |
| XVII. Coordinate Systems & DPI | N/A | Keyboard input not coordinate-based |
| XVIII. Elevated Process Handling | ✅ PASS | Detect elevated foreground window, return clear error |
| XIX. Accessibility | ✅ PASS | Respect system settings where applicable |
| XX. Window Activation | N/A | Keyboard control sends to foreground window |
| XXI. Modern .NET CLI Architecture | ✅ PASS | DI, IConfiguration, ILogger<T> |
| XXII. Open Source Dependencies Only | ✅ PASS | All dependencies are OSS (MIT/Apache) |

**Gate Result**: ✅ PASS - All applicable principles satisfied

## Project Structure

### Documentation (this feature)

```text
specs/002-keyboard-control/
├── plan.md              # This file
├── research.md          # Phase 0 output - Windows keyboard API research
├── data-model.md        # Phase 1 output - Entity definitions
├── quickstart.md        # Phase 1 output - Getting started guide
├── contracts/           # Phase 1 output - API contracts
│   └── keyboard_control.json  # MCP tool schema
└── tasks.md             # Phase 2 output - Implementation tasks
```

### Source Code (repository root)

```text
src/Sbroenne.WindowsMcp/
├── Tools/
│   └── KeyboardControlTool.cs      # NEW: MCP tool implementation
├── Input/
│   ├── IKeyboardInputService.cs    # NEW: Interface for keyboard operations
│   ├── KeyboardInputService.cs     # NEW: SendInput-based implementation
│   ├── VirtualKeyMapper.cs         # NEW: Key name to virtual key code mapping
│   ├── HeldKeyTracker.cs           # NEW: Track keys held via key_down
│   ├── IModifierKeyManager.cs      # EXISTING: Reuse for modifier handling
│   └── ModifierKeyManager.cs       # EXISTING: Reuse for modifier handling
├── Models/
│   ├── KeyboardAction.cs           # NEW: Action enum (type, press, key_down, etc.)
│   ├── KeyboardControlRequest.cs   # NEW: Request model
│   ├── KeyboardControlResult.cs    # NEW: Result model with window info
│   ├── KeyboardControlErrorCode.cs # NEW: Error codes
│   ├── KeyboardLayout.cs           # NEW: Layout information
│   └── ModifierKey.cs              # EXISTING: Already includes Win key support needed
├── Configuration/
│   └── KeyboardConfiguration.cs    # NEW: Timeout and delay settings
├── Logging/
│   └── KeyboardOperationLogger.cs  # NEW: Structured logging
├── Native/
│   ├── NativeMethods.cs            # EXTEND: Add keyboard-specific P/Invoke
│   ├── NativeConstants.cs          # EXTEND: Add keyboard constants (VK_*, KEYEVENTF_*)
│   └── NativeStructs.cs            # EXISTING: INPUT/KEYBDINPUT already usable
└── Services/
    └── MouseOperationLock.cs       # RENAME to InputOperationLock.cs (shared)

tests/Sbroenne.WindowsMcp.Tests/
├── Integration/
│   ├── KeyboardTypeTests.cs        # NEW: Type action tests
│   ├── KeyboardPressTests.cs       # NEW: Press action tests
│   ├── KeyboardModifierTests.cs    # NEW: Modifier combination tests
│   ├── KeyboardHoldReleaseTests.cs # NEW: key_down/key_up/release_all tests
│   ├── KeyboardSequenceTests.cs    # NEW: Sequence action tests
│   ├── KeyboardLayoutTests.cs      # NEW: Layout detection tests
│   └── KeyboardIntegrationTestCollection.cs  # NEW: Test collection
└── Unit/
    ├── VirtualKeyMapperTests.cs    # NEW: Key name parsing tests
    └── KeyboardConfigurationTests.cs # NEW: Configuration validation
```

**Structure Decision**: Follows existing single-project pattern. Keyboard input services parallel mouse input services. Shared mutex renamed from `MouseOperationLock` to `InputOperationLock` for cross-tool synchronization.

## Complexity Tracking

> No Constitution Check violations to justify.

| Item | Decision | Rationale |
|------|----------|-----------|
| Shared Mutex | InputOperationLock | Prevents interleaved mouse/keyboard operations per FR-025 |
| Reuse ModifierKeyManager | Yes | Existing implementation handles Ctrl/Shift/Alt; extend for Win key |
| Unicode Input | KEYEVENTF_UNICODE | Layout-independent per FR-026, handles all languages |
