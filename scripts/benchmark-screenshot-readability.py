"""Read and validate historical screenshot-readability evidence without model calls."""

import base64
from collections import defaultdict
from copy import deepcopy
import hashlib
import json
from statistics import median


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






if __name__ == "__main__":
    raise SystemExit("This standalone model runner is retired. Model benchmarks must use pytest-skill-engineering.")
