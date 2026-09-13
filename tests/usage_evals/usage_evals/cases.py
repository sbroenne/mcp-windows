import hashlib
import re
from dataclasses import dataclass
from pathlib import Path

CASE_NAMES = ("create_note", "edit_note", "choose_document")


@dataclass(frozen=True)
class Case:
    name: str
    prompt: str
    expected: dict[Path, str]
    unchanged: tuple[Path, ...] = ()


@dataclass(frozen=True)
class Verification:
    passed: bool
    checks: list[dict[str, str | bool]]


def prepare_case(name: str, directory: Path, run_id: str) -> Case:
    if name not in CASE_NAMES:
        raise ValueError(f"Unknown usage case: {name}")
    if not re.fullmatch(r"[a-zA-Z0-9-]+", run_id):
        raise ValueError("Invalid run ID")
    directory.mkdir(parents=True, exist_ok=True)
    note = directory / f"release-{run_id}.txt"
    if name == "create_note":
        if note.exists():
            raise FileExistsError(f"Output already exists before evaluation: {note}")
        content = "Project: Aurora\nStatus: Ready\nOwner: Morgan"
        return Case(
            name,
            f"In Notepad, create a note containing these three lines:\n{content}\n"
            f'Save it as "{note}". Leave the document open when finished.',
            {note: content},
        )

    initial = "Project: Aurora\nStatus: Draft\nOwner: Morgan"
    with note.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(initial)
    if name == "edit_note":
        return Case(
            name,
            f'Open "{note}" in Notepad. Change the status from Draft to Ready without '
            "changing the project or owner, and save the updated document. Leave it open.",
            {note: initial.replace("Draft", "Ready")},
        )

    companion = directory / f"release-{run_id}-archive.txt"
    with companion.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(initial)
    return Case(
        name,
        f'The current release note is "{note}" and its archive is "{companion}". '
        "Using Notepad, change the current note's owner from Morgan to Taylor and save it. "
        "Keep the archive unchanged. Leave the current note open.",
        {note: initial.replace("Morgan", "Taylor"), companion: initial},
        unchanged=(companion,),
    )


def _normalize(text: str) -> str:
    return text.replace("\r\n", "\n").removesuffix("\n")


def verify_files(case: Case) -> Verification:
    if not case.expected:
        return Verification(False, [{"passed": False, "reason": "no_expectations"}])
    checks: list[dict[str, str | bool]] = []
    for path, expected in case.expected.items():
        check: dict[str, str | bool] = {"file": path.name, "passed": False}
        try:
            actual = path.read_text(encoding="utf-8-sig")
        except FileNotFoundError:
            check["reason"] = "missing"
        except UnicodeError:
            check["reason"] = "unreadable_text"
        except OSError as error:
            check["reason"] = f"read_error: {type(error).__name__}"
        else:
            check["passed"] = _normalize(actual) == _normalize(expected)
            check["reason"] = "matched" if check["passed"] else "content_mismatch"
            check["actual_text"] = actual[:4096]
            check["actual_sha256"] = hashlib.sha256(actual.encode("utf-8")).hexdigest()
        checks.append(check)
    return Verification(passed=all(check["passed"] for check in checks), checks=checks)
