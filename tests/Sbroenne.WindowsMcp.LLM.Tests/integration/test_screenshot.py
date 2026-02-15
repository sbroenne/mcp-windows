"""
Screenshot Test — Screenshot Control Tools.

Tests screenshot_control tool actions: annotated capture, plain capture,
monitor listing, and window-specific screenshots.

Tools Covered: screenshot_control, app, window_management
"""

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
    assert_tool_called,
    assert_tool_param_equals,
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
# Session 1: Annotated Screenshot — capture with element labels
# ---------------------------------------------------------------------------


@pytest.mark.session("annotated-screenshot")
class TestAnnotatedScreenshot:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close all Notepad windows if any are open."
        )
        assert_quality(result)

    async def test_launch_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_annotated_screenshot_of_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Take a screenshot of the Notepad window with element labels showing all UI elements.",
        )
        assert_quality(result)
        assert_tool_called(result, "screenshot_control")
        assert_tool_param_equals(result, "screenshot_control", "annotate", True)
        assert_output_matches(
            result, r"(?i)(element|label|annotation|ui|menu|edit|file)"
        )

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Plain Screenshot — capture without annotations
# ---------------------------------------------------------------------------


@pytest.mark.session("plain-screenshot")
class TestPlainScreenshot:
    async def test_cleanup_close_existing_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close all Paint windows if any are open."
        )
        assert_quality(result)

    async def test_launch_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Microsoft Paint.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_plain_screenshot_of_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Take a screenshot of the Paint window without any annotations or labels.",
        )
        assert_quality(result)
        assert_tool_called(result, "screenshot_control")
        assert_tool_param_equals(result, "screenshot_control", "annotate", False)

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 3: Monitor List — enumerate available displays
# ---------------------------------------------------------------------------


@pytest.mark.session("monitor-list")
class TestMonitorList:
    async def test_list_all_monitors(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "List all available monitors and displays on this computer.",
        )
        assert_quality(result)
        assert_tool_called(result, "screenshot_control")
        assert_tool_param_equals(
            result, "screenshot_control", "action", "list_monitors"
        )
        assert_output_matches(
            result, r"(?i)(monitor|display|screen|primary|\d+x\d+)"
        )


# ---------------------------------------------------------------------------
# Session 4: Window Screenshot — capture specific window by handle
# ---------------------------------------------------------------------------


@pytest.mark.session("window-screenshot")
class TestWindowScreenshot:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close all Notepad windows if any are open."
        )
        assert_quality(result)

    async def test_launch_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_find_notepad_window(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Find the Notepad window and get its handle."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(result, r"(?i)(handle|notepad|found|window)")

    async def test_screenshot_of_notepad_window(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Take a screenshot of only the Notepad window, not the entire screen.",
        )
        assert_quality(result)
        assert_tool_called(result, "screenshot_control")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)
