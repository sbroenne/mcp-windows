import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run_pytest(*arguments):
    return subprocess.run(
        [sys.executable, "-m", "pytest", "tests/live", *arguments],
        cwd=ROOT,
        capture_output=True,
        text=True,
        timeout=30,
    )


def test_collection_does_not_require_model_desktop_or_build():
    result = run_pytest("--collect-only", "-q")
    assert result.returncode == 0, result.stdout + result.stderr
    assert "6 tests collected" in result.stdout


def test_live_tests_are_not_started_by_default():
    result = run_pytest("-q")
    assert result.returncode == 0, result.stdout + result.stderr
    assert "6 skipped" in result.stdout


def test_insufficient_budget_fails_before_any_model_or_desktop_access():
    result = run_pytest(
        "--run-usage-evals",
        "--usage-model",
        "not-a-real-model",
        "--usage-max-runs",
        "1",
        "--usage-timeout",
        "30",
        "-q",
    )
    assert result.returncode != 0
    assert "Selected 6 live runs; approved cap is 1" in result.stderr + result.stdout


def test_baseline_requires_both_entry_points():
    result = run_pytest("--collect-only", "--usage-baseline-cli", "missing.exe", "-q")
    assert result.returncode != 0
    assert "both baseline" in result.stderr + result.stdout


def test_two_models_collect_separate_comparable_cases():
    result = run_pytest(
        "--collect-only",
        "-q",
        "--usage-model",
        "gpt-5.6-sol",
        "--usage-model",
        "gpt-5.6-luna",
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert "12 tests collected" in result.stdout
    assert "gpt-5.6-sol" in result.stdout
    assert "gpt-5.6-luna" in result.stdout
    cases = [line for line in result.stdout.splitlines() if "::test_windows_usage[" in line]
    assert cases[0].endswith("gpt-5.6-sol]")
    assert cases[1].endswith("gpt-5.6-luna]")
    assert cases[2].endswith("gpt-5.6-luna]")
    assert cases[3].endswith("gpt-5.6-sol]")


def test_budget_counts_both_models():
    result = run_pytest(
        "--run-usage-evals",
        "--usage-model",
        "gpt-5.6-sol",
        "--usage-model",
        "gpt-5.6-luna",
        "--usage-max-runs",
        "6",
        "--usage-timeout",
        "300",
        "-q",
    )
    assert result.returncode != 0
    assert "Selected 12 live runs; approved cap is 6" in result.stderr + result.stdout


def test_duplicate_models_require_explicit_repeats_instead():
    result = run_pytest(
        "--collect-only",
        "-q",
        "--usage-model",
        "gpt-5.6-sol",
        "--usage-model",
        "gpt-5.6-sol",
    )
    assert result.returncode != 0
    assert "Duplicate models" in result.stderr + result.stdout
