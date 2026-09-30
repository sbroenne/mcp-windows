"""Validate paired model runs and export their complete evidence and comparison."""

import argparse
import importlib.util
import json
from pathlib import Path
from statistics import median


spec = importlib.util.spec_from_file_location(
    "readability", Path(__file__).with_name("benchmark-screenshot-readability.py")
)
readability = importlib.util.module_from_spec(spec)
spec.loader.exec_module(readability)


def summarize_runs(runs):
    summaries = []
    models = set()
    baseline = None
    for run in runs:
        manifest, rows = run["manifest"], run["observations"]
        model = manifest["model"]
        if model in models:
            raise ValueError(f"Duplicate model: {model}")
        models.add(model)
        arms = manifest["arms"]
        if set(arms) != set(readability.ARMS) or len(arms) != len(readability.ARMS):
            raise ValueError(f"Unexpected benchmark arms for {model}")
        if len(rows) != manifest["case_count"] * len(arms):
            raise ValueError(f"Incomplete trial count for {model}")
        settings = tuple(manifest[key] for key in (
            "question", "instructions", "reasoning_effort", "sdk_version", "runtime_version", "runner_sha256"
        ))
        cases = {}
        identities = set()
        instructions = set()
        for row in rows:
            identity = (row["sample"], row["step"], row["arm"])
            if identity in identities or row["arm"] not in arms:
                raise ValueError(f"Duplicate or unexpected trial for {model}: {identity}")
            identities.add(identity)
            case_id = identity[:2]
            case = tuple(row[key] for key in (
                "image_sha256", "text_sha256", "expected", "width", "height"
            ))
            if case_id in cases and cases[case_id] != case:
                raise ValueError(f"Unpaired inputs in {model}: {case_id}")
            cases[case_id] = case
            if row["errors"] or row["tool_calls"] or not row["requests"] or not row["usage"]:
                raise ValueError(f"Trial lacks an isolated, metered answer: {model} {identity}")
            score = readability.score_reply(row["reply"], row["expected"])
            if any(row.get(key) != value for key, value in score.items()):
                raise ValueError(f"Stored answer score differs from the reply: {model} {identity}")
            for request in row["requests"]:
                if (request["model"] != model
                        or request["image_detail"] != (None if row["arm"] == "text" else row["arm"])
                        or request["image_count"] != (0 if row["arm"] == "text" else 1)
                        or request["reasoning"]["effort"] != manifest["reasoning_effort"]):
                    raise ValueError(f"Unexpected outbound model settings: {model} {identity}")
                instructions.add(request["instruction_sha256"])
            for usage in row["usage"]:
                if usage["model"] != model or usage["reasoning_effort"] != manifest["reasoning_effort"]:
                    raise ValueError(f"Unexpected response model settings: {model} {identity}")
                for field in ("input_tokens", "output_tokens", "reasoning_tokens",
                              "cache_read_tokens", "cache_write_tokens"):
                    if not isinstance(usage[field], (int, float)) or usage[field] < 0:
                        raise ValueError(f"Missing or invalid {field}: {model} {identity}")
            for field in ("input_tokens", "output_tokens"):
                if row[field] != sum(usage[field] for usage in row["usage"]):
                    raise ValueError(f"Incorrect usage total: {model} {identity} {field}")
            if not isinstance(row["seconds"], (int, float)) or row["seconds"] <= 0:
                raise ValueError(f"Invalid elapsed time: {model} {identity}")
        expected_trials = {(*case, arm) for case in cases for arm in arms}
        if identities != expected_trials or len(cases) != manifest["case_count"]:
            raise ValueError(f"Missing paired trial in {model}")
        if len({row["expected"] for row in rows}) != manifest["unique_values"]:
            raise ValueError(f"Incorrect unique-value count for {model}")
        if len(instructions) != 1:
            raise ValueError(f"System instructions changed within {model}")
        signature = (settings, cases, instructions)
        if baseline is not None and signature != baseline:
            raise ValueError(f"Inputs or settings differ between models: {model}")
        baseline = signature
        summary = {"model": model, "arms": {}}
        for arm in arms:
            selected = [row for row in rows if row["arm"] == arm]
            metrics = {
                "count": len(selected),
                "correct": sum(row["correct"] for row in selected),
                "requests": sum(len(row["requests"]) for row in selected),
                "median_seconds": median(row["seconds"] for row in selected),
                "min_seconds": min(row["seconds"] for row in selected),
                "max_seconds": max(row["seconds"] for row in selected),
            }
            for field in ("input_tokens", "output_tokens"):
                metrics[f"total_{field}"] = sum(row[field] for row in selected)
                metrics[f"median_{field}"] = median(row[field] for row in selected)
            for field in ("reasoning_tokens", "cache_read_tokens", "cache_write_tokens"):
                metrics[f"total_{field}"] = sum(
                    usage[field] for row in selected for usage in row["usage"]
                )
            summary["arms"][arm] = metrics
        text_tokens = summary["arms"]["text"]["total_input_tokens"]
        summary["savings_percent"] = {
            arm: 100 * (1 - text_tokens / summary["arms"][arm]["total_input_tokens"])
            for arm in ("low", "high")
        }
        summaries.append(summary)
    if not summaries:
        raise ValueError("No completed runs supplied")
    return summaries


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", nargs="+", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    runs = []
    for directory in args.runs:
        if not (directory / "report.md").is_file() or (directory / "incomplete-trial.json").exists():
            raise ValueError(f"Run is not complete: {directory}")
        runs.append({
            "manifest": json.loads((directory / "manifest.json").read_text(encoding="utf-8")),
            "observations": [
                json.loads(line)
                for line in (directory / "observations.jsonl").read_text(encoding="utf-8").splitlines()
            ],
        })
    summary = summarize_runs(runs)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps({"schema_version": 2, "runs": runs, "summary": summary}, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
