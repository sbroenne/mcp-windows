# Test Case: TC-{CATEGORY}-{NNN}

<!--
  This is the template for LLM-based integration test scenarios.
  Copy this file to scenarios/TC-{CATEGORY}-{NNN}.md and fill in all sections.
  
  Categories: MOUSE, KEYBOARD, WINDOW, SCREENSHOT, WORKFLOW, ERROR, VISUAL
  
  Fields marked with * are REQUIRED per data-model.md
  See contracts/scenario-schema.json for validation rules
-->

## Metadata *

<!--
  Fields marked with * are REQUIRED. All others are optional.
  Priority MUST be P1, P2, or P3 (P0 is NOT valid per schema).
-->

| Field | Value |
|-------|-------|
| **ID** * | TC-{CATEGORY}-{NNN} |
| **Category** * | {MOUSE/KEYBOARD/WINDOW/SCREENSHOT/WORKFLOW/ERROR/VISUAL} |
| **Priority** * | {P1/P2/P3} |
| **Target App** | {Notepad/Calculator/None} |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** (optional) | {list of MCP tools used, e.g., mouse_control, keyboard_control} |
| **Dependencies** (optional) | {list of prerequisite test case IDs, or None} |

## Objective *

{Brief description of what this test verifies - one or two sentences}

## Preconditions *

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] {Target app} is installed (Windows 11 built-in)
- [ ] No modal dialogs blocking the target area
- [ ] {Add any test-specific preconditions}

## Steps *

<!--
  Each step should specify:
  - stepNumber (implied by heading)
  - action: What to do
  - tool: MCP tool name (if applicable)
  - parameters: Tool parameters (if applicable)
  - verification: How to verify success (if applicable)
-->

### Step 1: Setup
{Describe any setup needed before the main action}

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for targeting

### Step 2: Before Screenshot
{Capture initial state}

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 3: Execute Action
{The main action being tested}

**MCP Tool**: `{tool_name}`  
**Action**: `{action}`  
**Parameters**: 
- `param1`: {value}
- `param2`: {value}

### Step 4: After Screenshot
{Capture final state}

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 5: Verification
{Describe what to look for in the screenshots}

## Expected Result *

{Describe the expected state after the action completes}

## Pass Criteria *

<!--
  At least one criterion is REQUIRED
  Each should be checkable/observable
-->

- [ ] {Criterion 1 - checkable/observable}
- [ ] {Criterion 2 - checkable/observable}
- [ ] {Criterion 3 - checkable/observable}
- [ ] Tool returns success (no error in response)
- [ ] "After" screenshot shows expected state change

## Failure Indicators

- {What would indicate a failure}
- {Error messages to watch for}
- {Visual states that indicate failure}

## Notes

{Any additional context, known issues, or special handling instructions}
