# Annotated Example: TC-MOUSE-001

This document shows a complete test scenario with annotations explaining each section.

---

<!--
  ANNOTATION: File Naming
  
  The file MUST be named following the pattern: TC-{CATEGORY}-{NNN}.md
  - TC = Test Case prefix (always present)
  - CATEGORY = One of: MOUSE, KEYBOARD, WINDOW, SCREENSHOT, WORKFLOW, ERROR, VISUAL
  - NNN = Three-digit sequential number (001, 002, etc.)
  
  This file would be: scenarios/TC-MOUSE-001.md
-->

# Test Case: TC-MOUSE-001

<!--
  ANNOTATION: Header
  
  The title MUST match the filename and ID in the metadata.
  Format: "Test Case: {ID}"
-->

## Metadata

<!--
  ANNOTATION: Metadata Table
  
  This table provides quick-reference information for the test.
  Required fields (*): ID, Category, Priority
  
  The table format allows for easy parsing and scanning.
-->

| Field | Value |
|-------|-------|
| **ID** | TC-MOUSE-001 |
| **Category** | MOUSE |
| **Priority** | P1 |
| **Target App** | None |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |
| **Tools** | screenshot_control, mouse_control |
| **Dependencies** | None |

<!--
  ANNOTATION: Field Details
  
  - ID: Must match filename and header. Pattern: TC-{CATEGORY}-{NNN}
  - Category: Determines which test suite this belongs to
  - Priority: P1=Critical (MVP), P2=Important, P3=Nice-to-have
  - Target App: Application needed for the test (None if just desktop)
  - Target Monitor: Where to run the test (Secondary preferred)
  - Timeout: Maximum execution time before failure
  - Tools: MCP tools exercised (from contracts/scenario-schema.json)
  - Dependencies: Other tests that must pass first (or None)
-->

## Objective

<!--
  ANNOTATION: Objective
  
  REQUIRED. One or two sentences explaining WHAT this test verifies.
  Should answer: "What capability does this test confirm works?"
  
  Good: "Verify that the mouse cursor moves to specified absolute coordinates."
  Bad: "Test mouse movement." (too vague)
-->

Verify that the mouse cursor moves to specified absolute coordinates on the secondary monitor, and that the cursor position can be visually confirmed via screenshot.

## Preconditions

<!--
  ANNOTATION: Preconditions
  
  REQUIRED. Checklist of requirements that must be true BEFORE execution.
  Use checkbox format so they can be checked off during execution.
  
  Common preconditions:
  - Secondary monitor available
  - Target application open
  - No blocking dialogs
  - Specific application state
-->

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] No obstructions in target area (center of monitor)
- [ ] No full-screen applications blocking
- [ ] Mouse cursor not captured by any application

## Steps

<!--
  ANNOTATION: Steps Section
  
  REQUIRED. Ordered list of actions to perform.
  Each step should specify:
  - What to do (action description)
  - Which MCP tool to use (if applicable)
  - Parameters for the tool call
  - Purpose or verification (why this step matters)
  
  Use ### for step headings to create clear structure.
-->

### Step 1: Detect and Target Secondary Monitor

<!--
  ANNOTATION: Step 1 Pattern
  
  Most tests should start by detecting available monitors.
  This establishes the target coordinates for subsequent actions.
-->

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds for coordinate calculation

**Record the following from the response**:
- Secondary monitor index (usually 0 or 1)
- Monitor bounds: `x`, `y`, `width`, `height`
- Calculate center: `center_x = x + width/2`, `center_y = y + height/2`

### Step 2: Before Screenshot

<!--
  ANNOTATION: Before Screenshot
  
  ALWAYS take a "before" screenshot to document initial state.
  This is essential for visual comparison and debugging.
  
  Include cursor (`includeCursor=true`) for mouse tests.
