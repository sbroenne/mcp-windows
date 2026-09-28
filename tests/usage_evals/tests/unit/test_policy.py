from types import SimpleNamespace

import pytest

from usage_evals.policy import classify_calls, is_cli_invocation, validate_budget, validate_desktop

CLI = r"D:\build with spaces\wincli.exe"


@pytest.mark.parametrize(
    "command",
    [
        f"& '{CLI}' --help",
        f'& "{CLI}" ui type --text "Hello; World" --window 123',
        f"& '{CLI}' guidance",
        f"& '{CLI}' tools --json",
        f"& '{CLI}' ui type --text 'Project: Aurora\nStatus: Ready\nOwner: Morgan'",
        f"& '{CLI}' ui type --text 'Project: Aurora\r\nStatus: Ready'",
        f'& "{CLI}" keyboard type --text "Project: Aurora`nStatus: Ready"',
        f'& "{CLI}" clipboard set --text "Project: Aurora`r`nStatus: Ready"',
    ],
)
def test_single_literal_cli_invocation_is_accepted(command):
    assert is_cli_invocation(command, CLI)


@pytest.mark.parametrize(
    "command",
    [
        f"& '{CLI}' --help; Set-Content output.txt done",
        f"& '{CLI}' --help | Out-File output.txt",
        f"& '{CLI}' --help\nSet-Content output.txt done",
        f'& "{CLI}" ui type --text "$(Get-Content secret.txt)"',
        f"& '{CLI}' @args",
        f"& '{CLI}' --text (Get-Content secret.txt)",
        f"& '{CLI}' --help && python helper.py",
        f"& '{CLI}' ui type --text 'safe\ntext'\nSet-Content output.txt done",
        f'& "{CLI}" clipboard set --text "safe`n$(Get-Content secret.txt)"',
        f'& "{CLI}" clipboard set --text "safe`ntext"; Set-Content output.txt done',
        f"& '{CLI}' --help\rSet-Content output.txt done",
        f"& '{CLI}' --help `\nSet-Content output.txt done",
        "& 'D:\\other\\wincli.exe' --help",
        "Set-Content output.txt done",
        "",
    ],
)
def test_bypass_or_ambiguous_shell_is_not_accepted(command):
    assert not is_cli_invocation(command, CLI)


def test_mcp_lane_does_not_accept_shell_or_another_server():
    calls = [
        SimpleNamespace(name="windows-ui_type", arguments={}),
        SimpleNamespace(name="powershell", arguments={"command": "echo done"}),
        SimpleNamespace(name="other-ui_type", arguments={}),
    ]
    issues = classify_calls("mcp", calls, CLI, {"ui_type"})
    assert len(issues) == 2


def test_cli_lane_records_mcp_bypass():
    calls = [SimpleNamespace(name="windows-ui_type", arguments={})]
    assert classify_calls("cli", calls, CLI, {"ui_type"})


def test_empty_trace_cannot_prove_interface_usage():
    assert classify_calls("mcp", [], CLI, {"ui_type"})


def test_relative_cli_path_requires_the_selected_working_directory():
    assert is_cli_invocation(r"& '.\cli\wincli.exe' --help", r"D:\run\cli\wincli.exe", r"D:\run")
    assert not is_cli_invocation(r"& '.\cli\wincli.exe' --help", CLI, r"D:\run")
    assert not is_cli_invocation(r"& '.\cli\wincli.exe' --help", CLI)


def test_cli_can_read_only_shell_sessions_started_by_its_own_invocations():
    calls = [
        SimpleNamespace(
            name="powershell", arguments={"command": f"& '{CLI}' app --path notepad.exe"},
            result="<command with shellId: 4 is still running after 30 seconds.>",
        ),
        SimpleNamespace(name="read_powershell", arguments={"shellId": "4", "delay": 5},
                        result="<shellId: 4 completed with exit code 0>"),
    ]
    assert classify_calls("cli", calls, CLI, {"app"}) == []
    calls.append(SimpleNamespace(name="read_powershell", arguments={"shellId": "foreign"}))
    assert len(classify_calls("cli", calls, CLI, {"app"})) == 1


def test_rejected_shell_command_cannot_authorize_a_followup_read():
    calls = [
        SimpleNamespace(
            name="powershell", arguments={"command": "Read-Host"}, result="<shellId: 4>"
        ),
        SimpleNamespace(name="read_powershell", arguments={"shellId": "4"}),
    ]
    assert len(classify_calls("cli", calls, CLI, {"app"})) == 2


@pytest.mark.parametrize("selected,cap", [(6, 5), (1, 0), (0, 6)])
def test_budget_mismatch_fails_before_running(selected, cap):
    with pytest.raises(ValueError):
        validate_budget(selected, cap, "chosen-model", 120)


def test_model_and_timeout_are_required():
    with pytest.raises(ValueError):
        validate_budget(1, 1, "", 120)
    with pytest.raises(ValueError):
        validate_budget(1, 1, "chosen-model", 0)


def test_valid_budget_does_not_choose_model_or_add_runs():
    assert validate_budget(6, 6, "chosen-model", 120) is None


@pytest.mark.parametrize(
    "environment",
    [
        {},
        {"MCP_TEST_DESKTOP_INPUT": "1"},
        {"MCP_USAGE_RESERVED_DESKTOP": "1"},
        {"MCP_USAGE_DISPOSABLE_DESKTOP": "1"},
        {"MCP_TEST_DESKTOP_INPUT": "1", "MCP_USAGE_RESERVED_DESKTOP": "true"},
    ],
)
def test_live_desktop_requires_input_and_explicit_ownership_approval(environment):
    with pytest.raises(ValueError, match="exclusive"):
        validate_desktop(environment)


@pytest.mark.parametrize(
    "flag,expected",
    [
        ("MCP_USAGE_DISPOSABLE_DESKTOP", "disposable"),
        ("MCP_USAGE_RESERVED_DESKTOP", "reserved-existing-profile"),
    ],
)
def test_desktop_approval_records_existing_profile_without_claiming_clean_isolation(flag, expected):
    assert validate_desktop({"MCP_TEST_DESKTOP_INPUT": "1", flag: "1"}) == expected
