"""
File Dialog Test — UI File Tools.

Tests file_save tool for Save As dialog handling in Notepad and Paint.

Tools Covered: app, ui_type, keyboard_control, file_save, mouse_control, ui_click
"""

import uuid

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_not_contains,
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
# Session 1: Notepad Save — save text file via file_save
# ---------------------------------------------------------------------------


@pytest.mark.session("notepad-save")
class TestNotepadSave:
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
            a,
            'Type "File dialog test content - this text will be saved to a file." in Notepad.',
        )
        assert_quality(result)
        calls = result.tool_calls_for("ui_type") + result.tool_calls_for("keyboard_control")
        assert calls, "Expected ui_type or keyboard_control to be called"

    async def test_save_the_file(
        self, aitest_run, windows_mcp_server, gpt41_provider, test_results_path
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        filename = f"notepad-file-test-{uuid.uuid4()}.txt"
        save_path = test_results_path / filename
        result = await aitest_run(
            a, f"Save the document to {save_path}"
        )
        assert_quality(result)
        calls = (
            result.tool_calls_for("file_save")
            + result.tool_calls_for("keyboard_control")
            + result.tool_calls_for("ui_click")
        )
        assert calls, "Expected file_save, keyboard_control, or ui_click to be called"
        assert_output_not_contains(result, "failed")
        assert_output_not_contains(result, "SecureDesktop")

    async def test_close_notepad(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Notepad.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Paint Save — save image as PNG via file_save
# ---------------------------------------------------------------------------


@pytest.mark.session("paint-save")
class TestPaintSave:
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

    async def test_draw_something_simple(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Draw a simple diagonal line across the canvas so there is something to save.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")

    async def test_save_image_as_png(
        self, aitest_run, windows_mcp_server, gpt41_provider, test_results_path
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        filename = f"paint-file-test-{uuid.uuid4()}.png"
        save_path = test_results_path / filename
        result = await aitest_run(
            a, f"Save the image as a PNG file to {save_path}"
        )
        assert_quality(result)
        calls = (
            result.tool_calls_for("file_save")
            + result.tool_calls_for("keyboard_control")
            + result.tool_calls_for("ui_click")
        )
        assert calls, "Expected file_save, keyboard_control, or ui_click to be called"
        assert_output_not_contains(result, "failed")
        assert_output_not_contains(result, "SecureDesktop")

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint.")
        assert_quality(result)
