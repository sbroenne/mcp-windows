# Implementation Plan: LLM-Based Integration Testing Framework

**Branch**: `007-llm-integration-testing` | **Date**: 2025-12-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-llm-integration-testing/spec.md`

## Summary

Create an LLM-based integration testing framework where GitHub Copilot (the LLM) acts as the test executor, invoking MCP tools directly and using screenshots for visual verification. Unlike traditional test frameworks, this approach leverages existing MCP tools already available in Copilot's session—no external test runner code is required. The "framework" consists of:
1. Test scenario definitions (markdown files with structured prompts)
2. Results storage conventions (directories and naming)
3. Execution guide for developers
4. Optional: Convenience scripts for batch execution

## Technical Context

**Language/Version**: N/A (Test scenarios are natural language prompts; no compiled code)
**Primary Dependencies**: 
- GitHub Copilot (already available in VS Code)
- MCP tools: `mcp_windows_mcp_s_mouse_control`, `mcp_windows_mcp_s_keyboard_control`, `mcp_windows_mcp_s_window_management`, `mcp_windows_mcp_s_screenshot_control`
**Storage**: Filesystem (screenshots saved as PNG, results as markdown)
**Testing**: Self-referential (the framework IS the test execution mechanism)
**Target Platform**: Windows 11 (same as MCP server)
**Project Type**: Documentation + test scenarios (no source code)
**Performance Goals**: Single-action tests complete within 30 seconds (SC-001)
**Constraints**: Tests run sequentially; secondary monitor preferred (Constitution v2.3.0, Principle XIV)
**Scale/Scope**: 74 test cases defined in spec.md
**Target Applications**: Notepad (keyboard/text tests) + Calculator (button/click tests) - Windows 11 built-in apps

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ PASS | This feature IS about testing |
| VI. Augmentation, Not Duplication | ✅ PASS | LLM does the visual analysis—we just provide raw screenshots |
| VII. Windows API Documentation-First | ✅ N/A | No new Windows APIs needed |
| XIII. Modern .NET & C# Best Practices | ✅ N/A | No C# code in this feature |
| XIV. xUnit Testing Best Practices | ✅ PASS | Secondary monitor requirement incorporated |
| XXII. Open Source Dependencies | ✅ PASS | No external dependencies |

**All gates pass.** No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/007-llm-integration-testing/
├── plan.md              # This file
├── spec.md              # Feature specification with 74 test cases
├── research.md          # Phase 0 output (framework research summary)
├── data-model.md        # Phase 1 output (test scenario format)
├── quickstart.md        # Phase 1 output (execution guide)
├── contracts/           # Phase 1 output (scenario schema)
│   └── scenario-schema.json
├── checklists/
│   └── requirements.md  # Specification checklist
├── scenarios/           # Test scenario files (Phase 3+)
│   ├── TC-MOUSE-001.md
│   ├── TC-KEYBOARD-001.md
│   └── ...
├── templates/           # Reusable templates
│   ├── scenario-template.md
│   ├── result-template.md
│   └── report-template.md
└── results/             # Test execution results (created at runtime)
    └── [YYYY-MM-DD]/
        ├── TC-MOUSE-001/
        │   ├── before.png
        │   ├── after.png
        │   └── result.md
        └── ...
```

### Source Code (repository root)

```text
# No source code changes required for this feature
# The "framework" is:
# 1. Test scenario markdown files (in specs/007-llm-integration-testing/)
# 2. GitHub Copilot executing prompts
# 3. MCP tools already implemented

# Optional convenience scripts (Phase 2, if desired):
scripts/
└── test-llm/
    ├── run-test.ps1       # Helper to structure test execution
    └── save-results.ps1   # Helper to organize screenshots
```

**Structure Decision**: This feature is primarily documentation and test scenario definitions. No changes to the `src/` or `tests/` directories are required. The test execution happens through GitHub Copilot chat, which already has access to all MCP tools.

## Complexity Tracking

