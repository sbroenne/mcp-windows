# Research: LLM-Based Integration Testing Framework

**Feature**: 007-llm-integration-testing | **Date**: 2025-12-08 | **Version**: 1.0

## Executive Summary

This research document captures findings for the LLM-based integration testing framework. The key insight is that **no external testing framework is needed**—GitHub Copilot already has direct access to MCP tools and can execute test scenarios through natural language prompts.

---

## 1. Existing Testing Frameworks

### Research Task
Evaluate existing LLM/MCP testing frameworks for potential adoption.

### Findings

| Framework | Purpose | Verdict |
|-----------|---------|---------|
| [MCP Inspector](https://github.com/modelcontextprotocol/inspector) | Official MCP debugging/testing tool | **Not needed** - Tests individual tool calls, not LLM-driven flows |
| [langwatch/scenario](https://github.com/langwatch/scenario) | Agent-tests-agent pattern | **Overkill** - Designed to connect TO an LLM; we ARE the LLM |
| [golf-mcp/golf-testing](https://github.com/golf-mcp/golf-testing) | MCP-specific test harness | **Not needed** - Adds orchestration layer we don't need |
| [pixelmatch](https://github.com/mapbox/pixelmatch) | Visual diff for screenshots | **Defer** - LLM can visually analyze; add if human review needed |
| [contextcheck](https://github.com/contextcheck/contextcheck) | YAML test scenarios | **Not needed** - Natural language scenarios are more flexible |

### Decision
**Keep framework-agnostic.** Test scenarios are markdown files with natural language instructions. GitHub Copilot executes them directly via MCP tools.

### Rationale
1. **We ARE the LLM** - Those frameworks are designed to connect TO an LLM, but Copilot already IS the LLM with direct MCP access
2. **Adding orchestration defeats the purpose** - We want to test "real LLM-to-MCP interactions," not orchestrated simulations
3. **Zero dependencies** - Aligns with Constitution Principle XXII (Open Source Dependencies)

### Alternatives Rejected
- External test runners add complexity without value
- YAML/JSON test definitions are less flexible than natural language
- Automated assertion libraries can't match LLM visual analysis capability

---

## 2. Screenshot Verification Strategy

### Research Task
Determine best approach for visual verification of test results.

### Findings

**Current Capability**:
- `mcp_windows_mcp_s_screenshot_control` returns base64-encoded PNG data
- GitHub Copilot can view images inline via VS Code image preview
- LLM can describe image contents and identify visual elements

**Verification Approaches**:

| Approach | Implementation | Verdict |
|----------|----------------|---------|
| LLM Visual Analysis | LLM describes what it sees in screenshot | **Primary** - Already works, no code needed |
| Pixel Diff (pixelmatch) | Compare before/after at pixel level | **Defer** - Add later if false positives become issue |
| OCR Text Extraction | Extract text from screenshots | **Defer** - Add later if text verification needed |
| Reference Image Comparison | Compare against known-good screenshots | **Defer** - Brittle; resolution/theme dependent |

### Decision
**LLM Visual Analysis** as primary verification method. The LLM takes a screenshot, describes what it sees, and determines if the expected state is present.

### Rationale
1. **Already works** - No additional tooling required
2. **Flexible** - LLM can interpret partial matches, handle minor variations
3. **Self-documenting** - LLM explains what it sees, creating readable test results

---

## 3. Secondary Monitor Targeting

### Research Task
Determine how to consistently target secondary monitor for test execution.

### Findings

**Available MCP Tool Parameters**:
- `screenshot_control`: `target="monitor"` with `monitorIndex` parameter (0-based)
- `screenshot_control`: `action="list_monitors"` returns available monitors
- `mouse_control`: `x`, `y` coordinates can target any monitor (coordinates extend beyond primary)
- `window_management`: `move` action with `x`, `y` can position windows on any monitor

**Multi-Monitor Coordinate System**:
- Windows uses virtual screen coordinates
- Primary monitor typically starts at (0, 0)
- Secondary monitor coordinates depend on arrangement (e.g., left of primary = negative X)

### Decision
**Test scenarios MUST call `list_monitors` first**, then use secondary monitor coordinates for all operations.

### Rationale
1. Aligns with Constitution v2.3.0, Principle XIV (Secondary Monitor Preference)
2. Prevents test interference with VS Code on primary monitor
3. Provides consistent test environment

### Implementation Pattern
```text
1. Call screenshot_control with action="list_monitors"
2. Parse response to get secondary monitor bounds
3. Use those bounds for all window positioning and click coordinates
4. Take screenshots targeting that monitorIndex
```

---

## 4. Test Result Storage

### Research Task
Define storage format and location for test results.

### Findings

**Requirements from Spec**:
- Visual evidence for each test step (FR-007)
- Test execution history (FR-009)
- Batch reports (FR-012)

**Storage Options**:

| Option | Format | Verdict |
|--------|--------|---------|
| Filesystem (flat) | PNG + MD files | **Too simple** - No organization |
| Filesystem (dated) | `results/YYYY-MM-DD/TC-XXX/` | **Selected** - Simple, browsable, git-friendly |
| SQLite database | Structured data + blob storage | **Overkill** - Not needed for this scale |
| JSON files | Structured test results | **Defer** - Add if machine parsing needed |

### Decision
**Filesystem with dated directories**:
```text
specs/007-llm-integration-testing/results/
└── 2025-12-08/
    └── TC-MOUSE-001/
        ├── before.png
        ├── after.png
        └── result.md
```

### Rationale
1. Human-browsable without tooling
2. Git-friendly (can .gitignore if desired)
3. Easy to archive or clean up by date

---

## 5. Test Scenario Format

### Research Task
Define structured format for test scenario definitions.

### Findings

**Format Options**:

| Format | Example | Verdict |
|--------|---------|---------|
| YAML | Structured fields, parsing required | **Not needed** - Adds parsing complexity |
| JSON | Machine-readable, verbose | **Not needed** - Less readable for humans |
| Markdown (structured) | Human-readable, natural language | **Selected** - Aligns with LLM execution model |
| Plain text | Unstructured instructions | **Too loose** - Need some structure for consistency |

### Decision
**Markdown with structured sections**:
```markdown
# Test Case: TC-MOUSE-001

## Objective
Verify basic mouse movement to specific coordinates.

## Preconditions
- Secondary monitor available
- No windows in target area

## Steps
1. Call `list_monitors` to get secondary monitor bounds
2. Take "before" screenshot of secondary monitor
3. Move mouse to center of secondary monitor
4. Take "after" screenshot
5. Verify mouse cursor is visible at expected location

## Expected Result
- Mouse cursor visible at center of secondary monitor
- No errors returned from MCP tools

## Pass Criteria
- [ ] Screenshot shows cursor at target location
- [ ] No tool invocation errors
```

### Rationale
1. Readable by both humans and LLM
2. Structured enough for consistency
3. Flexible enough for complex scenarios
4. No parsing/tooling required

---

## 6. Clarifications Resolved

| Topic | Resolution | Source |
|-------|------------|--------|
| Multi-monitor handling | Explicitly target secondary monitor | User clarification, spec updated |
| External frameworks | Not needed; use Copilot directly | Research above |
| Visual verification | LLM describes screenshots | Research above |
| Result storage | Filesystem with dated directories | Research above |
| Scenario format | Structured markdown | Research above |

---

## Summary of Decisions

1. **No external testing framework** - GitHub Copilot executes scenarios directly
2. **LLM visual analysis** - Primary verification method (no pixelmatch/OCR)
3. **Secondary monitor targeting** - Call `list_monitors` first, use those bounds
4. **Filesystem storage** - `results/YYYY-MM-DD/TC-XXX/` structure
5. **Structured markdown scenarios** - Human-readable, LLM-executable

**All NEEDS CLARIFICATION items have been resolved. Proceed to Phase 1.**
