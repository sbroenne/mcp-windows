"""
Plugin skill LLM eval tests — skill-creator improvement loop.

Each scenario runs TWICE (A/B):
  - agent_no_skill: raw MCP server, no skill injected (baseline)
  - agent_with_skill: same server + windows-automation skill injected

The skill should steer the model toward correct tool choices.
Failures here mean the skill text needs improvement, not the code.

Uses the standard Eval + eval_run approach so tool calling works correctly
(PydanticAI orchestration, proper OpenAI-compatible tool routing).
"""

import pytest

from conftest import (
    REPO_ROOT,
    SYSTEM_PROMPT,
    assert_quality,
    assert_tool_called,
    assert_tool_call_order,
)
from pytest_skill_engineering import Eval as Agent, ClarificationDetection, load_skill

SKILL_DIR = REPO_ROOT / "plugin" / "skills" / "windows-automation"

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture(scope="module")
def skill():
    return load_skill(SKILL_DIR)


@pytest.fixture(scope="module")
def agent_no_skill(windows_mcp_server, gpt41_provider):
    """Baseline: raw MCP server, no skill injected. Documents natural model behaviour."""
    return Agent(
        name="skill-eval-no-skill",
        provider=gpt41_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        # Disabled: we measure tool choice, not verbosity. Reporting intermediate
        # steps can trigger false-positive clarification detection.
        clarification_detection=ClarificationDetection(enabled=False),
    )


@pytest.fixture(scope="module")
def agent_with_skill(windows_mcp_server, gpt41_provider, skill):
    """Skill-injected agent. Should outperform the baseline on all scenarios."""
    return Agent(
        name="skill-eval-with-skill",
        provider=gpt41_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        skill=skill,
        # Disabled: we measure tool choice, not verbosity.
        clarification_detection=ClarificationDetection(enabled=False),
    )


# ---------------------------------------------------------------------------
# Scenario 1 — file_save steering
# Skill rule: "Use file_save for Save / Save As flows instead of raw Ctrl+S"
# ---------------------------------------------------------------------------

SAVE_PROMPT = (
    "Open Notepad, type the word 'hello', "
    "then save the file to C:\\Users\\Public\\skill_eval_save.txt"
)


@pytest.mark.llm_eval
async def test_file_save_baseline(aitest_run, agent_no_skill):
    """Baseline (no skill): record whether model reaches for Ctrl+S or file_save."""
    result = await aitest_run(agent_no_skill, SAVE_PROMPT)
    assert_quality(result)
    assert result.tool_was_called("app") or result.tool_was_called("window_management"), \
        "Baseline should at least open/find Notepad"


@pytest.mark.llm_eval
async def test_file_save_with_skill(aitest_run, agent_with_skill):
    """With skill: must use file_save, not raw keyboard Ctrl+S for saving."""
    result = await aitest_run(agent_with_skill, SAVE_PROMPT)
    assert_quality(result)

    assert_tool_called(result, "file_save")

    raw_ctrl_s = [
        c for c in result.tool_calls_for("keyboard_control")
        if c.arguments.get("key") == "s"
        and "ctrl" in str(c.arguments.get("modifiers", "")).lower()
    ]
    assert len(raw_ctrl_s) == 0, (
        "Skill should prevent raw Ctrl+S — use file_save instead. "
        f"Got {len(raw_ctrl_s)} raw Ctrl+S call(s). Improve SKILL.md anti-pattern guidance."
    )


# ---------------------------------------------------------------------------
# Scenario 2 — semantic-first (window handle before UI interaction)
# Skill rule: "Use window_management to find or activate the target window first"
# ---------------------------------------------------------------------------

SEMANTIC_PROMPT = (
    "Open Notepad and type the text 'skill eval test' into it."
)


@pytest.mark.llm_eval
async def test_semantic_first_baseline(aitest_run, agent_no_skill):
    """Baseline: does the model establish a window handle before interacting?"""
    result = await aitest_run(agent_no_skill, SEMANTIC_PROMPT)
    assert_quality(result)


