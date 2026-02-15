"""
Screenshot Workflow Tests — Screenshot Capture Operations (DISABLED).

Tools Covered: screenshot_control, window_management, app
"""

import pytest
from conftest import (
    assert_output_matches,
    assert_quality,
    assert_tool_call_order,
    assert_tool_called,
    assert_tool_param_matches,
    make_agents,
)


pytestmark = pytest.mark.skip(reason="Screenshot workflow tests disabled — environment-specific")


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=20
    )


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_desktop_screenshot(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Capture desktop and describe content."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Capture and analyze the desktop:\n\n"
            "1. Take a screenshot of the primary screen\n"
            "2. Describe what you see in the screenshot (desktop icons, taskbar, any open windows)\n"
            "3. List available monitors using screenshot_control list_monitors action\n"
            "4. Report the monitor configuration\n\n"
            'Report "Desktop captured" and describe at least 3 visible elements.'
        ),
    )

    assert_tool_called(result, "screenshot_control")
    assert_output_matches(
        result, r"(?i)(desktop|taskbar|screen|icon|window|captured|monitor)"
    )
    assert_output_matches(
        result, r"(?i)(1\.|2\.|3\.|first|second|third|taskbar|icon|start)"
    )
    assert_quality(result)


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_window_screenshot(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Notepad, capture its window specifically, close."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Capture a specific window's screenshot:\n\n"
            "1. Launch Notepad (notepad.exe)\n"
            '2. Type some text: "Screenshot test content"\n'
            "3. Get the window handle for Notepad\n"
            "4. Take a screenshot specifically of the Notepad window (using window target)\n"
            "5. Describe what you see in the Notepad screenshot\n"
            "6. Close Notepad (click \"Don't Save\" if prompted)\n\n"
            'Report "Window captured" and describe the Notepad content you see.'
        ),
    )

    assert_tool_call_order(result, "app", "screenshot_control")
    assert_tool_param_matches(
        result, "screenshot_control", "target", r"(?i)window"
    )
    assert_output_matches(
        result, r"(?i)(notepad|text|content|screenshot.*test|captured)"
    )
    assert_output_matches(result, r"(?i)(closed|notepad.*(closed|exited))")
    assert_quality(result)
