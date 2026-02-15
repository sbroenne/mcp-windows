"""
4sysops Real-World Workflow Tests — CursorTouch/Windows-MCP.

Same tasks as test_4sysops_workflow.py but for CursorTouch's Windows-MCP.
https://github.com/CursorTouch/Windows-MCP

Key Differences:
  - Tool names: App-Tool, State-Tool, Click-Tool, Type-Tool, Shortcut-Tool, Powershell-Tool
  - Coordinate-based UI: Must call State-Tool first to get element coordinates
  - Has Powershell-Tool: Can execute PowerShell commands directly
"""

import pytest
from pytest_aitest import Agent, ClarificationDetection, MCPServer, Provider

from conftest import (
    SYSTEM_PROMPT,
    assert_output_matches,
    assert_quality,
)


@pytest.fixture(scope="module")
def cursortouch_server():
    """CursorTouch Windows-MCP server."""
    return MCPServer(
        command=[
            r"D:\source\Windows-MCP\.venv\Scripts\python.exe",
            "-m",
            "windows_mcp",
            "--transport",
            "stdio",
        ]
    )


def _agents(cursortouch_server, gpt41_provider, gpt52_provider):
    return [
        Agent(
            name="gpt41-agent",
            provider=gpt41_provider,
            mcp_servers=[cursortouch_server],
            system_prompt=SYSTEM_PROMPT,
            max_turns=50,
            clarification_detection=ClarificationDetection(enabled=True),
        ),
        Agent(
            name="gpt52-agent",
            provider=gpt52_provider,
            mcp_servers=[cursortouch_server],
            system_prompt=SYSTEM_PROMPT,
            max_turns=50,
            clarification_detection=ClarificationDetection(enabled=True),
        ),
    ]


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_create_10_files(
    aitest_run, cursortouch_server, gpt41_provider, gpt52_provider, agent, temp_dir, run_id
):
    """Create 0.txt through 9.txt using CursorTouch server."""
    agents = _agents(cursortouch_server, gpt41_provider, gpt52_provider)
    a = agents[0] if agent == "gpt41" else agents[1]

    folder = (temp_dir / f"4sysops-cursortouch-{run_id}").as_posix()
    result = await aitest_run(
        a,
        (
            f"Create 10 numbered text files in this folder: {folder}/\n\n"
            "Files needed: 0.txt, 1.txt, 2.txt, 3.txt, 4.txt, 5.txt, 6.txt, 7.txt, 8.txt, 9.txt\n"
            'Each file should contain just its number (e.g., "0" in 0.txt, "1" in 1.txt, etc.)\n\n'
            'Report "VERIFIED: Created 10 numbered files (0.txt - 9.txt)" when complete.'
        ),
    )

    assert_output_matches(result, r"(?i)(created.*files|files.*created|0\.txt|9\.txt|10.*files)")
    assert_output_matches(result, r"(?i)(verified|complete|success|all.*files)")
    assert_quality(result)


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_check_windows_update(
    aitest_run, cursortouch_server, gpt41_provider, gpt52_provider, agent
):
    """Check Windows Update using CursorTouch server."""
    agents = _agents(cursortouch_server, gpt41_provider, gpt52_provider)
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

    assert (
        result.tool_was_called("App-Tool")
        or result.tool_was_called("Shortcut-Tool")
        or result.tool_was_called("Powershell-Tool")
    )
    assert_output_matches(result, r"(?i)(verified|update|checked|available|installed|pending)")
    assert_quality(result)


@pytest.mark.parametrize("agent", ["gpt41", "gpt52"], indirect=False)
async def test_verify_firefox(
    aitest_run, cursortouch_server, gpt41_provider, gpt52_provider, agent
):
    """Check if Firefox is installed using CursorTouch server."""
    agents = _agents(cursortouch_server, gpt41_provider, gpt52_provider)
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

    assert result.tool_was_called("App-Tool") or result.tool_was_called("Powershell-Tool")
    assert_output_matches(
        result, r"(?i)(verified|firefox.*(installed|found|not found|not installed)|version)"
    )
    assert_quality(result)
