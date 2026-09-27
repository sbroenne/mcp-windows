"""Control Windows Update from a hosted runner, surviving guest restarts."""

import argparse
import base64
from datetime import datetime, timezone
import json
from pathlib import Path
import subprocess
import tempfile
import time
import uuid


GUEST_PATH = r"C:\ProgramData\WindowsMcp\Maintenance\update-runner.ps1"
MARKER = "WINDOWS_MCP_MAINTENANCE="


def parse_guest_response(response, pass_id):
    lines = [
        line[len(MARKER):]
        for value in response.get("value", [])
        for line in value.get("message", "").splitlines()
        if line.startswith(MARKER)
    ]
    if len(lines) != 1:
        raise RuntimeError(f"Expected one guest result, received: {response}")
    result = json.loads(lines[0])
    if result.get("passId") != pass_id:
        raise RuntimeError("Guest result belongs to a different update pass")
    return result


class AzureRunner:
    def __init__(self, resource_group, vm_name, root, timeout_minutes=150):
        self.target = ["--resource-group", resource_group, "--name", vm_name]
        self.root = root
        self.deadline = time.monotonic() + timeout_minutes * 60

    def remaining(self):
        seconds = self.deadline - time.monotonic()
        if seconds <= 0:
            raise TimeoutError("Runner maintenance exceeded its time budget")
        return seconds

    def az(self, *arguments):
        result = subprocess.run(
            ["az", *arguments, *self.target, "--only-show-errors", "--output", "json"],
            capture_output=True, text=True, timeout=min(180, self.remaining()),
            check=False,
        )
        if result.returncode:
            raise RuntimeError(f"Azure command failed: {result.stderr.strip()}")
        return json.loads(result.stdout) if result.stdout.strip() else None

    def guest(self, script, pass_id):
        with tempfile.TemporaryDirectory(prefix="runner-maintenance-") as directory:
            path = Path(directory) / "command.ps1"
            path.write_text("$ErrorActionPreference = 'Stop'\n" + script, encoding="utf-8")
            response = self.az(
                "vm", "run-command", "invoke", "--command-id", "RunPowerShellScript",
                "--scripts", f"@{path}",
            )
        return parse_guest_response(response, pass_id)

    def pause(self):
        time.sleep(min(30, self.remaining()))

    def wait_ready(self):
        deadline = min(self.deadline, time.monotonic() + 20 * 60)
        while time.monotonic() < deadline:
            try:
                status = self.az(
                    "vm", "get-instance-view", "--query",
                    "instanceView.vmAgent.statuses[0].displayStatus",
                )
                if status == "Ready":
                    return
                print(f"Waiting for Azure guest agent: {status}", flush=True)
            except (RuntimeError, subprocess.TimeoutExpired) as error:
                print(f"Guest agent probe failed; retrying: {error}", flush=True)
            self.pause()
        raise TimeoutError("Azure guest agent did not become ready within 20 minutes")

    def install_worker(self):
        source = self.root / "infrastructure" / "azure" / "update-runner.ps1"
        encoded = base64.b64encode(source.read_bytes()).decode("ascii")
        pass_id = uuid.uuid4().hex
        # Keep the SYSTEM task's script and output outside the runner's work directory.
        self.guest(
            f"""
$task = Get-ScheduledTask -TaskName 'WindowsMcp-Windows-Update' -ErrorAction SilentlyContinue
if ($task -and $task.State -eq 'Running') {{ throw 'An update task is already running.' }}
$directory = Split-Path '{GUEST_PATH}'
New-Item $directory -ItemType Directory -Force | Out-Null
& icacls.exe $directory /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) {{ throw 'Could not protect the maintenance directory.' }}
[IO.File]::WriteAllBytes('{GUEST_PATH}', [Convert]::FromBase64String('{encoded}'))
Write-Output 'WINDOWS_MCP_MAINTENANCE={{"passId":"{pass_id}","state":"ready"}}'
""", pass_id,
        )

    def update_pass(self, pass_id):
        result = self.guest(f"& '{GUEST_PATH}' -Action Start -PassId {pass_id}", pass_id)
        if result.get("state") != "started":
            raise RuntimeError(f"Update task did not start: {result}")
        deadline = min(self.deadline, time.monotonic() + 65 * 60)
        while time.monotonic() < deadline:
            self.pause()
            result = self.guest(f"& '{GUEST_PATH}' -Action Status -PassId {pass_id}", pass_id)
            if result.get("state") != "running":
                return result
            print(f"Update pass {pass_id} is running", flush=True)
        raise TimeoutError(f"Update pass {pass_id} did not finish within 65 minutes")

    def restart(self):
        # Do not replay a restart after a timeout: its outcome is then unknown.
        self.az("vm", "restart")

    def wait_desktop(self):
        deadline = min(self.deadline, time.monotonic() + 20 * 60)
        pass_id = uuid.uuid4().hex
        while time.monotonic() < deadline:
            try:
                result = self.guest(f"& '{GUEST_PATH}' -Action Health -PassId {pass_id}", pass_id)
                if result.get("state") == "ready":
                    return
                print(f"Waiting for interactive runner: {result}", flush=True)
            except (RuntimeError, subprocess.TimeoutExpired) as error:
                print(f"Desktop probe failed; retrying: {error}", flush=True)
            self.pause()
        raise TimeoutError("Interactive runner did not recover, or a restart remains pending")


def maintain(runner, report, max_passes=4):
    runner.wait_ready()
    runner.install_worker()
    restarted = False
    for _ in range(max_passes):
        result = runner.update_pass(uuid.uuid4().hex)
        report["passes"].append(result)
        if result.get("state") != "complete":
            raise RuntimeError(f"Windows Update failed: {result.get('error', result)}")
        if result["rebootRequired"] or not restarted:
            runner.restart()
            runner.wait_ready()
            restarted = True
        elif result["installed"] == 0:
            runner.wait_desktop()
            report["state"] = "patched"
            report["checkedAt"] = datetime.now(timezone.utc).isoformat()
            return
    raise RuntimeError(f"Windows Update did not reach a clean scan in {max_passes} passes")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--resource-group", required=True)
    parser.add_argument("--vm-name", required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    report = {"state": "failed", "passes": []}
    try:
        runner = AzureRunner(args.resource_group, args.vm_name, Path(__file__).resolve().parents[1])
        maintain(runner, report)
    except (RuntimeError, OSError, ValueError, subprocess.SubprocessError) as error:
        report["error"] = str(error)
        print(f"Runner maintenance failed: {error}", flush=True)
        return 1
    finally:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("Windows Update is clear after restart; the desktop smoke test must still pass.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
