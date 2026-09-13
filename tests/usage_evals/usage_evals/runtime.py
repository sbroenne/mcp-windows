import ctypes
import hashlib
import inspect
import json
import os
import platform
import re
import subprocess
from dataclasses import dataclass, replace
from importlib.metadata import version
from pathlib import Path

import psutil
from pytest_skill_engineering.copilot import CopilotCLIPersona, CopilotEval

from usage_evals.policy import is_cli_invocation


@dataclass(frozen=True)
class ProcessRecord:
    pid: int
    created: float
    document_matches: bool
    launch_verified: bool = False


def require_capture_api(call_type, result_type) -> None:
    if not hasattr(call_type, "completion_received") or not hasattr(
        result_type, "evidence_complete"
    ):
        raise RuntimeError(
            "This framework lacks the required capture API. Use the evidence-capture framework "
            "worktree with uv run --with-editable PATH until those changes are published. "
            "No live evaluation has started."
        )


def build_agent(
    interface: str,
    model: str,
    timeout: int,
    working_directory: Path,
    cli: Path,
    server: Path,
    tool_names: set[str],
    *,
    reasoning_effort: str | None = None,
) -> CopilotEval:
    common = dict(
        name=f"windows-usage-{interface}",
        model=model,
        reasoning_effort=reasoning_effort,
        timeout_s=float(timeout),
        max_retries=0,
        working_directory=str(working_directory),
        persona=CopilotCLIPersona(),
        extra_config={
            "config_directory": str(working_directory / "configuration"),
            "enable_config_discovery": False,
            "skip_custom_instructions": True,
            "enable_on_demand_instruction_discovery": False,
            "enable_file_hooks": False,
        },
    )
    if interface == "cli":
        return CopilotEval(
            **common,
            allowed_tools=["powershell"],
            instructions=(
                f"Windows automation is available through this executable: {cli}\n"
                "Complete the user's task using that executable through PowerShell. "
                "Use one invocation with literal arguments per shell call. "
                "Do not use other executables, scripts, file APIs, or another automation route. "
                "Work only with the task's documents. Report what actually happened."
            ),
        )
    if interface == "mcp":
        return CopilotEval(
            **common,
            allowed_tools=sorted(tool_names | {f"windows-{name}" for name in tool_names}),
            mcp_servers={
                "windows": {"command": str(server), "args": [], "type": "stdio", "tools": ["*"]}
            },
            instructions=(
                "Complete the user's task using the attached Windows automation tools. "
                "Work only with the task's documents. Report what actually happened."
            ),
        )
    raise ValueError(f"Unknown interface: {interface}")


