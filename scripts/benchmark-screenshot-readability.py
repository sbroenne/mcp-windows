"""Run blinded, tool-free model reads against completed paired form captures."""

import argparse
import asyncio
import base64
from collections import defaultdict
from copy import deepcopy
from datetime import UTC, datetime
import hashlib
from importlib.metadata import version
import json
import os
from pathlib import Path
from statistics import median
import subprocess
import tempfile
import time


QUESTION = (
    'What is the username entered in this form? '
    'Reply with JSON only: {"username": "the exact value"}.'
)
INSTRUCTIONS = "Answer the user's question using only the supplied observation."
ARMS = ("low", "high", "text")


def image_blocks(value):
    if isinstance(value, dict):
        if value.get("type") == "input_image":
            yield value
        for child in value.values():
            yield from image_blocks(child)
    elif isinstance(value, list):
        for child in value:
            yield from image_blocks(child)


def prepare_request(payload, arm, image_sha256):
    """Change only the supported image-detail field, retaining the exact pixels."""
    if arm not in ARMS:
        raise ValueError(f"Unknown benchmark arm: {arm}")
    result = deepcopy(payload)
    if result.get("tools"):
        raise ValueError("A blinded read must not have tools")
    images = list(image_blocks(result.get("input", [])))
    expected_count = 0 if arm == "text" else 1
    if len(images) != expected_count:
        raise ValueError(f"Expected {expected_count} image blocks, got {len(images)}")
    for image in images:
        prefix, data = image["image_url"].split(",", 1)
        if prefix != "data:image/jpeg;base64":
            raise ValueError("Expected an inline JPEG, not a remote image or another format")
        actual_sha = hashlib.sha256(base64.b64decode(data, validate=True)).hexdigest()
        if actual_sha != image_sha256:
            raise ValueError("The submitted image differs from the captured screenshot")
        image["detail"] = arm
    return result


def score_reply(reply, expected):
    try:
        decoded = json.loads(reply)
    except json.JSONDecodeError as error:
        return {"correct": False, "answer": None, "answer_error": str(error)}
    if not isinstance(decoded, dict) or not isinstance(decoded.get("username"), str):
        return {
            "correct": False,
            "answer": None,
            "answer_error": "Response must contain a string username",
        }
    return {"correct": decoded["username"] == expected, "answer": decoded["username"]}


def load_cases(directory):
    if not (directory / "report.md").is_file():
        raise ValueError("Use a completed paired-capture run with report.md")
    observations = json.loads((directory / "observations.json").read_text(encoding="utf-8"))
    cases = []
    seen = set()
    for observation in observations:
        identity = (observation["Sample"], observation["Step"])
        if identity in seen:
            raise ValueError(f"Duplicate observation: {identity}")
        seen.add(identity)
        prefix = f"sample-{identity[0]}-step-{identity[1]}"
        replies = [
            (directory / f"{prefix}-focused-{index}.json").read_text(encoding="utf-8")
            for index in range(2)
        ]
        found, read = (json.loads(reply) for reply in replies)
        if not found.get("success") or not read.get("success"):
            raise ValueError(f"Unsuccessful ground-truth read: {prefix}")
        if found.get("action") != "find" or found.get("elementCount") != 1:
            raise ValueError(f"Expected a unique discovered field: {prefix}")
        if read.get("action") != "get_text" or not isinstance(read.get("text"), str):
            raise ValueError(f"Missing verified field text: {prefix}")
        image = (directory / f"{prefix}.jpg").read_bytes()
        cases.append({
            "sample": identity[0],
            "step": identity[1],
            "expected": read["text"],
            "image": image,
            "image_sha256": hashlib.sha256(image).hexdigest(),
            "width": observation["Width"],
            "height": observation["Height"],
            "text": "\n".join(replies),
            "text_sha256": hashlib.sha256("\n".join(replies).encode()).hexdigest(),
        })
    if not cases:
        raise ValueError("The capture run contains no observations")
    return sorted(cases, key=lambda case: (case["sample"], case["step"]))


