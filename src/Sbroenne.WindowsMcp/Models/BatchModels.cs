using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// A single step in a ui_batch sequence. Deserialized from the tool's JSON <c>steps</c> array.
/// Selector fields mirror ui_find; <see cref="Action"/> selects which operation runs.
/// </summary>
public sealed class BatchStep
{
    /// <summary>Operation to run: find, click, type, select, wait, read, snapshot, or key.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>Optional per-step window handle override (decimal string). Falls back to the batch windowHandle.</summary>
    [JsonPropertyName("windowHandle")]
    public string? WindowHandle { get; set; }

    /// <summary>Stable element id from a prior step/find. Use "$prev" to reference the previous step's element.</summary>
    [JsonPropertyName("elementId")]
    public string? ElementId { get; set; }

    /// <summary>Element name (exact match, case-insensitive).</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Substring in element name (case-insensitive).</summary>
    [JsonPropertyName("nameContains")]
    public string? NameContains { get; set; }

    /// <summary>Regex pattern for element name matching.</summary>
    [JsonPropertyName("namePattern")]
    public string? NamePattern { get; set; }

    /// <summary>Control type (Button, Edit, ComboBox, etc.).</summary>
    [JsonPropertyName("controlType")]
    public string? ControlType { get; set; }

    /// <summary>AutomationId for precise matching.</summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; set; }

    /// <summary>Element class name.</summary>
    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    /// <summary>Return Nth match (1-based, default: 1).</summary>
    [JsonPropertyName("foundIndex")]
    public int FoundIndex { get; set; } = 1;

    /// <summary>Value to select (action=select).</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>Text to type (action=type).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Clear existing text before typing (action=type).</summary>
    [JsonPropertyName("clearFirst")]
    public bool ClearFirst { get; set; }

    /// <summary>Key to press (action=key), e.g. enter, tab, f5, a.</summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>Modifier keys held during the key press (action=key): ctrl, shift, alt, win (comma-separated).</summary>
    [JsonPropertyName("modifiers")]
    public string? Modifiers { get; set; }

    /// <summary>Number of times to press the key (action=key, default: 1).</summary>
    [JsonPropertyName("repeat")]
    public int Repeat { get; set; } = 1;

    /// <summary>Wait mode (action=wait): appear, disappear, or state. Default: appear.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>Desired state for wait mode=state: enabled, disabled, on, off, indeterminate, visible, offscreen.</summary>
    [JsonPropertyName("desiredState")]
    public string? DesiredState { get; set; }

    /// <summary>Timeout in milliseconds for action=wait (default: 5000).</summary>
    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>Max tree depth for action=snapshot (default: 5).</summary>
    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; } = 5;

    /// <summary>Include child text for action=read (default: false).</summary>
    [JsonPropertyName("includeChildren")]
    public bool IncludeChildren { get; set; }
}

/// <summary>Outcome of a single <see cref="BatchStep"/>.</summary>
public sealed record BatchStepResult
{
    /// <summary>Zero-based index of the step within the batch.</summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>The step's action.</summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>Whether the step succeeded.</summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>Short human-readable summary of what happened.</summary>
    [JsonPropertyName("summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; init; }

    /// <summary>Error message when the step failed.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>Element id resolved by this step (usable as "$prev" by the next step).</summary>
    [JsonPropertyName("elementId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ElementId { get; init; }

    /// <summary>Text extracted by a read step.</summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }
}

/// <summary>Aggregate result of a ui_batch call.</summary>
public sealed record BatchResult
{
    /// <summary>True when every executed step succeeded.</summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>Always "batch".</summary>
    [JsonPropertyName("action")]
    public string Action { get; init; } = "batch";

    /// <summary>Number of steps that were executed.</summary>
    [JsonPropertyName("stepsRun")]
    public required int StepsRun { get; init; }

    /// <summary>Number of executed steps that succeeded.</summary>
    [JsonPropertyName("stepsSucceeded")]
    public required int StepsSucceeded { get; init; }

    /// <summary>True when execution stopped early because a step failed and stopOnError was set.</summary>
    [JsonPropertyName("stopped")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stopped { get; init; }

    /// <summary>Per-step outcomes in execution order.</summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<BatchStepResult> Steps { get; init; }

    /// <summary>Post-batch window snapshot when withSnapshot=true.</summary>
    [JsonPropertyName("postActionTree")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree[]? PostActionTree { get; init; }

    /// <summary>Top-level error (e.g. invalid steps JSON) when the batch could not run.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}
