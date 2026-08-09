#!/usr/bin/env python3
"""Persist an exact daily GitHub star count and render aggregate history as SVG."""

from __future__ import annotations

import argparse
import html
import json
import os
import re
import sys
import tempfile
import urllib.error
import urllib.request
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
REPOSITORY_PATTERN = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")
MONTH_NAMES = (
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
)


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Update aggregate GitHub star history and generate its SVG chart."
    )
    parser.add_argument("--repository", required=True, help="GitHub owner/repository")
    parser.add_argument("--history", required=True, type=Path, help="Aggregate JSON path")
    parser.add_argument("--output", required=True, type=Path, help="Generated SVG path")
    parser.add_argument(
        "--star-count",
        type=int,
        help="Exact count override for tests or an authenticated local refresh",
    )
    parser.add_argument(
        "--snapshot-date",
        type=date.fromisoformat,
        default=datetime.now(timezone.utc).date(),
        help="UTC snapshot date in YYYY-MM-DD format",
    )
    return parser.parse_args()


def load_history(path: Path, repository: str) -> dict[str, Any]:
    try:
        history = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise ValueError(f"History file does not exist: {path}") from error
    except json.JSONDecodeError as error:
        raise ValueError(f"History file is not valid JSON: {error}") from error

    if not isinstance(history, dict):
        raise ValueError("History root must be a JSON object.")

    expected_root_keys = {"schemaVersion", "repository", "snapshots"}
    if set(history) != expected_root_keys:
        raise ValueError(
            "History root must contain only 'schemaVersion', 'repository', and 'snapshots'."
        )
    if history["schemaVersion"] != SCHEMA_VERSION:
        raise ValueError(f"Unsupported history schema version: {history['schemaVersion']}")
    if history["repository"] != repository:
        raise ValueError(
            f"History repository '{history['repository']}' does not match '{repository}'."
        )
    if not isinstance(history["snapshots"], list) or not history["snapshots"]:
        raise ValueError("History must contain at least one aggregate snapshot.")

    previous_date: date | None = None
    for index, snapshot in enumerate(history["snapshots"]):
        if not isinstance(snapshot, dict) or set(snapshot) != {"date", "count"}:
            raise ValueError(
                f"Snapshot {index} must contain only 'date' and 'count' aggregate fields."
            )
        try:
            snapshot_date = date.fromisoformat(snapshot["date"])
        except (TypeError, ValueError) as error:
            raise ValueError(f"Snapshot {index} has an invalid ISO date.") from error
        count = snapshot["count"]
        if isinstance(count, bool) or not isinstance(count, int) or count < 0:
            raise ValueError(f"Snapshot {index} count must be a non-negative integer.")
        if previous_date is not None and snapshot_date <= previous_date:
            raise ValueError("Snapshot dates must be unique and strictly increasing.")
        previous_date = snapshot_date

    return history


