"""Run the real-app matrix through pytest-skill-engineering, never a private SDK runner."""

import asyncio
import os
import shutil
import subprocess
import uuid
from pathlib import Path

import psutil
import pytest
import pytest_asyncio
from mcp import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client

from usage_evals.comparison import (
    APPS,
    build_comparison_agent,
    comparison_matrix,
    desktop_helpers,
    guarded_tools,
    require_execution_evidence,
)
from usage_evals.policy import validate_desktop
from usage_evals.runtime import build_metadata, require_interactive_desktop

pytestmark = [pytest.mark.comparison_live, pytest.mark.asyncio(loop_scope="session")]


@pytest.fixture(scope="session")
def comparison_inputs(request):
    config = request.config
    require_interactive_desktop()
    existing = [
        process.info["pid"]
        for process in psutil.process_iter(["pid", "name"])
        if (process.info["name"] or "").lower() in {"notepad.exe", "winword.exe", "powerpnt.exe"}
    ]
    if existing:
        raise RuntimeError(
            f"Close existing Notepad, Word and PowerPoint instances first: {existing}"
        )
    root = Path(config.getoption("--comparison-output")).resolve()
    root.mkdir(parents=True, exist_ok=False)
    desktop = desktop_helpers()
    templates = {app: desktop.create_fixture(app, root / "templates" / app)[0] for app in APPS}
    server = Path(config.getoption("--comparison-server")).resolve()
    with pytest.MonkeyPatch.context() as patch:
        if not os.environ.get("GITHUB_TOKEN") and not os.environ.get("GH_TOKEN"):
            token = subprocess.run(
                ["gh", "auth", "token", "--hostname", "github.com"],
                capture_output=True,
                text=True,
                check=True,
                timeout=30,
            ).stdout.strip()
            if not token:
                raise RuntimeError("GitHub authentication returned no token.")
            patch.setenv("GITHUB_TOKEN", token)
        yield root, templates, {"runtime": build_metadata(server, server)}


@pytest_asyncio.fixture(scope="session", loop_scope="session")
async def comparison_server(request, comparison_inputs):
    root, _, _ = comparison_inputs
    executable = str(Path(request.config.getoption("--comparison-server")).resolve())
    with (root / "server.log").open("w", encoding="utf-8") as log:
        async with (
            stdio_client(
                StdioServerParameters(command=executable, cwd=str(root), env=dict(os.environ)),
                errlog=log,
            ) as streams,
            ClientSession(*streams) as client,
        ):
            async with asyncio.timeout(30):
                await client.initialize()
                catalog = (await client.list_tools()).tools
            yield client, catalog


async def test_real_app(
    comparison_case, comparison_inputs, comparison_server, request, copilot_eval, record_property
):
    desktop = desktop_helpers()
    app, model, route = comparison_case
    config = request.config
    root, templates, observed = comparison_inputs
    mcp, catalog = comparison_server
    matrix = comparison_matrix(
        config.getoption("--comparison-model"),
        config.getoption("--comparison-app"),
        config.getoption("--comparison-route"),
    )
    directory = root / f"trial-{matrix.index(comparison_case) + 1:03d}"
    directory.mkdir()
    source = directory / f"input-{uuid.uuid4().hex[:12]}{templates[app].suffix}"
    shutil.copyfile(templates[app], source)
    output = directory / f"completed{source.suffix}"
    workspace = directory / "agent"
    (workspace / "configuration").mkdir(parents=True)
    initial_hash = desktop.digest(source)
    server_path = Path(config.getoption("--comparison-server")).resolve()
    record_property("runtime", observed["runtime"])
    record_property(
        "comparison",
        {
            "app": app,
            "model": model,
            "route": route,
            "desktop_mode": validate_desktop(os.environ),
            "source_revision": subprocess.run(
                ["git", "rev-parse", "HEAD"], capture_output=True, text=True, check=True
            ).stdout.strip(),
            "server_sha256": desktop.digest(server_path.with_suffix(".dll")),
            "input_sha256": initial_hash,
            "directory": str(directory),
            "timeout_seconds": config.getoption("--comparison-timeout"),
            "max_tool_calls": config.getoption("--comparison-max-calls"),
            "reasoning_effort": "medium",
            "image_detail": "high",
            "notepad_verification": "exact text after CRLF normalization, including final newline",
            "timing_scope": "framework session including startup and cleanup",
            "timeout_scope": "framework startup, session creation and execution",
        },
    )
    booking = desktop.BookingServer(output) if app == "chrome" else None
    owned = bridge = None
    try:
        if observed.setdefault(f"input:{app}", initial_hash) != initial_hash:
            raise RuntimeError(f"The {app} input changed during the comparison.")
        server_hash = desktop.digest(server_path.with_suffix(".dll"))
        if observed.setdefault("server_sha256", server_hash) != server_hash:
            raise RuntimeError("The server build changed during the comparison.")
        owned = desktop.OwnedApp(app, source, directory, booking.url if booking else None)
        activated = await mcp.call_tool(
            "window_management", {"action": "activate", "handle": owned.root}
        )
        if activated.is_error:
            raise RuntimeError(f"Could not activate owned app: {activated.content}")
        if app == "notepad":
            record_property(
                "notepad_setup", await desktop.isolate_notepad_window(mcp, owned, source)
            )
        record_property(
            "application",
            {
                "version": owned.version,
                "initial_bounds": owned.initial_bounds,
                "initial_dpi": owned.initial_dpi,
            },
        )
        if observed.setdefault(app, owned.version) != owned.version:
            raise RuntimeError(f"The {app} version changed during the comparison.")
        bridge = desktop.TrialBridge(mcp, owned, route, output, directory)
        tools = guarded_tools(catalog, bridge, route)
        agent = build_comparison_agent(
            model,
            config.getoption("--comparison-timeout"),
            config.getoption("--comparison-max-calls"),
            workspace,
            tools,
        )
        prompt = desktop.task_prompt(app, output) + f"\nApplication window handle: {owned.root}."
        result = await copilot_eval(agent, prompt)
        bridge.closed = True
        async with bridge.lock:
            pass
        verified = desktop.verify_output(app, output)
        unchanged = desktop.digest(source) == initial_hash
        record_property(
            "verification",
            {
                "output": verified,
                "source_unchanged": unchanged,
                "output_sha256": desktop.digest(output) if output.exists() else None,
            },
        )
        if booking:
            record_property("submitted_values", booking.submissions)
        instruction_hash = require_execution_evidence(result, model, [tool.name for tool in tools])
        if observed.setdefault("instructions_sha256", instruction_hash) != instruction_hash:
            raise RuntimeError("Actual instructions changed between comparison cases.")
    except Exception as error:
        record_property("comparison_error", f"{type(error).__name__}: {error}")
        request.session.shouldstop = (
            "Comparison setup, transport, or evidence failed; no more model calls."
        )
        raise
    finally:
        if bridge is not None:
            bridge.closed = True
            async with bridge.lock:
                pass
        try:
            if owned is not None:
                owned.close()
                record_property("cleanup", {"terminated_owned_pids": owned.cleanup})
        except Exception:
            request.session.shouldstop = "Owned app cleanup failed; no more model calls."
            raise
        finally:
            if booking is not None:
                booking.close()
    assert result.success, f"Model session did not complete: {result.error}"
    assert verified["success"] and unchanged, "Independent saved-result checks failed."
