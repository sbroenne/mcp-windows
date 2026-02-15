"""
Keyboard and Mouse Control Test.

Tests keyboard_control and mouse_control tools for typing, hotkeys,
clicking, dragging, and mouse position.

Tools Covered: keyboard_control, mouse_control, app, window_management, ui_type
"""

import re

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
    assert_tool_called,
    assert_tool_param_equals,
    assert_tool_param_matches,
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
# Session 1: Typing — keyboard_control(type) with text in Notepad
# ---------------------------------------------------------------------------


@pytest.mark.session("typing")
class TestTyping:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close all Notepad windows if any are open.")
        assert_quality(result)

    async def test_launch_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_type_a_sentence(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            'Type "Hello World from keyboard control!" using the keyboard.',
        )
        assert_quality(result)
        calls = result.tool_calls_for("keyboard_control") + result.tool_calls_for("ui_type")
        assert calls, "Expected keyboard_control or ui_type to be called"

    async def test_type_on_new_line(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            'Press Enter and type "This is a second line of text" on the new line.',
        )
        assert_quality(result)
        calls = result.tool_calls_for("keyboard_control") + result.tool_calls_for("ui_type")
        assert calls, "Expected keyboard_control or ui_type to be called"

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Hotkeys — keyboard_control(press) with Ctrl+A, Ctrl+C, Ctrl+V
# ---------------------------------------------------------------------------


@pytest.mark.session("hotkeys")
class TestHotkeys:
    async def test_cleanup_close_existing_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close all Notepad windows if any are open.")
        assert_quality(result)

    async def test_launch_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Notepad.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_type_some_text(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, 'Type "Text to select and copy" in Notepad.'
        )
        assert_quality(result)
        calls = result.tool_calls_for("keyboard_control") + result.tool_calls_for("ui_type")
        assert calls, "Expected keyboard_control or ui_type to be called"

    async def test_select_all_with_ctrl_a(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Select all the text in Notepad using the keyboard shortcut.",
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")
        assert_tool_param_matches(result, "keyboard_control", "key", r"(?i)a")

    async def test_copy_with_ctrl_c(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Copy the selected text to the clipboard using the keyboard shortcut.",
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")
        assert_tool_param_matches(result, "keyboard_control", "key", r"(?i)c")

    async def test_go_to_end_and_new_line(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Press End to go to the end of the document, then press Enter for a new line.",
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")

    async def test_paste_with_ctrl_v(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Paste the text from clipboard using the keyboard shortcut.",
        )
        assert_quality(result)
        assert_tool_called(result, "keyboard_control")
        assert_tool_param_matches(result, "keyboard_control", "key", r"(?i)v")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 3: Mouse Click — mouse_control(click) at coordinates in Paint
# ---------------------------------------------------------------------------


@pytest.mark.session("mouse-click")
class TestMouseClick:
    async def test_cleanup_close_existing_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close all Paint windows if any are open.")
        assert_quality(result)

    async def test_launch_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Microsoft Paint.")
        assert_quality(result)
        assert_tool_called(result, "app")
        assert_tool_param_equals(result, "app", "programPath", "mspaint.exe")

    async def test_click_in_canvas(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Use mouse_control to click at coordinates (900, 600) in the Paint window to make a mark on the canvas.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "click")

    async def test_click_another_location(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Use mouse_control to click at coordinates (1100, 500) in the Paint window to make another mark.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "click")

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "close")


# ---------------------------------------------------------------------------
# Session 4: Mouse Drag — mouse_control(drag) for drawing a line in Paint
# ---------------------------------------------------------------------------


@pytest.mark.session("mouse-drag")
class TestMouseDrag:
    async def test_cleanup_close_existing_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close all Paint windows if any are open.")
        assert_quality(result)

    async def test_launch_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Open Microsoft Paint.")
        assert_quality(result)
        assert_tool_called(result, "app")

    async def test_draw_a_line_by_dragging(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Use mouse_control with action drag to draw a line from (700, 500) to (1200, 700) in the Paint window.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "drag")

    async def test_draw_another_line(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Use mouse_control with action drag to draw a horizontal line from (700, 600) to (1200, 600) in the Paint window.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "drag")

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)
        assert_tool_called(result, "window_management")
        assert_tool_param_equals(result, "window_management", "action", "close")


# ---------------------------------------------------------------------------
# Session 5: Mouse Position — mouse_control(get_position)
# ---------------------------------------------------------------------------


@pytest.mark.session("mouse-position")
class TestMousePosition:
    async def test_get_current_mouse_position(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Get the current position of the mouse cursor on the screen.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "get_position")
        assert_output_matches(result, r"(?i)(x|y|position|coordinate|\d+)")

    async def test_move_mouse_and_get_position(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Move the mouse to position 500, 400 and then report the current mouse position.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
