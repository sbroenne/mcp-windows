# Test Result: {TC-ID}

<!--
  This is the template for recording LLM-based integration test results.
  Copy this file to results/{YYYY-MM-DD}/{TC-ID}/result.md after running a test.
  
  Matches TestRun entity from data-model.md:
  - runId: Implicit from directory path
  - scenarioId: TC-ID
  - startTime: Start Time field
  - endTime: End Time field
  - status: PASS/FAIL/BLOCKED/SKIPPED
  - screenshots: Screenshot section
  - observations: Observations section
  - passCriteriaResults: Pass Criteria Results table
-->

**Run ID**: {YYYY-MM-DDTHH:MM:SS}  
**Scenario**: [{TC-ID}](../../scenarios/{TC-ID}.md)  
**Status**: {PASS / FAIL / BLOCKED / SKIPPED}

---

## Summary

| Metric | Value |
|--------|-------|
| **Start Time** | {YYYY-MM-DD HH:MM:SS} |
| **End Time** | {YYYY-MM-DD HH:MM:SS} |
| **Duration** | {X.X seconds} |
| **Monitor Used** | {Primary / Secondary} |
| **Monitor Index** | {0-based index} |
| **Target App** | {Notepad / Calculator / None} |

## Pass Criteria Results

<!--
  List all pass criteria from the scenario.
  Mark each as PASS or FAIL with observations.
  
  Status cannot be PASS if any criterion is FAIL (per data-model.md validation rules)
-->

| Criterion | Result | Notes |
|-----------|--------|-------|
| {Criterion 1 from scenario} | ✅ PASS / ❌ FAIL | {Observation} |
| {Criterion 2 from scenario} | ✅ PASS / ❌ FAIL | {Observation} |
| {Criterion 3 from scenario} | ✅ PASS / ❌ FAIL | {Observation} |

## Screenshots

<!--
  Screenshots match Screenshot entity from data-model.md:
  - filename: File name in this directory
  - captureTime: When captured
  - monitorIndex: Which monitor
  - stepNumber: Which step (optional)
  - description: LLM description
  
  At least 1 screenshot is REQUIRED per data-model.md
-->

### Before
**Filename**: `before.png`  
**Capture Time**: {HH:MM:SS}  
**Monitor Index**: {N}  
**Step Number**: 2

![Before](before.png)

**LLM Observation**: {Description of what the LLM sees in this screenshot}

### After
**Filename**: `after.png`  
**Capture Time**: {HH:MM:SS}  
**Monitor Index**: {N}  
**Step Number**: 4

![After](after.png)

**LLM Observation**: {Description of what the LLM sees in this screenshot}

### Intermediate Screenshots (if applicable)

{Add any step-NN.png screenshots captured during the test}

### Visual Changes Detected

{LLM description of differences between before and after screenshots}

## Tool Invocations

### Step 1: {Action Description}

**Tool**: `{mcp_tool_name}`  
**Parameters**:
```json
{
  "action": "{action}",
  "param1": "value1"
}
```

**Response**:
```json
{
  "success": true,
  "result": "..."
}
```

**Elapsed**: {X.X}s

### Step 2: {Action Description}

{Repeat for each tool invocation}

## Observations

<!--
  LLM's detailed narrative of the test execution.
  This is the main "observations" field from TestRun entity.
-->

{LLM's detailed observations about test execution}

### What Happened

{Narrative description of the test execution}

### Anomalies

{Any unexpected behavior observed, even if test passed}

## Failure Details (if applicable)

<!--
  Required if Status is FAIL.
  Provides debugging context for investigation.
-->

**Failure Reason**: {Brief description of why test failed}

**Expected**: {What should have happened}

**Actual**: {What actually happened}

**Diagnostic Info**:
- {Relevant error messages}
- {State at time of failure}
- {Suggested root cause}

## Environment

| Property | Value |
|----------|-------|
| **OS** | Windows 11 |
| **OS Build** | {Build number} |
| **Screen Resolution** | {WxH} |
| **DPI Scaling** | {100% / 125% / 150% / etc.} |
| **Monitor Count** | {N} |
| **Target Monitor Index** | {N} |
| **MCP Server Version** | {Version} |
