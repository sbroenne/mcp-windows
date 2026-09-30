from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location(
    "comparison", Path(__file__).resolve().parents[1] / "summarize-screenshot-readability.py"
)
comparison = importlib.util.module_from_spec(spec)
spec.loader.exec_module(comparison)


def fixture(model):
    manifest = {
        "model": model, "case_count": 2, "unique_values": 2,
        "arms": ["low", "high", "text"], "question": "Fixed question",
        "instructions": "Fixed instructions", "reasoning_effort": "medium",
        "sdk_version": "test", "runtime_version": "test", "runner_sha256": "a" * 64,
    }
    rows = []
    for step, expected in enumerate(("Alice", "Bob")):
        for arm, tokens in (("text", 100), ("low", 200), ("high", 400)):
            answer = "Wrong" if step == 1 and arm == "low" else expected
            rows.append({
                "sample": 1, "step": step, "expected": expected, "answer": answer,
                "reply": json.dumps({"username": answer}), "correct": answer == expected,
                "arm": arm, "image_sha256": str(step), "text_sha256": str(step),
                "width": 700, "height": 550, "input_tokens": tokens,
                "output_tokens": 10 + step * 2, "seconds": 1 + step * 2,
                "errors": [], "tool_calls": [],
                "requests": [{"model": model, "image_detail": None if arm == "text" else arm,
                              "image_count": 0 if arm == "text" else 1,
                              "reasoning": {"effort": "medium"}, "instruction_sha256": "b" * 64}],
                "usage": [{"model": model, "input_tokens": tokens, "output_tokens": 10 + step * 2,
                           "reasoning_tokens": 2, "reasoning_effort": "medium",
                           "cache_read_tokens": 0, "cache_write_tokens": 0}],
            })
    return {"manifest": manifest, "observations": rows}


class ReadabilityComparisonTests(unittest.TestCase):
    def test_compares_all_models_without_discarding_wrong_answers(self):
        result = comparison.summarize_runs([fixture("model-a"), fixture("model-b")])
        self.assertEqual(2, len(result))
        summary = result[0]
        self.assertEqual("model-a", summary["model"])
        self.assertEqual(50, summary["savings_percent"]["low"])
        self.assertEqual(75, summary["savings_percent"]["high"])
        self.assertEqual(1, summary["arms"]["low"]["correct"])
        self.assertEqual(200, summary["arms"]["text"]["total_input_tokens"])
        self.assertEqual(11, summary["arms"]["text"]["median_output_tokens"])
        self.assertEqual(2, summary["arms"]["text"]["median_seconds"])

    def test_rejects_duplicate_or_missing_trials(self):
        for mutate in (lambda rows: rows.pop(), lambda rows: rows.append(deepcopy(rows[0]))):
            with self.subTest(mutate=mutate):
                run = fixture("model-a")
                mutate(run["observations"])
                with self.assertRaisesRegex(ValueError, "trial"):
                    comparison.summarize_runs([run])

    def test_requires_identical_cases_and_settings_between_models(self):
        for mutate in (
            lambda run: run["observations"][0].update(image_sha256="different"),
            lambda run: run["observations"][0].update(text_sha256="different"),
            lambda run: run["manifest"].update(question="different"),
            lambda run: run["manifest"].update(reasoning_effort="high"),
        ):
            with self.subTest(mutate=mutate):
                first, second = fixture("model-a"), fixture("model-b")
                mutate(second)
                with self.assertRaises(ValueError):
                    comparison.summarize_runs([first, second])

    def test_rejects_invalid_usage_wrong_model_and_false_success(self):
        for mutate in (
            lambda row: row.update(input_tokens=999),
            lambda row: row["requests"][0].update(model="unexpected"),
            lambda row: row["requests"][0].update(image_detail="auto"),
            lambda row: row["usage"][0].update(reasoning_effort="high"),
            lambda row: row.update(correct=False),
            lambda row: row.update(errors=["transport failed"]),
        ):
            with self.subTest(mutate=mutate):
                run = fixture("model-a")
                mutate(run["observations"][1])
                with self.assertRaises(ValueError):
                    comparison.summarize_runs([run])

    def test_rejects_duplicate_models(self):
        with self.assertRaisesRegex(ValueError, "Duplicate model"):
            comparison.summarize_runs([fixture("model-a"), fixture("model-a")])

    def test_keeps_malformed_answers_as_failures(self):
        run = fixture("model-a")
        row = run["observations"][0]
        row["reply"] = "The username is Alice"
        row.update(comparison.readability.score_reply(row["reply"], row["expected"]))
        result = comparison.summarize_runs([run])[0]
        self.assertEqual(1, result["arms"]["text"]["correct"])

    def test_savings_use_all_tokens_and_can_be_negative(self):
        run = fixture("model-a")
        for row in run["observations"]:
            if row["arm"] == "text":
                row["input_tokens"] = 600
                row["usage"][0]["input_tokens"] = 600
        result = comparison.summarize_runs([run])[0]
        self.assertEqual(-200, result["savings_percent"]["low"])
        self.assertEqual(-50, result["savings_percent"]["high"])

    def test_records_cached_input_without_subtracting_it(self):
        run = fixture("model-a")
        run["observations"][0]["usage"][0]["cache_read_tokens"] = 50
        result = comparison.summarize_runs([run])[0]
        self.assertEqual(50, result["arms"]["text"]["total_cache_read_tokens"])
        self.assertEqual(200, result["arms"]["text"]["total_input_tokens"])


if __name__ == "__main__":
    unittest.main()
