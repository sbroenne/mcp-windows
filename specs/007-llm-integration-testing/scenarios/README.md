# Test Scenarios Index

This directory contains all test scenario definitions for the LLM-Based Integration Testing Framework.

## Summary

| Category | Count | ID Range |
|----------|-------|----------|
| MOUSE | 12 | TC-MOUSE-001 to TC-MOUSE-012 |
| KEYBOARD | 15 | TC-KEYBOARD-001 to TC-KEYBOARD-015 |
| WINDOW | 14 | TC-WINDOW-001 to TC-WINDOW-014 |
| SCREENSHOT | 10 | TC-SCREENSHOT-001 to TC-SCREENSHOT-010 |
| VISUAL | 5 | TC-VISUAL-001 to TC-VISUAL-005 |
| WORKFLOW | 10 | TC-WORKFLOW-001 to TC-WORKFLOW-010 |
| ERROR | 8 | TC-ERROR-001 to TC-ERROR-008 |
| **Total** | **74** | |

## Scenarios by Category

### Mouse Control (12 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-MOUSE-001](TC-MOUSE-001.md) | Move cursor to absolute position | P1 |
| [TC-MOUSE-002](TC-MOUSE-002.md) | Move cursor to screen corners | P1 |
| [TC-MOUSE-003](TC-MOUSE-003.md) | Single left click | P1 |
| [TC-MOUSE-004](TC-MOUSE-004.md) | Move and click combined | P2 |
| [TC-MOUSE-005](TC-MOUSE-005.md) | Double-click action | P2 |
| [TC-MOUSE-006](TC-MOUSE-006.md) | Right-click context menu | P2 |
| [TC-MOUSE-007](TC-MOUSE-007.md) | Middle-click action | P3 |
| [TC-MOUSE-008](TC-MOUSE-008.md) | Scroll up | P1 |
| [TC-MOUSE-009](TC-MOUSE-009.md) | Scroll down | P1 |
| [TC-MOUSE-010](TC-MOUSE-010.md) | Horizontal scroll | P3 |
| [TC-MOUSE-011](TC-MOUSE-011.md) | Mouse drag operation | P2 |
| [TC-MOUSE-012](TC-MOUSE-012.md) | Click with modifier key | P2 |

### Keyboard Control (15 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-KEYBOARD-001](TC-KEYBOARD-001.md) | Type simple text | P1 |
| [TC-KEYBOARD-002](TC-KEYBOARD-002.md) | Type special characters | P1 |
| [TC-KEYBOARD-003](TC-KEYBOARD-003.md) | Press Enter key | P1 |
| [TC-KEYBOARD-004](TC-KEYBOARD-004.md) | Press Tab key | P2 |
| [TC-KEYBOARD-005](TC-KEYBOARD-005.md) | Press Escape key | P2 |
| [TC-KEYBOARD-006](TC-KEYBOARD-006.md) | Press function key F1 | P2 |
| [TC-KEYBOARD-007](TC-KEYBOARD-007.md) | Keyboard shortcut Ctrl+A | P1 |
| [TC-KEYBOARD-008](TC-KEYBOARD-008.md) | Keyboard shortcut Ctrl+C | P1 |
| [TC-KEYBOARD-009](TC-KEYBOARD-009.md) | Keyboard shortcut Ctrl+V | P1 |
| [TC-KEYBOARD-010](TC-KEYBOARD-010.md) | Keyboard shortcut Alt+Tab | P2 |
| [TC-KEYBOARD-011](TC-KEYBOARD-011.md) | Keyboard shortcut Win+D | P2 |
| [TC-KEYBOARD-012](TC-KEYBOARD-012.md) | Hold and release key | P2 |
| [TC-KEYBOARD-013](TC-KEYBOARD-013.md) | Key sequence/combo | P2 |
| [TC-KEYBOARD-014](TC-KEYBOARD-014.md) | Arrow key navigation | P2 |
| [TC-KEYBOARD-015](TC-KEYBOARD-015.md) | Get keyboard layout | P3 |

### Window Management (14 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-WINDOW-001](TC-WINDOW-001.md) | List all windows | P1 |
| [TC-WINDOW-002](TC-WINDOW-002.md) | Find window by title | P1 |
| [TC-WINDOW-003](TC-WINDOW-003.md) | Find window by partial title | P1 |
| [TC-WINDOW-004](TC-WINDOW-004.md) | Get foreground window | P1 |
| [TC-WINDOW-005](TC-WINDOW-005.md) | Activate window by handle | P1 |
| [TC-WINDOW-006](TC-WINDOW-006.md) | Minimize window | P2 |
| [TC-WINDOW-007](TC-WINDOW-007.md) | Maximize window | P2 |
| [TC-WINDOW-008](TC-WINDOW-008.md) | Restore window | P2 |
| [TC-WINDOW-009](TC-WINDOW-009.md) | Move window to position | P1 |
| [TC-WINDOW-010](TC-WINDOW-010.md) | Resize window | P1 |
| [TC-WINDOW-011](TC-WINDOW-011.md) | Set window bounds | P2 |
| [TC-WINDOW-012](TC-WINDOW-012.md) | Close window | P2 |
| [TC-WINDOW-013](TC-WINDOW-013.md) | Wait for window to appear | P2 |
| [TC-WINDOW-014](TC-WINDOW-014.md) | Filter windows by process name | P2 |

