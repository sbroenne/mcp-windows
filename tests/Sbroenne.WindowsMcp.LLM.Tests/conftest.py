"""
Shared fixtures for Windows MCP Server LLM integration tests.

Provides MCP server configuration and PydanticAI agent fixtures. The eval
harness uses PydanticAI directly (openai:gpt-4.1 via GitHub Models), which
supports proper tool calling without any LiteLLM dependency.
"""

import os
import re
import subprocess
import tempfile
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import pytest
from pydantic_ai import Agent
from pydantic_ai.mcp import MCPServerStdio
from pydantic_ai.messages import ModelResponse, ToolCallPart

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
PROJECT_PATH = REPO_ROOT / "src" / "Sbroenne.WindowsMcp" / "Sbroenne.WindowsMcp.csproj"
TEST_RESULTS_DIR = Path(__file__).resolve().parent / "TestResults"
SKILL_DIR = REPO_ROOT / "plugin" / "skills" / "windows-automation"

# Server command as a list to handle paths with spaces correctly.
# Uses --no-build because dotnet build output on stdout corrupts MCP's
# JSON-RPC protocol.  The _build_mcp_server fixture below guarantees a
# current build exists before any test starts.
SERVER_COMMAND = [
    "dotnet", "run", "--no-build", "--project", str(PROJECT_PATH),
    "-c", "Release", "--",
]

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
# Thin eval result wrapper — mirrors the API our tests expect
# ---------------------------------------------------------------------------

@dataclass
class _ToolCallRecord:
    name: str
    arguments: dict[str, Any] = field(default_factory=dict)


@dataclass
class EvalResult:
    """Lightweight result object wrapping a PydanticAI run."""

    _tool_calls: list[_ToolCallRecord]
    _responses: list[str]
    success: bool = True
    error: str | None = None

    # Clarification detection is intentionally disabled in eval context —
    # we measure tool choice, not verbosity.
    asked_for_clarification: bool = False

    @property
    def all_tool_calls(self) -> list[_ToolCallRecord]:
        return self._tool_calls

    @property
    def tool_names_called(self) -> set[str]:
        return {c.name for c in self._tool_calls}

    def tool_was_called(self, name: str) -> bool:
        return name in self.tool_names_called

    def tool_call_count(self, name: str) -> int:
        return len(self.tool_calls_for(name))

    def tool_calls_for(self, name: str) -> list[_ToolCallRecord]:
        return [c for c in self._tool_calls if c.name == name]

    @property
    def all_responses(self) -> list[str]:
        return self._responses

    @classmethod
    def from_pydantic(cls, run_result: Any, *, success: bool = True) -> "EvalResult":
        tool_calls: list[_ToolCallRecord] = []
        responses: list[str] = []
        for msg in run_result.all_messages():
            if isinstance(msg, ModelResponse):
                for part in msg.parts:
                    if isinstance(part, ToolCallPart):
                        args = part.args_as_dict() if hasattr(part, "args_as_dict") else {}
                        tool_calls.append(_ToolCallRecord(name=part.tool_name, arguments=args))
                    elif hasattr(part, "content") and isinstance(part.content, str):
                        responses.append(part.content)
        return cls(_tool_calls=tool_calls, _responses=responses, success=success)


# ---------------------------------------------------------------------------
# Eval runner
# ---------------------------------------------------------------------------

async def run_eval(agent: Agent, prompt: str) -> EvalResult:
    """Run a PydanticAI agent against a prompt and return a structured EvalResult."""
    try:
        async with agent.run_mcp_servers():
            result = await agent.run(prompt)
        return EvalResult.from_pydantic(result, success=True)
    except Exception as exc:
        return EvalResult(_tool_calls=[], _responses=[], success=False, error=str(exc))


def _make_mcp_server() -> MCPServerStdio:
    """Build the MCPServerStdio for the Windows MCP server."""
    return MCPServerStdio(SERVER_COMMAND[0], args=SERVER_COMMAND[1:])


