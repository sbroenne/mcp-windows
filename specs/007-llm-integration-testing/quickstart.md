# Quickstart: LLM-Based Integration Testing

**Feature**: 007-llm-integration-testing | **Date**: 2025-12-08 | **Version**: 1.0

## Overview

This guide explains how to execute LLM-based integration tests for the Windows MCP Server. Unlike traditional test frameworks, **you execute tests by instructing GitHub Copilot through chat**. Copilot invokes the MCP tools directly and uses screenshots for visual verification.

---

## Quick Links

| Resource | Description |
|----------|-------------|
| [scenarios/README.md](scenarios/README.md) | Index of all 74 test scenarios |
| [templates/chat-prompts.md](templates/chat-prompts.md) | Ready-to-use Copilot chat prompts |
| [templates/scenario-template.md](templates/scenario-template.md) | Template for creating new scenarios |
| [templates/result-template.md](templates/result-template.md) | Template for recording test results |
| [templates/report-template.md](templates/report-template.md) | Template for daily summary reports |
| [templates/workflow-guide.md](templates/workflow-guide.md) | Guide for multi-step workflow tests |
| [templates/visual-verification-guide.md](templates/visual-verification-guide.md) | Guide for visual comparison tests |
| [templates/results-guide.md](templates/results-guide.md) | Guide for storing test results |
| [docs/CONTRIBUTING-TESTS.md](docs/CONTRIBUTING-TESTS.md) | How to write new test scenarios |
| [spec.md](spec.md) | Full specification document |
| [data-model.md](data-model.md) | Entity definitions |

---

## Prerequisites

1. **GitHub Copilot** enabled in VS Code
2. **MCP Server** connected (verify by checking MCP tools are available)
3. **Secondary monitor** available (recommended for test isolation)
4. **Windows 11** with the MCP server running

### Verify MCP Connection

Before running tests, confirm MCP tools are available by asking Copilot:

```text
What MCP tools do you have available for Windows automation?
```

Expected response should list:
- `mcp_windows_mcp_s_mouse_control`
- `mcp_windows_mcp_s_keyboard_control`
- `mcp_windows_mcp_s_window_management`
- `mcp_windows_mcp_s_screenshot_control`

---

## Running a Single Test

### Step 1: Identify the Test Case

Test cases are defined in [spec.md](spec.md) under the "Test Cases" section, or as individual files in `scenarios/` (if created).

Example: **TC-MOUSE-001** - Basic Mouse Movement

### Step 2: Execute via Copilot Chat

Copy and paste this prompt to Copilot:

```text
Execute test case TC-MOUSE-001: Basic Mouse Movement

**Objective**: Verify mouse movement to specific coordinates on secondary monitor.

**Steps**:
1. Call screenshot_control with action="list_monitors" to find secondary monitor
2. Take a "before" screenshot of the secondary monitor
3. Move the mouse to the center of the secondary monitor
4. Take an "after" screenshot
5. Verify the mouse cursor is visible at the target location

**Pass Criteria**:
- list_monitors returns at least 2 monitors
- Mouse move completes without error
- Screenshot shows cursor at target location

Please execute each step, take screenshots, and report the result.
```

### Step 3: Review Results

Copilot will:
1. Invoke each MCP tool
2. Take screenshots at appropriate points
3. Analyze the screenshots visually
4. Report PASS/FAIL with observations

---

## Running Multiple Tests

### By Category

```text
Execute all TC-MOUSE-* tests from the spec:
- TC-MOUSE-001 through TC-MOUSE-012

For each test:
1. Execute the steps as defined
2. Take before/after screenshots
3. Report PASS/FAIL with observations
4. Continue to next test even if one fails

At the end, provide a summary table.
```

### By Priority

```text
Execute all P1 (Critical) tests for the Windows MCP Server:

From the spec.md, these include:
- TC-MOUSE-001, TC-MOUSE-002, TC-MOUSE-003, TC-MOUSE-007, TC-MOUSE-008
- TC-KEYBOARD-001, TC-KEYBOARD-002, TC-KEYBOARD-003
- TC-WINDOW-001 through TC-WINDOW-005
- TC-SCREENSHOT-001, TC-SCREENSHOT-002
- TC-ERROR-001 through TC-ERROR-003

Execute each and provide pass/fail summary.
```

