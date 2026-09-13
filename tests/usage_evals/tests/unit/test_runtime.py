from types import SimpleNamespace
from unittest.mock import Mock

import psutil
import pytest

from usage_evals.runtime import (
    ProcessRecord,
    build_agent,
    build_metadata,
    cleanup_owned,
    identify_launched_notepads,
    require_capture_api,
)


def test_cleanup_terminates_only_identified_run_documents():
    owned = ProcessRecord(101, 100.0, True)
    unrelated = ProcessRecord(202, 200.0, False)
    process = Mock()
    process.create_time.return_value = 100.0
    other_process = Mock()
    other_process.create_time.return_value = 200.0
    other_process.wait.side_effect = psutil.TimeoutExpired(5, pid=202)
    lookup = Mock(side_effect=[process, other_process])
    with pytest.raises(RuntimeError, match="Unowned"):
        cleanup_owned([owned, unrelated], lookup)
    assert [call.args for call in lookup.call_args_list] == [(101,), (202,)]
    process.terminate.assert_called_once()
    process.wait.assert_called_once_with(timeout=5)
    other_process.terminate.assert_not_called()


def test_already_exited_unowned_process_does_not_block_the_next_case():
    lookup = Mock(side_effect=psutil.NoSuchProcess(202))
    assert cleanup_owned([ProcessRecord(202, 200.0, False)], lookup) == []


def test_unowned_launch_helper_can_exit_naturally_without_termination():
    process = Mock()
    process.create_time.return_value = 200.0
    assert cleanup_owned([ProcessRecord(202, 200.0, False)], lambda _: process) == []
    process.wait.assert_called_once_with(timeout=5)
    process.terminate.assert_not_called()


def test_reused_unowned_pid_is_not_waited_on_or_terminated():
    process = Mock()
    process.create_time.return_value = 999.0
    with pytest.raises(RuntimeError, match="identity changed"):
        cleanup_owned([ProcessRecord(202, 200.0, False)], lambda _: process)
    process.wait.assert_not_called()
    process.terminate.assert_not_called()


def test_reused_process_id_is_never_terminated():
    process = Mock()
    process.create_time.return_value = 999.0
    with pytest.raises(RuntimeError, match="identity changed"):
        cleanup_owned([ProcessRecord(101, 100.0, True)], lambda _: process)
    process.terminate.assert_not_called()


def test_already_exited_owned_process_is_not_a_cleanup_failure():
    lookup = Mock(side_effect=psutil.NoSuchProcess(101))
    assert cleanup_owned([ProcessRecord(101, 100.0, True)], lookup) == [101]


def test_cleanup_failure_is_not_hidden():
    process = Mock()
    process.create_time.return_value = 100.0
    process.terminate.side_effect = psutil.AccessDenied(101)
    with pytest.raises(psutil.AccessDenied):
        cleanup_owned([ProcessRecord(101, 100.0, True)], lambda _: process)


def test_old_framework_fails_before_model_calls():
    with pytest.raises(RuntimeError, match="capture"):
        require_capture_api(SimpleNamespace(), SimpleNamespace())


def test_supported_capture_contract():
    require_capture_api(
        SimpleNamespace(completion_received=None),
        SimpleNamespace(evidence_complete=True),
    )


def test_cli_agent_has_no_mcp_and_no_automatic_retry(tmp_path):
    agent = build_agent(
        "cli",
        "selected-model",
        123,
        tmp_path,
        tmp_path / "wincli.exe",
        tmp_path / "server.exe",
        {"ui_type", "app"},
    )
    assert agent.model == "selected-model"
    assert agent.timeout_s == 123
    assert agent.max_retries == 0
    assert agent.allowed_tools == ["powershell"]
    assert agent.mcp_servers == {}
    assert agent.working_directory == str(tmp_path)


def test_mcp_agent_does_not_expose_shell(tmp_path):
    agent = build_agent(
        "mcp",
        "selected-model",
        123,
        tmp_path,
        tmp_path / "wincli.exe",
        tmp_path / "server.exe",
        {"ui_type", "app"},
    )
    assert "powershell" not in agent.allowed_tools
    assert set(agent.mcp_servers) == {"windows"}
    assert agent.max_retries == 0
    assert "ui_type" not in agent.instructions


def test_unknown_interface_is_not_silently_mcp(tmp_path):
    with pytest.raises(ValueError):
        build_agent("typo", "model", 123, tmp_path, tmp_path, tmp_path, set())