-->

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"monitor"`
- `monitorIndex`: `{secondary_monitor_index}`
- `includeCursor`: `true`

**Save As**: `before.png`

**Document**: Current cursor position in the screenshot.

### Step 3: Move Mouse to Center

<!--
  ANNOTATION: Main Action Step
  
  This is the core action being tested.
  Document exactly what parameters to use and what success looks like.
-->

**MCP Tool**: `mouse_control`  
**Action**: `move`  
**Parameters**:
- `x`: `{center_x}` (calculated from Step 1)
- `y`: `{center_y}` (calculated from Step 1)

**Expected Response**: Success with no error.

### Step 4: After Screenshot

<!--
  ANNOTATION: After Screenshot
  
  ALWAYS take an "after" screenshot to document final state.
  This provides visual evidence for pass/fail determination.
-->

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"monitor"`
- `monitorIndex`: `{secondary_monitor_index}`
- `includeCursor`: `true`

**Save As**: `after.png`

### Step 5: Visual Verification

<!--
  ANNOTATION: Verification Step
  
  Describe HOW to verify the action succeeded.
  This guides the LLM in analyzing the screenshots.
-->

Compare `before.png` and `after.png`:

1. **Cursor Position**: Cursor should be at center of monitor in "after" screenshot
2. **Cursor Movement**: Cursor position should differ between screenshots
3. **No Side Effects**: Desktop should otherwise be unchanged

## Expected Result

<!--
  ANNOTATION: Expected Result
  
  REQUIRED. Describe the success state in complete sentences.
  This is what the test documentation will say when the test passes.
-->

The mouse cursor successfully moves to the center of the secondary monitor. The "before" screenshot shows the cursor at its original position, and the "after" screenshot shows the cursor at the calculated center coordinates. The move action returns success with no error.

## Pass Criteria

<!--
  ANNOTATION: Pass Criteria
  
  REQUIRED. At least one criterion.
  Use checkbox format for each individual criterion.
  Each should be independently verifiable.
  
  Common criteria:
  - Tool returns success
  - Visual change observed
  - Response data matches expected values
-->

- [ ] `list_monitors` returns at least 2 monitors (or 1 if fallback accepted)
- [ ] `move` action completes without error response
- [ ] "Before" screenshot captured successfully
- [ ] "After" screenshot captured successfully
- [ ] Cursor visible in "after" screenshot
- [ ] Cursor position in "after" matches target coordinates (within tolerance)
- [ ] Cursor moved from original position (before ≠ after)

## Failure Indicators

<!--
  ANNOTATION: Failure Indicators
  
  OPTIONAL but recommended. Common failure patterns.
  Helps with debugging when tests fail.
-->

- Cursor not visible in "after" screenshot
- Cursor at wrong coordinates (not center of monitor)
- Cursor position unchanged between screenshots
- `move` action returns error
- Secondary monitor not detected
- Screenshot capture failed

## Notes

<!--
  ANNOTATION: Notes
  
  OPTIONAL. Additional context, known issues, tips.
  Include anything that helps future test executors.
-->

- **Coordinate System**: Windows uses absolute screen coordinates. Secondary monitor coordinates include the offset from primary monitor.
- **DPI Scaling**: High DPI displays may affect coordinate calculations. Use monitor bounds from `list_monitors` for accurate targeting.
- **Cursor Visibility**: Some remote desktop or VM environments may not show the cursor in screenshots.
- **Monitor Configuration**: Secondary monitor position varies by user setup. Always use detected bounds, not hardcoded values.

---

## Summary: Scenario Structure

```
TC-{CATEGORY}-{NNN}.md
├── # Test Case: {ID}           <- Header (matches filename)
├── ## Metadata                 <- Quick-reference table
├── ## Objective *              <- What does this test verify?
├── ## Preconditions *          <- What must be true before running?
├── ## Steps *                  <- What actions to perform?
│   ├── ### Step 1              <- First action
│   ├── ### Step 2              <- Second action
│   └── ...                     <- Additional steps
├── ## Expected Result *        <- What should happen?
├── ## Pass Criteria *          <- How do we know it passed?
├── ## Failure Indicators       <- What indicates failure?
└── ## Notes                    <- Additional context
```

**Required sections marked with ***
