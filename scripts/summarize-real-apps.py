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


def rows_from_framework(report, calls):
    rows = []
    runtime_values = []
    source_revisions = set()
    server_hashes = set()
    for index, test in enumerate(report.get("tests", []), 1):
        properties = dict(test.get("properties", []))
        required = {"runtime", "comparison", "application", "verification", "cleanup"}
        if not required.issubset(properties):
            raise ValueError("Framework report is missing benchmark properties")
        runtime = properties["runtime"]
        comparison = properties["comparison"]
        application = properties["application"]
        verification = properties["verification"]
        result = test["eval_result"]
        directory = f"trial-{index:03d}"
        if Path(comparison["directory"]).name != directory:
            raise ValueError("Trial directory does not match framework order")
        configuration = result["configuration"]
        expected_configuration = {
            "model": comparison["model"],
            "reasoning_effort": "medium",
            "image_detail": "high",
            "max_tool_calls": comparison["max_tool_calls"],
            "timeout_s": comparison["timeout_seconds"],
            "max_retries": 0,
            "audit_requests": True,
            "client_mode": "empty",
            "system_message_mode": "replace",
        }
        if any(configuration.get(key) != value for key, value in expected_configuration.items()):
            raise ValueError("Framework configuration differs from the approved benchmark")
        actual_calls = calls.get(directory)
        if actual_calls is None:
            raise ValueError("Tool-call evidence is missing")
        totals = benchmark.sum_usage(result["usage"], comparison["model"])
        output_check = verification["output"]
        success = (
            test["outcome"] == "passed"
            and result["success"]
            and output_check["success"]
            and verification["source_unchanged"]
            and result["stop_reason"] == "completed"
        )
        rows.append({
            "app": comparison["app"],
            "model": comparison["model"],
            "route": comparison["route"],
            "directory": directory,
            "input_sha256": comparison["input_sha256"],
            "source_unchanged": verification["source_unchanged"],
            "output_sha256": verification["output_sha256"],
            "success": success,
            "verification": output_check,
            "requests": result["request_audit"],
            "usage": result["usage"],
            **totals,
            "seconds": result["duration_ms"] / 1000,
            "tool_calls": len(actual_calls),
            "tool_calls_admitted": result["tool_calls_admitted"],
            "failed_tool_calls": sum(
                bool(call.get("is_error") or call.get("error")) for call in actual_calls
            ),
            "screenshot_calls": sum(
                call["name"] == "screenshot_control" for call in actual_calls
            ),
            "errors": result["capture_errors"],
            "evidence_complete": result["evidence_complete"],
            "stop_reason": result["stop_reason"],
            "model_success": result["success"],
            "test_outcome": test["outcome"],
            "app_version": application["version"],
            "initial_window_bounds": application["initial_bounds"],
            "setup": properties.get("notepad_setup"),
            "cleanup_recorded": isinstance(
                properties["cleanup"].get("terminated_owned_pids"), list
            ),
        })
        runtime_values.append(runtime)
        source_revisions.add(comparison["source_revision"])
        server_hashes.add(comparison["server_sha256"])
    if not rows:
        raise ValueError("Framework report contains no benchmark trials")
    runtime_identity = {
        key: runtime_values[0].get(key)
        for key in ("framework", "framework_source", "sdk", "python", "platform")
    }
    if any(
        any(runtime.get(key) != value for key, value in runtime_identity.items())
        for runtime in runtime_values
    ):
        raise ValueError("Runtime changed during the benchmark")
    if len(source_revisions) != 1 or len(server_hashes) != 1:
        raise ValueError("Source revision or server build changed during the benchmark")
    return {
        "purpose": "benchmark",
        "timestamp": report.get("timestamp"),
        "models": list(benchmark.MODELS),
        "apps": list(benchmark.APPS),
        "routes": list(benchmark.ROUTES),
        "input_sha256": {
            app: next(row["input_sha256"] for row in rows if row["app"] == app)
            for app in benchmark.APPS
            if any(row["app"] == app for row in rows)
        },
        "source_revision": source_revisions.pop(),
        "server_sha256": server_hashes.pop(),
        "runtime": runtime_identity,
    }, rows


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
        if row["errors"] or not row["requests"] or not row.get("evidence_complete", True):
            raise ValueError("Model transport errors or missing request audit")
        if not row.get("cleanup_recorded", True):
            raise ValueError("Owned-app cleanup was not recorded")
        if row["input_sha256"] != manifest["input_sha256"][row["app"]]:
            raise ValueError("Input files differ within an app")
        if row["app"] == "notepad" and not row.get("setup", {}).get("single_document_verified"):
            raise ValueError("Notepad tab isolation was not verified")
        execution_ok = (
            row.get("model_success", not row.get("stop_reason"))
            and row.get("test_outcome", "passed") == "passed"
            and row.get("stop_reason", "completed") == "completed"
        )
        if row["success"] != bool(
            row["verification"]["success"] and row["source_unchanged"] and execution_ok
        ):
            raise ValueError("Recorded success disagrees with the independent checks")
        for key, total in benchmark.sum_usage(row["usage"], row["model"]).items():
            if row[key] != total:
                raise ValueError(f"Incorrect {key} total")
        for key in ("input_tokens", "output_tokens", "seconds", "tool_calls"):
            if not isinstance(row[key], (int, float)) or not math.isfinite(row[key]) or row[key] < 0:
                raise ValueError(f"Invalid metric: {key}")
        for request in row["requests"]:
            if (
                request["model"] != row["model"]
                or set(request["tool_names"]) != benchmark.allowed_tools(row["route"])
            ):
                raise ValueError("Unexpected model or tools on an actual request")
            if (
                request["reasoning_effort"] != "medium"
                or request["image_count"] != len(request["image_details"])
                or any(detail != "high" for detail in request["image_details"])
            ):
                raise ValueError("Mismatched model settings")
            instructions.add(request["instructions_sha256"])
        actual = calls[row["directory"]]
        if row["tool_calls"] != len(actual):
            raise ValueError("Incorrect tool-call count")
        if row.get("tool_calls_admitted", len(actual)) != len(actual):
            raise ValueError("Admitted tool-call count differs from the tool log")
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
        if (
            not (control["success"] and image["success"])
            or control.get("stop_reason", "completed") != "completed"
            or image.get("stop_reason", "completed") != "completed"
        ):
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
        "failed_tool_calls", "screenshot_calls", "stop_reason", "evidence_complete",
        "tool_calls_admitted",
        "app_version", "initial_window_bounds",
        "setup",
    }
    request_fields = {
        "model", "tool_names", "reasoning_effort", "image_count", "image_details",
        "instructions_sha256",
    }
    public_rows = []
    for row in rows:
        public_row = {key: value for key, value in row.items() if key in allowed}
        public_row["requests"] = [
            {key: value for key, value in request.items() if key in request_fields}
            for request in row["requests"]
        ]
        public_rows.append(public_row)
    return {
        "schema_version": 1, "manifest": manifest, "summary": summary,
        "trials": public_rows,
    }


