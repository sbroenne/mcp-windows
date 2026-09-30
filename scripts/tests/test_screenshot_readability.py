import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


spec = importlib.util.spec_from_file_location(
    "readability", Path(__file__).resolve().parents[1] / "benchmark-screenshot-readability.py"
)
readability = importlib.util.module_from_spec(spec)
spec.loader.exec_module(readability)


class ScreenshotReadabilityTests(unittest.TestCase):
    def setUp(self):
        self.image = b"captured image bytes"
        self.sha = hashlib.sha256(self.image).hexdigest()
        self.payload = {
            "model": "gpt-5.5",
            "instructions": "Fixed instructions",
            "tools": [],
            "input": [{"role": "user", "content": [
                {"type": "input_text", "text": "Fixed question"},
                {"type": "input_image", "detail": "auto",
                 "image_url": "data:image/jpeg;base64," + base64.b64encode(self.image).decode()},
            ]}],
        }

    def test_changes_native_detail_without_changing_image_or_question(self):
        for detail in ("low", "high"):
            with self.subTest(detail=detail):
                changed = readability.prepare_request(self.payload, detail, self.sha)
                image = changed["input"][0]["content"][1]
                self.assertEqual(detail, image["detail"])
                self.assertEqual(self.payload["input"][0]["content"][1]["image_url"], image["image_url"])
                self.assertEqual(self.payload["input"][0]["content"][0], changed["input"][0]["content"][0])
                self.assertEqual(self.payload["instructions"], changed["instructions"])
        self.assertEqual("auto", self.payload["input"][0]["content"][1]["detail"])

    def test_rejects_changed_image_missing_image_and_tools(self):
        with self.assertRaisesRegex(ValueError, "differs"):
            readability.prepare_request(self.payload, "low", "wrong hash")
        with self.assertRaisesRegex(ValueError, "Expected 1 image"):
            readability.prepare_request({"input": [], "tools": []}, "high", self.sha)
        with self.assertRaisesRegex(ValueError, "must not have tools"):
            readability.prepare_request({**self.payload, "tools": [{"name": "read_file"}]}, "high", self.sha)
        with self.assertRaisesRegex(ValueError, "Expected 0 image"):
            readability.prepare_request(self.payload, "text", self.sha)

    def test_text_request_stays_unchanged(self):
        payload = {"model": "gpt-5.5", "tools": [], "input": [{"type": "input_text", "text": "observed reply"}]}
        self.assertEqual(payload, readability.prepare_request(payload, "text", self.sha))

    def test_scores_exact_values_without_excusing_bad_answers(self):
        self.assertTrue(readability.score_reply('{"username":"Alice Smith"}', "Alice Smith")["correct"])
        self.assertFalse(readability.score_reply('{"username":"Alice"}', "Alice Smith")["correct"])
        self.assertFalse(readability.score_reply('{"username":"alice smith"}', "Alice Smith")["correct"])
        self.assertIn("answer_error", readability.score_reply("I think Alice", "Alice"))
        self.assertIn("answer_error", readability.score_reply('{"username":42}', "42"))

    def test_report_retains_failures_and_actual_token_counts(self):
        manifest = {"model": "gpt-5.5", "sdk_version": "test", "started_at": "test", "capture_directory": "test"}
        rows = [
            {"sample": 1, "step": 0, "arm": arm, "expected": "Alice", "answer": answer,
             "correct": correct, "input_tokens": tokens, "output_tokens": 10, "seconds": 1}
            for arm, answer, correct, tokens in [
                ("low", "Wrong", False, 350), ("high", "Alice", True, 576), ("text", "Alice", True, 200)
            ]
        ]
        report = readability.format_report(manifest, rows)
        self.assertIn("| low | 0/1 | 350 |", report)
        self.assertIn("| high | 1/1 | 576 |", report)
        self.assertIn("Wrong", report)
        self.assertIn("not image-only tokens", report)
        self.assertNotIn("GPT-4.1", report)

    def test_loads_both_saved_replies_without_putting_truth_in_question(self):
        with tempfile.TemporaryDirectory() as storage:
            directory = Path(storage)
            (directory / "report.md").write_text("Completed capture", encoding="utf-8")
            observations = [{"Sample": 1, "Step": 0, "Width": 700, "Height": 550}]
            (directory / "observations.json").write_text(json.dumps(observations), encoding="utf-8")
            found = {"success": True, "action": "find", "elementCount": 1}
            read = {"success": True, "action": "get_text", "text": "Withheld answer"}
            for index, reply in enumerate((found, read)):
                (directory / f"sample-1-step-0-focused-{index}.json").write_text(
                    json.dumps(reply), encoding="utf-8"
                )
            (directory / "sample-1-step-0.jpg").write_bytes(self.image)
            case = readability.load_cases(directory)[0]
            self.assertEqual(read["text"], case["expected"])
            self.assertEqual(self.sha, case["image_sha256"])
            self.assertEqual(json.dumps(found) + "\n" + json.dumps(read), case["text"])
            self.assertEqual(hashlib.sha256(case["text"].encode()).hexdigest(), case["text_sha256"])
            self.assertNotIn(read["text"], readability.QUESTION + readability.INSTRUCTIONS)
            (directory / "observations.json").write_text(json.dumps(observations * 2), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Duplicate"):
                readability.load_cases(directory)

    def test_rejects_ambiguous_discovery(self):
        with tempfile.TemporaryDirectory() as storage:
            directory = Path(storage)
            (directory / "report.md").write_text("Completed capture", encoding="utf-8")
            (directory / "observations.json").write_text(
                '[{"Sample":1,"Step":0,"Width":700,"Height":550}]', encoding="utf-8"
            )
            for index, reply in enumerate((
                {"success": True, "action": "find", "elementCount": 2},
                {"success": True, "action": "get_text", "text": "Alice"},
            )):
                (directory / f"sample-1-step-0-focused-{index}.json").write_text(
                    json.dumps(reply), encoding="utf-8"
                )
            with self.assertRaisesRegex(ValueError, "unique"):
                readability.load_cases(directory)


if __name__ == "__main__":
    unittest.main()
