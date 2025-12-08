# Chat Prompts for LLM-Based Testing

This document provides ready-to-use prompts for executing tests via GitHub Copilot Chat.

## Single Test Execution

### Execute by Test Case ID

```text
Execute test case TC-MOUSE-001.

Read the scenario from specs/007-llm-integration-testing/scenarios/TC-MOUSE-001.md and follow these steps:

1. Check all preconditions
2. Execute each step in order
3. Take screenshots as specified (before, after, and any intermediate)
4. Verify each pass criterion
5. Report the result as PASS, FAIL, or BLOCKED

Include your observations for each step.
```

### Execute with Inline Scenario

```text
Execute this test:

**Test Case**: TC-MOUSE-001
**Objective**: Verify mouse movement to specific coordinates

**Steps**:
1. List monitors to find secondary monitor
2. Take before screenshot of secondary monitor
3. Move mouse to center of secondary monitor
4. Take after screenshot
5. Verify cursor position changed

**Pass Criteria**:
- [ ] list_monitors returns at least 2 monitors
- [ ] move action completes without error
- [ ] Cursor visible at target in after screenshot

Execute now and report PASS/FAIL.
```

## Batch Test Execution

### Execute by Category

```text
Execute all MOUSE category tests.

For each scenario file matching specs/007-llm-integration-testing/scenarios/TC-MOUSE-*.md:
1. Read the scenario
2. Execute all steps
3. Record PASS/FAIL/BLOCKED
4. Continue to next test even if one fails

At the end, provide a summary table:
| Test ID | Status | Duration | Notes |
```

### Execute by Priority

```text
Execute all P1 (Critical) tests from the test scenarios.

Look in specs/007-llm-integration-testing/scenarios/ for files with Priority: P1 in metadata.

Execute each test and provide a summary showing:
- Total tests
- Passed
- Failed
- Blocked

List any failures with the reason.
```

### Execute Multiple Specific Tests

```text
Execute these test cases in order:
- TC-WINDOW-002 (Find window by title)
- TC-WINDOW-005 (Activate window)
- TC-KEYBOARD-001 (Type simple text)

This is a workflow validation. Stop if any test fails.
Report final status and all observations.
```

## Workflow Test Execution

### Execute Multi-Step Workflow

```text
Execute workflow test TC-WORKFLOW-003: Type text in window

This is a multi-step workflow that chains multiple tools:
1. Find and activate Notepad window
2. Type test text
3. Verify text appears via screenshot

Read the full scenario from:
specs/007-llm-integration-testing/scenarios/TC-WORKFLOW-003.md

Take intermediate screenshots at each major step.
Report the final result.
```

### Execute Full Workflow Suite

```text
Execute all workflow tests (TC-WORKFLOW-*).

These tests validate tool chaining and require:
- Notepad open on secondary monitor
- Calculator open on secondary monitor

Execute in order from TC-WORKFLOW-001 to TC-WORKFLOW-010.
Provide a workflow summary at the end.
```

## Result Saving Prompts

### Save Test Result

```text
Save the results of the test we just ran.

Create result file at:
specs/007-llm-integration-testing/results/2025-12-08/TC-MOUSE-001/result.md

Use the result template from:
specs/007-llm-integration-testing/templates/result-template.md

Fill in all fields including:
- Summary metrics
- Pass criteria results
- Screenshot references
- Tool invocations
- Observations
```

### Create Daily Report

```text
Create a daily test report for today's test runs.

Create report at:
specs/007-llm-integration-testing/results/2025-12-08/report.md

Use the template from:
specs/007-llm-integration-testing/templates/report-template.md

Include all tests executed today with their results.
```

## Environment Check Prompts

### Verify Prerequisites

```text
Before running tests, verify the test environment:

1. List available monitors (need secondary monitor)
2. List open windows (check for Notepad/Calculator)
3. Take a screenshot of the secondary monitor
4. Report environment status

If secondary monitor not available, indicate tests will be BLOCKED.
```

### Check MCP Connection

```text
Verify MCP tools are available:

1. Call screenshot_control with action="list_monitors"
2. Call window_management with action="list"

Report whether the MCP server is connected and responsive.
```

## Troubleshooting Prompts

### Debug Failed Test

```text
Test TC-KEYBOARD-001 failed. Debug the failure:

1. Take a screenshot of current state
2. Check which window has focus
3. Verify Notepad is open and active
4. Attempt to type text again
5. Report what went wrong
```

### Retry Failed Test

```text
Retry TC-MOUSE-003 which failed previously.

Before retrying:
1. Reset the environment (close unnecessary windows)
2. Take a fresh before screenshot
3. Execute the test again
4. Compare results
```

## Quick Commands

### Quick Smoke Test

```text
Run a quick smoke test of all MCP tools:
1. screenshot_control: list_monitors
2. mouse_control: move to center of secondary monitor
3. window_management: list windows
4. keyboard_control: get keyboard layout

Report which tools are working.
```

### Quick Visual Test

```text
Take a screenshot of the secondary monitor and describe what you see.
This validates the screenshot_control tool is working.
```

### Quick Mouse Test

```text
Move the mouse to (100, 100) on the secondary monitor.
Take before and after screenshots.
Verify the cursor moved.
```

### Quick Keyboard Test

```text
Open Notepad, type "Hello World", take a screenshot.
Verify the text appears.
```

## Advanced Prompts

### Execute with Custom Timeout

```text
Execute TC-WINDOW-013 (Wait for window) with a 10 second timeout.
The scenario expects a timeout; verify the tool returns an error after waiting.
```

### Execute Error Scenario

```text
Execute TC-ERROR-001: Invalid mouse coordinates

This test intentionally uses invalid coordinates to verify error handling.
Expected result: Tool returns error message without crashing.
```

### Execute with Result Comparison

```text
Execute TC-VISUAL-001 and compare before/after screenshots.
Describe in detail what visual differences you observe.
Use this to verify the visual comparison capability.
```
