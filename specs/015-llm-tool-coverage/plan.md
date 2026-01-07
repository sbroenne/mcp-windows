# Implementation Plan: Comprehensive LLM Tool Coverage Tests

**Branch**: `015-llm-tool-coverage` | **Date**: 2026-01-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/015-llm-tool-coverage/spec.md`

## Summary

Create comprehensive LLM integration tests using agent-benchmark to validate all 11 MCP tools across all actions, using Notepad and Paint as real-world test applications. Tests will verify that LLMs correctly discover and use tools from natural language prompts, with flexible assertions to accept valid alternative approaches.

## Technical Context

**Language/Version**: YAML (agent-benchmark test format), PowerShell (test runner scripts)  
**Primary Dependencies**: agent-benchmark, Azure OpenAI (GPT-4.1, GPT-5.2-chat), Filesystem MCP Server  
**Storage**: Timestamped output folders under `tests/Sbroenne.WindowsMcp.LLM.Tests/output/` (gitignored)  
**Testing**: agent-benchmark with YAML scenario definitions  
**Target Platform**: Windows 10/11 with desktop session (not headless CI)
**Project Type**: Test suite extension (YAML test files + PowerShell scripts)  
**Performance Goals**: Each test step < 30 seconds  
**Constraints**: Rate limiting via agent-benchmark built-in support; all configured providers must pass  
**Scale/Scope**: 11 tools × multiple actions = ~50+ test cases across 7 YAML files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Status | Notes |
|-----------|-------------|--------|-------|
| I. Test-First Development | LLM tests validate tool usability | ✅ PASS | This feature IS the test implementation |
| XXIII. LLM Integration Testing | Every tool must have agent-benchmark test | ✅ PASS | Core goal of this feature |
| XXIV. Token Optimization | Tests should verify token-efficient responses | ✅ PASS | `max_tokens` assertions included |
| XXV. UI Automation First | Tests validate semantic UI tools are preferred | ✅ PASS | Assertions accept ui_* tools as primary |
| VII. Microsoft Libraries First | Use existing infrastructure | ✅ PASS | Using existing agent-benchmark setup |
| XII. Graceful Lifecycle | Clean state between sessions | ✅ PASS | Close all test apps before each session |
| FR-022-025 | Plain English prompts only | ✅ PASS | No tool names in user prompts |

## Project Structure

### Documentation (this feature)

```text
specs/015-llm-tool-coverage/
├── plan.md              # This file
├── research.md          # Phase 0 output (agent-benchmark patterns, assertion types)
├── data-model.md        # Phase 1 output (test file structure, assertion catalog)
├── quickstart.md        # Phase 1 output (how to run tests)
├── contracts/           # N/A - no API contracts for test suite
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
tests/Sbroenne.WindowsMcp.LLM.Tests/
├── Scenarios/                           # YAML test scenarios
│   ├── notepad-test.yaml                # [EXISTS] Legacy test - to be enhanced
│   ├── paint-smiley-test.yaml           # [EXISTS] Legacy test - to be enhanced
│   ├── notepad-ui-test.yaml             # [NEW] Core UI tools against Notepad
│   ├── paint-ui-test.yaml               # [NEW] Tool selection, canvas ops against Paint
│   ├── window-management-test.yaml      # [NEW] All 10 window_management actions
│   ├── keyboard-mouse-test.yaml         # [NEW] keyboard_control + mouse_control
│   ├── screenshot-test.yaml             # [NEW] screenshot_control actions
│   ├── file-dialog-test.yaml            # [NEW] ui_file Save As dialog handling
│   └── real-world-workflows-test.yaml   # [NEW] Multi-step workflow scenarios
├── output/                              # [NEW] Timestamped test artifact folders (gitignored)
├── Run-LLMTests.ps1                     # [EXISTS] General test runner
├── llm-tests.config.json                # [EXISTS] Shared config
├── llm-tests.config.local.json          # [EXISTS] Personal config (gitignored)
├── llm-tests.config.schema.json         # [EXISTS] Config schema
├── TestResults/                         # [EXISTS] HTML reports
└── README.md                            # [EXISTS] To be updated
```

**Structure Decision**: This is a test suite extension. All new files are YAML test scenarios under `Scenarios/` directory. No source code changes needed - only test artifacts.

## Complexity Tracking

No constitution violations requiring justification. This feature:
- Uses existing agent-benchmark infrastructure (no new dependencies)
- Creates test files only (no production code changes)
- Uses standard Windows applications (no custom harness)
- Follows established patterns from existing `notepad-test.yaml` and `paint-smiley-test.yaml`

---

## Phase 0 Complete: Research

✅ Generated [research.md](research.md) with findings on:
- Agent-benchmark YAML structure
- Assertion types catalog  
- Notepad UI element map
- Paint UI element map (ribbon, canvas)
- Multi-provider configuration
- Session state management
- Test artifact output strategy
- Plain English prompt patterns

---

## Phase 1 Complete: Design

✅ Generated [data-model.md](data-model.md) with:
- Tool coverage matrix (11 tools mapped to 7 YAML files)
- Test file specifications (sessions, tool coverage, test counts)
- Standard assertion patterns (5 reusable templates)
- Session structure template
- Provider and agent configuration block
- Environment variables reference
- Success criteria mapping

✅ Generated [quickstart.md](quickstart.md) with:
- Prerequisites checklist
- Quick setup commands
- Test running commands
- Test file summary with durations
- Troubleshooting guide

✅ Updated agent context via `update-agent-context.ps1 -AgentType copilot`

---

## Constitution Check (Post-Design)

| Principle | Status | Verification |
|-----------|--------|--------------|
| I. Test-First Development | ✅ PASS | Feature creates tests before implementation |
| XXIII. LLM Integration Testing | ✅ PASS | All 11 tools covered |
| XXIV. Token Optimization | ✅ PASS | `max_tokens` and `max_latency_ms` assertions |
| XXV. UI Automation First | ✅ PASS | ui_* tools are primary, mouse/keyboard are fallback |
| FR-022-025 Plain English | ✅ PASS | All prompts in natural language |

---

## Next Steps

Run `/speckit.tasks` to generate implementation tasks for:
1. Create 7 new YAML test files
2. Add output/ folder to .gitignore
3. Update README.md with new test coverage
4. Validate tests pass on both GPT-4.1 and GPT-5.2-chat
