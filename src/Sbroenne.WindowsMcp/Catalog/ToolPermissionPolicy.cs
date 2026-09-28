using System.Collections.Frozen;
using System.Text.Json;

namespace Sbroenne.WindowsMcp.Catalog;

/// <summary>Checks explicitly requested tool dependencies before any action is dispatched.</summary>
internal sealed class ToolPermissionPolicy(IEnumerable<string> enabledTools)
{
    private readonly FrozenSet<string> _enabledTools = enabledTools.ToFrozenSet(StringComparer.Ordinal);

    internal string? Validate(string toolName, IDictionary<string, JsonElement>? arguments)
    {
        if (toolName is "ui_batch" or "ui_click" or "ui_type" or "ui_select" &&
            arguments is not null && arguments.TryGetValue("withSnapshot", out var snapshot))
        {
            if (snapshot.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return "withSnapshot must be a boolean (true or false). No actions were run.";
            }
            if (snapshot.ValueKind == JsonValueKind.True && !_enabledTools.Contains("ui_snapshot"))
            {
                return $"Tool '{toolName}' withSnapshot requires disabled tool 'ui_snapshot'. No actions were run.";
            }
        }

        if (toolName != "ui_batch")
        {
            return null;
        }

        if (arguments is null || !arguments.TryGetValue("steps", out var stepsJson) ||
            stepsJson.ValueKind != JsonValueKind.String)
        {
            return "steps is required: a JSON array encoded as a string.";
        }

        var error = BatchStepValidation.Parse(stepsJson.GetString(), out var steps);
        if (error is not null)
        {
            return error;
        }

        for (var i = 0; i < steps.Length; i++)
        {
            var action = steps[i].Action.Trim().ToLowerInvariant();
            var dependency = action switch
            {
                "find" => "ui_find",
                "click" => "ui_click",
                "type" => "ui_type",
                "select" => "ui_select",
                "wait" => "ui_wait",
                "read" => "ui_read",
                "snapshot" => "ui_snapshot",
                "key" => "keyboard_control",
                "mouse" or "polyline" => "mouse_control",
                _ => null,
            };
            if (dependency is null)
            {
                return $"Batch step {i + 1} has unsupported action '{action}'. No actions were run.";
            }
            if (!_enabledTools.Contains(dependency))
            {
                return $"Batch step {i + 1} ('{action}') requires disabled tool '{dependency}'. No actions were run.";
            }
        }

        return null;
    }
}
