import ntpath
import re
from collections.abc import Iterable
from typing import Protocol


class Call(Protocol):
    name: str
    arguments: dict


_TOKEN = re.compile(
    r"""('(?:[^']|'')*'|"(?:[^"$`]|`[rn])*"|&|[^\s'";&|(){}<>$`@]+)(?:[ \t]+|$)"""
)


def validate_budget(selected: int, cap: int, model: str, timeout: int) -> None:
    if selected < 1 or cap < 1 or selected > cap:
        raise ValueError(f"Selected {selected} live runs; approved cap is {cap}.")
    if not model.strip():
        raise ValueError("An explicit --usage-model is required.")
    if timeout < 1:
        raise ValueError("A positive --usage-timeout is required.")


def is_cli_invocation(command: str, executable: str) -> bool:
    """Conservatively recognize one literal CLI call; this is not a shell sandbox."""
    command = command.strip()
    tokens: list[str] = []
    position = 0
    while position < len(command):
        match = _TOKEN.match(command, position)
        if match is None:
            return False
        token = match.group(1)
        if token.startswith(("'", '"')):
            token = token[1:-1].replace("''", "'")
        tokens.append(token)
        position = match.end()
    if tokens and tokens[0] == "&":
        tokens.pop(0)
    if not tokens or "&" in tokens:
        return False
    return ntpath.normcase(ntpath.normpath(tokens[0])) == ntpath.normcase(
        ntpath.normpath(executable)
    )


def classify_calls(
    interface: str, calls: Iterable[Call], executable: str, mcp_names: set[str]
) -> list[str]:
    calls = list(calls)
    if not calls:
        return ["No calls recorded; interface usage is not verified."]
    issues = []
    for index, call in enumerate(calls):
        if interface == "mcp":
            allowed = call.name in mcp_names or call.name in {
                f"windows-{name}" for name in mcp_names
            }
        elif interface == "cli":
            allowed = call.name == "powershell" and is_cli_invocation(
                str(call.arguments.get("command", "")), executable
            )
        else:
            raise ValueError(f"Unknown interface: {interface}")
        if not allowed:
            issues.append(f"Call {index}: {call.name} is outside the verified {interface} route.")
    return issues
