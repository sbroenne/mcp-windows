# LLM Integration Testing Templates

This directory contains reusable templates for the LLM-based integration testing framework.

## Templates

| Template | Purpose |
|----------|---------|
| [scenario-template.md](scenario-template.md) | Base template for all test scenarios |
| [result-template.md](result-template.md) | Template for individual test results |
| [report-template.md](report-template.md) | Template for daily aggregate reports |
| [monitor-preamble.md](monitor-preamble.md) | Standard monitor detection snippet |

---

## Scenario Naming Conventions

### Test Case ID Format

```
TC-{CATEGORY}-{NNN}
```

Where:
- **TC** = Test Case prefix
- **{CATEGORY}** = One of: MOUSE, KEYBOARD, WINDOW, SCREENSHOT, WORKFLOW, ERROR, VISUAL
- **{NNN}** = 3-digit sequential number (001, 002, etc.)

### Examples

| ID | Category | Description |
|----|----------|-------------|
| TC-MOUSE-001 | MOUSE | Move cursor to absolute position |
| TC-KEYBOARD-007 | KEYBOARD | Keyboard shortcut Ctrl+A |
| TC-WINDOW-005 | WINDOW | Activate window by handle |
| TC-SCREENSHOT-003 | SCREENSHOT | Capture specific monitor by index |
| TC-WORKFLOW-010 | WORKFLOW | Full UI interaction sequence |
| TC-ERROR-004 | ERROR | Screenshot during secure desktop |
| TC-VISUAL-002 | VISUAL | Detect text content change |

---

## File Naming

### Scenario Files
```
scenarios/TC-{CATEGORY}-{NNN}.md
```

### Result Directories
```
results/{YYYY-MM-DD}/TC-{CATEGORY}-{NNN}/
├── before.png
├── after.png
├── step-{NN}.png (if multi-step)
└── result.md
```

### Daily Reports
```
results/{YYYY-MM-DD}/report.md
```

---

## Screenshot Naming Conventions

Screenshots captured during test execution follow these conventions:

### Before/After Pattern

| Filename | Purpose |
|----------|---------|
| `before.png` | Screen state before the action under test |
| `after.png` | Screen state after the action under test |

### Multi-Step Workflows

For workflows with multiple actions:
```
step-01.png    # After step 1
step-02.png    # After step 2
step-03.png    # After step 3
...
```

### Context Screenshots

| Filename | Purpose |
|----------|---------|
| `context.png` | General context (for informational captures) |
| `error.png` | Screenshot when error occurred |
| `after-{action}.png` | Named action result (e.g., `after-minimize.png`) |

### Monitor-Specific Captures

When capturing specific monitors:
```
monitor-0.png  # Primary monitor
monitor-1.png  # Secondary monitor
monitor-all.png # All monitors combined
```

### Visual Verification Screenshots

For visual comparison tests (TC-VISUAL-*):
```
before.png     # Initial state (baseline)
after.png      # State after action
diff.png       # (Optional) Visual diff overlay
```

---

## Category Definitions

| Category | Description | Target App | Example Tools |
|----------|-------------|------------|---------------|
| **MOUSE** | Mouse movement, clicks, scrolling, drag | None/Any | `mouse_control` |
| **KEYBOARD** | Text input, key presses, shortcuts | Notepad | `keyboard_control` |
| **WINDOW** | Window enumeration, manipulation | Notepad | `window_management` |
| **SCREENSHOT** | Screen/monitor/region capture | None | `screenshot_control` |
| **WORKFLOW** | Multi-step sequences | Notepad/Calculator | Multiple tools |
| **ERROR** | Error handling and edge cases | Various | Any tool |
| **VISUAL** | Visual change detection | Various | `screenshot_control` |

---

## Priority Levels

| Priority | Meaning | Example |
|----------|---------|---------|
| **P1** | Critical - Core functionality | Basic mouse move, keyboard type |
| **P2** | Important - Common usage | Multi-step workflows, shortcuts |
| **P3** | Nice-to-have - Edge cases | Advanced error handling |

---

## Target Applications

Tests use these Windows 11 built-in applications:

| Application | Used For |
|-------------|----------|
| **Notepad** | Keyboard input, text verification, window manipulation |
| **Calculator** | Button clicks, UI interaction, visual feedback |

Both are always available on Windows 11 with no installation required.

---

## Creating a New Scenario

1. Copy `scenario-template.md` to `../scenarios/TC-{CATEGORY}-{NNN}.md`
2. Fill in all metadata fields
3. Include the monitor detection preamble from `monitor-preamble.md`
4. Define clear pass criteria
5. Validate scenario structure before committing

### Checklist for New Scenarios

- [ ] ID follows `TC-{CATEGORY}-{NNN}` format
- [ ] Category is one of the 7 valid categories
- [ ] Priority is P1, P2, or P3
- [ ] Target monitor is "Secondary (fallback: Primary)"
- [ ] Preconditions list is complete
- [ ] Steps include monitor detection preamble
- [ ] Steps include before/after screenshots
- [ ] Pass criteria are checkable/observable
- [ ] Expected result is clear and measurable

---

## Executing a Scenario

1. Open scenario file in VS Code
2. In Copilot chat, paste the scenario content
3. Ask Copilot to execute each step
4. Copilot will invoke MCP tools and capture screenshots
5. Review results against pass criteria
6. Save results to `results/{YYYY-MM-DD}/{TC-ID}/`

See [quickstart.md](../quickstart.md) for detailed execution instructions.