---

## Saving Test Results

After test execution, you can save results:

### Manual Save

```text
Save the test results for TC-MOUSE-001 to:
specs/007-llm-integration-testing/results/2025-12-08/TC-MOUSE-001/

Include:
- result.md with pass/fail status and observations
- Any screenshots taken during the test
```

### Automated Save (Future)

A PowerShell script can be added to automate result organization. See `scripts/test-llm/save-results.ps1` (Phase 2 deliverable).

---

## Test Execution Tips

### 1. Use Secondary Monitor

Always start with monitor enumeration:

```text
Before running tests, list available monitors and identify the secondary monitor bounds.
Use the secondary monitor for all test execution to avoid interfering with VS Code.
```

### 2. Clear Test Environment

For repeatable results:

```text
Before running window tests, close any unnecessary windows on the secondary monitor.
```

### 3. Handle Failures Gracefully

If a test fails:

```text
TC-KEYBOARD-005 failed. Please:
1. Describe what went wrong
2. Take a screenshot of the current state
3. Suggest what might have caused the failure
4. Continue with the next test
```

### 4. Skip Blocked Tests

If preconditions aren't met:

```text
If secondary monitor is not available, mark monitor-dependent tests as BLOCKED and skip them.
```

---

## Example Test Execution Session

Here's a complete example session:

**You:**
> Execute TC-MOUSE-001: Move the mouse to coordinates (500, 300) on the secondary monitor. Take before and after screenshots. Verify the cursor moved correctly.

**Copilot:**
> Let me execute this test step by step.
>
> **Step 1: Enumerate monitors**
> [Calls screenshot_control with action="list_monitors"]
> Found 2 monitors:
> - Monitor 0 (Primary): 0,0 to 2560,1440
> - Monitor 1 (Secondary): 2560,0 to 5120,1440
>
> **Step 2: Before screenshot**
> [Calls screenshot_control targeting monitor 1]
> Screenshot captured.
>
> **Step 3: Move mouse**
> [Calls mouse_control with action="move", x=3060, y=300]
> Mouse moved to (3060, 300) - center-left of secondary monitor.
>
> **Step 4: After screenshot**
> [Calls screenshot_control targeting monitor 1]
> Screenshot captured.
>
> **Step 5: Visual verification**
> Analyzing the "after" screenshot... I can see the mouse cursor is now visible at approximately the target location on the secondary monitor.
>
> **Result: PASS** ✅
> - Monitor enumeration: ✅ Found 2 monitors
> - Mouse movement: ✅ Completed without error
> - Visual verification: ✅ Cursor visible at target location

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| MCP tools not available | Restart VS Code, check MCP extension is installed |
| Only 1 monitor detected | Tests will be BLOCKED; run on primary monitor if acceptable |
| Screenshot shows black screen | May be on secure desktop (UAC); expected behavior |
| Mouse didn't move | Check if running elevated; some actions require admin |
| Keyboard input not working | Ensure target window has focus before typing |

---

## Next Steps

1. **Explore test cases**: Review [spec.md](spec.md) for all 74 defined test cases
2. **Run P1 tests first**: Start with critical path validation
3. **Create custom scenarios**: Add new test cases to `scenarios/` directory
4. **Review results**: Check `results/` for historical test data

---

## Chat Command Patterns

For ready-to-use prompts, see [templates/chat-prompts.md](templates/chat-prompts.md).

### Single Test Execution Pattern

The most basic pattern for running a single test:

```text
Execute test case {TC-ID}.
Read the scenario from specs/007-llm-integration-testing/scenarios/{TC-ID}.md
Follow all steps, take screenshots as specified, and report PASS/FAIL/BLOCKED.
```

