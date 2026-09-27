"""Maintenance tests never connect to Azure or change the local Windows installation."""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
from unittest.mock import Mock

from scripts import maintain_windows_runner as maintenance


ROOT = Path(__file__).resolve().parents[2]


class MaintenanceTests(unittest.TestCase):
    def test_clean_scan_still_reboots_and_rescans_before_desktop_check(self):
        runner = Mock()
        runner.update_pass.return_value = {
            "state": "complete", "installed": 0, "rebootRequired": False
        }
        report = {"passes": []}

        maintenance.maintain(runner, report)

        self.assertEqual(runner.update_pass.call_count, 2)
        runner.restart.assert_called_once()
        runner.wait_desktop.assert_called_once()
        self.assertEqual(report["state"], "patched")
        self.assertIn("checkedAt", report)

    def test_install_and_restart_require_a_new_clean_scan(self):
        runner = Mock()
        runner.update_pass.side_effect = [
            {"state": "complete", "installed": 3, "rebootRequired": True},
            {"state": "complete", "installed": 1, "rebootRequired": False},
            {"state": "complete", "installed": 0, "rebootRequired": False},
        ]
        report = {"passes": []}

        maintenance.maintain(runner, report)

        self.assertEqual(len(report["passes"]), 3)
        runner.restart.assert_called_once()
        self.assertEqual(runner.wait_ready.call_count, 2)

    def test_failed_or_partial_update_never_reports_success(self):
        for state in ("failed", "partial", "running"):
            with self.subTest(state=state):
                runner = Mock()
                runner.update_pass.return_value = {
                    "state": state, "error": "Installation failed"
                }
                report = {"passes": []}
                with self.assertRaisesRegex(RuntimeError, "Installation failed"):
                    maintenance.maintain(runner, report)
                self.assertNotIn("checkedAt", report)
                runner.wait_desktop.assert_not_called()

    def test_repeated_pending_restarts_are_bounded(self):
        runner = Mock()
        runner.update_pass.return_value = {
            "state": "complete", "installed": 0, "rebootRequired": True
        }
        with self.assertRaisesRegex(RuntimeError, "clean scan"):
            maintenance.maintain(runner, {"passes": []}, max_passes=3)
        self.assertEqual(runner.update_pass.call_count, 3)
        runner.wait_desktop.assert_not_called()

    def test_desktop_failure_does_not_record_success(self):
        runner = Mock()
        runner.update_pass.return_value = {
            "state": "complete", "installed": 0, "rebootRequired": False
        }
        runner.wait_desktop.side_effect = RuntimeError("No interactive desktop")
        report = {"passes": []}
        with self.assertRaisesRegex(RuntimeError, "interactive desktop"):
            maintenance.maintain(runner, report)
        self.assertNotIn("checkedAt", report)

    def test_guest_response_requires_one_matching_pass(self):
        def response(payload):
            return {"value": [{"message": "noise\nWINDOWS_MCP_MAINTENANCE=" +
                               json.dumps(payload) + "\n"}]}

        payload = {"passId": "current", "state": "complete"}
        self.assertEqual(maintenance.parse_guest_response(response(payload), "current"), payload)
        with self.assertRaisesRegex(RuntimeError, "pass"):
            maintenance.parse_guest_response(response(payload), "previous")
        with self.assertRaises(RuntimeError):
            maintenance.parse_guest_response({"value": [{"message": "no result"}]}, "current")
        duplicate = response(payload)
        duplicate["value"] *= 2
        with self.assertRaises(RuntimeError):
            maintenance.parse_guest_response(duplicate, "current")

    def test_azure_commands_have_a_timeout_and_check_exit_status(self):
        runner = maintenance.AzureRunner("group", "vm", ROOT, timeout_minutes=1)
        with unittest.mock.patch.object(maintenance.subprocess, "run") as run:
            run.return_value = subprocess.CompletedProcess([], 1, "", "Azure denied access")
            with self.assertRaisesRegex(RuntimeError, "Azure denied access"):
                runner.az("vm", "restart")
            self.assertGreater(run.call_args.kwargs["timeout"], 0)
            self.assertLessEqual(run.call_args.kwargs["timeout"], 60)

    def test_expired_budget_does_not_launch_another_command(self):
        runner = maintenance.AzureRunner("group", "vm", ROOT, timeout_minutes=1)
        runner.deadline = 0
        with unittest.mock.patch.object(maintenance.subprocess, "run") as run:
            with self.assertRaises(TimeoutError):
                runner.az("vm", "restart")
            run.assert_not_called()

    def test_main_persists_failure_report(self):
        with tempfile.TemporaryDirectory() as directory:
            report = Path(directory) / "report.json"
            with unittest.mock.patch("sys.argv", [
                "maintain_windows_runner.py", "--resource-group", "group",
                "--vm-name", "vm", "--report", str(report),
            ]), unittest.mock.patch.object(maintenance, "AzureRunner") as constructor:
                constructor.return_value.wait_ready.side_effect = RuntimeError("Azure unavailable")
                self.assertEqual(maintenance.main(), 1)
            saved = json.loads(report.read_text())
            self.assertEqual(saved["state"], "failed")
            self.assertIn("Azure unavailable", saved["error"])
            self.assertNotIn("checkedAt", saved)

    def test_guest_installation_is_not_replayed_after_unknown_start_outcome(self):
        runner = maintenance.AzureRunner("group", "vm", ROOT)
        with unittest.mock.patch.object(runner, "guest") as guest:
            guest.side_effect = subprocess.TimeoutExpired("az", 180)
            with self.assertRaises(subprocess.TimeoutExpired):
                runner.update_pass("a" * 32)
            guest.assert_called_once()