@pytest.mark.llm_eval
async def test_semantic_first_with_skill(aitest_run, agent_with_skill):
    """With skill: window handle must be obtained before ui_type."""
    result = await aitest_run(agent_with_skill, SEMANTIC_PROMPT)
    assert_quality(result)

    names = [c.name for c in result.all_tool_calls]
    handle_tools = {"app", "window_management"}
    assert any(n in handle_tools for n in names), \
        "Skill should establish a window handle first (app or window_management)"

    assert_tool_called(result, "ui_type")

    handle_idx = next(i for i, n in enumerate(names) if n in handle_tools)
    type_idx = next((i for i, n in enumerate(names) if n == "ui_type"), len(names))
    assert handle_idx < type_idx, (
        "window_management/app must be called before ui_type. "
        "Improve SKILL.md preferred workflow ordering."
    )


# ---------------------------------------------------------------------------
# Scenario 3 — no coordinate-based clicks for semantic UIs
# Skill rule: "Prefer element names … over screen coordinates"
# ---------------------------------------------------------------------------

NO_COORDS_PROMPT = (
    "Open Notepad, then click the Format menu using semantic UI tools."
)


@pytest.mark.llm_eval
async def test_no_coordinate_clicks_baseline(aitest_run, agent_no_skill):
    """Baseline: does the model reach for coordinates or semantic names?"""
    result = await aitest_run(agent_no_skill, NO_COORDS_PROMPT)
    assert_quality(result)


@pytest.mark.llm_eval
async def test_no_coordinate_clicks_with_skill(aitest_run, agent_with_skill):
    """With skill: should use ui_click by name, not mouse_control with x/y coordinates."""
    result = await aitest_run(agent_with_skill, NO_COORDS_PROMPT)
    assert_quality(result)

    # The primary assertion: no raw coordinate clicks.
    coord_clicks = [
        c for c in result.tool_calls_for("mouse_control")
        if c.arguments.get("action") == "click"
        and c.arguments.get("x") is not None
    ]
    assert len(coord_clicks) == 0, (
        "Skill should prevent coordinate mouse clicks — use ui_click by element name instead. "
        f"Got {len(coord_clicks)} coordinate click(s). Improve SKILL.md anti-pattern guidance."
    )

    # Secondary: if the window was found, semantic click should have been attempted.
    if result.tool_was_called("window_management") or result.tool_was_called("ui_find"):
        assert result.tool_was_called("ui_click") or result.tool_was_called("ui_find"), (
            "If a window was located, ui_click or ui_find should have been used for menu interaction."
        )


# ---------------------------------------------------------------------------
# Scenario 4 — browser semantic-first
# Skill rule: browsers → window_management then ui_find/ui_click on ARIA labels
# ---------------------------------------------------------------------------

BROWSER_PROMPT = (
    "Open Microsoft Edge, navigate to https://www.bing.com, "
    "then find the search box and type 'Windows MCP Server'."
)


@pytest.mark.llm_eval
async def test_browser_semantic_baseline(aitest_run, agent_no_skill):
    """Baseline: does the model use semantic tools on browser page content?"""
    result = await aitest_run(agent_no_skill, BROWSER_PROMPT)
    assert_quality(result)


@pytest.mark.llm_eval
async def test_browser_semantic_with_skill(aitest_run, agent_with_skill):
    """With skill: browser page content via ui_find/ui_type, not screenshot clicks."""
    result = await aitest_run(agent_with_skill, BROWSER_PROMPT)
    assert_quality(result)

    assert result.tool_was_called("app") or result.tool_was_called("window_management"), \
        "Should launch or find Edge"

    assert result.tool_was_called("ui_find") or result.tool_was_called("ui_type"), \
        "Skill should drive semantic page interaction (ui_find / ui_type), not screenshot clicking"

    screenshot_clicks = [
        c for c in result.tool_calls_for("mouse_control")
        if c.arguments.get("action") == "click"
    ]
    ui_interactions = (
        result.tool_calls_for("ui_click")
        + result.tool_calls_for("ui_type")
        + result.tool_calls_for("ui_find")
    )
    assert len(ui_interactions) >= len(screenshot_clicks), (
        "Semantic UI interactions should outnumber coordinate clicks on browser page content. "
        "Improve SKILL.md browser section."
    )