def fetch_star_count(repository: str, token: str) -> int:
    owner, name = repository.split("/", maxsplit=1)
    request = urllib.request.Request(
        f"https://api.github.com/repos/{owner}/{name}",
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "User-Agent": "sbroenne/mcp-windows-star-history",
            "X-GitHub-Api-Version": "2022-11-28",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            repository_data = json.load(response)
    except urllib.error.HTTPError as error:
        raise RuntimeError(
            f"GitHub repository request failed with HTTP {error.code}."
        ) from error
    except urllib.error.URLError as error:
        raise RuntimeError(f"GitHub repository request failed: {error.reason}") from error

    count = repository_data.get("stargazers_count")
    if isinstance(count, bool) or not isinstance(count, int) or count < 0:
        raise RuntimeError("GitHub did not return a valid stargazers_count.")
    return count


def update_snapshot(
    history: dict[str, Any], snapshot_date: date, star_count: int
) -> None:
    if star_count < 0:
        raise ValueError("Star count must be a non-negative integer.")

    snapshots = history["snapshots"]
    latest_date = date.fromisoformat(snapshots[-1]["date"])
    snapshot_date_text = snapshot_date.isoformat()
    if snapshot_date < latest_date:
        raise ValueError(
            f"Snapshot date {snapshot_date_text} is older than the latest persisted "
            f"snapshot {latest_date.isoformat()}."
        )
    if snapshot_date == latest_date:
        snapshots[-1]["count"] = star_count
    else:
        snapshots.append({"date": snapshot_date_text, "count": star_count})


def write_history(path: Path, history: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    serialized = json.dumps(history, indent=2) + "\n"
    with tempfile.NamedTemporaryFile(
        "w", encoding="utf-8", dir=path.parent, delete=False
    ) as temporary_file:
        temporary_file.write(serialized)
        temporary_path = Path(temporary_file.name)
    temporary_path.replace(path)


def svg_number(value: float) -> str:
    return f"{value:.2f}".rstrip("0").rstrip(".")


def format_month(value: date) -> str:
    return f"{MONTH_NAMES[value.month - 1]} {value.year}"


def render_svg(repository: str, snapshots: list[dict[str, Any]]) -> str:
    width = 900
    height = 480
    left = 72
    right = 24
    top = 76
    bottom = 62
    plot_width = width - left - right
    plot_height = height - top - bottom

    first_date = date.fromisoformat(snapshots[0]["date"])
    last_date = date.fromisoformat(snapshots[-1]["date"])
    duration_days = max((last_date - first_date).days, 1)
    maximum_count = max(snapshot["count"] for snapshot in snapshots)
    y_maximum = max(maximum_count, 1)
    current_count = snapshots[-1]["count"]

    points: list[tuple[float, float]] = []
    for snapshot in snapshots:
        snapshot_date = date.fromisoformat(snapshot["date"])
        x = left + (((snapshot_date - first_date).days / duration_days) * plot_width)
        y = top + plot_height - ((snapshot["count"] / y_maximum) * plot_height)
        points.append((x, y))

    line_parts = [f"M {svg_number(points[0][0])} {svg_number(points[0][1])}"]
    for x, y in points[1:]:
        line_parts.extend((f"H {svg_number(x)}", f"V {svg_number(y)}"))
    line_path = " ".join(line_parts)

    baseline_y = top + plot_height
    area_parts = [
        f"M {svg_number(points[0][0])} {svg_number(baseline_y)}",
        f"V {svg_number(points[0][1])}",
    ]
    for x, y in points[1:]:
        area_parts.extend((f"H {svg_number(x)}", f"V {svg_number(y)}"))
    area_parts.extend(
        (
            f"V {svg_number(baseline_y)}",
            f"H {svg_number(points[0][0])}",
            "Z",
        )
    )
    area_path = " ".join(area_parts)

    escaped_repository = html.escape(repository, quote=True)
    date_range = f"{format_month(first_date)} - {format_month(last_date)}"
    subtitle = html.escape(
        f"{repository} - {current_count} stars - {date_range}", quote=True
    )
    description = html.escape(
        f"Exact aggregate GitHub star counts for {repository} from "
        f"{first_date.isoformat()} to {last_date.isoformat()}.",
        quote=True,
    )

    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        (
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" '
            f'height="{height}" viewBox="0 0 {width} {height}" role="img" '
            'aria-labelledby="title description">'
        ),
        f'  <title id="title">GitHub stars over time for {escaped_repository}</title>',
        f'  <desc id="description">{description}</desc>',
        "  <style>",
        "    .background { fill: #ffffff; }",
        "    .grid { stroke: #d0d7de; stroke-width: 1; }",
        (
            "    .axis-text { fill: #57606a; font: 13px -apple-system, "
            "BlinkMacSystemFont, 'Segoe UI', sans-serif; }"
        ),
        (
            "    .title { fill: #1f2328; font: 600 22px -apple-system, "
            "BlinkMacSystemFont, 'Segoe UI', sans-serif; }"
        ),
        (
            "    .subtitle { fill: #57606a; font: 14px -apple-system, "
            "BlinkMacSystemFont, 'Segoe UI', sans-serif; }"
        ),
        "    .area { fill: #2b88d8; opacity: 0.14; }",
        (
            "    .line { fill: none; stroke: #0078d4; stroke-linecap: square; "
            "stroke-linejoin: round; stroke-width: 3; }"
        ),
        "    @media (prefers-color-scheme: dark) {",
        "      .background { fill: #0d1117; }",
        "      .grid { stroke: #30363d; }",
        "      .axis-text, .subtitle { fill: #8b949e; }",
        "      .title { fill: #f0f6fc; }",
        "      .area { fill: #2b88d8; opacity: 0.18; }",
        "      .line { stroke: #50a0e0; }",
        "    }",
        "  </style>",
        f'  <rect class="background" width="{width}" height="{height}" rx="8" />',
        f'  <text class="title" x="{left}" y="34">GitHub stars over time</text>',
        f'  <text class="subtitle" x="{left}" y="57">{subtitle}</text>',
    ]

    for index in range(5):
        value = round((y_maximum * index) / 4)
        y = top + plot_height - ((value / y_maximum) * plot_height)
        y_text = svg_number(y)
        lines.extend(
            (
                (
                    f'  <line class="grid" x1="{left}" y1="{y_text}" '
                    f'x2="{left + plot_width}" y2="{y_text}" />'
                ),
                (
                    f'  <text class="axis-text" x="{left - 12}" y="{y_text}" '
                    f'text-anchor="end" dominant-baseline="middle">{value}</text>'
                ),
            )
        )

    for index in range(5):
        x = left + ((plot_width * index) / 4)
        tick_ordinal = first_date.toordinal() + round((duration_days * index) / 4)
        tick_date = date.fromordinal(tick_ordinal)
        anchor = "start" if index == 0 else "end" if index == 4 else "middle"
        lines.append(
            f'  <text class="axis-text" x="{svg_number(x)}" '
            f'y="{top + plot_height + 28}" text-anchor="{anchor}">'
            f"{format_month(tick_date)}</text>"
        )

    lines.extend(
        (
            f'  <path class="area" d="{area_path}" />',
            f'  <path class="line" d="{line_path}" />',
            "</svg>",
        )
    )
    return "\n".join(lines) + "\n"


def main() -> int:
    arguments = parse_arguments()
    if not REPOSITORY_PATTERN.fullmatch(arguments.repository):
        raise ValueError("Repository must use the owner/name format.")

    history = load_history(arguments.history, arguments.repository)
    if arguments.star_count is None:
        token = os.environ.get("GITHUB_TOKEN")
        if not token:
            raise ValueError(
                "GITHUB_TOKEN is required when --star-count is not provided."
            )
        star_count = fetch_star_count(arguments.repository, token)
    else:
        star_count = arguments.star_count

    update_snapshot(history, arguments.snapshot_date, star_count)
    write_history(arguments.history, history)
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_text(
        render_svg(arguments.repository, history["snapshots"]), encoding="utf-8"
    )
    print(
        f"Generated {arguments.output} from {len(history['snapshots'])} aggregate "
        f"snapshots; current count is {star_count}."
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        sys.exit(1)
