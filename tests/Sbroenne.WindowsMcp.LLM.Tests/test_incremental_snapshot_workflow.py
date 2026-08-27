"""
Incremental UI snapshot workflow tests.

The prompt describes the user outcome without naming tools or parameters. The assertion verifies
that models discover the server-managed automatic snapshot mode for repeated inspection.
"""

import pytest
from conftest import Agent, Provider, SYSTEM_PROMPT, assert_quality


MODELS = [
    "claude-sonnet-5",
    "claude-haiku-4.5",
    "gpt-5-mini",
    "gemini-3.5-flash",
]


@pytest.mark.parametrize("model", MODELS)
async def test_model_uses_incremental_snapshots_for_repeated_inspection(
    aitest_run, windows_mcp_server, copilot_auth, model
):
    del copilot_auth
    agent = Agent(
        name=f"incremental-snapshot-{model}",
        provider=Provider(model=f"copilot/{model}"),
        mcp_servers=[windows_mcp_server],
        system_prompt=SYSTEM_PROMPT,
        max_turns=15,
    )

    result = await aitest_run(
        agent,
        (
            "Open Notepad and inspect its window before making a change. Type "
            '"incremental snapshot check" into the document, inspect the same window again '
            "to determine what changed, then close Notepad without saving. Briefly report "
            "the change you observed."
        ),
    )

    snapshot_calls = [
        call for call in result.all_tool_calls if call.name == "ui_snapshot"
    ]
    automatic_checks = [
        call
        for call in snapshot_calls
        if call.arguments.get("mode") == "auto"
    ]
    automatic_checks.extend(
        call
        for call in result.all_tool_calls
        if call.name in {"ui_click", "ui_type", "ui_select", "ui_batch", "ui_macro"}
        and call.arguments.get("withSnapshot") is True
        and call.arguments.get("snapshotMode") == "auto"
    )

    assert len(snapshot_calls) >= 1, "Expected the model to inspect the window structurally"
    assert len(automatic_checks) >= 2, (
        "Expected automatic snapshots to establish and compare repeated inspections"
    )
    assert_quality(result)
