import itertools
import os
from pathlib import Path

import pytest
from pytest_skill_engineering.copilot.result import CopilotResult, ToolCall

from usage_evals.cases import CASE_NAMES
from usage_evals.policy import validate_budget
from usage_evals.runtime import require_capture_api


def pytest_addoption(parser):
    group = parser.getgroup("windows-usage")
    group.addoption(
        "--run-usage-evals", action="store_true", help="Opt in to live model/desktop use."
    )
    group.addoption(
        "--usage-model", action="append", help="Explicit model; repeat to compare models."
    )
    group.addoption(
        "--usage-reasoning-effort",
        choices=("none", "low", "medium", "high", "xhigh", "max"),
        default=None,
        help="Use the same explicit reasoning setting for all compared models.",
    )
    group.addoption(
        "--usage-max-runs", type=int, default=0, help="Approved total live execution cap."
    )
    group.addoption(
        "--usage-timeout", type=int, default=0, help="Approved per-run timeout in seconds."
    )
    group.addoption("--usage-repeats", type=int, default=1)
    group.addoption("--usage-interface", choices=("cli", "mcp", "both"), default="both")
    group.addoption("--usage-case", action="append", choices=CASE_NAMES)
    group.addoption("--usage-cli", default="", help="Candidate wincli.exe.")
    group.addoption("--usage-server", default="", help="Candidate MCP server executable.")
    group.addoption("--usage-baseline-cli", default="", help="Optional baseline wincli.exe.")
    group.addoption("--usage-baseline-server", default="", help="Optional baseline MCP executable.")


def pytest_generate_tests(metafunc):
    if "usage_case" not in metafunc.fixturenames:
        return
    config = metafunc.config
    repeats = config.getoption("--usage-repeats")
    if repeats < 1:
        raise pytest.UsageError("--usage-repeats must be positive.")
    baseline_cli = config.getoption("--usage-baseline-cli")
    baseline_server = config.getoption("--usage-baseline-server")
    if bool(baseline_cli) != bool(baseline_server):
        raise pytest.UsageError("Provide both baseline entry points or neither.")
    variants = ("baseline", "candidate") if baseline_cli else ("candidate",)
    selected = config.getoption("--usage-interface")
    interfaces = ("cli", "mcp") if selected == "both" else (selected,)
    cases = config.getoption("--usage-case") or CASE_NAMES
    models = config.getoption("--usage-model") or [""]
    if len(models) != len(set(models)):
        raise pytest.UsageError("Duplicate models: use --usage-repeats for repeated runs.")
    parameters = []
    for index, group in enumerate(
        itertools.product(cases, interfaces, range(1, repeats + 1), variants)
    ):
        ordered_models = models if index % 2 == 0 else list(reversed(models))
        parameters.extend((*group, model) for model in ordered_models)
    metafunc.parametrize(
        "usage_case",
        parameters,
        ids=["-".join(str(value) or "unselected-model" for value in p) for p in parameters],
    )


def pytest_collection_modifyitems(config, items):
    if not config.getoption("--run-usage-evals"):
        for item in items:
            if item.get_closest_marker("usage_live"):
                item.add_marker(pytest.mark.skip(reason="Live usage was not requested."))


def pytest_collection_finish(session):
    config = session.config
    if config.option.collectonly or not config.getoption("--run-usage-evals"):
        return
    selected = sum(item.get_closest_marker("usage_live") is not None for item in session.items)
    try:
        for model in config.getoption("--usage-model") or [""]:
            validate_budget(
                selected,
                config.getoption("--usage-max-runs"),
                model,
                config.getoption("--usage-timeout"),
            )
        require_capture_api(ToolCall, CopilotResult)
    except (ValueError, RuntimeError) as error:
        raise pytest.UsageError(str(error)) from error
    if (
        os.environ.get("MCP_TEST_DESKTOP_INPUT") != "1"
        or os.environ.get("MCP_USAGE_DISPOSABLE_DESKTOP") != "1"
    ):
        raise pytest.UsageError(
            "Live runs need an exclusive disposable Windows desktop. Set "
            "MCP_TEST_DESKTOP_INPUT=1 and MCP_USAGE_DISPOSABLE_DESKTOP=1 only on that desktop."
        )
    options = ["--usage-cli", "--usage-server"]
    if config.getoption("--usage-baseline-cli"):
        options += ["--usage-baseline-cli", "--usage-baseline-server"]
    for option in options:
        value = config.getoption(option)
        if not value or not Path(value).is_file() or Path(value).suffix.lower() != ".exe":
            raise pytest.UsageError(f"{option} must name an existing built executable.")
