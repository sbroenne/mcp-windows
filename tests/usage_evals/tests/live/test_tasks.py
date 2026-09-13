import os
import subprocess
import time
import uuid
from dataclasses import asdict
from pathlib import Path

import pytest

from usage_evals.cases import prepare_case, verify_files
from usage_evals.policy import classify_calls
from usage_evals.runtime import (
    build_agent,
    build_metadata,
    cleanup_owned,
    discover_tools,
    identify_launched_notepads,
    require_interactive_desktop,
    snapshot_notepads,
)

pytestmark = pytest.mark.usage_live


async def test_windows_usage(
    usage_case, request, tmp_path, copilot_eval, record_property, monkeypatch
):
    name, interface, repetition, variant, model = usage_case
    config = request.config
    prefix = "--usage-baseline-" if variant == "baseline" else "--usage-"
    cli = Path(config.getoption(prefix + "cli")).resolve()
    server = Path(config.getoption(prefix + "server")).resolve()
    require_interactive_desktop()
    if snapshot_notepads(""):
        pytest.fail("Notepad is already running; refusing to touch existing application state.")
    started_at = time.time()
    if not os.environ.get("GITHUB_TOKEN") and os.environ.get("GH_TOKEN"):
        monkeypatch.setenv("GITHUB_TOKEN", os.environ["GH_TOKEN"])
    if not os.environ.get("GITHUB_TOKEN"):
        subprocess.run(
            ["gh", "auth", "status", "--hostname", "github.com"],
            capture_output=True,
            check=True,
            timeout=30,
        )

    run_id = uuid.uuid4().hex
    case = prepare_case(name, tmp_path / "documents", run_id)
    workspace = tmp_path / "agent"
    workspace.mkdir()
    (workspace / "configuration").mkdir()
    tools = discover_tools(cli)
    agent = build_agent(
        interface,
        model,
        config.getoption("--usage-timeout"),
        workspace,
        cli,
        server,
        tools,
        reasoning_effort=config.getoption("--usage-reasoning-effort"),
    )
    record_property(
        "usage",
        {
            "case": name,
            "interface": interface,
            "variant": variant,
            "repetition": repetition,
            "run_id": run_id,
            "model": agent.model,
            "reasoning_effort": agent.reasoning_effort,
            "session_isolation": agent.extra_config,
            "timeout_s": agent.timeout_s,
            "documents": str(tmp_path / "documents"),
            "configured_tools": agent.allowed_tools,
            **build_metadata(cli, server),
        },
    )
    result = None
    try:
        result = await copilot_eval(agent, case.prompt)
        verdict = verify_files(case)
        route_issues = classify_calls(interface, result.all_tool_calls, str(cli), tools)
        record_property("verification", asdict(verdict))
        record_property(
            "interface_route",
            {
                "issues": route_issues,
                "observed_tools": [call.name for call in result.all_tool_calls],
                "limitation": "Route checks are not a sandbox or proof of no indirect bypass.",
            },
        )
        record_property(
            "execution_evidence",
            {
                "session_success": result.success,
                "model_used": result.model_used,
                "evidence_complete": result.evidence_complete,
                "capture_errors": result.capture_errors,
            },
        )
        assert result.success, f"Session failed: {result.error}"
        assert result.model_used == model, (
            f"Model identity is not verified: requested {model}, reported {result.model_used}."
        )
        assert result.evidence_complete, (
            "The framework did not capture complete execution evidence."
        )
        assert not route_issues, f"Interface usage is not verified: {route_issues}"
        assert verdict.passed, f"Independent document verification failed: {verdict.checks}"
    finally:
        records = identify_launched_notepads(
            snapshot_notepads(run_id),
            result.all_tool_calls if result is not None else [],
            started_at,
            str(cli),
        )
        record_property("cleanup_candidates", [asdict(record) for record in records])
        cleaned = cleanup_owned(records)
        record_property("cleanup", {"terminated_owned_pids": cleaned})
