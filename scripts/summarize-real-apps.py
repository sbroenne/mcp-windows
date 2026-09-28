"""Validate a completed real-app matrix and export privacy-safe benchmark evidence."""

import argparse
import importlib.util
import itertools
import json
import math
from pathlib import Path
import re
from statistics import median

spec = importlib.util.spec_from_file_location(
    "real_app_benchmark", Path(__file__).with_name("benchmark-real-apps.py")
)
benchmark = importlib.util.module_from_spec(spec)
spec.loader.exec_module(benchmark)


def validate_and_summarize(manifest, rows, calls):
    if manifest.get("purpose") != "benchmark":
        raise ValueError("Pilot runs cannot be published as the benchmark")
    for key, expected in (("models", benchmark.MODELS), ("apps", benchmark.APPS), ("routes", benchmark.ROUTES)):
        if sorted(manifest.get(key, [])) != sorted(expected):
            raise ValueError(f"The matrix must contain the complete requested {key}")
    expected = set(itertools.product(benchmark.APPS, benchmark.MODELS, benchmark.ROUTES))
    identities = [(row["app"], row["model"], row["route"]) for row in rows]
    if len(identities) != len(set(identities)) or set(identities) != expected:
        raise ValueError("Missing or duplicate trials")
    instructions = set()
    for row in rows:
        if row["errors"] or not row["requests"]:
            raise ValueError("Model transport errors or missing request audit")
        if row["input_sha256"] != manifest["input_sha256"][row["app"]]:
            raise ValueError("Input files differ within an app")
        if row["app"] == "notepad" and not row.get("setup", {}).get("single_document_verified"):
            raise ValueError("Notepad tab isolation was not verified")
        if row["success"] != bool(row["verification"]["success"] and row["source_unchanged"] and not row.get("stop_reason")):
            raise ValueError("Recorded success disagrees with the independent checks")
        for key, total in benchmark.sum_usage(row["usage"], row["model"]).items():
            if row[key] != total:
                raise ValueError(f"Incorrect {key} total")
        for key in ("input_tokens", "output_tokens", "seconds", "tool_calls"):
            if not isinstance(row[key], (int, float)) or not math.isfinite(row[key]) or row[key] < 0:
                raise ValueError(f"Invalid metric: {key}")
        for request in row["requests"]:
            if request["model"] != row["model"] or set(request["tools"]) != benchmark.allowed_tools(row["route"]):
                raise ValueError("Unexpected model or tools on an actual request")
            if request["reasoning"].get("effort") != "medium" or request["image_detail"] != "high":
                raise ValueError("Mismatched model settings")
            instructions.add(request["instruction_sha256"])
        actual = calls[row["directory"]]
        if row["tool_calls"] != len(actual):
            raise ValueError("Incorrect tool-call count")
        if row["failed_tool_calls"] != sum(bool(call.get("is_error") or call.get("error")) for call in actual):
            raise ValueError("Incorrect failed-tool count")
        if row["screenshot_calls"] != sum(call["name"] == "screenshot_control" for call in actual):
            raise ValueError("Incorrect screenshot count")
        for call in actual:
            if call["name"] not in benchmark.allowed_tools(row["route"]):
                raise ValueError("A route used an unavailable tool")
            if call["name"] == "screenshot_control" and "forwarded_arguments" in call:
                args = call["forwarded_arguments"]
                if args.get("annotate") is not False or args.get("target") != "window" or args.get("includeImage") is not True:
                    raise ValueError("Screenshot was annotated, omitted, or not window-scoped")
    if len(instructions) != 1:
        raise ValueError("Actual system instructions differed")
    for app in benchmark.APPS:
        versions = {row.get("app_version") for row in rows if row["app"] == app}
        if len(versions) != 1:
            raise ValueError(f"The {app} version changed during the comparison")
    routes = {}
    for route in benchmark.ROUTES:
        items = [row for row in rows if row["route"] == route]
        routes[route] = {
            "successes": sum(row["success"] for row in items), "trials": len(items),
            "reported_input_tokens": sum(row["input_tokens"] for row in items),
            "reported_output_tokens": sum(row["output_tokens"] for row in items),
            "seconds": sum(row["seconds"] for row in items),
            "tool_calls": sum(row["tool_calls"] for row in items),
        }
    pairs = []
    for app, model in itertools.product(benchmark.APPS, benchmark.MODELS):
        pair = {row["route"]: row for row in rows if row["app"] == app and row["model"] == model}
        control, image = pair["controls"], pair["screenshots"]
        if not (control["success"] and image["success"]) or control.get("stop_reason") or image.get("stop_reason"):
            continue
        if image["input_tokens"] <= 0 or image["seconds"] <= 0:
            raise ValueError("Successful screenshot trial has invalid denominators")
        pairs.append({
            "app": app, "model": model,
            "input_saving_percent": 100 * (1 - control["input_tokens"] / image["input_tokens"]),
            "time_saving_percent": 100 * (1 - control["seconds"] / image["seconds"]),
        })
    return {
        "trial_count": len(rows), "routes": routes, "successful_pairs": len(pairs), "pairs": pairs,
        "median_paired_input_saving_percent": median(p["input_saving_percent"] for p in pairs) if pairs else None,
        "median_paired_time_saving_percent": median(p["time_saving_percent"] for p in pairs) if pairs else None,
    }


