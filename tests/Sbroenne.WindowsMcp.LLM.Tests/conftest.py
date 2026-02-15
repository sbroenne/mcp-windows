"""
Shared fixtures for Windows MCP Server LLM integration tests.

Provides MCP server, provider, and agent fixtures used across all test files.
"""

import os
import re
import subprocess
import uuid
import tempfile
from pathlib import Path

import pytest
from pytest_aitest import Agent, ClarificationDetection, MCPServer, Provider

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
PROJECT_PATH = REPO_ROOT / "src" / "Sbroenne.WindowsMcp" / "Sbroenne.WindowsMcp.csproj"
TEST_RESULTS_DIR = Path(__file__).resolve().parent / "TestResults"

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


@pytest.fixture(scope="session")
def windows_mcp_server():
    """The Windows MCP Server under test."""
    return MCPServer(command=SERVER_COMMAND)


@pytest.fixture(scope="session")
def azure_openai_endpoint():
    """Azure OpenAI endpoint from environment."""
    endpoint = os.environ.get("AZURE_OPENAI_ENDPOINT") or os.environ.get("AZURE_API_BASE")
    if not endpoint:
        pytest.skip("AZURE_OPENAI_ENDPOINT or AZURE_API_BASE not set")
    return endpoint


@pytest.fixture(scope="session")
def gpt41_provider():
    """Azure OpenAI GPT-4.1 provider."""
    return Provider(model="azure/gpt-4.1")


@pytest.fixture(scope="session")
def gpt52_provider():
    """Azure OpenAI GPT-5.2-chat provider."""
    return Provider(model="azure/gpt-5.2-chat")


@pytest.fixture(scope="session")
def gpt41_agent(windows_mcp_server, gpt41_provider):
    """Agent using GPT-4.1 with the Windows MCP Server."""
    return Agent(
        name="gpt41-agent",
        provider=gpt41_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        clarification_detection=ClarificationDetection(enabled=True),
    )


@pytest.fixture(scope="session")
def gpt52_agent(windows_mcp_server, gpt52_provider):
    """Agent using GPT-5.2-chat with the Windows MCP Server."""
    return Agent(
        name="gpt52-agent",
        provider=gpt52_provider,
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
        clarification_detection=ClarificationDetection(enabled=True),
    )


def make_agents(windows_mcp_server, gpt41_provider, gpt52_provider, *, max_turns=15):
    """Create both agents for parametrize usage."""
    return [
        Agent(
            name="gpt41-agent",
            provider=gpt41_provider,
            mcp_servers=[windows_mcp_server],
            system_prompt=SYSTEM_PROMPT,
            max_turns=max_turns,
            clarification_detection=ClarificationDetection(enabled=True),
        ),
        Agent(
            name="gpt52-agent",
            provider=gpt52_provider,
            mcp_servers=[windows_mcp_server],
            system_prompt=SYSTEM_PROMPT,
            max_turns=max_turns,
            clarification_detection=ClarificationDetection(enabled=True),
        ),
    ]


@pytest.fixture(scope="session")
def agents(windows_mcp_server, gpt41_provider, gpt52_provider):
    """Both agents for parametrize usage."""
    return make_agents(windows_mcp_server, gpt41_provider, gpt52_provider)


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


def assert_tool_called(result, tool_name: str):
    """Assert a tool was called at least once."""
    assert result.tool_was_called(tool_name), f"Expected tool '{tool_name}' to be called"


def assert_tool_call_order(result, first: str, second: str):
    """Assert that first tool was called before second tool."""
    names = [c.name for c in result.all_tool_calls]
    assert first in names, f"Tool '{first}' was never called"
    assert second in names, f"Tool '{second}' was never called"
    assert names.index(first) < names.index(second), (
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
    calls = result.tool_calls_for(tool_name)
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
    calls = result.tool_calls_for(tool_name)
    assert calls, f"Tool '{tool_name}' was never called"
    matched = any(c.arguments.get(param) == value for c in calls)
    assert matched, (
        f"No call to '{tool_name}' had param '{param}' == {value!r}"
    )


def assert_quality(result):
    """Standard quality assertions used across all tests."""
    assert result.success, f"Agent failed: {result.error}"
    assert not result.asked_for_clarification, "Agent asked for clarification"
