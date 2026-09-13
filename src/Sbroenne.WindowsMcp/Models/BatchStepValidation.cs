using System.Text.Json;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>Pure validation shared by inline batches and saved macros before binding erases presence.</summary>
internal static class BatchStepValidation
{
    private static readonly string[] WaitSelectorNames =
        ["name", "nameContains", "namePattern", "controlType", "automationId", "className"];

    internal static string? Validate(JsonElement steps)
    {
        if (steps.ValueKind != JsonValueKind.Array || steps.GetArrayLength() == 0)
        {
            return "steps must be a non-empty JSON array of step objects.";
        }

        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object)
            {
                return "Every batch step must be an object.";
            }

            // Match the serializer's last-property-wins behavior, including case-insensitive names.
            var action = step.EnumerateObject().LastOrDefault(property =>
                property.Name.Equals("action", StringComparison.OrdinalIgnoreCase)).Value;
            var mode = step.EnumerateObject().LastOrDefault(property =>
                property.Name.Equals("mode", StringComparison.OrdinalIgnoreCase)).Value;
            var actionText = action.ValueKind == JsonValueKind.String ? action.GetString()?.Trim().ToLowerInvariant() : null;
            var stateWait = actionText == "wait" && mode.ValueKind == JsonValueKind.String &&
                string.Equals(mode.GetString()?.Trim(), "state", StringComparison.OrdinalIgnoreCase);
            var discoveryWait = actionText == "wait" && !stateWait;

            foreach (var property in step.EnumerateObject())
            {
                var name = property.Name.ToLowerInvariant();
                if ((actionText == "find" || discoveryWait) && name == "elementid")
                {
                    return "Discovery and appear/disappear waits accept selectors, not elementId (including null).";
                }
                if (discoveryWait && name == "desiredstate")
                {
                    return "Appear/disappear waits do not accept desiredState, including null.";
                }
                if (actionText == "read" && name == "elementid" &&
                    (property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString())))
                {
                    return "A supplied read elementId must be a non-empty ID. Omit elementId only for a whole-window read.";
                }
                if ((actionText is "click" or "type" or "select" or "read" || stateWait) &&
                    name is "name" or "namecontains" or "namepattern" or "controltype" or "automationid" or
                        "classname" or "foundindex" or "scope" or "parentelementid" or "requireunique" or
                        "visibleonly" or "enabledonly")
                {
                    return "Targeted steps do not accept selectors, including null/default values. Discover first and use elementId.";
                }
            }

            var actionError = ValidateAction(step, actionText, stateWait);
            if (actionError is not null)
            {
                return actionError;
            }
        }

        return null;
    }

    private static string? ValidateAction(JsonElement step, string? action, bool stateWait)
    {
        const string Selectors = "name namecontains namepattern controltype automationid classname foundindex parentelementid scope requireunique visibleonly enabledonly";
        const string MouseFields = "mouseaction x y endx endy points direction amount modifiers button target monitorindex expectedprocessname expectedwindowtitle activate";
        var fields = action switch
        {
            "find" => Selectors,
            "click" => "elementid doubleclick",
            "type" => "elementid text clearfirst inputmode",
            "select" => "elementid value",
            "wait" => stateWait ? "mode timeoutms elementid desiredstate" : $"mode timeoutms {Selectors}",
            "read" => "elementid includechildren",
            "snapshot" => "maxdepth",
            "key" => "key modifiers repeat",
            "mouse" or "polyline" => MouseFields,
            _ => null,
        };
        if (fields is null)
        {
            return $"unknown action '{action}'. Valid: find, click, type, select, wait, read, snapshot, key, mouse, polyline.";
        }

        var allowed = ("action windowhandle " + fields).Split(' ').ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in step.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                return $"Field '{property.Name}' is not supported for batch action '{action}'.";
            }
            values[property.Name] = property.Value;
        }

        string? Text(string name) =>
            values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        if ((action is "click" or "type" or "select" || stateWait) && string.IsNullOrWhiteSpace(Text("elementId")))
        {
            return $"{action} step requires a non-empty elementId.";
        }
        if (action == "type")
        {
            if (Text("text") is null)
            {
                return "type step requires 'text'.";
            }
            if (Text("inputMode") is { } input && !string.IsNullOrWhiteSpace(input) &&
                input.Trim().ToLowerInvariant() is not ("auto" or "keyboard" or "value"))
            {
                return "type inputMode must be auto, keyboard, or value.";
            }
        }
        if (action == "select" && string.IsNullOrEmpty(Text("value")))
        {
            return "select step requires 'value'.";
        }
        if (action == "key" && string.IsNullOrWhiteSpace(Text("key")))
        {
            return "key step requires 'key'.";
        }
        if (action == "wait")
        {
            var mode = Text("mode")?.Trim().ToLowerInvariant();
            if (mode is not (null or "" or "appear" or "disappear" or "state"))
            {
                return "wait mode must be appear, disappear, or state.";
            }
            if (stateWait && Text("desiredState")?.Trim().ToLowerInvariant() is not
                ("enabled" or "disabled" or "on" or "off" or "indeterminate" or "visible" or "offscreen"))
            {
                return "wait mode=state requires desiredState: enabled, disabled, on, off, indeterminate, visible, or offscreen.";
            }
            if (!stateWait && !WaitSelectorNames
                .Any(name => !string.IsNullOrEmpty(Text(name))))
            {
                return "Appear/disappear waits require a selector.";
            }
        }
        if (action is "mouse" or "polyline")
        {
            var requested = action == "polyline" ? "polyline" : Text("mouseAction");
            if (string.IsNullOrWhiteSpace(requested))
            {
                return "mouse step requires 'mouseAction'.";
            }
            var mouseAction = requested.Trim().ToLowerInvariant().Replace("_", "", StringComparison.Ordinal);
            if (mouseAction is not ("move" or "click" or "doubleclick" or "rightclick" or "middleclick" or "drag" or "polyline" or "scroll" or "getposition"))
            {
                return $"invalid mouseAction '{requested}'. Valid: move, click, double_click, right_click, middle_click, drag, polyline, scroll, get_position.";
            }
            bool HasCoordinate(string name) => values.TryGetValue(name, out var value) &&
                value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _);
            if (mouseAction is "move" or "drag" && (!HasCoordinate("x") || !HasCoordinate("y")))
            {
                return $"{mouseAction} requires x and y coordinates.";
            }
            if (mouseAction == "drag" && (!HasCoordinate("endX") || !HasCoordinate("endY")))
            {
                return "drag requires endX and endY coordinates.";
            }
            if (mouseAction == "scroll" && Text("direction")?.Trim().ToLowerInvariant() is not ("up" or "down" or "left" or "right"))
            {
                return "scroll requires direction: up, down, left, or right.";
            }
            if (mouseAction == "polyline" &&
                (!values.TryGetValue("points", out var points) || points.ValueKind != JsonValueKind.Array ||
                points.GetArrayLength() < 2 || points.EnumerateArray().Any(point =>
                    point.ValueKind != JsonValueKind.Array || point.GetArrayLength() != 2 ||
                    point.EnumerateArray().Any(coordinate => coordinate.ValueKind != JsonValueKind.Number || !coordinate.TryGetInt32(out _)))))
            {
                return "polyline requires points with at least 2 [x,y] integer pairs.";
            }
        }

        return null;
    }
}
