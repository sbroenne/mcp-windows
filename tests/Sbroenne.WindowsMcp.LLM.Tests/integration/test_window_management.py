"""
Window Management Test — App and Window Management Tools.

Tests the app tool and all window_management actions: list, find, activate,
minimize, maximize, restore, close, move, resize, and wait_for.

Tools Covered: app, window_management, ui_type, keyboard_control
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
# Session 1: Launch and Find — app, window_management(find), window_management(list)
# ---------------------------------------------------------------------------


@pytest.mark.session("launch-and-find")
class TestLaunchAndFind:
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
        result = await aitest_run(a, "Launch Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")
        assert_tool_param_equals(result, "app", "programPath", "notepad.exe")

    async def test_list_all_open_windows(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "List all currently open windows on the desktop."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "list")
        assert_output_matches(result, r"(?i)(notepad|window|title)")

    async def test_find_notepad_window(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Find the Notepad window that is currently open."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "find")
        assert_output_matches(result, r"(?i)(notepad|found|window|handle)")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Close Notepad without saving any changes."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")


# ---------------------------------------------------------------------------
# Session 2: Window State — minimize, maximize, restore, activate
# ---------------------------------------------------------------------------


@pytest.mark.session("window-state")
class TestWindowState:
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
        result = await aitest_run(a, "Launch Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_minimize_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Minimize the Notepad window.")
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "minimize")

    async def test_restore_from_minimized(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Restore the Notepad window from minimized state."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "restore")

    async def test_maximize_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Maximize the Notepad window to fill the screen."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "maximize")

    async def test_restore_from_maximized(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Restore the Notepad window from maximized state to normal size."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "restore")

    async def test_activate_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Activate the Notepad window and bring it to the foreground."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "activate")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)
        assert_tool_called(result, "window_management")


# ---------------------------------------------------------------------------
# Session 3: Window Position — move and resize
# ---------------------------------------------------------------------------


@pytest.mark.session("window-position")
class TestWindowPosition:
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
        result = await aitest_run(a, "Launch Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_move_to_top_left(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Move the Notepad window to position 100, 100 on the screen."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "move")

    async def test_resize_to_800x600(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Resize the Notepad window to 800 pixels wide and 600 pixels tall.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "resize")

    async def test_move_to_center(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Move the Notepad window to position 300, 200 on the screen."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "move")

    async def test_resize_to_smaller(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Resize the Notepad window to 640 pixels wide and 480 pixels tall.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "resize")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)
        assert_tool_called(result, "window_management")


# ---------------------------------------------------------------------------
# Session 4: Window Lifecycle — wait_for and close with options
# ---------------------------------------------------------------------------


@pytest.mark.session("window-lifecycle")
class TestWindowLifecycle:
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
        result = await aitest_run(a, "Launch Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_wait_for_notepad_ready(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Wait for the Notepad window to become fully visible and ready.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        # May use wait_for or wait_for_state
        wm_calls = result.tool_calls_for("window_management")
        action_match = any(
            str(c.arguments.get("action", "")).lower()
            in ("wait_for", "wait_for_state")
            for c in wm_calls
        )
        assert action_match, "Expected window_management with wait_for or wait_for_state"
        assert_output_matches(
            result, r"(?i)(notepad|ready|visible|found|success)"
        )

    async def test_type_some_text(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, 'Type "Lifecycle test" in Notepad.')
        assert_quality(result)
        calls = result.tool_calls_for("ui_type") + result.tool_calls_for("keyboard_control")
        assert calls, "Expected ui_type or keyboard_control to be called"

    async def test_close_notepad_discard(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Close the Notepad window and discard any unsaved changes.",
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "close")

    async def test_verify_notepad_closed(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Verify that Notepad is no longer running."
        )
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_output_matches(
            result,
            r"(?i)(no.*notepad|not.*found|closed|not.*running|0.*match)",
        )
