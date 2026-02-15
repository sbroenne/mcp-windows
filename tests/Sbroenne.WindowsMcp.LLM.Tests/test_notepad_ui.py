"""
Notepad UI Workflow Tests — End-to-End Notepad Automation.

Tools Covered: app, keyboard_control, ui_type, window_management, file_save
"""

import re

import pytest
from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
    assert_tool_call_order,
    assert_tool_param_matches,
    make_agents,
)
from pytest_aitest import Agent, ClarificationDetection, MCPServer, Provider


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=15
    )


@pytest.fixture(scope="module")
def all_agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return _agents(windows_mcp_server, gpt41_provider, gpt52_provider)


# ---------------------------------------------------------------------------
# Session 1: Notepad Workflow — Discard
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_notepad_workflow_discard(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Open Notepad, type text, close without saving."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "1. Launch Notepad\n"
            '2. Type "Hello World"\n'
            "3. Record proof that this worked (no screenshots, just text)\n"
            "4. Close Notepad and click \"Don't Save\" if prompted\n"
            "5. Verify no Notepad window is open (search for it)\n\n"
            'Report "VERIFIED: No Notepad window open" if closed successfully.'
        ),
    )

    # REQUIRED: Must launch Notepad first, then type text
    names = [c.name for c in result.all_tool_calls]
    app_idx = names.index("app") if "app" in names else -1
    typed = (
        ("keyboard_control" in names and app_idx < names.index("keyboard_control"))
        or ("ui_type" in names and app_idx < names.index("ui_type"))
    )
    assert typed, "Expected app before keyboard_control or ui_type"

    # OUTCOME: Proof that "Hello World" was typed
    text = " ".join(result.all_responses)
    param_match = any(
        re.search(r"(?i)hello.*world", str(c.arguments.get("text", "")))
        for c in result.all_tool_calls
        if c.name in ("keyboard_control", "ui_type")
    )
    assert param_match or re.search(r"(?i)hello\s*world", text)

    # OUTCOME: LLM verified Notepad was closed
    assert_output_matches(
        result,
        r"(?i)(no.*(notepad|window).*(open|found|running)|verified|notepad.*(closed|not found|no longer))",
    )

    assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Notepad Workflow — Save
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_notepad_workflow_save(
    aitest_run,
    windows_mcp_server,
    gpt41_provider,
    gpt52_provider,
    agent,
    temp_dir,
    run_id,
):
    """Open Notepad, type text, save to temp directory, close."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    save_path = (temp_dir / f"llm-test-{run_id}.txt").as_posix()

    result = await aitest_run(
        a,
        (
            "1. Launch Notepad\n"
            '2. Type "Hello World from LLM Test"\n'
            f"3. Save the file to {save_path}\n"
            "4. Record proof that this worked (confirm the file was saved)\n"
            "5. Close Notepad\n"
            "6. Verify no Notepad window is open (search for it)\n\n"
            'Report "VERIFIED: File saved and Notepad closed" if successful.'
        ),
    )

    # REQUIRED: Must launch Notepad first, then type text
    names = [c.name for c in result.all_tool_calls]
    app_idx = names.index("app") if "app" in names else -1
    typed = (
        ("keyboard_control" in names and app_idx < names.index("keyboard_control"))
        or ("ui_type" in names and app_idx < names.index("ui_type"))
    )
    assert typed, "Expected app before keyboard_control or ui_type"

    # OUTCOME: Proof that text was typed
    text = " ".join(result.all_responses)
    param_match = any(
        re.search(r"(?i)hello.*world", str(c.arguments.get("text", "")))
        for c in result.all_tool_calls
        if c.name in ("keyboard_control", "ui_type")
    )
    assert param_match or re.search(r"(?i)hello\s*world", text)

    # OUTCOME: File was saved
    assert_output_matches(
        result, r"(?i)(saved|file.*created|wrote.*file|save.*successful)"
    )

    # OUTCOME: LLM verified Notepad was closed
    assert_output_matches(
        result,
        r"(?i)(no.*(notepad|window).*(open|found|running)|verified|notepad.*(closed|not found|no longer))",
    )

    assert_quality(result)
