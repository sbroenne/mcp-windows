"""
Run Dialog Test — Launch Apps via Windows Run (Win+R).

Tests launching applications using the Windows Run dialog instead of the app
tool, validating keyboard_control for hotkeys and ui_type for dialog input.

Tools Covered: keyboard_control, window_management, ui_type
"""

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
    assert_tool_called,
)
from pytest_aitest import Agent, ClarificationDetection


def _agent(windows_mcp_server, gpt41_provider):
    return Agent(
        name="gpt41-agent",
        provider=gpt41_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        clarification_detection=ClarificationDetection(enabled=True),
    )


# ---------------------------------------------------------------------------
# Session 1: Launch Notepad via Run Dialog
# ---------------------------------------------------------------------------


@pytest.mark.session("run-dialog-launch")
class TestRunDialogLaunch:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close all Notepad windows if any are open."
        )
        assert_quality(result)

    async def test_open_windows_run_dialog(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Open the Windows Run dialog using the keyboard shortcut Win+R.",
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")
        assert_output_matches(result, r"(?i)(run|dialog|win.*r|opened)")

    async def test_type_notepad_and_launch(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            'Type "notepad" in the Run dialog and press Enter to launch it.',
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")

    async def test_verify_notepad_launched(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Verify that Notepad is now open by finding its window.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(result, r"(?i)(notepad|found|open|visible)")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)
        assert_tool_called(result, "window_management")


# ---------------------------------------------------------------------------
# Session 2: Launch Calculator via Run Dialog
# ---------------------------------------------------------------------------


@pytest.mark.session("run-dialog-calculator")
class TestRunDialogCalculator:
    async def test_cleanup_close_existing_calculator(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close all Calculator windows if any are open."
        )
        assert_quality(result)

    async def test_launch_calculator_via_run(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            'Open the Windows Run dialog with Win+R, type "calc", and press Enter to launch Calculator.',
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")

    async def test_verify_calculator_launched(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Verify that Calculator is now open."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(result, r"(?i)(calculator|calc|found|open)")

    async def test_close_calculator(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Calculator.")
        assert_quality(result)
        assert_output_matches(
            result, r"(?i)(calculator|closed|not open|no.*windows)"
        )
