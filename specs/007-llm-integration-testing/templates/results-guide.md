# Results Guide

This guide explains how to store and organize test results for the LLM-Based Integration Testing Framework.

## Directory Structure

Test results are stored in a date-based hierarchy:

```
results/
├── 2025-12-08/                   # Date of test run
│   ├── report.md                 # Daily summary report
│   ├── TC-MOUSE-001/             # Individual test result
│   │   ├── before.png            # Before screenshot
│   │   ├── after.png             # After screenshot
│   │   └── result.md             # Result document
│   ├── TC-MOUSE-002/
│   │   ├── before.png
│   │   ├── step-03.png           # Intermediate screenshot
│   │   ├── after.png
│   │   └── result.md
│   └── TC-KEYBOARD-001/
│       ├── before.png
│       ├── after.png
│       └── result.md
└── 2025-12-09/                   # Next day's results
    └── ...
```

## File Naming Conventions

### Result Directories

- **Date Directory**: `YYYY-MM-DD` (e.g., `2025-12-08`)
- **Test Directory**: Same as test case ID (e.g., `TC-MOUSE-001`)

### Screenshot Files

| Filename | Purpose |
|----------|---------|
| `before.png` | Initial state before main action |
| `after.png` | Final state after main action |
| `step-{NN}.png` | Intermediate screenshot at step number NN |
| `step-{NN}-{label}.png` | Labeled intermediate (e.g., `step-03-selected.png`) |

### Result Files

- `result.md` - Primary result document (required)
- `report.md` - Daily aggregate report (one per date directory)

## Creating Results

### After Running a Test

1. Create date directory if not exists: `results/YYYY-MM-DD/`
2. Create test directory: `results/YYYY-MM-DD/{TC-ID}/`
3. Save screenshots with proper names
4. Create result.md from template

### Screenshot Requirements

Per data-model.md, at least one screenshot is required per test result.

**Before Screenshot** (required):
- Capture BEFORE the main action
- Shows initial state for comparison

**After Screenshot** (required):
- Capture AFTER the main action
- Shows final state for verification

**Intermediate Screenshots** (optional):
- Capture at significant steps
- Name with step number: `step-03.png`

### Result Document

Copy `templates/result-template.md` and fill in:

1. **Summary**: Times, duration, monitor info
2. **Pass Criteria Results**: Mark each criterion PASS or FAIL
3. **Screenshots**: Reference all screenshots with descriptions
4. **Tool Invocations**: Document each MCP tool call
5. **Observations**: LLM's narrative of what happened
6. **Failure Details**: Required if status is FAIL
7. **Environment**: System information

## Status Values

| Status | Meaning |
|--------|---------|
| **PASS** | All pass criteria verified |
| **FAIL** | One or more criteria not met |
| **BLOCKED** | Preconditions not satisfied |
| **SKIPPED** | Intentionally not executed |

### Status Rules (from data-model.md)

- Status cannot be PASS if any pass criteria result is FAIL
- BLOCKED should include which precondition failed
- SKIPPED should include reason (e.g., dependency failed)

## Daily Reports

After running tests, create a daily summary report:

1. Use `templates/report-template.md`
2. Aggregate all test results for the day
3. Calculate pass/fail/blocked/skipped counts
4. List failed tests with details
5. Include environment information

## Git Integration

By default, results are .gitignored:
- See `.gitignore` entry: `specs/007-llm-integration-testing/results/`

To track results:
1. Remove or modify the .gitignore entry
2. Commit result files

## Example Result

See `results/example/TC-EXAMPLE-001/result.md` for a complete filled-in example.

## Best Practices

### DO:
- ✅ Take screenshots immediately before and after actions
- ✅ Include cursor in screenshots (`includeCursor=true`)
- ✅ Document observations even for passing tests
- ✅ Record exact tool parameters used
- ✅ Note any anomalies or warnings

### DON'T:
- ❌ Skip screenshots (at least 1 required)
- ❌ Leave template placeholders unfilled
- ❌ Mark PASS if any criterion fails
- ❌ Forget to document environment info

## Querying Results

To find results for a specific test:
```
results/*/TC-MOUSE-001/result.md
```

To find all failures:
```
grep -r "Status.*FAIL" results/*/result.md
```

To list all tests run on a date:
```
ls results/2025-12-08/*/result.md
```