def public_evidence(manifest, rows, summary):
    allowed = {
        "app", "model", "route", "started_at", "success", "source_unchanged", "input_sha256",
        "output_sha256", "seconds", "input_tokens", "output_tokens", "cache_read_tokens",
        "cache_write_tokens", "reasoning_tokens", "usage", "requests", "tool_calls",
        "failed_tool_calls", "screenshot_calls", "stop_reason",
        "app_version", "initial_window_bounds",
        "setup",
    }
    return {
        "schema_version": 1, "manifest": manifest, "summary": summary,
        "trials": [{key: value for key, value in row.items() if key in allowed} for row in rows],
    }


def load_run(directory):
    if not (directory / "report.md").is_file():
        raise ValueError("The benchmark has not completed")
    manifest = json.loads((directory / "manifest.json").read_text(encoding="utf-8"))
    rows = json.loads((directory / "results.json").read_text(encoding="utf-8"))
    for app, expected_hash in manifest["input_sha256"].items():
        templates = list((directory / "templates" / app).glob("source.*"))
        if len(templates) != 1 or benchmark.digest(templates[0]) != expected_hash:
            raise ValueError("Input template hash differs")
    calls = {}
    for row in rows:
        if not re.fullmatch(r"trial-\d{3}", row["directory"]):
            raise ValueError("Unexpected trial directory")
        trial = directory / row["directory"]
        if (trial / "incomplete.json").exists() or not (trial / "cleanup.json").is_file():
            raise ValueError("Incomplete trial or cleanup")
        saved = json.loads((trial / "result.json").read_text(encoding="utf-8"))
        if saved != row:
            raise ValueError("Per-trial evidence differs from the result matrix")
        calls[row["directory"]] = json.loads((trial / "tool-calls.json").read_text(encoding="utf-8"))
        inputs = list(trial.glob("input-*"))
        if len(inputs) != 1 or (benchmark.digest(inputs[0]) == row["input_sha256"]) != row["source_unchanged"]:
            raise ValueError("Original input hash check failed")
        output = trial / f"completed{inputs[0].suffix}"
        checked = benchmark.verify_output(row["app"], output)
        if checked != row["verification"]:
            raise ValueError("Saved-output verification changed")
        if (benchmark.digest(output) if output.exists() else None) != row["output_sha256"]:
            raise ValueError("Output hash differs")
        for call in calls[row["directory"]]:
            for image in call.get("images", []):
                path = trial / image["path"]
                if path.parent != trial or benchmark.digest(path) != image["sha256"]:
                    raise ValueError("Captured image hash differs")
    return manifest, rows, calls


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest, rows, calls = load_run(args.run)
    summary = validate_and_summarize(manifest, rows, calls)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    benchmark.write_json(args.output, public_evidence(manifest, rows, summary))
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
