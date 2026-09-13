using System.Text.Json;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>Pure validation shared by inline batches and saved macros before binding erases presence.</summary>
internal static class BatchStepValidation
{
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
        }

        return null;
    }
}