### Screenshot Capture (10 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-SCREENSHOT-001](TC-SCREENSHOT-001.md) | Capture primary screen | P1 |
| [TC-SCREENSHOT-002](TC-SCREENSHOT-002.md) | List available monitors | P1 |
| [TC-SCREENSHOT-003](TC-SCREENSHOT-003.md) | Capture specific monitor by index | P1 |
| [TC-SCREENSHOT-004](TC-SCREENSHOT-004.md) | Capture rectangular region | P2 |
| [TC-SCREENSHOT-005](TC-SCREENSHOT-005.md) | Capture with cursor included | P1 |
| [TC-SCREENSHOT-006](TC-SCREENSHOT-006.md) | Capture window by handle | P2 |
| [TC-SCREENSHOT-007](TC-SCREENSHOT-007.md) | Capture with invalid monitor index | P2 |
| [TC-SCREENSHOT-008](TC-SCREENSHOT-008.md) | Capture region with zero dimensions | P3 |
| [TC-SCREENSHOT-009](TC-SCREENSHOT-009.md) | Capture region extending beyond screen | P3 |
| [TC-SCREENSHOT-010](TC-SCREENSHOT-010.md) | Rapid consecutive captures | P3 |

### Visual Verification (5 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-VISUAL-001](TC-VISUAL-001.md) | Detect window position change | P1 |
| [TC-VISUAL-002](TC-VISUAL-002.md) | Detect text content change | P1 |
| [TC-VISUAL-003](TC-VISUAL-003.md) | Detect no change - negative test | P2 |
| [TC-VISUAL-004](TC-VISUAL-004.md) | Detect button state change | P2 |
| [TC-VISUAL-005](TC-VISUAL-005.md) | Detect window close | P2 |

### Workflow Tests (10 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-WORKFLOW-001](TC-WORKFLOW-001.md) | Find and activate window | P2 |
| [TC-WORKFLOW-002](TC-WORKFLOW-002.md) | Move window and verify position | P2 |
| [TC-WORKFLOW-003](TC-WORKFLOW-003.md) | Type text in window | P2 |
| [TC-WORKFLOW-004](TC-WORKFLOW-004.md) | Click button and verify state | P2 |
| [TC-WORKFLOW-005](TC-WORKFLOW-005.md) | Open application via keyboard | P2 |
| [TC-WORKFLOW-006](TC-WORKFLOW-006.md) | Resize and screenshot window | P2 |
| [TC-WORKFLOW-007](TC-WORKFLOW-007.md) | Copy-paste workflow | P2 |
| [TC-WORKFLOW-008](TC-WORKFLOW-008.md) | Window cascade manipulation | P2 |
| [TC-WORKFLOW-009](TC-WORKFLOW-009.md) | Drag and drop simulation | P2 |
| [TC-WORKFLOW-010](TC-WORKFLOW-010.md) | Full UI interaction sequence | P2 |

### Error Handling (8 scenarios)

| ID | Description | Priority |
|----|-------------|----------|
| [TC-ERROR-001](TC-ERROR-001.md) | Invalid mouse coordinates | P2 |
| [TC-ERROR-002](TC-ERROR-002.md) | Window action on invalid handle | P2 |
| [TC-ERROR-003](TC-ERROR-003.md) | Type text with no focused input | P2 |
| [TC-ERROR-004](TC-ERROR-004.md) | Screenshot during secure desktop | P2 |
| [TC-ERROR-005](TC-ERROR-005.md) | Timeout on window wait | P2 |
| [TC-ERROR-006](TC-ERROR-006.md) | Close already-closed window | P2 |
| [TC-ERROR-007](TC-ERROR-007.md) | Invalid key name | P2 |
| [TC-ERROR-008](TC-ERROR-008.md) | Keyboard combo with invalid modifier | P2 |

## Priority Distribution

| Priority | Count | Percentage |
|----------|-------|------------|
| P1 (Critical) | 23 | 31% |
| P2 (Important) | 44 | 59% |
| P3 (Nice-to-have) | 7 | 10% |

## Quick Links

- [Scenario Template](../templates/scenario-template.md)
- [Contributing Guide](../docs/CONTRIBUTING-TESTS.md)
- [Quickstart Guide](../quickstart.md)
- [Workflow Guide](../templates/workflow-guide.md)

## Creating New Scenarios

1. Copy the template: `cp ../templates/scenario-template.md TC-{CATEGORY}-{NNN}.md`
2. Fill in all required sections
3. Follow naming convention: `TC-{CATEGORY}-{NNN}.md`
4. Add to this index

See [CONTRIBUTING-TESTS.md](../docs/CONTRIBUTING-TESTS.md) for detailed guidance.
