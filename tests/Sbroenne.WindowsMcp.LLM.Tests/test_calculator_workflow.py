"""
Calculator Workflow Tests — Keyboard and UI Automation.

Tools Covered: app, keyboard_control, ui_find, ui_click, ui_read
"""

import re

import pytest
from conftest import (
    assert_output_matches,
    assert_output_not_contains,
    assert_quality,
    assert_tool_call_order,
    assert_tool_called,
    assert_tool_param_matches,
    make_agents,
)


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=20
    )


# ---------------------------------------------------------------------------
# Session 1: Calculator Keyboard Workflow
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_calculator_keyboard(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Calculator, type calculation using keyboard, read result."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Perform a calculation using keyboard input:\n\n"
            "1. Launch Calculator (calc.exe)\n"
            "2. Get the window handle\n"
            "3. Use keyboard input to type: 25 + 17 =\n"
            "4. Read the result from the Calculator display using UI automation\n"
            "5. Report the result (should be 42)\n"
            "6. Close Calculator\n\n"
            'Report "Calculation result: [value]" and confirm Calculator was closed.'
        ),
    )

    assert_tool_call_order(result, "app", "keyboard_control")
    assert_tool_param_matches(
        result, "keyboard_control", "action", r"(?i)(type|press|sequence)"
    )
    assert_output_matches(result, r"(?i)(result|answer|equals|display).*(\d+|42)")
    assert_output_matches(result, r"(?i)(closed|calculator.*(closed|exited)|verified)")
    assert_output_not_contains(result, "failed")
    assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Calculator UI Click Workflow
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_calculator_ui_click(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Calculator, click buttons to perform calculation."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Perform a calculation using UI button clicks (not keyboard):\n\n"
            "1. Launch Calculator (calc.exe)\n"
            "2. Get the window handle\n"
            "3. Use UI automation to find and click the following buttons in order:\n"
            '   - Click "5" button\n'
            '   - Click "×" (multiply) button\n'
            '   - Click "8" button\n'
            '   - Click "=" button\n'
            "4. Read the result from the display (should be 40)\n"
            "5. Close Calculator\n\n"
            'Report "Calculation result: [value]" and confirm Calculator was closed.'
        ),
    )

    assert_tool_call_order(result, "app", "ui_click")
    assert_tool_param_matches(result, "ui_click", "controlType", r"(?i)button")
    assert_output_matches(result, r"(?i)(result|answer|equals|display).*(\d+|40)")
    assert_output_matches(result, r"(?i)(closed|calculator.*(closed|exited)|verified)")
    assert_output_not_contains(result, "failed")
    assert_quality(result)
