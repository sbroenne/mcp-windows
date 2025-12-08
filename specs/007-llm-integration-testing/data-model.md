# Data Model: LLM-Based Integration Testing Framework

**Feature**: 007-llm-integration-testing | **Date**: 2025-12-08 | **Version**: 1.0

## Overview

This data model defines the entities used in the LLM-based integration testing framework. Unlike traditional test frameworks, these are **documentation structures** rather than code entities—they exist as markdown files and directory conventions, not as compiled classes.

---

## Entity Definitions

### 1. TestScenario

**Description**: A single test case definition that can be executed by the LLM.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | ✅ | Unique identifier (e.g., `TC-MOUSE-001`) |
| `category` | enum | ✅ | One of: MOUSE, KEYBOARD, WINDOW, SCREENSHOT, WORKFLOW, ERROR, VISUAL |
| `priority` | enum | ✅ | One of: P1, P2, P3 |
| `objective` | string | ✅ | What this test verifies |
| `preconditions` | string[] | ✅ | Required state before test execution |
| `steps` | TestStep[] | ✅ | Ordered list of actions to perform |
| `expectedResult` | string | ✅ | What should happen if test passes |
| `passCriteria` | string[] | ✅ | Checkable items for pass/fail determination |

**Storage**: `scenarios/TC-{CATEGORY}-{NNN}.md`

**Example**:
```markdown
# Test Case: TC-MOUSE-001

## Objective
Verify basic mouse movement to specific coordinates on secondary monitor.

## Priority
P1 (Critical)

## Preconditions
- Secondary monitor available and detected
- No obstructions in target area

## Steps
1. Call `screenshot_control` with action="list_monitors" to enumerate displays
2. Parse response to identify secondary monitor bounds
3. Take "before" screenshot of secondary monitor
4. Call `mouse_control` with action="move", x={center_x}, y={center_y}
5. Take "after" screenshot of secondary monitor
6. Visually verify cursor position in screenshot

## Expected Result
Mouse cursor moves to center of secondary monitor without error.

## Pass Criteria
- [ ] list_monitors returns at least 2 monitors
- [ ] Mouse move action completes without error
- [ ] "After" screenshot shows cursor at target location
```

---

### 2. TestStep

**Description**: A single action within a test scenario.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `stepNumber` | integer | ✅ | Order of execution (1-based) |
| `action` | string | ✅ | What to do (natural language or MCP tool call) |
| `tool` | string | ❌ | MCP tool to invoke (if applicable) |
| `parameters` | object | ❌ | Parameters for MCP tool (if applicable) |
| `verification` | string | ❌ | How to verify this step succeeded |

**Storage**: Embedded in TestScenario as numbered list items.

**Relationship**: Many TestSteps belong to one TestScenario.

---

### 3. TestRun

**Description**: A single execution of a test scenario.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `runId` | string | ✅ | Unique run identifier (e.g., `2025-12-08T14:30:00`) |
| `scenarioId` | string | ✅ | Reference to TestScenario.id |
| `startTime` | datetime | ✅ | When execution began |
| `endTime` | datetime | ✅ | When execution completed |
| `status` | enum | ✅ | PASS, FAIL, BLOCKED, SKIPPED |
| `screenshots` | Screenshot[] | ✅ | Visual evidence collected |
| `observations` | string | ✅ | LLM's description of what happened |
| `passCriteriaResults` | object | ✅ | Which criteria passed/failed |

**Storage**: `results/{YYYY-MM-DD}/{TC-ID}/result.md`

**Relationship**: One TestRun references one TestScenario.

---

### 4. Screenshot

**Description**: A captured screen image for verification.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `filename` | string | ✅ | File name (e.g., `before.png`, `after.png`, `step-03.png`) |
| `captureTime` | datetime | ✅ | When screenshot was taken |
| `monitorIndex` | integer | ✅ | Which monitor was captured (0-based) |
| `stepNumber` | integer | ❌ | Which step this screenshot verifies |
| `description` | string | ❌ | LLM's description of screenshot contents |

**Storage**: `results/{YYYY-MM-DD}/{TC-ID}/{filename}.png`

