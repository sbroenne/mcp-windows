"""
Paint UI Test — Paint Canvas and Tool Operations.

Tests Paint ribbon UI and canvas drawing operations including tool discovery,
tool selection, color selection, canvas drawing, and state discovery.

Tools Covered: ui_find, ui_click, mouse_control, screenshot_control, app
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
# Session 1: Tool Discovery — ui_find for ribbon toolbar elements
# ---------------------------------------------------------------------------


@pytest.mark.session("tool-discovery")
class TestToolDiscovery:
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

    async def test_find_pencil_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Find the pencil tool in the Paint toolbar."
        )
        assert_quality(result)
        assert_tool_called(result, "ui_find")
        assert_output_matches(result, r"(?i)(pencil|tool|found|element)")

    async def test_find_brush_tools(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Find the brush or brushes tool in the Paint toolbar."
        )
        assert_quality(result)
        assert_tool_called(result, "ui_find")
        assert_output_matches(result, r"(?i)(brush|tool|found|element)")

    async def test_find_shapes_tools(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Find the shapes or shape tools in the Paint toolbar."
        )
        assert_quality(result)
        assert_tool_called(result, "ui_find")
        assert_output_matches(result, r"(?i)(shape|tool|found|element)")

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Tool Selection — ui_click for tool buttons
# ---------------------------------------------------------------------------


@pytest.mark.session("tool-selection")
class TestToolSelection:
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

    async def test_click_pencil_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the pencil tool to select it."
        )
        assert_quality(result)
        calls = result.tool_calls_for("ui_click") + result.tool_calls_for("ui_find")
        assert calls, "Expected ui_click or ui_find to be called"

    async def test_click_brush_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the brush tool to select it."
        )
        assert_quality(result)
        calls = result.tool_calls_for("ui_click") + result.tool_calls_for("ui_find")
        assert calls, "Expected ui_click or ui_find to be called"

    async def test_click_eraser_tool(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the eraser tool to select it."
        )
        assert_quality(result)
        calls = result.tool_calls_for("ui_click") + result.tool_calls_for("ui_find")
        assert calls, "Expected ui_click or ui_find to be called"

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 3: Color Selection — ui_click for color palette
# ---------------------------------------------------------------------------


@pytest.mark.session("color-selection")
class TestColorSelection:
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

    async def test_select_red_color(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the red color in the color palette to select it."
        )
        assert_quality(result)
        calls = (
            result.tool_calls_for("ui_click")
            + result.tool_calls_for("ui_find")
            + result.tool_calls_for("mouse_control")
        )
        assert calls, "Expected ui_click, ui_find, or mouse_control to be called"

    async def test_select_blue_color(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the blue color in the color palette to select it."
        )
        assert_quality(result)
        calls = (
            result.tool_calls_for("ui_click")
            + result.tool_calls_for("ui_find")
            + result.tool_calls_for("mouse_control")
        )
        assert calls, "Expected ui_click, ui_find, or mouse_control to be called"

    async def test_select_green_color(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a, "Click on the green color in the color palette to select it."
        )
        assert_quality(result)
        calls = (
            result.tool_calls_for("ui_click")
            + result.tool_calls_for("ui_find")
            + result.tool_calls_for("mouse_control")
        )
        assert calls, "Expected ui_click, ui_find, or mouse_control to be called"

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)


# ---------------------------------------------------------------------------
# Session 4: Canvas Drawing — mouse_control(drag) for drawing
# ---------------------------------------------------------------------------


@pytest.mark.session("canvas-drawing")
class TestCanvasDrawing:
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

    async def test_draw_diagonal_line(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Draw a diagonal line from the top-left area of the canvas to the bottom-right area.",
        )
        assert_quality(result)
        assert_tool_called(result, "mouse_control")
        assert_tool_param_equals(result, "mouse_control", "action", "drag")

    async def test_draw_horizontal_line(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Draw a horizontal line across the middle of the canvas.",
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


# ---------------------------------------------------------------------------
# Session 5: State Discovery — screenshot_control(annotate=true)
# ---------------------------------------------------------------------------


@pytest.mark.session("state-discovery")
class TestStateDiscovery:
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

    async def test_annotated_screenshot_discover_elements(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(
            a,
            "Take a screenshot of Paint with element labels to discover all available UI elements and tools.",
        )
        assert_quality(result)
        assert_tool_called(result, "screenshot_control")
        assert_tool_param_equals(result, "screenshot_control", "annotate", True)
        assert_output_matches(
            result, r"(?i)(element|label|annotation|tool|canvas|color)"
        )

    async def test_close_paint(
        self, aitest_run, windows_mcp_server, gpt41_provider
    ):
        a = _agent(windows_mcp_server, gpt41_provider)
        result = await aitest_run(a, "Close Paint without saving.")
        assert_quality(result)
