import ntpath
import re
from collections.abc import Iterable, Mapping
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


def validate_desktop(environment: Mapping[str, str]) -> str:
    if environment.get("MCP_TEST_DESKTOP_INPUT") == "1":
        if environment.get("MCP_USAGE_RESERVED_DESKTOP") == "1":
            return "reserved-existing-profile"
        if environment.get("MCP_USAGE_DISPOSABLE_DESKTOP") == "1":
            return "disposable"
    raise ValueError(
        "Live runs need an exclusive Windows desktop. Set MCP_TEST_DESKTOP_INPUT=1 and "
        "MCP_USAGE_DISPOSABLE_DESKTOP=1 on a disposable account, or "
        "MCP_USAGE_RESERVED_DESKTOP=1 only after its owner reserves the desktop and confirms "
        "Notepad has no work to preserve. Existing profile state is not clean isolation."
    )


def is_cli_invocation(command: str, executable: str, working_directory: str | None = None) -> bool:
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
    invoked = tokens[0]
    if not ntpath.isabs(invoked) and working_directory:
        invoked = ntpath.join(working_directory, invoked)
    return ntpath.normcase(ntpath.normpath(invoked)) == ntpath.normcase(
        ntpath.normpath(executable)
    )


def cli_route_command(
    call: Call, executable: str, working_directory: str | None, sessions: dict[str, str]
) -> str | None:
    """Bind output reads to shell handles actually issued for approved CLI commands."""
    command = None
    if call.name == "powershell":
        candidate = str(call.arguments.get("command", ""))
        if is_cli_invocation(candidate, executable, working_directory):
            command = candidate
    elif call.name == "read_powershell":
        command = sessions.get(str(call.arguments.get("shellId", "")))
    if command is not None:
        result = getattr(call, "result", None)
        if isinstance(result, str):
            issued = re.search(r"<(?:shellId:|command with shellId:)\s*([\w-]+)[^>]*>\s*$", result)
            if issued:
                sessions[issued[1]] = command
    return command


def classify_calls(
    interface: str, calls: Iterable[Call], executable: str, mcp_names: set[str],
    working_directory: str | None = None,
) -> list[str]:
    calls = list(calls)
    if not calls:
        return ["No calls recorded; interface usage is not verified."]
    issues = []
    sessions: dict[str, str] = {}
    for index, call in enumerate(calls):
        if interface == "mcp":
            allowed = call.name in mcp_names or call.name in {
                f"windows-{name}" for name in mcp_names
            }
        elif interface == "cli":
            allowed = cli_route_command(call, executable, working_directory, sessions) is not None
        else:
            raise ValueError(f"Unknown interface: {interface}")
        if not allowed:
            issues.append(f"Call {index}: {call.name} is outside the verified {interface} route.")
    return issues
