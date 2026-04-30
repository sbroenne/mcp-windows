"""
Shared fixtures for Windows MCP Server LLM integration tests.

Provides MCP server, provider, and agent fixtures used across all test files.
"""

import os
import re
import subprocess
import tempfile
import uuid
from dataclasses import dataclass
from pathlib import Path

import pytest
from pytest_skill_engineering import MCPServer
from pytest_skill_engineering.copilot import CopilotCLIPersona, CopilotEval

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
PROJECT_PATH = REPO_ROOT / "src" / "Sbroenne.WindowsMcp" / "Sbroenne.WindowsMcp.csproj"
TEST_RESULTS_DIR = Path(__file__).resolve().parent / "TestResults"
TEST_APP_CLEANUP_SCRIPT = r"""
$targets = @('CalculatorApp', 'ApplicationFrameHost', 'Notepad', 'mspaint')
$matches = Get-Process | Where-Object {
    $_.MainWindowHandle -ne 0 -and (
        $targets -contains $_.ProcessName -or
        $_.MainWindowTitle -match 'Calculator|Notepad|Paint'
    )
}

foreach ($process in $matches) {
    [void]$process.CloseMainWindow()
}

Start-Sleep -Milliseconds 1000

foreach ($process in $matches) {
    $current = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($current -and $current.MainWindowHandle -ne 0) {
        Stop-Process -Id $current.Id -Force
    }
}
"""

# Server command as a list to handle paths with spaces correctly.
# Uses --no-build because dotnet build output on stdout corrupts MCP's
# JSON-RPC protocol.  The _build_mcp_server fixture below guarantees a
# current build exists before any test starts.
SERVER_COMMAND = ["dotnet", "run", "--no-build", "--project", str(PROJECT_PATH), "-c", "Release", "--"]

# ---------------------------------------------------------------------------
# System prompt shared by all agents
# ---------------------------------------------------------------------------
SYSTEM_PROMPT = """\
You are an autonomous Windows automation agent.

CRITICAL RULES:
- Execute all tasks IMMEDIATELY without asking for confirmation
- NEVER ask "Would you like me to...", "Should I proceed...", or similar questions
- NEVER request clarification - make reasonable assumptions and proceed
- Use available tools directly to complete the requested tasks
- Report results after completion, not before starting
"""

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@dataclass(frozen=True)
class Provider:
    """Compatibility wrapper for tests that name providers explicitly."""

    model: str


@dataclass(frozen=True)
class ClarificationDetection:
    """Compatibility wrapper; clarification detection is covered by assertions."""

    enabled: bool = True


def _mcp_server_config(server: MCPServer) -> dict[str, object]:
    if not server.command:
        raise ValueError("stdio MCP server requires a command")

    config: dict[str, object] = {
        "command": server.command[0],
        "args": server.command[1:],
        "type": "stdio",
        "tools": ["*"],
    }
    if server.cwd:
        config["cwd"] = server.cwd
    if server.env:
        config["env"] = server.env
    return config


def Agent(
    *,
    name: str,
    provider: Provider,
    mcp_servers: list[MCPServer],
    system_prompt: str,
    max_turns: int,
    clarification_detection: ClarificationDetection | None = None,
) -> CopilotEval:
    """Create a CopilotEval from the legacy Eval-style test configuration."""
    del clarification_detection
    return CopilotEval(
        name=name,
        model=provider.model.removeprefix("copilot/"),
        instructions=system_prompt,
        max_turns=max_turns,
        timeout_s=300.0,
        persona=CopilotCLIPersona(),
        mcp_servers={
            f"mcp-{index}": _mcp_server_config(server)
            for index, server in enumerate(mcp_servers, start=1)
        },
    )