def format_report(manifest, rows):
    grouped = defaultdict(list)
    for row in rows:
        grouped[row["arm"]].append(row)
    lines = [
        "# Live screenshot readability benchmark",
        "",
        f"Model: {manifest['model']}; SDK: {manifest['sdk_version']}; started: {manifest['started_at']}.",
        f"Source captures: `{manifest['capture_directory']}`.",
        "",
        "Each read starts a fresh session with no tools, repository instructions, or previous answers.",
        "Expected answers stay in the scorer, outside the model's working directory and prompts.",
        "Low/high requests use the original JPEG bytes and the Responses API image detail field.",
        "Text requests include both saved discovery and value-reading replies, not just the answer.",
        "The question and system instruction are the same across arms.",
        "Exact username matches count as correct; invalid response formats and wrong answers are retained.",
        "",
        "Input tokens below are reported by the service, including the question and instructions.",
        "They are not image-only tokens or whole-task bills. Cached input is recorded separately.",
        "This is a field-reading test, not a test of clicking, navigation, or arbitrary Windows apps.",
        "",
        "| Input | Correct reads | Median input tokens / read | Median output tokens / read | Median seconds / read |",
        "|---|---:|---:|---:|---:|",
    ]
    for arm in ARMS:
        items = grouped[arm]
        if not items:
            raise ValueError(f"No completed observations for {arm}")
        lines.append(
            f"| {arm} | {sum(row['correct'] for row in items)}/{len(items)} | "
            f"{median(row['input_tokens'] for row in items):g} | "
            f"{median(row['output_tokens'] for row in items):g} | "
            f"{median(row['seconds'] for row in items):.2f} |"
        )
    lines.extend([
        "",
        "## Individual answers",
        "",
        "| Sample | Step | Input | Expected | Answer | Correct | Input tokens | Output tokens |",
        "|---:|---:|---|---|---|---|---:|---:|",
    ])
    for row in rows:
        answer = str(row["answer"]).replace("|", "\\|").replace("\n", " ")
        expected = row["expected"].replace("|", "\\|").replace("\n", " ")
        lines.append(
            f"| {row['sample']} | {row['step']} | {row['arm']} | {expected} | "
            f"{answer} | {row['correct']} | {row['input_tokens']} | {row['output_tokens']} |"
        )
    return "\n".join(lines) + "\n"


