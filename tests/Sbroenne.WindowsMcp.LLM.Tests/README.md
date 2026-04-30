# Windows MCP Server - LLM Integration Tests

This project contains integration tests for the Windows MCP Server using [pytest-skill-engineering](https://github.com/sbroenne/pytest-skill-engineering) — the successor to `pytest-aitest` for testing AI agents with MCP tools.

## Test Applications

Tests use standard Windows applications as test targets (no custom test harnesses):

- **Notepad** (`notepad.exe`) — Text editing, menu navigation, keyboard shortcuts
- **Calculator** (`calc.exe`) — Keyboard input and UI element interaction
- **Microsoft Paint** (`mspaint.exe`) — Tool selection, color palette, canvas drawing (disabled)

## Prerequisites

### 1. .NET 10 SDK

The MCP server is built automatically when tests start (via a session-scoped fixture).

### 2. Python 3.12+ and uv

Install [uv](https://docs.astral.sh/uv/getting-started/installation/) and Python 3.12+.

### 3. GitHub Copilot SDK Configuration

Tests use the GitHub Copilot SDK via `pytest-skill-engineering[copilot]`.

- In CI, `GITHUB_TOKEN` is used automatically
- Locally, either set `GITHUB_TOKEN` or sign in with `gh auth login`

### 4. Windows Desktop Environment

These tests automate Windows UI elements. They require:
- Windows desktop with UI access
- **NOT suitable for headless CI/CD pipelines**

## Setup

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
uv sync
gh auth login
```

## Running the Tests

```powershell
# Run all scenario tests
uv run pytest test_*.py -v

# Run a specific test file
uv run pytest test_notepad_ui.py -v

# Run a single test
uv run pytest test_calculator_workflow.py::test_calculator_keyboard -v

# Run integration tests
uv run pytest integration/ -v

# Collect all tests (dry run)
uv run pytest --collect-only
```

## Project Structure

```
Sbroenne.WindowsMcp.LLM.Tests/
├── conftest.py                            # Shared fixtures (server, agents, providers)
├── pyproject.toml                         # Python project config and dependencies
├── test_notepad_ui.py                     # Notepad discard/save workflows
├── test_calculator_workflow.py            # Calculator keyboard + UI tests
├── test_window_workflow.py                # Multi-window management
├── test_paint_workflow.py                 # Paint workflows (skipped)
├── test_screenshot_workflow.py            # Screenshot workflows (skipped)
├── integration/                           # Tool-level integration tests
│   ├── test_app_tool_uwp.py              # UWP app launching
│   ├── test_file_dialog.py               # File save dialog handling
│   ├── test_keyboard_mouse.py            # Keyboard/mouse control
│   ├── test_paint_ui.py                  # Paint UI tools
│   ├── test_run_dialog.py                # Win+R dialog
│   ├── test_screenshot.py                # Screenshot control
│   ├── test_window_activate.py           # Window activation
│   └── test_window_management.py         # All 10 window_management actions
├── eval/                                  # Evaluation tests (external servers)
│   ├── test_4sysops_workflow.py
│   ├── test_4sysops_cursortouch.py
│   └── README.md
├── TestResults/                           # Reports (generated, git-ignored)
└── README.md                              # This file
```

## Test Organization

### Scenario Tests (root directory)

Multi-step workflow tests that verify end-to-end user scenarios. Run with `gpt-5.5` through GitHub Copilot.

| Test File | Description |
|-----------|-------------|
| `test_notepad_ui.py` | Notepad text editing with discard and save workflows |
| `test_calculator_workflow.py` | Calculator operations via keyboard and UI clicks |
| `test_window_workflow.py` | Multi-window management across Notepad instances |

### Integration Tests (`integration/`)

Tool-level tests that verify individual MCP tool functionality. Run with `gpt-5.5` only.

| Test File | Tools Covered |
|-----------|---------------|
| `test_window_management.py` | window_management (all 10 actions) |
| `test_keyboard_mouse.py` | keyboard_control, mouse_control |
| `test_file_dialog.py` | file_save |
| `test_screenshot.py` | screenshot_control |
| `test_app_tool_uwp.py` | app (UWP launching) |
| `test_paint_ui.py` | ui_find, ui_click, mouse_control |
| `test_run_dialog.py` | keyboard_control (Win+R) |
| `test_window_activate.py` | app, window_management (activate) |

## Writing Tests

Tests use `pytest-skill-engineering` through the Copilot SDK. The local `aitest_run` fixture delegates to the package's `copilot_eval` fixture and normalizes MCP-prefixed tool names such as `mcp-1-app` back to `app` for assertions.

```python
import pytest
from conftest import assert_quality, assert_tool_called

async def test_example(aitest_run):
    result = await aitest_run(
        agent=agent,
        prompt="Open Notepad and type hello",
    )
    assert_quality(result)
    assert_tool_called(result, "app")
```

### Test Prompts: Use Natural Language

**❌ BAD — Leading the witness:**
```python
prompt="Use window_management with action 'find' and title 'Notepad'"
```

**✅ GOOD — Natural user request:**
```python
prompt="I need to find that Notepad window so I can work with it."
```

### Assertions

```python
# Tool was called
assert_tool_called(result, "app")

# Tool call order
assert_tool_call_order(result, "app", "keyboard_control")

# Tool parameter check
assert_tool_param_matches(result, "keyboard_control", "keys", r"hello")

# Response content
assert re.search(r"notepad", result.final_response, re.IGNORECASE)

# Quality checks
assert_quality(result)
```

## Test Reports

HTML reports are generated when using `--aitest-html` together with an AI summary model:

```powershell
uv run pytest test_notepad_ui.py -v --aitest-summary-model=azure/gpt-5.5-chat --aitest-html=TestResults/report.html
```
