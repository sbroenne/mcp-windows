"""
4sysops Real-World Workflow Tests — sbroenne/mcp-windows.

Recreates the exact tasks from the 4sysops article:
"Windows automation with AI via MCP"
https://4sysops.com/archives/windows-automation-with-ai-via-mcp/

Tools Covered: app, ui_type, keyboard_control, file_save, window_management,
               ui_click, ui_read, screenshot_control
"""

import pytest
from conftest import (
    assert_output_matches,
    assert_quality,
    assert_tool_called,
    make_agents,
)


def _agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    return make_agents(
        windows_mcp_server, gpt41_provider, gpt52_provider, max_turns=50
    )


# ---------------------------------------------------------------------------
# Session 1: Create 10 Files in Documents (4sysops Task 1)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_create_10_files(
    aitest_run,
    windows_mcp_server,
    gpt41_provider,
    gpt52_provider,
    agent,
    temp_dir,
    run_id,
):
    """Create 0.txt through 9.txt (exact 4sysops task)."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    folder = (temp_dir / f"4sysops-documents-{run_id}").as_posix()
    result = await aitest_run(
        a,
        (
            f"Create 10 numbered text files in this folder: {folder}/\n\n"
            "Files needed: 0.txt, 1.txt, 2.txt, 3.txt, 4.txt, 5.txt, 6.txt, 7.txt, 8.txt, 9.txt\n"
            'Each file should contain just its number (e.g., "0" in 0.txt, "1" in 1.txt, etc.)\n\n'
            'Report "VERIFIED: Created 10 numbered files (0.txt - 9.txt)" when complete.'
        ),
    )

    assert_output_matches(
        result, r"(?i)(created.*files|files.*created|0\.txt|9\.txt|10.*files)"
    )
    assert_output_matches(
        result, r"(?i)(verified|complete|success|all.*files)"
    )
    assert_quality(result)


# ---------------------------------------------------------------------------
# Session 2: Check Windows Update (4sysops Task 2)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_check_windows_update(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Open Windows Update settings and report available updates."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Check Windows Update for available updates.\n\n"
            "Report any available updates you find (names, KB numbers, etc.)\n"
            "Close Settings when done.\n\n"
            'Report "VERIFIED: Windows Update checked" and list any available updates.'
        ),
    )

    assert result.tool_was_called("app") or result.tool_was_called("keyboard_control")
    assert (
        result.tool_was_called("ui_click")
        or result.tool_was_called("ui_read")
        or result.tool_was_called("screenshot_control")
        or any(
            "update" in r.lower() or "KB" in r
            for r in result.all_responses
        )
    )
    assert_output_matches(
        result, r"(?i)(verified|update|checked|available|installed|pending)"
    )
    assert_quality(result)


# ---------------------------------------------------------------------------
# Session 3: Verify Firefox Installation (4sysops Task 3 — Modified)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_verify_firefox(
    aitest_run, windows_mcp_server, gpt41_provider, gpt52_provider, agent
):
    """Check if Firefox is installed (without triggering UAC)."""
    agents = _agents(windows_mcp_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    result = await aitest_run(
        a,
        (
            "Check if Mozilla Firefox is installed on this system.\n\n"
            "Report whether Firefox is installed and what version if found.\n"
            "Do NOT attempt to install Firefox - just check if it's already there.\n\n"
            'Report "VERIFIED: Firefox [installed/not installed]" with version if found.'
        ),
    )

    assert_tool_called(result, "app")
    assert (
        result.tool_was_called("ui_type")
        or result.tool_was_called("ui_find")
        or result.tool_was_called("keyboard_control")
        or any("firefox" in r.lower() for r in result.all_responses)
    )
    assert_output_matches(
        result,
        r"(?i)(verified|firefox.*(installed|found|not found|not installed)|version)",
    )
    assert_quality(result)
