import asyncio
import importlib.util
import subprocess
import sys
from contextlib import asynccontextmanager
from dataclasses import replace
from pathlib import Path
from types import SimpleNamespace

import anyio
import pytest
from pytest_skill_engineering.copilot import RequestAudit

from usage_evals.comparison import (
    build_comparison_agent,
    comparison_matrix,
    desktop_helpers,
    require_comparison_budget,
    require_execution_evidence,
)

ROOT = Path(__file__).resolve().parents[2]


@pytest.mark.parametrize("failure", [None, "startup", "teardown", "cancel"])
async def test_shared_mcp_lifetime_stays_in_one_task(tmp_path, monkeypatch, failure):
    spec = importlib.util.spec_from_file_location(
        "comparison_live_test", ROOT / "tests" / "comparison" / "test_real_apps.py"
    )
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    entries, exits = [], []
    closing, finish = asyncio.Event(), asyncio.Event()

    @asynccontextmanager
    async def transport(*args, **kwargs):
        entries.append(asyncio.current_task())
        try:
            async with anyio.create_task_group():
                yield (None, None)
        finally:
            closing.set()
            if failure == "cancel":
                await finish.wait()
            exits.append(asyncio.current_task())
        if failure == "teardown":
            raise RuntimeError("transport cleanup failed")

    class Client:
        async def initialize(self):
            if failure == "startup":
                raise RuntimeError("initialization failed")

        async def list_tools(self):
            return SimpleNamespace(tools=["test-tool"])

    @asynccontextmanager
    async def session(*args):
        async with anyio.create_task_group():
            yield Client()

    monkeypatch.setattr(module, "stdio_client", transport)
    monkeypatch.setattr(module, "ClientSession", session)
    request = SimpleNamespace(config=SimpleNamespace(getoption=lambda _: "server.exe"))
    fixture = module.comparison_server.__wrapped__(request, (tmp_path, {}, {}))
    if failure == "startup":
        with pytest.raises(BaseExceptionGroup, match="unhandled errors"):
            await asyncio.create_task(anext(fixture))
    else:
        _, catalog = await asyncio.create_task(anext(fixture))
        assert catalog == ["test-tool"]
        if failure == "teardown":
            with pytest.raises(RuntimeError, match="transport cleanup failed"):
                await asyncio.create_task(fixture.aclose())
        elif failure == "cancel":
            teardown = asyncio.create_task(fixture.aclose())
            await asyncio.wait_for(closing.wait(), timeout=5)
            teardown.cancel()
            await asyncio.sleep(0)
            assert not exits and not teardown.done()
            finish.set()
            with pytest.raises(asyncio.CancelledError):
                await teardown
        else:
            await asyncio.create_task(fixture.aclose())
    assert len(entries) == 1
    assert exits == entries


def test_published_framework_audit_objects_are_supported():
    result = captured_result()
    assert isinstance(result.request_audit[0], RequestAudit)
    assert (
        require_execution_evidence(result, "gpt-6.1-sol", ["screenshot_control"])
        == "same-instructions"
    )


def test_actual_framework_configuration_owns_session_limits_and_auditing(tmp_path):
    from copilot import Tool, ToolResult

    async def handler(_):
        return ToolResult(text_result_for_llm="Unit test only.")

    tools = [Tool(name="unit_tool", description="Unit test", parameters={}, handler=handler)]
    agent = build_comparison_agent("gpt-6.1-sol", 600, 80, tmp_path, tools)
    assert agent.client_mode == "empty"
    assert agent.max_tool_calls == 80
    assert agent.max_retries == 0
    assert agent.timeout_s == 600
    assert agent.audit_requests and agent.image_detail == "high"
    config = agent.build_session_config()
    assert config["tools"] == tools
    assert config["available_tools"] == ["unit_tool"]
    assert config["reasoning_effort"] == "medium"
    assert config["system_message"]["mode"] == "replace"
    assert config["skip_custom_instructions"] is True


def test_desktop_helpers_expose_independent_fixture_checks(tmp_path):
    desktop = desktop_helpers()
    assert set(desktop.APPS) == {"notepad", "word", "powerpoint", "chrome"}
    source, output = desktop.create_fixture("notepad", tmp_path)
    assert source.is_file()
    assert not desktop.verify_output("notepad", output)["success"]
    assert "Notepad" in desktop.task_prompt("notepad", output)


def test_retired_helpers_cannot_start_model_sessions():
    for name in ("benchmark-real-apps.py", "benchmark-screenshot-readability.py"):
        source = (ROOT.parents[1] / "scripts" / name).read_text(encoding="utf-8")
        for prohibited in ("CopilotClient", "create_session(", "send_and_wait("):
            assert prohibited not in source


def test_matrix_contains_all_16_pairs_and_alternates_routes():
    rows = comparison_matrix()
    assert len(rows) == len(set(rows)) == 16
    assert rows[:4] == [
        ("notepad", "gpt-6.1-sol", "controls"),
        ("notepad", "gpt-6.1-sol", "screenshots"),
        ("notepad", "gpt-6-luna", "screenshots"),
        ("notepad", "gpt-6-luna", "controls"),
    ]


