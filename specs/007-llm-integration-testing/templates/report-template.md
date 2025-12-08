# Daily Test Report: {YYYY-MM-DD}

**Generated**: {YYYY-MM-DD HH:MM:SS}  
**Report Type**: {Full / Category / Custom}  
**Categories Included**: {All / MOUSE / KEYBOARD / etc.}

---

## Executive Summary

| Metric | Count | Percentage |
|--------|-------|------------|
| **Total Tests** | {N} | 100% |
| **Passed** | {N} | {X}% |
| **Failed** | {N} | {X}% |
| **Blocked** | {N} | {X}% |
| **Skipped** | {N} | {X}% |

### Overall Status: {✅ ALL PASS / ⚠️ PARTIAL / ❌ FAILURES}

---

## Results by Category

### Mouse Control (TC-MOUSE-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-MOUSE-001 | Move cursor to absolute position | ✅ PASS | 2.3s |
| TC-MOUSE-002 | Move cursor to screen corners | ✅ PASS | 1.8s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Keyboard Control (TC-KEYBOARD-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-KEYBOARD-001 | Type simple text | ✅ PASS | 3.1s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Window Management (TC-WINDOW-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-WINDOW-001 | List all windows | ✅ PASS | 1.2s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Screenshot Capture (TC-SCREENSHOT-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-SCREENSHOT-001 | Capture primary screen | ✅ PASS | 0.8s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Visual Verification (TC-VISUAL-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-VISUAL-001 | Detect window position change | ✅ PASS | 4.2s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Workflow Tests (TC-WORKFLOW-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-WORKFLOW-001 | Find and activate window | ✅ PASS | 5.1s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

### Error Handling (TC-ERROR-*)

| Test ID | Description | Status | Duration |
|---------|-------------|--------|----------|
| TC-ERROR-001 | Invalid mouse coordinates | ✅ PASS | 1.5s |
| ... | ... | ... | ... |

**Category Summary**: {X}/{Y} passed

---

## Failed Tests Detail

### {TC-ID}: {Test Description}

**Status**: ❌ FAIL  
**Duration**: {X.X}s  
**Failure Reason**: {Brief description}

**Expected**: {What should have happened}  
**Actual**: {What actually happened}

**Screenshots**: [before](TC-ID/before.png) | [after](TC-ID/after.png)

**Suggested Action**: {What to investigate or fix}

---

{Repeat for each failed test}

---

## Blocked Tests

| Test ID | Reason Blocked |
|---------|----------------|
| {TC-ID} | {Precondition not met - e.g., "No secondary monitor"} |

---

## Environment

| Property | Value |
|----------|-------|
| **Test Machine** | {Computer Name} |
| **OS Version** | Windows 11 {Build} |
| **Primary Monitor** | {Resolution} |
| **Secondary Monitor** | {Resolution or "Not Available"} |
| **MCP Server** | Sbroenne.WindowsMcp v{X.X.X} |
| **VS Code** | v{X.X.X} |
| **GitHub Copilot** | v{X.X.X} |

---

## Execution Timeline

```
{HH:MM:SS} Started test run
{HH:MM:SS} TC-MOUSE-001 - PASS (2.3s)
{HH:MM:SS} TC-MOUSE-002 - PASS (1.8s)
...
{HH:MM:SS} Completed test run - {N} tests in {MM:SS}
```

---

## Notes

{Any observations, anomalies, or recommendations from this test run}