**Example**:
```text
Execute test case TC-MOUSE-001.
Read the scenario from specs/007-llm-integration-testing/scenarios/TC-MOUSE-001.md
Follow all steps, take screenshots as specified, and report PASS/FAIL/BLOCKED.
```

### Batch Execution Pattern

For running multiple tests in sequence:

```text
Execute all {CATEGORY} category tests from specs/007-llm-integration-testing/scenarios/TC-{CATEGORY}-*.md

For each test:
1. Execute all steps
2. Record result
3. Continue to next test

Provide summary table at end with columns: Test ID, Status, Duration, Notes.
```

**Example**:
```text
Execute all KEYBOARD category tests from specs/007-llm-integration-testing/scenarios/TC-KEYBOARD-*.md

For each test:
1. Execute all steps
2. Record result
3. Continue to next test

Provide summary table at end with columns: Test ID, Status, Duration, Notes.
```

### Category-Based Execution Pattern

Target specific test categories:

| Category | Pattern |
|----------|---------|
| Mouse Tests | `TC-MOUSE-*` |
| Keyboard Tests | `TC-KEYBOARD-*` |
| Window Tests | `TC-WINDOW-*` |
| Screenshot Tests | `TC-SCREENSHOT-*` |
| Workflow Tests | `TC-WORKFLOW-*` |
| Error Tests | `TC-ERROR-*` |
| Visual Tests | `TC-VISUAL-*` |

**Example**:
```text
Execute all WORKFLOW tests (TC-WORKFLOW-001 through TC-WORKFLOW-010).
These tests require Notepad and Calculator to be open on the secondary monitor.
Take intermediate screenshots at key workflow steps.
```

### Priority-Based Execution Pattern

Focus on critical tests first:

```text
Execute all P1 (Critical) priority tests.
These are the MVP tests that must pass for basic functionality.

Find tests with Priority: P1 in their metadata.
Stop immediately if any P1 test fails.
```

### Workflow Execution Pattern

For multi-step workflow tests:

```text
Execute workflow TC-WORKFLOW-{NNN}.

This is a multi-step test that chains multiple MCP tools.
Read the full scenario from scenarios/TC-WORKFLOW-{NNN}.md.

Take screenshots:
- Before starting
- After each major step
- After completion

Report which tools were chained and whether the workflow succeeded.
```

### Error Testing Pattern

For testing error handling:

```text
Execute error test TC-ERROR-{NNN}.

This test intentionally triggers an error condition.
Expected behavior: Tool returns error message without crashing.

Verify:
1. Error message is clear and descriptive
2. Tool remains responsive after error
3. No unexpected side effects
```

### Result Saving Pattern

To save test results:

```text
Save the test result for {TC-ID} to:
specs/007-llm-integration-testing/results/{YYYY-MM-DD}/{TC-ID}/

Create result.md using the template.
Include all screenshots taken.
Record pass criteria results.
```

### Quick Verification Patterns

For rapid validation:

| Purpose | Prompt |
|---------|--------|
| Check MCP | "List available monitors using screenshot_control" |
| Check Mouse | "Move mouse to (500,500) and take screenshot" |
| Check Keyboard | "Type 'test' in Notepad and screenshot" |
| Check Windows | "List all open windows" |

---

## Full Session Example

Here's a complete testing session workflow:

### 1. Environment Setup
```text
Verify test prerequisites:
1. List monitors (need secondary)
2. Open Notepad on secondary monitor
3. Open Calculator on secondary monitor
4. Take screenshot to confirm setup
```

### 2. Execute P1 Tests
```text
Execute all P1 priority tests.
Continue even if tests fail.
Provide summary at end.
```

### 3. Execute Workflow Tests
```text
Execute workflow tests TC-WORKFLOW-001 through TC-WORKFLOW-010.
These validate tool chaining.
```

### 4. Save Results
```text
Create daily report for today's test run.
Save to results/YYYY-MM-DD/report.md.
Include all test results and summary statistics.
```

### 5. Review Failures
```text
List all failed tests and their failure reasons.
Suggest fixes or investigation steps for each failure.
```