def load_run(directory):
    report_path = directory / "framework.json"
    if not report_path.is_file():
        raise ValueError("The native framework report is missing")
    report = json.loads(report_path.read_text(encoding="utf-8"))
    if len(report.get("tests", [])) != len(benchmark.APPS) * len(benchmark.MODELS) * len(benchmark.ROUTES):
        raise ValueError("The native framework report is incomplete")
    calls = {}
    for index in range(1, len(report["tests"]) + 1):
        name = f"trial-{index:03d}"
        trial = directory / name
        calls[name] = json.loads((trial / "tool-calls.json").read_text(encoding="utf-8"))
    manifest, rows = rows_from_framework(report, calls)
    expected_trials = {f"trial-{index:03d}" for index in range(1, len(rows) + 1)}
    actual_trials = {path.name for path in directory.glob("trial-*") if path.is_dir()}
    if actual_trials != expected_trials:
        raise ValueError("Unexpected or missing trial directories")
    for row, test in zip(rows, report["tests"], strict=True):
        trial = directory / row["directory"]
        comparison = dict(test["properties"])["comparison"]
        if Path(comparison["directory"]).resolve() != trial.resolve():
            raise ValueError("Framework trial path differs from the selected run")
        inputs = list(trial.glob("input-*"))
        if (
            len(inputs) != 1
            or benchmark.digest(inputs[0]) != row["input_sha256"]
            or not row["source_unchanged"]
        ):
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
