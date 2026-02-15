"""
App Tool UWP Test — Tests launching UWP/Windows Store apps via the app() tool.

Validates dynamic stub detection for apps like Calculator that exit immediately
and spawn a separate UWP process under ApplicationFrameHost.

Tools Covered: app, window_management
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
# Session 1: Launch Calculator via App Tool (UWP Stub Test)
# ---------------------------------------------------------------------------


@pytest.mark.session("calculator-via-app-tool")
class TestCalculatorViaAppTool:
    async def test_cleanup_close_existing_calculator(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Close any existing Calculator windows that may be open.",
        )
        assert_quality(result)

    async def test_launch_calculator_using_app_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            (
                'Launch Calculator using the app tool with programPath set to "calc.exe".\n'
                "Report whether the launch was successful and what window handle was returned."
            ),
        )
        assert_quality(result)
        assert_tool_called(result, "app")
        assert_output_matches(result, r"(?i)(calculator|launched|success|handle|window)")

    async def test_verify_calculator_window_exists(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Verify that Calculator is now open by finding its window using window_management.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(result, r"(?i)(calculator|found|open|visible)")

    async def test_close_calculator(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close the Calculator window.")
        assert_quality(result)
        assert_output_matches(result, r"(?i)(calculator|closed|success)")


# ---------------------------------------------------------------------------
# Session 2: Launch Notepad via App Tool (Control — Win32 App)
# ---------------------------------------------------------------------------


@pytest.mark.session("notepad-via-app-tool")
class TestNotepadViaAppTool:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close any existing Notepad windows that may be open."
        )
        assert_quality(result)

    async def test_launch_notepad_using_app_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            (
                'Launch Notepad using the app tool with programPath set to "notepad.exe".\n'
                "Report whether the launch was successful and what window handle was returned."
            ),
        )
        assert_quality(result)
        assert_tool_called(result, "app")
        assert_output_matches(result, r"(?i)(notepad|launched|success|handle|window)")

    async def test_verify_notepad_window_exists(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Verify that Notepad is now open by finding its window using window_management.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(result, r"(?i)(notepad|found|open|visible)")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close the Notepad window without saving.")
        assert_quality(result)
        assert_output_matches(result, r"(?i)(notepad|closed|success)")
