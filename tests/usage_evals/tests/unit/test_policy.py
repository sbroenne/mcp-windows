from types import SimpleNamespace

import pytest

from usage_evals.policy import classify_calls, is_cli_invocation, validate_budget

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