def make_agent(*, system_prompt: str = SYSTEM_PROMPT) -> Agent:
    """Create a PydanticAI agent with the Windows MCP server attached."""
    return Agent(
        "openai:gpt-4.1",
        system_prompt=system_prompt,
        mcp_servers=[_make_mcp_server()],
    )


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture(scope="session", autouse=True)
def _build_mcp_server():
    """Build the MCP server once before any test runs."""
    result = subprocess.run(
        ["dotnet", "build", str(PROJECT_PATH), "-c", "Release", "-v:q"],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        pytest.fail(f"dotnet build failed:\n{result.stdout}\n{result.stderr}")


@pytest.fixture(scope="session")
def copilot_auth():
    """Require GitHub auth via GITHUB_TOKEN or an existing gh login.

    Configures the OpenAI-compatible GitHub Models endpoint so that
    Agent("openai:gpt-4.1") routes through GitHub Models, which
    supports full OpenAI-style tool calling.
    """
    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        result = subprocess.run(
            ["gh", "auth", "token"],
            capture_output=True,
            text=True,
        )
        if result.returncode == 0:
            token = result.stdout.strip()

    if not token:
        pytest.skip("GITHUB_TOKEN not set and `gh auth token` failed")

    # Point PydanticAI's OpenAI client at GitHub Models.
    os.environ.setdefault("OPENAI_API_KEY", token)
    os.environ.setdefault("OPENAI_BASE_URL", "https://models.inference.ai.azure.com")
    return "gh"


@pytest.fixture
def aitest_run(copilot_auth):
    """Fixture that runs a PydanticAI agent and returns an EvalResult.

    Usage::

        async def test_foo(aitest_run, my_agent):
            result = await aitest_run(my_agent, "Do the thing")
            assert result.tool_was_called("file_save")
    """
    async def _run(agent: Agent, prompt: str) -> EvalResult:
        return await run_eval(agent, prompt)

    return _run


@pytest.fixture(autouse=True)
def _kill_test_processes():
    """Kill only the GUI processes spawned during a test (Notepad, Paint, etc.).

    Snapshots PIDs before the test, then after the test kills only NEW processes
    in the known test-app list. Pre-existing user windows are never touched.
    """
    _TEST_APPS = {"notepad", "mspaint", "calc", "wordpad"}

    def _get_pids(name):
        r = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             f"Get-Process -Name '{name}' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id"],
            capture_output=True, text=True,
        )
        return {int(p) for p in r.stdout.split() if p.strip().isdigit()}

    before = {app: _get_pids(app) for app in _TEST_APPS}

    yield  # ← test runs here

    for app, pre_pids in before.items():
        new_pids = _get_pids(app) - pre_pids
        for pid in new_pids:
            subprocess.run(
                ["powershell", "-NoProfile", "-Command",
                 f"Stop-Process -Id {pid} -Force -ErrorAction SilentlyContinue"],
                capture_output=True,
            )


@pytest.fixture
def temp_dir():
    """Temporary directory for test artifacts (cleaned up after test)."""
    d = Path(tempfile.mkdtemp(prefix="mcp-llm-test-"))
    yield d
    import shutil
    shutil.rmtree(d, ignore_errors=True)


@pytest.fixture
def run_id():
    """Unique run identifier for test isolation."""
    return str(uuid.uuid4())[:8]


# ---------------------------------------------------------------------------
# Assertion helpers
# ---------------------------------------------------------------------------


def assert_tool_called(result: EvalResult, tool_name: str):
    """Assert a tool was called at least once."""
    assert result.tool_was_called(tool_name), f"Expected tool '{tool_name}' to be called"


def assert_tool_call_order(result: EvalResult, first: str, second: str):
    """Assert that first tool was called before second tool."""
    names = [c.name for c in result.all_tool_calls]
    assert first in names, f"Tool '{first}' was never called"
    assert second in names, f"Tool '{second}' was never called"
    assert names.index(first) < names.index(second), (
        f"Expected '{first}' before '{second}', got order: {names}"
    )


def assert_output_matches(result: EvalResult, pattern: str):
    """Assert final response matches regex pattern."""
    text = " ".join(result.all_responses)
    assert re.search(pattern, text, re.IGNORECASE), (
        f"Expected pattern '{pattern}' in response"
    )


def assert_output_not_contains(result: EvalResult, value: str):
    """Assert final response does not contain value (case-insensitive)."""
    text = " ".join(result.all_responses).lower()
    assert value.lower() not in text, f"Response should not contain '{value}'"


def assert_tool_param_matches(result: EvalResult, tool_name: str, param: str, pattern: str):
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


def assert_tool_param_equals(result: EvalResult, tool_name: str, param: str, value):
    """Assert a tool was called with an exact parameter value."""
    calls = result.tool_calls_for(tool_name)
    assert calls, f"Tool '{tool_name}' was never called"
    matched = any(c.arguments.get(param) == value for c in calls)
    assert matched, (
        f"No call to '{tool_name}' had param '{param}' == {value!r}"
    )


def assert_quality(result: EvalResult):
    """Standard quality assertions used across all tests."""
    assert result.success, f"Agent failed: {result.error}"
    assert not result.asked_for_clarification, "Agent asked for clarification"

