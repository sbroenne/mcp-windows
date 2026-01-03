using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result from a UI Automation tool operation.
/// </summary>
/// <remarks>
/// Property names are intentionally short to minimize JSON token count:
/// - ok: Success
/// - a: Action
/// - Elements: Full element details (internal)
/// - Items: Compact element list
/// - Tree: Compact tree structure
/// - n: Element count
/// - hint: Usage hint
/// - txt: Text content
/// - et: Error type
/// - err: Error message
/// - fix: Recovery suggestion
/// - diag: Diagnostics
/// - tw: Target window
/// - img: Annotated image data
/// - fmt: Image format
/// - w: Image width
/// - h: Image height
/// - ae: Annotated elements
/// </remarks>
public sealed record UIAutomationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("ok")]
    public required bool Success { get; init; }

    /// <summary>
    /// Action that was performed.
    /// </summary>
    [JsonPropertyName("a")]
    public required string Action { get; init; }

    /// <summary>
    /// Full element details. Only populated for single-element results or get_element_details.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementInfo[]? Elements { get; init; }

    /// <summary>
    /// Compact element list for Find actions (token-optimized, flat list).
    /// Use elementId from this list with get_element_details to fetch full info.
    /// Format: id, n(name), t(type), c(click:[x,y,monitor]), e(enabled).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompact[]? Items { get; init; }

    /// <summary>
    /// Compact tree structure for GetTree actions (token-optimized, with hierarchy).
    /// Use elementId from this list with get_element_details to fetch full info.
    /// Format: id, n(name), t(type), c(click:[x,y,monitor]), e(enabled), ch(children).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIElementCompactTree[]? Tree { get; init; }

    /// <summary>
    /// Number of elements found.
    /// </summary>
    [JsonPropertyName("n")]
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
    [JsonPropertyName("txt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// Error type if failed.
    /// </summary>
    [JsonPropertyName("et")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorType { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("err")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Suggested recovery action for LLM agents when the operation fails.
    /// Provides actionable guidance on what to try next.
    /// </summary>
    [JsonPropertyName("fix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RecoverySuggestion { get; init; }

    /// <summary>
    /// Diagnostic info for debugging.
    /// </summary>
    [JsonPropertyName("diag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UIAutomationDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Information about the window that was the target of the action.
    /// Helps LLM agents verify the action was performed on the correct window.
    /// </summary>
    [JsonPropertyName("tw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TargetWindowInfo? TargetWindow { get; init; }

    /// <summary>
    /// Base64-encoded annotated screenshot image data (for capture_annotated action).
    /// </summary>
    [JsonPropertyName("img")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnnotatedImageData { get; init; }

    /// <summary>
    /// Format of the annotated image (jpeg or png).
    /// </summary>
    [JsonPropertyName("fmt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AnnotatedImageFormat { get; init; }

    /// <summary>
    /// Width of the annotated image in pixels.
    /// </summary>
    [JsonPropertyName("w")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AnnotatedImageWidth { get; init; }

    /// <summary>
    /// Height of the annotated image in pixels.
    /// </summary>
    [JsonPropertyName("h")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AnnotatedImageHeight { get; init; }

    /// <summary>
    /// Array of annotated elements with their numbered indices matching the labels on the screenshot.
    /// Use these to reference elements by number in subsequent operations.
    /// </summary>
    [JsonPropertyName("ae")]
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
            UsageHint = elements.Length == 1
                ? $"Use elementId='{elements[0].ElementId}' for subsequent actions. For full details: ui_automation(action='get_element_details', elementId='...')"
                : $"Found {elements.Length} elements. Use elementId from Items for actions. For full details: ui_automation(action='get_element_details', elementId='...')",
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Creates a success result with compact tree structure (token-optimized for hierarchical views).
    /// Use this for GetTree actions to reduce response token count while preserving hierarchy.
    /// Also populates Elements for internal use by services like AnnotatedScreenshotService.
    /// </summary>
    /// <param name="action">The action performed.</param>
    /// <param name="elements">The full elements with children (will be converted to compact tree).</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A success result with compact tree structure (Tree) and full Elements for internal use.</returns>
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
            Elements = elements, // Keep full elements for internal use (e.g., AnnotatedScreenshotService)
            ElementCount = totalCount,
            UsageHint = $"Tree contains {totalCount} elements. Use elementId from Tree nodes for actions. For full details: ui_automation(action='get_element_details', elementId='...')",
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
            "Element not found. Try get_tree (default depth=2) to explore, or use parentElementId to drill into a specific subtree.",

        UIAutomationErrorType.MultipleMatches =>
            "Multiple elements matched. Add automationId, use parentElementId to scope search, or specify foundIndex to select which match.",

        UIAutomationErrorType.PatternNotSupported =>
            "This element doesn't support the requested pattern. Use clickablePoint with mouse_control instead.",

        UIAutomationErrorType.ElementStale =>
            "Element reference expired. Use find action to get a fresh elementId.",

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
        var hasInvoke = patterns.Any(p => p.Contains("Invoke", StringComparison.OrdinalIgnoreCase));
        var hasToggle = patterns.Any(p => p.Contains("Toggle", StringComparison.OrdinalIgnoreCase));
        var hasValue = patterns.Any(p => p.Contains("Value", StringComparison.OrdinalIgnoreCase));

        // Primary recommendation: use clickablePoint for direct interaction
        var cp = element.ClickablePoint;
        hints.Add($"To click: mouse_control(action='click', x={cp.X}, y={cp.Y}, monitorIndex={cp.MonitorIndex})");

        // Add pattern-specific hints
        if (hasInvoke)
        {
            hints.Add($"Or use: ui_automation(action='invoke', elementId='{element.ElementId}', value='Invoke')");
        }

        if (hasToggle)
        {
            hints.Add($"To toggle: ui_automation(action='toggle', elementId='{element.ElementId}')");
        }

        if (hasValue)
        {
            hints.Add($"To type text: ui_automation(action='type', elementId='{element.ElementId}', text='...')");
        }

        return string.Join(" | ", hints);
    }
}
