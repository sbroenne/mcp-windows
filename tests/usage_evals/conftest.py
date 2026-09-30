import itertools
import os
from pathlib import Path

import pytest
from pytest_skill_engineering.copilot.result import CopilotResult, ToolCall

from usage_evals.cases import CASE_NAMES
from usage_evals.comparison import (
    APPS,
    MODELS,
    ROUTES,
    comparison_matrix,
    require_comparison_budget,
)
from usage_evals.policy import validate_budget, validate_desktop
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
    comparison = parser.getgroup("windows-comparison")
    comparison.addoption("--run-comparison", action="store_true")
    comparison.addoption("--comparison-model", action="append", choices=MODELS)
    comparison.addoption("--comparison-app", action="append", choices=APPS)
    comparison.addoption("--comparison-route", action="append", choices=ROUTES)
    comparison.addoption("--comparison-max-runs", type=int, default=0)
    comparison.addoption("--comparison-timeout", type=int, default=0)
    comparison.addoption("--comparison-max-calls", type=int, default=0)
    comparison.addoption("--comparison-server", default="")
    comparison.addoption(
        "--comparison-output", default="", help="New persistent evidence directory."
    )


def pytest_generate_tests(metafunc):
    if "comparison_case" in metafunc.fixturenames:
        config = metafunc.config
        try:
            parameters = comparison_matrix(
                config.getoption("--comparison-model"),
                config.getoption("--comparison-app"),
                config.getoption("--comparison-route"),
            )
        except ValueError as error:
            raise pytest.UsageError(str(error)) from error
        metafunc.parametrize("comparison_case", parameters, ids=["-".join(p) for p in parameters])
        return
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
    if not config.getoption("--run-comparison"):
        for item in items:
            if item.get_closest_marker("comparison_live"):
                item.add_marker(pytest.mark.skip(reason="Live comparison was not requested."))
    if not config.getoption("--run-usage-evals"):
        for item in items:
            if item.get_closest_marker("usage_live"):
                item.add_marker(pytest.mark.skip(reason="Live usage was not requested."))


def pytest_collection_finish(session):
    config = session.config
    if not config.option.collectonly and config.getoption("--run-comparison"):
        validate_comparison_collection(config, session.items)
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
        validate_desktop(os.environ)
    except (ValueError, RuntimeError) as error:
        raise pytest.UsageError(str(error)) from error
    options = ["--usage-cli", "--usage-server"]
    if config.getoption("--usage-baseline-cli"):
        options += ["--usage-baseline-cli", "--usage-baseline-server"]
    for option in options:
        value = config.getoption(option)
        if not value or not Path(value).is_file() or Path(value).suffix.lower() != ".exe":
            raise pytest.UsageError(f"{option} must name an existing built executable.")


def validate_comparison_collection(config, items):
    selected = sum(item.get_closest_marker("comparison_live") is not None for item in items)
    try:
        require_comparison_budget(
            selected,
            config.getoption("--comparison-max-runs"),
            config.getoption("--comparison-model"),
            config.getoption("--comparison-timeout"),
            config.getoption("--comparison-max-calls"),
        )
    except ValueError as error:
        raise pytest.UsageError(str(error)) from error
    output = config.getoption("--comparison-output")
    if not output or Path(output).exists():
        raise pytest.UsageError("--comparison-output must name a new persistent directory.")
    for option in ("--aitest-json", "--junitxml"):
        destination = config.getoption(option)
        if not destination or not Path(destination).resolve().is_relative_to(
            Path(output).resolve()
        ):
            raise pytest.UsageError(f"{option} must be inside --comparison-output.")
    if config.getoption("--run-usage-evals"):
        raise pytest.UsageError("Run the comparison separately from usage evaluations.")
    from pytest_skill_engineering.copilot import CopilotEval

    required = {"max_tool_calls", "image_detail", "audit_requests", "client_mode"}
    if not required <= CopilotEval.__dataclass_fields__.keys():
        raise pytest.UsageError("The pinned framework lacks required benchmark controls.")
    try:
        validate_desktop(os.environ)
        require_capture_api(ToolCall, CopilotResult)
    except (ValueError, RuntimeError) as error:
        raise pytest.UsageError(str(error)) from error
    server = Path(config.getoption("--comparison-server"))
    if server.suffix.lower() != ".exe" or not server.is_file():
        raise pytest.UsageError("--comparison-server must name an existing built MCP executable.")
    if getattr(config.option, "numprocesses", None):
        raise pytest.UsageError("Comparison cases must run serially on the reserved desktop.")


@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    report = outcome.get_result()
    if item.get_closest_marker("comparison_live") and report.failed:
        recorded = {name for name, _ in report.user_properties}
        if report.when != "call" or not {"verification", "cleanup"} <= recorded:
            item.session.shouldstop = (
                "Comparison setup or cleanup is incomplete; no more model calls."
            )