**Relationship**: Many Screenshots belong to one TestRun.

---

### 5. TestReport

**Description**: Aggregate summary of multiple test runs.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `reportDate` | date | ✅ | Date of report generation |
| `category` | string | ❌ | Category filter (if subset) |
| `totalTests` | integer | ✅ | Number of tests included |
| `passed` | integer | ✅ | Tests that passed |
| `failed` | integer | ✅ | Tests that failed |
| `blocked` | integer | ✅ | Tests blocked by preconditions |
| `skipped` | integer | ✅ | Tests intentionally skipped |
| `testRuns` | TestRun[] | ✅ | Individual test results |

**Storage**: `results/{YYYY-MM-DD}/report.md`

---

## State Transitions

### TestRun Status

```text
         ┌─────────────┐
         │  NOT STARTED │
         └──────┬──────┘
                │ (execution begins)
                ▼
         ┌─────────────┐
         │  IN PROGRESS │
         └──────┬──────┘
                │
    ┌───────────┼───────────┬───────────┐
    │           │           │           │
    ▼           ▼           ▼           ▼
┌──────┐   ┌──────┐   ┌─────────┐   ┌─────────┐
│ PASS │   │ FAIL │   │ BLOCKED │   │ SKIPPED │
└──────┘   └──────┘   └─────────┘   └─────────┘

PASS: All pass criteria verified
FAIL: One or more pass criteria not met
BLOCKED: Preconditions not satisfied (e.g., no secondary monitor)
SKIPPED: Intentionally not executed (e.g., dependency failed)
```

---

## Directory Structure

```text
specs/007-llm-integration-testing/
├── scenarios/                    # Test scenario definitions
│   ├── TC-MOUSE-001.md
│   ├── TC-MOUSE-002.md
│   ├── TC-KEYBOARD-001.md
│   └── ...
├── results/                      # Test execution results
│   ├── 2025-12-08/
│   │   ├── report.md            # Daily summary report
│   │   ├── TC-MOUSE-001/
│   │   │   ├── before.png
│   │   │   ├── after.png
│   │   │   └── result.md
│   │   └── TC-KEYBOARD-001/
│   │       ├── before.png
│   │       ├── step-03.png
│   │       ├── after.png
│   │       └── result.md
│   └── 2025-12-09/
│       └── ...
└── templates/                    # Reusable templates
    ├── scenario-template.md
    └── result-template.md
```

---

## Validation Rules

### TestScenario
1. `id` must match pattern: `TC-{CATEGORY}-{NNN}` where NNN is 3 digits
2. `category` must be one of the 7 defined categories
3. `steps` must have at least 1 entry
4. `passCriteria` must have at least 1 entry

### TestRun
1. `endTime` must be >= `startTime`
2. `screenshots` must have at least 1 entry (visual evidence required)
3. `status` cannot be PASS if any `passCriteriaResults` item is false

### Screenshot
1. `filename` must end in `.png`
2. `monitorIndex` must be >= 0

---

## Entity Relationships

```text
┌──────────────────┐
│   TestScenario   │ ─ ─ ─ ─ ┐
│  (scenarios/*.md)│          │ 1:N (one scenario, many runs)
└────────┬─────────┘          │
         │ contains           │
         ▼                    │
┌──────────────────┐          │
│    TestStep      │          │
│   (embedded)     │          │
└──────────────────┘          │
                              │
         ┌────────────────────┘
         │
         ▼
┌──────────────────┐
│     TestRun      │
│ (results/*.md)   │
└────────┬─────────┘
         │ contains
         ▼
┌──────────────────┐
│   Screenshot     │
│ (results/*.png)  │
└──────────────────┘

┌──────────────────┐
│   TestReport     │ ◄──── aggregates many TestRuns
│ (report.md)      │
└──────────────────┘
```

---

## Notes

1. **No Code Entities**: These are all documentation/file structures, not compiled classes
2. **LLM as Executor**: The LLM reads scenarios and executes them—no runtime engine needed
3. **Human-Readable First**: All formats optimized for human readability over machine parsing
4. **Git-Friendly**: Results can be committed or .gitignored as desired
