"""Reject fixed download locations in tracked npm lockfiles without exposing URLs."""

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath


LOCKFILE_NAMES = {"package-lock.json", "npm-shrinkwrap.json"}
REMOTE_LOCATION = re.compile(r"^(?!file:)(?:[a-z][a-z0-9+.-]*:|//)", re.IGNORECASE)
PORTABILITY_SETTING = re.compile(r"^omit-lockfile-registry-resolved\s*=\s*(.*?)\s*(?:[#;].*)?$")


def git(*arguments, cwd=None):
    return subprocess.run(
        ["git", *arguments],
        cwd=cwd,
        capture_output=True,
        check=True,
        timeout=30,
    ).stdout


def has_download_url(value):
    if isinstance(value, dict):
        for key, child in value.items():
            if key == "resolved" and isinstance(child, str) and REMOTE_LOCATION.match(child):
                return True
            if has_download_url(child):
                return True
    elif isinstance(value, list):
        return any(has_download_url(child) for child in value)
    return False


def validate(staged):
    root = Path(git("rev-parse", "--show-toplevel").decode("utf-8").strip())
    tracked = git("ls-files", "--cached", "-z", cwd=root).decode("utf-8").split("\0")
    lockfiles = sorted({
        path for path in tracked
        if PurePosixPath(path).name in LOCKFILE_NAMES
        and "node_modules" not in PurePosixPath(path).parts
    })
    errors = []
    for config in sorted({str(PurePosixPath(path).parent / ".npmrc") for path in lockfiles}):
        if config not in tracked:
            errors.append(f"{config}: project .npmrc must be tracked.")
            continue
        try:
            content = git("show", f":{config}", cwd=root) if staged else (root / config).read_bytes()
            settings = [
                match.group(1) for line in content.decode("utf-8-sig").splitlines()
                if (match := PORTABILITY_SETTING.fullmatch(line.strip()))
            ]
        except (OSError, UnicodeError):
            errors.append(f"{config}: cannot read project .npmrc.")
            continue
        if not settings or settings[-1] != "true":
            errors.append(f"{config}: must set omit-lockfile-registry-resolved=true.")

    for path in lockfiles:
        try:
            content = git("show", f":{path}", cwd=root) if staged else (root / path).read_bytes()
        except OSError:
            errors.append(f"{path}: cannot read tracked lockfile.")
            continue
        try:
            data = json.loads(content.decode("utf-8-sig"))
        except (UnicodeError, json.JSONDecodeError):
            errors.append(f"{path}: invalid JSON in lockfile.")
            continue
        if not isinstance(data, dict):
            errors.append(f"{path}: lockfile must be a JSON object.")
        elif has_download_url(data):
            errors.append(f"{path}: fixed download URL found in a resolved field.")

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        print(
            "Set omit-lockfile-registry-resolved=true in each npm project's .npmrc, "
            "then run npm install --package-lock-only --ignore-scripts in that project. "
            "Review version/integrity changes before staging the lockfile.",
            file=sys.stderr,
        )
        return 1
    print(f"Validated {len(lockfiles)} lockfile(s): no fixed download URLs.")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--staged", action="store_true", help="Check Git's index, not working files.")
    arguments = parser.parse_args()
    try:
        return validate(arguments.staged)
    except (OSError, subprocess.CalledProcessError, subprocess.TimeoutExpired, UnicodeError):
        # Git diagnostics can contain private source locations; never echo them.
        print("Cannot inspect tracked npm lockfiles: Git failed or timed out.", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