@pytest.fixture(scope="session", autouse=True)
def _build_mcp_server():
    """Build the MCP server once before any test runs.

    This ensures ``--no-build`` in SERVER_COMMAND always works with the
    latest source code.  The build is Release-mode and quiet to keep
    output clean.
    """
    result = subprocess.run(
        ["dotnet", "build", str(PROJECT_PATH), "-c", "Release", "-v:q"],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        pytest.fail(f"dotnet build failed:\n{result.stdout}\n{result.stderr}")


@pytest.fixture(scope="session", autouse=True)
def _cleanup_test_apps():
    yield
    subprocess.run(
        [
            "powershell",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-Command",
            TEST_APP_CLEANUP_SCRIPT,
        ],
        capture_output=True,
        text=True,
        timeout=10,
        check=False,
    )


@pytest.fixture(scope="session")
def windows_mcp_server():
    """The Windows MCP Server under test."""
    return MCPServer(command=SERVER_COMMAND)


@pytest.fixture
def aitest_run(copilot_eval):
    """Compatibility alias for the current pytest-skill-engineering fixture."""

    async def _run(agent: CopilotEval, prompt: str):
        result = await copilot_eval(agent, prompt)
        _normalize_mcp_tool_names(result)
        return result

    return _run


def _normalize_mcp_tool_names(result):
    for call in result.all_tool_calls:
        match = re.fullmatch(r"mcp-\d+-(.+)", call.name)
        if match:
            call.name = match.group(1)


@pytest.fixture(scope="session")
def copilot_auth():
    """Require GitHub SDK auth via GITHUB_TOKEN or an existing gh login."""
    if os.environ.get("GITHUB_TOKEN"):
        return "GITHUB_TOKEN"

    result = subprocess.run(
        ["gh", "auth", "status"],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        pytest.skip("GITHUB_TOKEN not set and gh auth status failed")
    return "gh"


@pytest.fixture(scope="session")
def gpt55_provider(copilot_auth):
    """GitHub Copilot GPT-5.5 provider."""
    return Provider(model="copilot/gpt-5.5")


@pytest.fixture(scope="session")
def gpt55_agent(windows_mcp_server, gpt55_provider):
    """Agent using GPT-5.5 with the Windows MCP Server."""
    return Agent(
        name="gpt55-agent",
        provider=gpt55_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        clarification_detection=ClarificationDetection(enabled=True),
    )


def make_agents(windows_mcp_server, gpt55_provider, *, max_turns=15):
    """Create the GPT-5.5 agent for parametrize usage."""
    return [
        Agent(
            name="gpt55-agent",
            provider=gpt55_provider,
            mcp_servers=[windows_mcp_server],
            system_prompt=SYSTEM_PROMPT,
            max_turns=max_turns,
            clarification_detection=ClarificationDetection(enabled=True),
        ),
    ]


@pytest.fixture(scope="session")
def agents(windows_mcp_server, gpt55_provider):
    """GPT-5.5 agent for parametrize usage."""
    return make_agents(windows_mcp_server, gpt55_provider)


@pytest.fixture
def test_results_path():
    """Path to the test results directory."""
    TEST_RESULTS_DIR.mkdir(parents=True, exist_ok=True)
    return TEST_RESULTS_DIR


@pytest.fixture
def temp_dir():
    """Temporary directory for test artifacts (cleaned up after test)."""
    d = Path(tempfile.mkdtemp(prefix="mcp-llm-test-"))
    yield d
    # Cleanup is best-effort
    import shutil
    shutil.rmtree(d, ignore_errors=True)


@pytest.fixture
def run_id():
    """Unique run identifier for test isolation."""
    return str(uuid.uuid4())[:8]


# ---------------------------------------------------------------------------
# Assertion helpers
# ---------------------------------------------------------------------------


def _tool_name_matches(actual: str, expected: str) -> bool:
    return actual == expected or actual.endswith(f"-{expected}")


def _tool_calls_for(result, tool_name: str):
    return [
        call
        for call in result.all_tool_calls
        if _tool_name_matches(call.name, tool_name)
    ]


def tool_was_called(result, *tool_names: str) -> bool:
    return any(_tool_calls_for(result, tool_name) for tool_name in tool_names)


def assert_tool_called(result, tool_name: str):
    """Assert a tool was called at least once."""
    assert _tool_calls_for(result, tool_name), f"Expected tool '{tool_name}' to be called"


def assert_tool_call_order(result, first: str, second: str):
    """Assert that first tool was called before second tool."""
    names = [c.name for c in result.all_tool_calls]
    first_indexes = [
        index for index, name in enumerate(names) if _tool_name_matches(name, first)
    ]
    second_indexes = [
        index for index, name in enumerate(names) if _tool_name_matches(name, second)
    ]
    assert first_indexes, f"Tool '{first}' was never called"
    assert second_indexes, f"Tool '{second}' was never called"
    assert first_indexes[0] < second_indexes[0], (
        f"Expected '{first}' before '{second}', got order: {names}"
    )


def assert_output_matches(result, pattern: str):
    """Assert final response matches regex pattern."""
    text = " ".join(result.all_responses)
    assert re.search(pattern, text, re.IGNORECASE), (
        f"Expected pattern '{pattern}' in response"
    )


def assert_output_not_contains(result, value: str):
    """Assert final response does not contain value (case-insensitive)."""
    text = " ".join(result.all_responses).lower()
    assert value.lower() not in text, f"Response should not contain '{value}'"


def assert_tool_param_matches(result, tool_name: str, param: str, pattern: str):
    """Assert a tool was called with a parameter matching a regex."""
    calls = _tool_calls_for(result, tool_name)
    assert calls, f"Tool '{tool_name}' was never called"
    matched = any(
        re.search(pattern, str(c.arguments.get(param, "")), re.IGNORECASE)
        for c in calls
    )
    assert matched, (
        f"No call to '{tool_name}' had param '{param}' matching '{pattern}'"
    )


def assert_tool_param_equals(result, tool_name: str, param: str, value):
    """Assert a tool was called with an exact parameter value."""
    calls = _tool_calls_for(result, tool_name)
    assert calls, f"Tool '{tool_name}' was never called"
    matched = any(c.arguments.get(param) == value for c in calls)
    assert matched, (
        f"No call to '{tool_name}' had param '{param}' == {value!r}"
    )


def assert_quality(result):
    """Standard quality assertions used across all tests."""
    assert result.success, f"Agent failed: {result.error}"
    assert not getattr(result, "asked_for_clarification", False), (
        "Agent asked for clarification"
    )