def discover_tools(cli: Path) -> set[str]:
    completed = subprocess.run(
        [str(cli), "tools", "--json"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        check=True,
        timeout=30,
    )
    names = {tool["name"] for tool in json.loads(completed.stdout)["tools"]}
    if not names or not all(isinstance(name, str) and name for name in names):
        raise ValueError("CLI discovery returned no valid tool names.")
    return names


def build_metadata(cli: Path, server: Path) -> dict:
    hashes = {}
    paths = {cli, server}
    for executable in (cli, server):
        paths.update(
            path
            for suffix in (".dll", ".deps.json", ".runtimeconfig.json")
            if (path := executable.with_suffix(suffix)).is_file()
        )
    for path in sorted(paths):
        with path.open("rb") as stream:
            hashes[str(path)] = hashlib.file_digest(stream, "sha256").hexdigest()
    package = Path(inspect.getfile(CopilotEval)).parents[1]
    source_hash = hashlib.sha256()
    for path in sorted(package.rglob("*.py")):
        source_hash.update(str(path.relative_to(package)).encode("utf-8"))
        source_hash.update(b"\0")
        source_hash.update(path.read_bytes())
    return {
        "framework": version("pytest-skill-engineering"),
        "framework_source": source_hash.hexdigest(),
        "sdk": version("github-copilot-sdk"),
        "python": platform.python_version(),
        "platform": platform.platform(),
        "build_hashes": hashes,
    }


def require_interactive_desktop() -> None:
    if os.name != "nt":
        raise RuntimeError("Live usage evaluations require Windows.")
    from ctypes import wintypes

    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.OpenInputDesktop.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    user32.OpenInputDesktop.restype = wintypes.HANDLE
    user32.CloseDesktop.argtypes = [wintypes.HANDLE]
    user32.CloseDesktop.restype = wintypes.BOOL
    desktop = user32.OpenInputDesktop(0, False, 1)
    if not desktop:
        raise ctypes.WinError(ctypes.get_last_error())
    if not user32.CloseDesktop(desktop):
        raise ctypes.WinError(ctypes.get_last_error())


def snapshot_notepads(run_id: str) -> list[ProcessRecord]:
    """Match a run's unique document titles, not just application names."""
    from ctypes import wintypes

    user32 = ctypes.WinDLL("user32", use_last_error=True)
    callback_type = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    user32.EnumWindows.argtypes = [callback_type, wintypes.LPARAM]
    user32.EnumWindows.restype = wintypes.BOOL
    user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
    user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
    user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    matching_pids: set[int] = set()

    @callback_type
    def visit(hwnd, _):
        length = user32.GetWindowTextLengthW(hwnd)
        title = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, title, length + 1)
        if run_id and run_id in title.value:
            pid = wintypes.DWORD()
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            matching_pids.add(pid.value)
        return True

    if not user32.EnumWindows(visit, 0):
        raise ctypes.WinError(ctypes.get_last_error())
    return [
        ProcessRecord(p.info["pid"], p.info["create_time"], p.info["pid"] in matching_pids)
        for p in psutil.process_iter(["pid", "name", "create_time"])
        if (p.info["name"] or "").lower() == "notepad.exe"
    ]


def identify_launched_notepads(records, calls, started_at: float, cli: str) -> list[ProcessRecord]:
    launched = set()
    for call in calls:
        is_app = call.name in {"app", "windows-app"}
        if call.name == "powershell":
            command = str(call.arguments.get("command", ""))
            is_app = is_cli_invocation(command, cli) and bool(
                re.match(r"""^(?:&\s+)?(?:'[^']*'|"[^"]*"|\S+)\s+app(?:\s|$)""", command)
            )
        if not is_app or not isinstance(call.result, str):
            continue
        try:
            payload, _ = json.JSONDecoder().raw_decode(call.result.lstrip())
        except ValueError:
            continue
        if not isinstance(payload, dict) or payload.get("success") is not True:
            continue
        window = payload.get("window")
        if not isinstance(window, dict) or str(window.get("processName")).lower() != "notepad":
            continue
        pid = window.get("pid")
        if type(pid) is int and pid > 0:
            launched.add(pid)
    return [
        replace(record, launch_verified=record.pid in launched and record.created >= started_at)
        for record in records
    ]


def cleanup_owned(records: list[ProcessRecord], get_process=psutil.Process) -> list[int]:
    cleaned = []
    for record in records:
        if not (record.document_matches or record.launch_verified):
            continue
        try:
            process = get_process(record.pid)
            if process.create_time() != record.created:
                raise RuntimeError(f"Process {record.pid} identity changed; refusing cleanup.")
            process.terminate()
            process.wait(timeout=5)
        except psutil.NoSuchProcess:
            pass  # The specifically identified process has already exited.
        cleaned.append(record.pid)
    unowned = []
    for record in records:
        if record.document_matches or record.launch_verified:
            continue
        try:
            process = get_process(record.pid)
            if process.create_time() != record.created:
                raise RuntimeError(f"Process {record.pid} identity changed; refusing cleanup.")
            # Launch helpers can exit after their owned application closes. Never terminate them.
            process.wait(timeout=5)
        except psutil.NoSuchProcess:
            continue
        except psutil.TimeoutExpired:
            unowned.append(record.pid)
    if unowned:
        raise RuntimeError(
            f"Unowned Notepad processes remain: {unowned}. They were not terminated. "
            "Inspect the disposable desktop before another run."
        )
    return cleaned
