using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result from a UI Automation tool operation.
/// </summary>
public sealed record UIAutomationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Action that was performed.
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>True when a click was sent successfully, not proof of an application outcome.</summary>
    [JsonPropertyName("actionDispatched")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ActionDispatched { get; init; }

    /// <summary>Clicks do not verify application outcomes, even when a control state changed.</summary>
    [JsonPropertyName("outcomeVerified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OutcomeVerified { get; init; }

    /// <summary>The click target as observed before sending the action.</summary>
    [JsonPropertyName("target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIActionElement? Target { get; init; }

    /// <summary>Whether the same target's post-action properties were available or unavailable.</summary>
    [JsonPropertyName("postActionState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostActionState { get; init; }

    /// <summary>An immediate observation after dispatch, not an expected-state check.</summary>
    [JsonPropertyName("postActionElement")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIActionElement? PostActionElement { get; init; }

    /// <summary>
    /// Full element details. Only populated for single-element results or get_element_details.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementInfo[]? Elements { get; init; }

    /// <summary>
    /// Compact element list for Find actions (token-optimized, flat list).
    /// Use elementId from this list with get_element_details to fetch full info.
    /// Format: id, name, type, click:[x,y,monitor], enabled.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompact[]? Items { get; init; }

    /// <summary>
    /// Compact tree structure for GetTree actions (token-optimized, with hierarchy).
    /// Use elementId from this list with get_element_details to fetch full info.
    /// Format: id, name, type, click:[x,y,monitor], enabled, children.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree[]? Tree { get; init; }

    /// <summary>
    /// Internal full-fidelity tree used by services that need properties omitted from compact responses.
    /// This value is never serialized to an MCP client.
    /// </summary>
    [JsonIgnore]
    public UIElementInfo[]? FullTree { get; init; }

    /// <summary>
    /// Snapshot response form: full for a complete tree or diff for changes from the remembered tree.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; init; }

    /// <summary>Opaque token for the latest automatic snapshot of this target and capture settings.</summary>
    [JsonPropertyName("snapshotToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SnapshotToken { get; init; }

    /// <summary>Token of the tree to which this diff applies.</summary>
    [JsonPropertyName("baseSnapshotToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseSnapshotToken { get; init; }

    /// <summary>
    /// Changes from the previous remembered tree when <see cref="Kind"/> is diff.
    /// </summary>
    [JsonPropertyName("changes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SnapshotChange[]? Changes { get; init; }

    /// <summary>
    /// Post-action window snapshot ("perceive/act fusion"). When an interactive tool is called
    /// with withSnapshot=true, this carries the window's element tree captured immediately after
    /// the action succeeded, so agents can verify the new state without a separate ui_snapshot call.
    /// Same shape as <see cref="Tree"/>: id, name, type, click:[x,y,monitor], enabled, children.
    /// </summary>
    [JsonPropertyName("postActionTree")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree[]? PostActionTree { get; init; }

    /// <summary>Post-action snapshot form: full or diff.</summary>
    [JsonPropertyName("postActionKind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostActionKind { get; init; }

    /// <summary>Token returned by the optional post-action snapshot.</summary>
    [JsonPropertyName("postActionSnapshotToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostActionSnapshotToken { get; init; }

    /// <summary>Baseline token for an optional post-action diff.</summary>
    [JsonPropertyName("postActionBaseSnapshotToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostActionBaseSnapshotToken { get; init; }

    /// <summary>Post-action changes when <see cref="PostActionKind"/> is diff.</summary>
    [JsonPropertyName("postActionChanges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SnapshotChange[]? PostActionChanges { get; init; }

    /// <summary>Warning when an optional post-action snapshot could not be captured.</summary>
    [JsonPropertyName("postActionWarning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostActionWarning { get; init; }

    /// <summary>
    /// Number of elements found.
    /// </summary>
    [JsonPropertyName("elementCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ElementCount { get; init; }

    /// <summary>
    /// Usage hint for LLM agents when elements are found.
    /// Provides guidance on how to interact with the found element(s).
    /// </summary>
    [JsonPropertyName("hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UsageHint { get; init; }

    /// <summary>
    /// Text content (for get_text action).
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// Structured tabular data (for read_table action). Rows/columns extracted from a
    /// grid/table/list control via the UIA Grid and Table patterns.
    /// </summary>
    [JsonPropertyName("table")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UITableData? Table { get; init; }

    /// <summary>
    /// Error type if failed.
    /// </summary>
    [JsonPropertyName("errorType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorType { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Suggested recovery action for LLM agents when the operation fails.
    /// Provides actionable guidance on what to try next.
    /// </summary>
    [JsonPropertyName("recoverySuggestion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RecoverySuggestion { get; init; }

    /// <summary>
    /// Diagnostic info for debugging.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIAutomationDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Information about the window that was the target of the action.
    /// Helps LLM agents verify the action was performed on the correct window.
    /// </summary>
    [JsonPropertyName("targetWindow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TargetWindowInfo? TargetWindow { get; init; }

    /// <summary>
    /// Base64-encoded annotated screenshot image data (for capture_annotated action).
    /// </summary>
    [JsonPropertyName("annotatedImageData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnnotatedImageData { get; init; }

    /// <summary>
    /// Format of the annotated image (jpeg or png).
    /// </summary>
    [JsonPropertyName("annotatedImageFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnnotatedImageFormat { get; init; }

    /// <summary>
    /// Width of the annotated image in pixels.
    /// </summary>
    [JsonPropertyName("annotatedImageWidth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AnnotatedImageWidth { get; init; }

    /// <summary>
    /// Height of the annotated image in pixels.
    /// </summary>
    [JsonPropertyName("annotatedImageHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AnnotatedImageHeight { get; init; }

    /// <summary>
    /// Array of annotated elements with their numbered indices matching the labels on the screenshot.
    /// Use these to reference elements by number in subsequent operations.
    /// </summary>
    [JsonPropertyName("annotatedElements")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AnnotatedElement[]? AnnotatedElements { get; init; }

    /// <summary>
    /// Creates a success result with a single element (wrapped in an array for consistency).
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="element">The element found.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result.</returns>
    public static UIAutomationResult CreateSuccess(string action, UIElementInfo element, UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(element);

        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Elements = [element],
            ElementCount = 1,
            UsageHint = GetUsageHintForElement(element),
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a success result without elements (for actions like hide_highlight).
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result.</returns>
    public static UIAutomationResult CreateSuccess(string action, UIAutomationDiagnostics? diagnostics = null)
    {
        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a success result with a hint message but no elements.
    /// Use this when an action succeeded but no element data should be returned (e.g., click that closed a window).
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="hint">A hint message explaining the outcome.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result with a hint.</returns>
    public static UIAutomationResult CreateSuccessWithHint(string action, string hint, UIAutomationDiagnostics? diagnostics = null)
    {
        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            UsageHint = hint,
            Diagnostics = diagnostics
        };
    }

    /// <summary>Reports click dispatch separately from its unverified application outcome.</summary>
    public static UIAutomationResult CreateClickDispatched(
        string action,
        UIActionElement target,
        UIActionElement? postActionElement,
        UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            ActionDispatched = true,
            OutcomeVerified = false,
            Target = target,
            PostActionState = postActionElement is null ? "unavailable" : "available",
            PostActionElement = postActionElement,
            UsageHint = "Action dispatched; application outcome not verified. " +
                (postActionElement is null ? "Post-action target unavailable (for example, a closed dialog). " : "") +
                "Inspect a snapshot or use ui_wait for the expected state; do not replay automatically.",
            Diagnostics = diagnostics,
        };
    }

    /// <summary>
    /// Creates a success result with multiple elements.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="elements">The elements found.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result.</returns>
    public static UIAutomationResult CreateSuccess(string action, UIElementInfo[] elements, UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(elements);

        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Elements = elements,
            ElementCount = elements.Length,
            UsageHint = elements.Length == 1 ? GetUsageHintForElement(elements[0]) : "Multiple elements found. Refine your query or iterate through the elements array.",
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a success result with compact elements (token-optimized for lists).
    /// Use this for Find actions to reduce response token count by ~70%.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="elements">The full elements (will be converted to compact).</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result with compact element list (Items).</returns>
    public static UIAutomationResult CreateSuccessCompact(string action, UIElementInfo[] elements, UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var compactElements = elements.Select(UIElementCompact.FromFull).ToArray();

        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Items = compactElements,
            ElementCount = elements.Length,
            UsageHint = GetActionHint(action, elements),
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Gets an action-specific hint that reinforces tool usage for subsequent operations.
    /// </summary>
    private static string GetActionHint(string action, UIElementInfo[] elements)
    {
        // For find actions, provide element usage guidance
        if (elements.Length == 1)
        {
            return "Use the returned id as elementId with ui_click, ui_type, or ui_select.";
        }

        return $"Found {elements.Length} elements. Choose the intended control and pass its id as elementId to the action.";
    }

    /// <summary>
    /// Creates a success result with compact tree structure (token-optimized for hierarchical views).
    /// Use this for GetTree actions to reduce response token count while preserving hierarchy.
    /// Also retains a non-serialized full tree for internal services like AnnotatedScreenshotService.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="elements">The full elements with children (will be converted to compact tree).</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result with compact tree structure and a non-serialized internal full tree.</returns>
    public static UIAutomationResult CreateSuccessCompactTree(string action, UIElementInfo[] elements, UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var compactTree = elements.Select(UIElementCompactTree.FromFull).ToArray();
        var totalCount = CountTreeElements(elements);

        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Tree = compactTree,
            FullTree = elements,
            Kind = "full",
            ElementCount = totalCount,
            UsageHint = $"Tree contains {totalCount} elements. Pass a control's id as elementId to ui_click/ui_type/ui_read. Rediscover if an ID is stale; never substitute a same-name control automatically.",
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Counts total elements in a tree structure (including nested children).
    /// </summary>
    private static int CountTreeElements(UIElementInfo[]? elements)
    {
        if (elements == null || elements.Length == 0)
        {
            return 0;
        }

        return elements.Length + elements.Sum(e => CountTreeElements(e.Children));
    }

    /// <summary>
    /// Creates a success result with text content.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="text">The text content.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result.</returns>
    public static UIAutomationResult CreateSuccessWithText(string action, string text, UIAutomationDiagnostics? diagnostics = null)
    {
        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Text = text,
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a success result with structured table data.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="table">The extracted tabular data.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result carrying the table payload.</returns>
    public static UIAutomationResult CreateSuccessWithTable(string action, UITableData table, UIAutomationDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        var hint = table.Truncated == true
            ? $"Extracted {table.Rows.Length} of {table.RowCount} rows x {table.ColumnCount} columns (truncated). Increase maxRows/maxColumns to read the rest."
            : $"Extracted {table.Rows.Length} rows x {table.ColumnCount} columns.";

        return new UIAutomationResult
        {
            Success = true,
            Action = action,
            Table = table,
            UsageHint = hint,
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="action">The action attempted.</param>
    /// <param name="errorType">The error type code.</param>
    /// <param name="errorMessage">A descriptive error message.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <param name="recoverySuggestion">Optional recovery suggestion for LLM agents.</param>
    /// <returns>A failure result.</returns>
    public static UIAutomationResult CreateFailure(string action, string errorType, string errorMessage, UIAutomationDiagnostics? diagnostics = null, string? recoverySuggestion = null)
    {
        // If no recovery suggestion provided, generate one based on error type
        var suggestion = recoverySuggestion ?? GetDefaultRecoverySuggestion(errorType);

        return new UIAutomationResult
        {
            Success = false,
            Action = action,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            RecoverySuggestion = suggestion,
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Gets a default recovery suggestion based on error type.
    /// </summary>
    private static string GetDefaultRecoverySuggestion(string errorType) => errorType switch
    {
        UIAutomationErrorType.ElementNotFound =>
            "Element not found. Run ui_snapshot or ui_find in the same CLI daemon or MCP connection, then act with a newly returned element ID.",

        UIAutomationErrorType.SearchIncomplete =>
            "Search stopped at its scan limit; the target may still exist. Narrow the search with exact name, " +
            "automationId, controlType or className, or parentElementId within the same MCP session or CLI daemon. " +
            "MCP sessions and the CLI daemon are separate owners; their element IDs are not interchangeable. " +
            "A longer timeout does not increase the scan limit.",

        UIAutomationErrorType.MultipleMatches =>
            "Multiple elements matched. Add automationId, scope with ui_snapshot parentElementId, or specify foundIndex to select which match.",

        UIAutomationErrorType.PatternNotSupported =>
            "This element doesn't support the requested pattern. Use clickablePoint with mouse_control instead.",

        UIAutomationErrorType.ElementStale =>
            "Element reference expired or belongs to another owner. Rediscover with ui_find or ui_snapshot and use the new ID; do not reuse IDs after a restart.",

        UIAutomationErrorType.ElevatedTarget =>
            "Target window runs as Administrator. Run MCP server elevated or target a non-admin window.",

        UIAutomationErrorType.WindowNotFound =>
            "Window not found. Verify the application is running.",

        UIAutomationErrorType.Timeout =>
            "Operation timed out. Increase timeoutMs or verify the expected UI state.",

        UIAutomationErrorType.WrongTargetWindow =>
            "Wrong window has focus. Use window_management(action='activate', handle='...') to focus the correct window first.",

        UIAutomationErrorType.InvalidParameter =>
            "Invalid parameter value. Check the parameter requirements.",

        UIAutomationErrorType.PathError =>
            "File path error. The directory does not exist. Create the directory first or use an existing path.",

        _ => string.Empty
    };

    /// <summary>
    /// Gets a usage hint for an element based on its properties.
    /// </summary>
    private static string GetUsageHintForElement(UIElementInfo element)
    {
        var hints = new List<string>();

        // Check for invokable patterns
        var patterns = element.SupportedPatterns ?? Array.Empty<string>();
        var hasToggle = patterns.Any(p => p.Contains("Toggle", StringComparison.OrdinalIgnoreCase));
        var hasValue = patterns.Any(p => p.Contains("Value", StringComparison.OrdinalIgnoreCase));

        var target = $"elementId='{element.ElementId}'";
        hints.Add($"To click: ui_click(windowHandle='...', {target})");

        if (hasToggle)
        {
            hints.Add($"To toggle: ui_click(windowHandle='...', {target})");
        }

        if (hasValue)
        {
            hints.Add($"To type text: ui_type(windowHandle='...', {target}, text='...')");
        }

        return string.Join(" | ", hints);
    }
}