class FreshnessTests(unittest.TestCase):
    def run_health(self, days=0, latest="success", workflow_state="active", issue=False,
                   patch_days=None, patch_receipt=True):
        workflow = (ROOT / ".github/workflows/runner-maintenance-health.yml").read_text()
        script = textwrap.dedent(workflow.split("          script: |\n", 1)[1])
        # Execute the workflow's actual JavaScript with only GitHub and the clock stubbed.
        harness = """
const input = JSON.parse(process.argv[1]);
const calls = [];
const last = input.days === null ? undefined : {
  id: 123,
  updated_at: new Date(Date.now() - input.days * 86400000).toISOString(),
  conclusion: 'success', html_url: 'https://example.test/success'
};
const latest = input.latest === null ? undefined : {
  conclusion: input.latest, html_url: 'https://example.test/latest'
};
const context = {repo: {owner: 'owner', repo: 'repo'},
  payload: {repository: {default_branch: 'main'}}};
const github = {rest: {
  actions: {
    getWorkflow: async () => ({data: {state: input.workflow_state}}),
    listJobsForWorkflowRun: async () => {},
    listWorkflowRuns: async args => {
      if (args.branch !== 'main') throw Error('Must check default branch');
      const run = args.status === 'success' ? last : latest;
      return {data: {workflow_runs: run ? [run] : []}};
    }
  },
  issues: Object.fromEntries(['listForRepo', 'create', 'update', 'createComment'].map(
    method => [method, async args => { calls.push({method, ...args}); }]
  ))
}, paginate: async (method, args) => {
  if (method === github.rest.actions.listJobsForWorkflowRun) {
    if (args.run_id !== 123 || args.filter !== 'all') throw Error('Wrong maintenance jobs');
    return input.patch_receipt ? [{
      name: 'Patch Windows and restart', conclusion: 'success',
      steps: [{name: 'Install updates and verify recovery', conclusion: 'success',
        completed_at: new Date(Date.now() -
          (input.patch_days ?? input.days) * 86400000).toISOString()}]
    }] : [];
  }
  return input.issue ? [{number: 42, body: '<!-- windows-runner-maintenance-health -->'}] : [];
}};
const core = {
  setFailed: message => calls.push({method: 'failed', message}),
  summary: {addHeading() {return this}, addRaw() {return this}, async write() {}}
};
(async () => {
"""
        result = subprocess.run(
            ["node", "-e", harness + script +
             "\nconsole.log(JSON.stringify(calls)); })().catch(e => {console.error(e); process.exit(1)});",
             json.dumps(dict(days=days, latest=latest, workflow_state=workflow_state, issue=issue,
                             patch_days=patch_days, patch_receipt=patch_receipt))],
            capture_output=True, text=True, timeout=30,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return json.loads(result.stdout)

    def test_fresh_success_does_not_alert(self):
        self.assertEqual(self.run_health(days=9), [])
        self.assertEqual(self.run_health(days=10 - 1 / 24), [])

    def test_stale_missing_disabled_and_failed_runs_alert(self):
        for arguments in (
            {"days": 11}, {"days": None, "latest": None},
            {"workflow_state": "disabled_manually"}, {"latest": "failure"},
            {"latest": "cancelled"}, {"latest": "timed_out"},
        ):
            with self.subTest(arguments=arguments):
                calls = self.run_health(**arguments)
                self.assertEqual([call["method"] for call in calls], ["create", "failed"])

    def test_existing_alert_is_updated_not_duplicated(self):
        calls = self.run_health(days=11, issue=True)
        self.assertEqual([call["method"] for call in calls], ["update", "failed"])
        self.assertEqual(calls[0]["issue_number"], 42)

    def test_successful_recovery_closes_alert(self):
        calls = self.run_health(issue=True)
        self.assertEqual([call["method"] for call in calls], ["createComment", "update"])
        self.assertEqual(calls[-1]["state"], "closed")

    def test_rerunning_only_cleanup_does_not_refresh_old_patching(self):
        calls = self.run_health(days=0, patch_days=11)
        self.assertEqual([call["method"] for call in calls], ["create", "failed"])

    def test_success_without_a_verified_update_step_is_not_patching(self):
        calls = self.run_health(patch_receipt=False)
        self.assertEqual([call["method"] for call in calls], ["create", "failed"])


class GuestScriptTests(unittest.TestCase):
    def test_windows_update_selection_and_result_handling_without_com(self):
        shell = shutil.which("pwsh")
        self.assertIsNotNone(shell, "PowerShell 7 is required to test the guest script")
        result = subprocess.run(
            [shell, "-NoProfile", "-File",
             str(ROOT / "scripts" / "tests" / "test_runner_update_worker.ps1")],
            cwd=ROOT, capture_output=True, text=True, timeout=60,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    @unittest.skipUnless(sys.platform == "win32", "Windows PowerShell 5.1 requires Windows")
    def test_guest_worker_under_the_actual_scheduled_task_shell(self):
        shell = shutil.which("powershell.exe")
        self.assertIsNotNone(shell, "The scheduled task uses Windows PowerShell 5.1")
        result = subprocess.run(
            [shell, "-NoProfile", "-NonInteractive", "-File",
             str(ROOT / "scripts" / "tests" / "test_runner_update_worker.ps1")],
            cwd=ROOT, capture_output=True, text=True, timeout=60,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


class WorkflowContractTests(unittest.TestCase):
    def test_maintenance_shares_test_lock_and_runs_on_a_hosted_controller(self):
        workflow = (ROOT / ".github/workflows/runner-maintenance.yml").read_text()
        integration = (ROOT / ".github/workflows/integration-tests.yml").read_text()
        for content in (workflow, integration):
            self.assertIn("group: windows-ui-integration-tests", content)
            self.assertIn("cancel-in-progress: false", content)
        self.assertIn("schedule:", workflow)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("github.event.repository.default_branch", workflow)
        self.assertIn("maintain_windows_runner.py", workflow)
        self.assertIn("if: always()", workflow)
        self.assertIn("az vm deallocate", workflow)
        self.assertIn("az vm auto-shutdown", workflow)
        self.assertIn("KeyboardControlToolIntegrationTests", workflow)

    def test_freshness_watch_runs_independently_of_the_vm_lock(self):
        workflow = (ROOT / ".github/workflows/runner-maintenance-health.yml").read_text()
        self.assertIn("schedule:", workflow)
        self.assertIn("workflow_run:", workflow)
        self.assertNotIn("group: windows-ui-integration-tests", workflow)
        self.assertIn("issues: write", workflow)
        self.assertIn("runner-maintenance.yml", workflow)
        self.assertIn("10 * 24 * 60 * 60 * 1000", workflow)
        self.assertIn("core.setFailed", workflow)


if __name__ == "__main__":
    unittest.main()
