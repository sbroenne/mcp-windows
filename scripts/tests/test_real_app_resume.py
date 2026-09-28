import copy
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
spec = importlib.util.spec_from_file_location(
    "resume_benchmark", Path(__file__).resolve().parents[1] / "benchmark-real-apps.py")
benchmark = importlib.util.module_from_spec(spec)
spec.loader.exec_module(benchmark)


class RealAppResumeTests(unittest.TestCase):
    def test_started_marker_blocks_resume_even_without_a_response(self):
        row = {"requests": [], "usage": []}
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            benchmark.require_setup_only_resume(directory, row)
            benchmark.mark_model_started(directory, {"prompt": "Complete the task."})
            self.assertEqual(
                "Complete the task.",
                json.loads((directory / "model-started.json").read_text())["prompt"],
            )
            with self.assertRaisesRegex(ValueError, "Cannot retry"):
                benchmark.require_setup_only_resume(directory, row)

    def test_partial_started_marker_also_blocks_resume(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / "model-started.json").touch()
            with self.assertRaisesRegex(ValueError, "Cannot retry"):
                benchmark.require_setup_only_resume(directory, {"requests": [], "usage": []})

    def test_resume_keeps_completed_failures_and_original_order(self):
        settings = {
            "apps": ["notepad", "word"], "models": ["gpt-6-astra", "gpt-6-luna"],
            "routes": list(benchmark.ROUTES), "purpose": "benchmark",
            "timeout_seconds": 600, "max_tool_calls": 80, "reasoning_effort": "medium",
            "image_detail": "high", "instructions": benchmark.INSTRUCTIONS,
            "server_sha256": "server", "fixtures_sha256": "fixtures",
            "dependencies": {"sdk": "same"}, "windows": "same",
        }
        rows = [
            {"app": "notepad", "model": "gpt-6-astra", "route": "controls", "success": False},
            {"app": "notepad", "model": "gpt-6-astra", "route": "screenshots", "success": True},
        ]
        benchmark.validate_resume(settings, copy.deepcopy(settings), rows)
        self.assertFalse(rows[0]["success"])
        for key in ("server_sha256", "fixtures_sha256", "instructions", "dependencies", "timeout_seconds"):
            changed = copy.deepcopy(settings)
            changed[key] = "different"
            with self.subTest(setting=key), self.assertRaises(ValueError):
                benchmark.validate_resume(settings, changed, rows)
        with self.assertRaises(ValueError):
            benchmark.validate_resume(settings, settings, list(reversed(rows)))
        with self.assertRaises(ValueError):
            benchmark.validate_resume(settings, settings, rows + rows)


if __name__ == "__main__":
    unittest.main()