def test_build_identity_includes_dependency_manifests(tmp_path):
    cli = tmp_path / "wincli.exe"
    server = tmp_path / "server.exe"
    cli.write_bytes(b"cli")
    server.write_bytes(b"server")
    dependencies = cli.with_suffix(".deps.json")
    dependencies.write_text('{"dependencies": "before"}', encoding="utf-8")
    before = build_metadata(cli, server)
    dependencies.write_text('{"dependencies": "after"}', encoding="utf-8")
    after = build_metadata(cli, server)
    assert before["build_hashes"][str(dependencies)] != after["build_hashes"][str(dependencies)]
    assert before["framework_source"] == after["framework_source"]


@pytest.mark.parametrize("interface", ["cli", "mcp"])
def test_comparison_can_fix_the_same_reasoning_effort(tmp_path, interface):
    agent = build_agent(
        interface,
        "selected-model",
        300,
        tmp_path,
        tmp_path / "wincli.exe",
        tmp_path / "server.exe",
        {"app"},
        reasoning_effort="medium",
    )
    assert agent.reasoning_effort == "medium"


@pytest.mark.parametrize("interface", ["cli", "mcp"])
def test_fresh_sessions_disable_ambient_instruction_discovery(tmp_path, interface):
    agent = build_agent(
        interface,
        "selected-model",
        300,
        tmp_path,
        tmp_path / "wincli.exe",
        tmp_path / "server.exe",
        {"app"},
    )
    config = agent.build_session_config()
    assert config["enable_config_discovery"] is False
    assert config["skip_custom_instructions"] is True
    assert config["enable_on_demand_instruction_discovery"] is False
    assert config["enable_file_hooks"] is False
    assert config["config_directory"] == str(tmp_path / "configuration")
    assert config["available_tools"] == agent.allowed_tools
    if interface == "mcp":
        assert config["mcp_servers"] == agent.mcp_servers


def test_recorded_launch_identifies_an_unsaved_test_window():
    call = SimpleNamespace(
        name="windows-app",
        arguments={"programPath": "notepad.exe"},
        result='{"success":true,"window":{"pid":101,"processName":"Notepad"}}',
    )
    records = identify_launched_notepads(
        [ProcessRecord(101, 100.0, False)], [call], 99.0, r"D:\wincli.exe"
    )
    assert records[0].launch_verified
    assert not records[0].document_matches
    process = Mock()
    process.create_time.return_value = 100.0
    assert cleanup_owned(records, lambda _: process) == [101]


@pytest.mark.parametrize(
    "call",
    [
        SimpleNamespace(
            name="windows-ui_read",
            arguments={},
            result='{"success":true,"window":{"pid":101,"processName":"Notepad"}}',
        ),
        SimpleNamespace(
            name="windows-app",
            arguments={},
            result='{"success":false,"window":{"pid":101,"processName":"Notepad"}}',
        ),
        SimpleNamespace(name="windows-app", arguments={}, result="not json"),
        SimpleNamespace(
            name="windows-app",
            arguments={},
            result='{"success":true,"window":{"pid":202,"processName":"Notepad"}}',
        ),
        SimpleNamespace(
            name="windows-app",
            arguments={},
            result='{"success":true,"window":{"pid":101,"processName":"Other"}}',
        ),
    ],
)
def test_unproven_launch_cannot_authorize_cleanup(call):
    records = identify_launched_notepads(
        [ProcessRecord(101, 100.0, False)], [call], 99.0, r"D:\wincli.exe"
    )
    assert not records[0].launch_verified


def test_process_predating_the_empty_desktop_check_is_not_claimed():
    call = SimpleNamespace(
        name="windows-app",
        arguments={},
        result='{"success":true,"window":{"pid":101,"processName":"Notepad"}}',
    )
    records = identify_launched_notepads(
        [ProcessRecord(101, 90.0, False)], [call], 99.0, r"D:\wincli.exe"
    )
    assert not records[0].launch_verified


def test_literal_cli_app_result_also_identifies_its_process():
    call = SimpleNamespace(
        name="powershell",
        arguments={"command": "& 'D:\\wincli.exe' app --path notepad.exe"},
        result=(
            '{"success":true,"window":{"pid":101,"processName":"Notepad"}}\n'
            "<shellId: 1 completed with exit code 0>"
        ),
    )
    records = identify_launched_notepads(
        [ProcessRecord(101, 100.0, False)], [call], 99.0, r"D:\wincli.exe"
    )
    assert records[0].launch_verified
