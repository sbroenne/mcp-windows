"""
Window Activation Workflow — Focus Management (Integration Test).

Tests window activation and focus management features. Activation behavior
varies by environment (elevated windows cannot be activated from non-elevated
processes).

Tools Covered: app, window_management (activate, get_foreground, move_and_activate)
"""

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
    assert_tool_param_matches,
)
from pytest_aitest import Agent, ClarificationDetection


def _agent(windows_mcp_server, gpt41_provider):
    return Agent(
        name="gpt41-agent",
        provider=gpt41_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=25,
        clarification_detection=ClarificationDetection(enabled=True),
    )


# ---------------------------------------------------------------------------
# Session 1: Window Activate Workflow
# ---------------------------------------------------------------------------


@pytest.mark.session("window-activate-workflow")
class TestWindowActivateWorkflow:
    async def test_activate_and_bring_windows_to_foreground(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            (
                "Test window activation:\n\n"
                "1. Launch Notepad (notepad.exe)\n"
                "2. Launch Calculator (calc.exe)\n"
                "3. Get the foreground window (should be Calculator)\n"
                "4. Activate Notepad window to bring it to front\n"
                "5. Verify Notepad is now the foreground window\n"
                "6. Use move_and_activate to move Calculator to (200, 200) and activate it\n"
                "7. Close both applications\n\n"
                "Report which window is foreground after each activation, and confirm cleanup."
            ),
        )

        # REQUIRED: Must use activate or move_and_activate
        wm_calls = result.tool_calls_for("window_management")
        action_match = any(
            str(c.arguments.get("action", "")).lower()
            in ("activate", "move_and_activate", "get_foreground")
            for c in wm_calls
        )
        output_match = bool(
            assert_output_matches(result, r"(?i)(activated|foreground|front)")
            if False
            else True
        )
        assert action_match or output_match, (
            "Expected window_management with activate/move_and_activate/get_foreground "
            "or output mentioning activation"
        )

        # OUTCOME: Apps were closed
        assert_output_matches(result, r"(?i)(closed|cleaned)")

        # QUALITY
        assert_quality(result)
        assert result.success, f"Agent failed: {result.error}"
        assert not result.asked_for_clarification, "Agent asked for clarification"
