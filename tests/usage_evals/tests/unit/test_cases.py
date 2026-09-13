from pathlib import Path

import pytest

from usage_evals.cases import CASE_NAMES, Case, prepare_case, verify_files


@pytest.mark.parametrize("name", CASE_NAMES)
def test_initial_state_is_not_a_success(tmp_path, name):
    case = prepare_case(name, tmp_path, "unique-run")
    assert not verify_files(case).passed


@pytest.mark.parametrize("name", CASE_NAMES)
def test_exact_expected_files_are_required(tmp_path, name):
    case = prepare_case(name, tmp_path, "unique-run")
    for path, content in case.expected.items():
        path.write_text(content, encoding="utf-8")
    verdict = verify_files(case)
    assert verdict.passed
    assert len(verdict.checks) == len(case.expected)


def test_wrong_target_fails_even_when_requested_file_is_correct(tmp_path):
    case = prepare_case("choose_document", tmp_path, "unique-run")
    for path, content in case.expected.items():
        path.write_text(content, encoding="utf-8")
    case.unchanged[0].write_text("Changed the wrong document", encoding="utf-8")
    assert not verify_files(case).passed


def test_claiming_saved_without_a_file_does_not_pass(tmp_path):
    case = prepare_case("create_note", tmp_path, "unique-run")
    verdict = verify_files(case)
    assert not verdict.passed
    assert verdict.checks[0]["reason"] == "missing"


def test_wrong_contents_and_non_text_are_reported(tmp_path):
    case = prepare_case("create_note", tmp_path, "unique-run")
    output = next(iter(case.expected))
    output.write_bytes(b"\xff\xfe\x00")
    assert verify_files(case).checks[0]["reason"] == "unreadable_text"
    output.write_text("I saved it successfully", encoding="utf-8")
    assert verify_files(case).checks[0]["reason"] == "content_mismatch"


def test_windows_line_endings_and_optional_final_newline_are_accepted(tmp_path):
    case = prepare_case("create_note", tmp_path, "unique-run")
    for path, content in case.expected.items():
        path.write_bytes((content.replace("\n", "\r\n") + "\r\n").encode("utf-8-sig"))
    assert verify_files(case).passed


def test_extra_content_is_not_normalized_away(tmp_path):
    case = prepare_case("create_note", tmp_path, "unique-run")
    for path, content in case.expected.items():
        path.write_text(content + "\n\n", encoding="utf-8")
    assert not verify_files(case).passed


def test_prompts_have_goals_not_tool_hints(tmp_path):
    for name in CASE_NAMES:
        case = prepare_case(name, tmp_path / name, "unique-run")
        assert "Notepad" in case.prompt
        for hint in ("ui_type", "file_save", "--window", "automationId", "windowHandle"):
            assert hint not in case.prompt


def test_unknown_case_is_an_error(tmp_path):
    with pytest.raises(ValueError, match="Unknown"):
        prepare_case("typo", tmp_path, "unique-run")


def test_preparation_does_not_overwrite_existing_inputs(tmp_path):
    prepare_case("edit_note", tmp_path, "unique-run")
    with pytest.raises(FileExistsError):
        prepare_case("edit_note", tmp_path, "unique-run")


def test_run_id_cannot_escape_directory(tmp_path):
    with pytest.raises(ValueError, match="run ID"):
        prepare_case("create_note", tmp_path, str(Path("..") / "escape"))


def test_existing_output_cannot_make_a_create_run_pass(tmp_path):
    case = prepare_case("create_note", tmp_path, "unique-run")
    for path, content in case.expected.items():
        path.write_text(content, encoding="utf-8")
    with pytest.raises(FileExistsError):
        prepare_case("create_note", tmp_path, "unique-run")


def test_missing_expectations_are_not_a_verified_outcome():
    assert not verify_files(Case("invalid", "Do something", {})).passed