@pytest.mark.parametrize(
    ("selected", "cap", "models", "timeout", "calls"),
    [
        (16, 15, ["gpt-6.1-sol"], 600, 80),
        (0, 16, ["gpt-6.1-sol"], 600, 80),
        (16, 16, [], 600, 80),
        (16, 16, ["gpt-6.1-sol"], 0, 80),
        (16, 16, ["gpt-6.1-sol"], 600, 0),
    ],
)
def test_explicit_budget_is_required_before_model_or_desktop_access(
    selected, cap, models, timeout, calls
):
    with pytest.raises(ValueError):
        require_comparison_budget(selected, cap, models, timeout, calls)


def test_approved_budget_is_accepted():
    require_comparison_budget(16, 16, ["gpt-6.1-sol"], 600, 80)


def captured_result(**changes):
    return SimpleNamespace(
        **{
            "model_used": "gpt-6.1-sol",
            "evidence_complete": True,
            "stop_reason": "completed",
            "error": None,
            "request_audit": [
                RequestAudit(
                    request_id="unit",
                    model="gpt-6.1-sol",
                    tool_names=["screenshot_control"],
                    reasoning_effort="medium",
                    image_count=1,
                    image_details=["high"],
                    instructions_sha256="same-instructions",
                )
            ],
            **changes,
        }
    )


@pytest.mark.parametrize("reason", ["completed", "tool_budget_exceeded", "timeout"])
def test_complete_evidence_keeps_expected_budget_failures(reason):
    result = captured_result(stop_reason=reason)
    assert (
        require_execution_evidence(result, "gpt-6.1-sol", ["screenshot_control"])
        == "same-instructions"
    )


@pytest.mark.parametrize(
    "changes",
    [
        {"model_used": "another-model"},
        {"evidence_complete": False},
        {"stop_reason": "execution_error"},
        {"stop_reason": "request_audit_error"},
        {"stop_reason": "cleanup_error"},
        {"request_audit": []},
    ],
)
def test_missing_or_untrustworthy_evidence_stops_the_matrix(changes):
    with pytest.raises(RuntimeError):
        require_execution_evidence(
            captured_result(**changes), "gpt-6.1-sol", ["screenshot_control"]
        )


def test_unapproved_request_settings_are_not_counted_as_a_valid_comparison():
    for changes in (
        {"tool_names": ["powershell"]},
        {"reasoning_effort": "high"},
        {"image_details": ["low"]},
        {"image_count": 2},
    ):
        result = captured_result()
        result.request_audit[0] = replace(result.request_audit[0], **changes)
        with pytest.raises(RuntimeError):
            require_execution_evidence(result, "gpt-6.1-sol", ["screenshot_control"])


def test_every_websocket_message_is_checked_even_with_a_shared_request_id():
    result = captured_result()
    first = result.request_audit[0]
    result.request_audit.append(replace(first, image_count=0, image_details=[]))
    assert (
        require_execution_evidence(result, "gpt-6.1-sol", ["screenshot_control"])
        == "same-instructions"
    )
    result.request_audit[1] = replace(first, reasoning_effort="high")
    with pytest.raises(RuntimeError, match="approved comparison settings"):
        require_execution_evidence(result, "gpt-6.1-sol", ["screenshot_control"])


def run_pytest(*arguments):
    return subprocess.run(
        [sys.executable, "-m", "pytest", "tests/comparison", *arguments],
        cwd=ROOT,
        capture_output=True,
        text=True,
        timeout=30,
    )


def test_collection_needs_no_desktop_build_or_model_access():
    result = run_pytest("--collect-only", "-q")
    assert result.returncode == 0, result.stdout + result.stderr
    assert "16 tests collected" in result.stdout


def test_comparison_is_skipped_without_explicit_opt_in():
    result = run_pytest("-q")
    assert result.returncode == 0, result.stdout + result.stderr
    assert "16 skipped" in result.stdout


def test_insufficient_approved_budget_rejects_whole_selected_matrix():
    result = run_pytest(
        "--run-comparison",
        "--comparison-model",
        "gpt-6.1-sol",
        "--comparison-max-runs",
        "7",
        "--comparison-timeout",
        "600",
        "--comparison-max-calls",
        "80",
        "-q",
    )
    assert result.returncode != 0
    assert "Selected 8 comparison runs; approved cap is 7" in result.stdout + result.stderr


def test_live_comparison_requires_a_fresh_persistent_report():
    result = run_pytest(
        "--run-comparison",
        "--comparison-model",
        "gpt-6.1-sol",
        "--comparison-max-runs",
        "8",
        "--comparison-timeout",
        "600",
        "--comparison-max-calls",
        "80",
        "-q",
    )
    assert result.returncode != 0
    assert "comparison-output" in result.stdout + result.stderr
