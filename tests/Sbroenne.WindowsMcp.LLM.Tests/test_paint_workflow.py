"""
Paint Workflow Tests — UI Automation and Drawing (DISABLED).

Tools Covered: app, ui_find, ui_click, mouse_control, screenshot_control
"""

import pytest
from conftest import (
    assert_output_matches,
    assert_quality,
    assert_tool_call_order,
    assert_tool_called,
    make_agents,
)


pytestmark = pytest.mark.skip(reason="Paint workflow tests disabled — environment-specific")


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=20
    )


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_paint_draw_shape_and_capture(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Paint, find canvas, draw a shape, take screenshot, close."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "1. Launch Microsoft Paint (mspaint.exe)\n"
            "2. Get the window handle for Paint\n"
            "3. Use UI automation to find the canvas element\n"
            "4. Draw a simple line or shape on the canvas using mouse drag operations\n"
            "5. Take a screenshot of the Paint window as proof\n"
            "6. Close Paint and discard changes (click \"Don't Save\" if prompted)\n"
            "7. Verify Paint is closed\n\n"
            'Report "VERIFIED: Drawing completed and Paint closed" when done.'
        ),
    )

    assert_tool_call_order(result, "app", "screenshot_control")
    assert result.tool_was_called("screenshot_control") or \
        any("screenshot" in r.lower() and "captured" in r.lower() for r in result.all_responses)
    assert_output_matches(
        result, r"(?i)(verified|paint.*(closed|not found)|no.*paint.*window)"
    )
    assert_quality(result)


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_paint_ui_exploration(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Paint, explore UI tree, click a toolbar button."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "1. Launch Microsoft Paint (mspaint.exe)\n"
            "2. Get the window handle\n"
            "3. Get the UI element tree for Paint to understand its structure\n"
            '4. Find and click the "Pencil" or "Brushes" button in the toolbar\n'
            "5. Verify the tool was selected (the UI should respond)\n"
            "6. Close Paint and discard any changes\n\n"
            "Report what toolbar button you clicked and confirm Paint was closed."
        ),
    )

    names = [c.name for c in result.all_tool_calls]
    launched_then_ui = (
        "app" in names
        and ("ui_click" in names or "ui_find" in names)
    )
    assert launched_then_ui

    assert result.tool_was_called("ui_click") or result.tool_was_called("ui_find") or \
        any(re.search(r"(?i)(clicked|selected|found).*(pencil|brush|tool)", r) for r in result.all_responses)
    assert_output_matches(result, r"(?i)(closed|paint.*(closed|not found|exited))")
    assert_quality(result)
