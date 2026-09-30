import importlib.util
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
spec = importlib.util.spec_from_file_location(
    "real_results", Path(__file__).resolve().parents[1] / "summarize-real-apps.py"
)
results = importlib.util.module_from_spec(spec)
spec.loader.exec_module(results)


def matrix():
    manifest = {
        "purpose": "benchmark", "models": list(results.benchmark.MODELS),
        "apps": list(results.benchmark.APPS), "routes": list(results.benchmark.ROUTES),
        "input_sha256": {app: app for app in results.benchmark.APPS},
    }
    rows, calls = [], {}
    for app in manifest["apps"]:
        for model in manifest["models"]:
            for route in manifest["routes"]:
                directory = f"trial-{len(rows) + 1:03d}"
                count = 100 if route == "controls" else 200
                row = {
                    "app": app, "model": model, "route": route, "directory": directory,
                    "input_sha256": app, "source_unchanged": True, "success": True,
                    "verification": {"success": True}, "requests": [{
                        "model": model,
                        "tool_names": sorted(results.benchmark.allowed_tools(route)),
                        "image_details": [],
                        "image_count": 0,
                        "reasoning_effort": "medium",
                        "instructions_sha256": "same",
                    }],
                    "usage": [{"model": model, "input_tokens": count, "output_tokens": 10}],
                    "input_tokens": count, "output_tokens": 10, "cache_read_tokens": 0,
                    "cache_write_tokens": 0, "reasoning_tokens": 0, "seconds": 10,
                    "tool_calls": 1, "failed_tool_calls": 0, "screenshot_calls": 0, "errors": [],
                    "reply": "private local path", "prompt": "private local path",
                    "setup": {"single_document_verified": True},
                }
                rows.append(row)
                calls[directory] = [{"name": "keyboard_control", "is_error": False, "seconds": 1}]
    return manifest, rows, calls


class RealAppResultTests(unittest.TestCase):
    def test_complete_matrix_and_successful_pair_savings(self):
        manifest, rows, calls = matrix()
        summary = results.validate_and_summarize(manifest, rows, calls)
        self.assertEqual(16, summary["trial_count"])
        self.assertEqual(8, summary["successful_pairs"])
        self.assertEqual(50, summary["median_paired_input_saving_percent"])

    def test_failed_tasks_are_retained_but_not_credited_as_token_savings(self):
        manifest, rows, calls = matrix()
        rows[0]["success"] = False
        rows[0]["verification"]["success"] = False
        summary = results.validate_and_summarize(manifest, rows, calls)
        self.assertEqual(16, summary["trial_count"])
        self.assertEqual(7, summary["successful_pairs"])
        self.assertEqual(7, summary["routes"]["controls"]["successes"])

    def test_missing_duplicates_pilots_and_tampered_counts_are_rejected(self):
        for corruption in ("missing", "duplicate", "pilot", "tokens", "route", "score"):
            with self.subTest(corruption=corruption):
                manifest, rows, calls = matrix()
                if corruption == "missing":
                    rows.pop()
                elif corruption == "duplicate":
                    rows[-1] = rows[0]
                elif corruption == "pilot":
                    manifest["purpose"] = "harness_pilot"
                elif corruption == "tokens":
                    rows[0]["input_tokens"] += 1
                elif corruption == "route":
                    rows[1]["requests"][0]["tool_names"].append("ui_read")
                else:
                    rows[0]["success"] = False
                with self.assertRaises(ValueError):
                    results.validate_and_summarize(manifest, rows, calls)

    def test_public_evidence_excludes_desktop_text_and_local_paths(self):
        manifest, rows, calls = matrix()
        summary = results.validate_and_summarize(manifest, rows, calls)
        public = results.public_evidence(manifest, rows, summary)
        self.assertNotIn("reply", public["trials"][0])
        self.assertNotIn("prompt", public["trials"][0])
        self.assertNotIn("verification", public["trials"][0])

    def test_late_saved_output_is_not_counted_as_completion_within_budget(self):
        manifest, rows, calls = matrix()
        rows[0]["stop_reason"] = "time_budget_exceeded"
        rows[0]["success"] = False
        summary = results.validate_and_summarize(manifest, rows, calls)
        self.assertEqual(7, summary["routes"]["controls"]["successes"])
        self.assertEqual(7, summary["successful_pairs"])

    def test_native_framework_row_uses_properties_and_strips_private_content(self):
        manifest, rows, calls = matrix()
        source = rows[0]
        report = {
            "timestamp": "2026-09-30T12:00:00Z",
            "tests": [
                {
                    "name": "test_real_app[notepad-gpt-6.1-sol-controls]",
                    "outcome": "passed",
                    "model": source["model"],
                    "properties": [
                        ["runtime", {
                            "framework": "0.6.23",
                            "framework_source": "framework-hash",
                            "sdk": "1.0.15",
                            "python": "3.14.6",
                            "platform": "Windows",
                        }],
                        ["comparison", {
                            "app": source["app"],
                            "model": source["model"],
                            "route": source["route"],
                            "directory": r"C:\private\trial-001",
                            "input_sha256": source["input_sha256"],
                            "server_sha256": "server-hash",
                            "source_revision": "revision",
                            "timeout_seconds": 600,
                            "max_tool_calls": 80,
                            "reasoning_effort": "medium",
                            "image_detail": "high",
                        }],
                        ["notepad_setup", {"single_document_verified": True}],
                        ["application", {
                            "version": "11.0",
                            "initial_bounds": [0, 0, 100, 100],
                        }],
                        ["verification", {
                            "output": {"success": True, "text": "private text"},
                            "source_unchanged": True,
                            "output_sha256": "output-hash",
                        }],
                        ["cleanup", {"terminated_owned_pids": [123]}],
                    ],
                    "eval_result": {
                        "success": True,
                        "evidence_complete": True,
                        "stop_reason": "completed",
                        "error": None,
                        "capture_errors": [],
                        "duration_ms": 10000,
                        "tool_calls_admitted": 1,
                        "usage": source["usage"],
                        "request_audit": source["requests"],
                        "configuration": {
                            "model": source["model"],
                            "reasoning_effort": "medium",
                            "image_detail": "high",
                            "max_tool_calls": 80,
                            "timeout_s": 600,
                            "max_retries": 0,
                            "audit_requests": True,
                            "client_mode": "empty",
                            "system_message_mode": "replace",
                        },
                    },
                }
            ],
        }

        parsed_manifest, parsed_rows = results.rows_from_framework(report, {"trial-001": calls["trial-001"]})
        self.assertEqual("benchmark", parsed_manifest["purpose"])
        self.assertEqual("revision", parsed_manifest["source_revision"])
        self.assertEqual("trial-001", parsed_rows[0]["directory"])
        self.assertEqual("framework-hash", parsed_manifest["runtime"]["framework_source"])
        public = results.public_evidence(parsed_manifest, parsed_rows, {})
        self.assertEqual(1, public["trials"][0]["request_audit"]["count"])
        self.assertNotIn("private text", str(public))


if __name__ == "__main__":
    unittest.main()