async def run(args):
    import httpx
    from copilot import CopilotClient
    from copilot.copilot_request_handler import CopilotRequestHandler

    class DetailHandler(CopilotRequestHandler):
        active = None

        async def send_request(self, request, ctx):
            raw = await request.aread()
            if raw and "json" in request.headers.get("content-type", ""):
                payload = json.loads(raw)
                if "model" in payload:
                    if self.active is None or payload["model"] != args.model:
                        raise ValueError("Unexpected model request outside the active trial")
                    payload = prepare_request(
                        payload, self.active["arm"], self.active["image_sha256"]
                    )
                    self.active["requests"].append({
                        "model": payload["model"],
                        "image_detail": self.active["arm"] if self.active["arm"] != "text" else None,
                        "image_count": len(list(image_blocks(payload.get("input", [])))),
                        "reasoning": payload.get("reasoning"),
                        "instruction_sha256": hashlib.sha256(
                            str(payload.get("instructions", "")).encode()
                        ).hexdigest(),
                    })
                    headers = request.headers.copy()
                    headers.pop("content-length", None)
                    request = httpx.Request(
                        request.method, request.url, headers=headers, json=payload,
                        extensions=request.extensions,
                    )
            return await super().send_request(request, ctx)

    cases = load_cases(args.captures)
    if args.limit is not None:
        cases = cases[:args.limit]
    args.output.mkdir(parents=True, exist_ok=False)
    manifest = {
        "model": args.model,
        "sdk_version": version("github-copilot-sdk"),
        "reasoning_effort": "medium",
        "started_at": datetime.now(UTC).isoformat(),
        "capture_directory": args.captures.name,
        "case_count": len(cases),
        "unique_values": len({case["expected"] for case in cases}),
        "question": QUESTION,
        "instructions": INSTRUCTIONS,
        "arms": ARMS,
        "source_revision": subprocess.run(
            ["git", "rev-parse", "HEAD"], check=True, capture_output=True, text=True
        ).stdout.strip(),
        "working_tree_dirty": bool(subprocess.run(
            ["git", "status", "--porcelain"], check=True, capture_output=True, text=True
        ).stdout.strip()),
        "runner_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    }
    (args.output / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    token = os.environ.get("GITHUB_TOKEN") or subprocess.run(
        ["gh", "auth", "token"], check=True, capture_output=True, text=True
    ).stdout.strip()
    rows = []
    handler = DetailHandler()
    with tempfile.TemporaryDirectory(prefix="windows-blinded-read-") as storage:
        async with CopilotClient(
            mode="empty", base_directory=storage, working_directory=storage,
            github_token=token, request_handler=handler,
        ) as client:
            manifest["runtime_version"] = (await client.get_status()).version
            (args.output / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
            models = await client.list_models()
            if not any(model.id == args.model and model.capabilities.supports.vision for model in models):
                raise ValueError(f"Requested vision model is not available: {args.model}")
            for case in cases:
                offset = (case["sample"] + case["step"]) % len(ARMS)
                for arm in ARMS[offset:] + ARMS[:offset]:
                    row = {key: value for key, value in case.items() if key not in ("image", "text")}
                    row.update({"arm": arm, "requests": [], "started_at": datetime.now(UTC).isoformat()})
                    handler.active = row
                    usage = []
                    errors = []
                    tool_calls = []

                    def on_event(event):
                        kind = event.type.value
                        if kind == "assistant.usage":
                            data = event.data
                            usage.append({
                                "model": data.model,
                                "input_tokens": data.input_tokens,
                                "output_tokens": data.output_tokens,
                                "cache_read_tokens": data.cache_read_tokens,
                                "cache_write_tokens": data.cache_write_tokens,
                                "reasoning_tokens": data.reasoning_tokens,
                                "reasoning_effort": data.reasoning_effort,
                            })
                        elif kind == "session.error":
                            errors.append(str(event.data))
                        elif kind == "tool.execution_start":
                            tool_calls.append(str(event.data))

                    session = await client.create_session(
                        model=args.model, available_tools=[], working_directory=storage,
                        system_message={"mode": "replace", "content": INSTRUCTIONS},
                        enable_config_discovery=False, skip_custom_instructions=True,
                        enable_skills=False, on_event=on_event, reasoning_effort="medium",
                    )
                    question = QUESTION
                    attachments = None
                    if arm == "text":
                        question += "\n\nObserved form controls:\n" + case["text"]
                    else:
                        attachments = [{
                            "type": "blob", "mimeType": "image/jpeg", "displayName": "form.jpg",
                            "data": base64.b64encode(case["image"]).decode("ascii"),
                        }]
                    started = time.monotonic()
                    result = None
                    incomplete = args.output / "incomplete-trial.json"
                    incomplete.write_text(json.dumps(row, indent=2), encoding="utf-8")
                    try:
                        result = await session.send_and_wait(
                            question, attachments=attachments, timeout=120,
                        )
                    finally:
                        row.update({
                            "seconds": time.monotonic() - started,
                            "usage": usage,
                            "errors": errors,
                            "tool_calls": tool_calls,
                            "reply": result.data.content if result is not None else None,
                        })
                        incomplete.write_text(json.dumps(row, indent=2), encoding="utf-8")
                    if errors or tool_calls or result is None or not usage or not row["requests"]:
                        raise RuntimeError("Trial did not produce an isolated, metered answer; see incomplete-trial.json")
                    if any(item["model"] != args.model for item in usage):
                        raise RuntimeError("The response used an unexpected model")
                    row["input_tokens"] = sum(item["input_tokens"] for item in usage)
                    row["output_tokens"] = sum(item["output_tokens"] for item in usage)
                    row.update(score_reply(row["reply"], case["expected"]))
                    rows.append(row)
                    with (args.output / "observations.jsonl").open("a", encoding="utf-8") as output:
                        output.write(json.dumps(row) + "\n")
                    incomplete.unlink()
                    print(
                        f"Sample {case['sample']} step {case['step']} {arm}: "
                        f"correct={row['correct']}, input={row['input_tokens']}, answer={row['answer']!r}",
                        flush=True,
                    )
                    await session.disconnect()
                    handler.active = None
    if len(rows) != len(cases) * len(ARMS):
        raise RuntimeError("The run did not complete every paired trial")
    (args.output / "report.md").write_text(format_report(manifest, rows), encoding="utf-8")
    print(f"Completed report: {args.output / 'report.md'}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--captures", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path, help="A new directory for this run")
    parser.add_argument("--model", required=True, help="Exact Copilot model ID to measure")
    parser.add_argument("--limit", type=int, help="Use only the first N captures for a pilot")
    args = parser.parse_args()
    if args.limit is not None and args.limit < 1:
        parser.error("--limit must be positive")
    asyncio.run(run(args))


if __name__ == "__main__":
    main()
