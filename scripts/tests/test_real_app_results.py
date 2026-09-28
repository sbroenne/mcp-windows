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
                        "model": model, "tools": sorted(results.benchmark.allowed_tools(route)),
                        "image_detail": "high", "images": 0, "reasoning": {"effort": "medium"},
                        "instruction_sha256": "same",
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
        self.assertEqual(32, summary["trial_count"])
        self.assertEqual(16, summary["successful_pairs"])
        self.assertEqual(50, summary["median_paired_input_saving_percent"])

    def test_failed_tasks_are_retained_but_not_credited_as_token_savings(self):
        manifest, rows, calls = matrix()
        rows[0]["success"] = False
        rows[0]["verification"]["success"] = False
        summary = results.validate_and_summarize(manifest, rows, calls)
        self.assertEqual(32, summary["trial_count"])
        self.assertEqual(15, summary["successful_pairs"])
        self.assertEqual(15, summary["routes"]["controls"]["successes"])

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
                    rows[1]["requests"][0]["tools"].append("ui_read")
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
        self.assertEqual(15, summary["routes"]["controls"]["successes"])
        self.assertEqual(15, summary["successful_pairs"])


if __name__ == "__main__":
    unittest.main()