> **No violations requiring justification.** This feature adds no production code complexity.

---

## Post-Design Constitution Check

*Re-evaluation after Phase 1 design completion*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| I. Test-First Development | ✅ PASS | Framework enables test-first for MCP tools |
| VI. Augmentation, Not Duplication | ✅ PASS | LLM analyzes screenshots; server only captures |
| VII. Windows API Documentation-First | ✅ N/A | No new Windows APIs introduced |
| VIII. Security Best Practices | ✅ PASS | No code changes; test scenarios are inert |
| XIV. xUnit Testing Best Practices | ✅ PASS | Secondary monitor requirement in all scenarios |
| XXII. Open Source Dependencies | ✅ PASS | Zero new dependencies added |

**All gates pass post-design. Proceed to Phase 2 (tasks.md).**

---

## Artifacts Generated

| Phase | Artifact | Path | Status |
|-------|----------|------|--------|
| 0 | Research | [research.md](research.md) | ✅ Complete |
| 1 | Data Model | [data-model.md](data-model.md) | ✅ Complete |
| 1 | Contract Schema | [contracts/scenario-schema.json](contracts/scenario-schema.json) | ✅ Complete |
| 1 | Quickstart Guide | [quickstart.md](quickstart.md) | ✅ Complete |
| 1 | Agent Context | `.github/agents/copilot-instructions.md` | ✅ Updated |
| 2 | Tasks | [tasks.md](tasks.md) | ✅ Complete |
| 3 | Mouse Scenarios | [scenarios/TC-MOUSE-*.md](scenarios/) | ✅ 12 scenarios |
| 3 | Keyboard Scenarios | [scenarios/TC-KEYBOARD-*.md](scenarios/) | ✅ 15 scenarios |
| 3 | Window Scenarios | [scenarios/TC-WINDOW-*.md](scenarios/) | ✅ 14 scenarios |
| 3 | Screenshot Scenarios | [scenarios/TC-SCREENSHOT-*.md](scenarios/) | ✅ 10 scenarios |
| 4 | Visual Scenarios | [scenarios/TC-VISUAL-*.md](scenarios/) | ✅ 5 scenarios |
| 4 | Visual Guide | [templates/visual-verification-guide.md](templates/visual-verification-guide.md) | ✅ Complete |
| 5 | Workflow Scenarios | [scenarios/TC-WORKFLOW-*.md](scenarios/) | ✅ 10 scenarios |
| 5 | Workflow Guide | [templates/workflow-guide.md](templates/workflow-guide.md) | ✅ Complete |
| 6 | Scenario Template | [templates/scenario-template.md](templates/scenario-template.md) | ✅ Complete |
| 6 | Contributor Guide | [docs/CONTRIBUTING-TESTS.md](docs/CONTRIBUTING-TESTS.md) | ✅ Complete |
| 6 | Annotated Example | [templates/example-scenario-annotated.md](templates/example-scenario-annotated.md) | ✅ Complete |
| 7 | Error Scenarios | [scenarios/TC-ERROR-*.md](scenarios/) | ✅ 8 scenarios |
| 7 | Result Template | [templates/result-template.md](templates/result-template.md) | ✅ Complete |
| 7 | Report Template | [templates/report-template.md](templates/report-template.md) | ✅ Complete |
| 7 | Results Guide | [templates/results-guide.md](templates/results-guide.md) | ✅ Complete |
| 7 | Sample Result | [results/example/TC-EXAMPLE-001/result.md](results/example/TC-EXAMPLE-001/result.md) | ✅ Complete |
| 8 | Chat Prompts | [templates/chat-prompts.md](templates/chat-prompts.md) | ✅ Complete |
| 9 | Scenario Index | [scenarios/README.md](scenarios/README.md) | ✅ Complete |

**Total Scenarios**: 74 (12 MOUSE + 15 KEYBOARD + 14 WINDOW + 10 SCREENSHOT + 5 VISUAL + 10 WORKFLOW + 8 ERROR)
