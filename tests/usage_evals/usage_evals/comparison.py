"""Windows-specific inputs and safeguards for framework-run comparisons."""

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
MODELS = ("gpt-6-astra", "gpt-6-luna", "gpt-5.6-sol", "gpt-5.6-luna")
APPS = ("notepad", "word", "powerpoint", "chrome")
ROUTES = ("controls", "screenshots")


def comparison_matrix(models=None, apps=None, routes=None):
    models = MODELS if models is None else models
    apps = APPS if apps is None else apps
    routes = ROUTES if routes is None else routes
    for name, selected, allowed in (
        ("models", models, MODELS),
        ("apps", apps, APPS),
        ("routes", routes, ROUTES),
    ):
        if not selected or len(selected) != len(set(selected)) or set(selected) - set(allowed):
            raise ValueError(f"Select unique supported comparison {name}.")
    return [
        (app, model, route)
        for app_index, app in enumerate(apps)
        for model_index, model in enumerate(models)
        for route in (routes if (app_index + model_index) % 2 == 0 else tuple(reversed(routes)))
    ]


def require_comparison_budget(selected, cap, models, timeout, calls):
    if not models:
        raise ValueError("Select each approved model with --comparison-model.")
    if selected < 1 or cap < selected:
        raise ValueError(f"Selected {selected} comparison runs; approved cap is {cap}.")
    if timeout < 1 or calls < 1:
        raise ValueError("Approve positive --comparison-timeout and --comparison-max-calls.")


def require_execution_evidence(result, model, tools):
    if result.model_used != model or not result.evidence_complete:
        raise RuntimeError(
            "The framework did not capture complete evidence for the requested model."
        )
    if result.stop_reason not in {"completed", "tool_budget_exceeded", "timeout"}:
        raise RuntimeError(f"Comparison execution failed: {result.stop_reason}: {result.error}")
    if not result.request_audit:
        raise RuntimeError("The framework recorded no actual model requests.")
    hashes = set()
    for request in result.request_audit:
        if (
            request.model != model
            or set(request.tool_names) != set(tools)
            or request.reasoning_effort != "medium"
            or request.image_count != len(request.image_details)
            or any(detail != "high" for detail in request.image_details)
        ):
            raise RuntimeError(
                "Actual model requests did not match the approved comparison settings."
            )
        hashes.add(request.instructions_sha256)
    if len(hashes) != 1 or not all(hashes):
        raise RuntimeError("Actual instructions changed during the comparison case.")
    return hashes.pop()


def desktop_helpers():
    spec = importlib.util.spec_from_file_location(
        "real_app_desktop", ROOT / "scripts" / "benchmark-real-apps.py"
    )
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def build_comparison_agent(model, timeout, max_calls, workspace, tools):
    from pytest_skill_engineering.copilot import CopilotEval
    from pytest_skill_engineering.copilot.personas import HeadlessPersona

    return CopilotEval(
        name="windows-controls-comparison",
        model=model,
        reasoning_effort="medium",
        timeout_s=float(timeout),
        max_retries=0,
        max_tool_calls=max_calls,
        image_detail="high",
        audit_requests=True,
        client_mode="empty",
        persona=HeadlessPersona(),
        system_message_mode="replace",
        instructions=desktop_helpers().INSTRUCTIONS,
        working_directory=str(workspace),
        allowed_tools=[tool.name for tool in tools],
        extra_config={
            "tools": tools,
            "config_directory": str(workspace / "configuration"),
            "enable_config_discovery": False,
            "skip_custom_instructions": True,
            "enable_skills": False,
            "enable_on_demand_instruction_discovery": False,
            "enable_file_hooks": False,
        },
    )


def guarded_tools(catalog, bridge, route):
    from copilot import Tool

    desktop = desktop_helpers()
    tools = []
    for tool in catalog:
        if tool.name not in desktop.allowed_tools(route):
            continue
        description = tool.description or tool.name
        parameters = desktop.restricted_schema(tool.name, tool.input_schema)
        if tool.name == "screenshot_control":
            description = (
                "Capture an unannotated image of a trial window. Image coordinates are "
                "window-relative. No control metadata is included."
            )
            parameters = {
                "type": "object",
                "properties": {"windowHandle": {"type": "string"}},
                "required": ["windowHandle"],
                "additionalProperties": False,
            }
        tools.append(
            Tool(
                name=tool.name,
                description=description,
                parameters=parameters,
                handler=bridge.invoke,
                skip_permission=True,
                defer="never",
            )
        )
    if {tool.name for tool in tools} != desktop.allowed_tools(route):
        raise ValueError("The server did not expose all required comparison tools.")
    return tools
