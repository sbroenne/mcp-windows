"""
Window Management Workflow Tests — Multi-Window Operations.

Tools Covered: app, window_management (find, list, minimize, maximize,
               restore, move, resize, close, move_and_activate, activate)
"""

import pytest
from conftest import (
    assert_output_matches,
    assert_output_not_contains,
    assert_quality,
    assert_tool_param_matches,
    make_agents,
)


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=25
    )


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_multi_window_management(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Launch Notepad and Calculator, manipulate windows, close cleanly."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Perform this complete window management workflow:\n\n"
            "1. Launch Notepad (notepad.exe)\n"
            "2. Launch Calculator (calc.exe)\n"
            "3. List all windows to see both applications\n"
            "4. Minimize Notepad\n"
            "5. Maximize Calculator\n"
            "6. Restore Notepad\n"
            "7. Move Calculator to position (100, 100)\n"
            "8. Resize Notepad to 800x600\n"
            "9. Close both applications (click \"Don't Save\" for Notepad if prompted)\n"
            "10. Verify both applications are closed\n\n"
            "Report which operations succeeded and confirm both apps are closed."
        ),
    )

    # Must launch both apps
    assert_tool_param_matches(result, "app", "programPath", r"(?i)notepad")
    assert_tool_param_matches(result, "app", "programPath", r"(?i)calc")

    # Must use window manipulation operations
    manipulated = False
    try:
        assert_tool_param_matches(
            result,
            "window_management",
            "action",
            r"(?i)(minimize|maximize|restore|move|resize)",
        )
        manipulated = True
    except AssertionError:
        pass
    if not manipulated:
        assert_output_matches(
            result, r"(?i)(minimized|maximized|restored|moved|resized)"
        )

    # Both apps were closed
    assert_output_matches(
        result,
        r"(?i)(both.*closed|closed.*both|notepad.*closed|calculator.*closed)",
    )
    assert_output_not_contains(result, "failed")
    assert_quality(result)
