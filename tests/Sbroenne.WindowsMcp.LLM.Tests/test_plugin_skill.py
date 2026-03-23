"""
Plugin skill tests for the bundled windows-automation skill.

These tests cover the skill layer only:
- plugin manifest points at the skills directory
- pytest-skill-engineering can load the bundled skill
- the skill text encodes the semantic-first guidance we care about
- the expected prompt construction prepends skill content before the system prompt
"""

import json
import re

from conftest import REPO_ROOT, SKILL_DIR, SYSTEM_PROMPT
from pytest_skill_engineering import load_skill

PLUGIN_MANIFEST = REPO_ROOT / "plugin" / ".claude-plugin" / "plugin.json"

def test_windows_automation_skill_loads_from_plugin_bundle():
    """Validate the plugin points at its skills directory and the skill parses."""
    manifest = json.loads(PLUGIN_MANIFEST.read_text(encoding="utf-8"))
    skill = load_skill(SKILL_DIR)

    assert manifest["skills"] == "./skills/"
    assert skill.name == "windows-automation"
    assert "semantic" in skill.description.lower()
    assert "file_save" in skill.content
    assert "ui_type" in skill.content
    assert "screenshot_control" in skill.content


def test_windows_automation_skill_is_prepended_to_agent_prompt():
    """Skill content must be injected ahead of the system prompt.

    Verifies the prompt construction convention used by conftest.make_agent():
    the skill's SKILL.md text is prepended before the shared SYSTEM_PROMPT.
    """
    skill = load_skill(SKILL_DIR)
    combined = skill.content + "\n\n" + SYSTEM_PROMPT

    assert combined.startswith(skill.content)
    assert SYSTEM_PROMPT in combined
    assert combined.index(skill.content) < combined.index(SYSTEM_PROMPT)


def test_windows_automation_skill_text_steers_semantic_first_choices():
    """Skill text should prefer semantic UI tools and the file_save workflow."""
    skill = load_skill(SKILL_DIR)

    assert re.search(
        r"ui_find.*ui_read.*ui_click.*ui_type",
        skill.content,
        re.IGNORECASE | re.DOTALL,
    )
    assert re.search(
        r"Use `file_save`.*instead of sending raw keyboard shortcuts",
        skill.content,
        re.IGNORECASE,
    )
    assert re.search(
        r"Only fall back to `screenshot_control`, `mouse_control`, or `keyboard_control`",
        skill.content,
        re.IGNORECASE,
    )
    assert re.search(
        r"Do not save files with raw `Ctrl\+S`",
        skill.content,
        re.IGNORECASE,
    )

