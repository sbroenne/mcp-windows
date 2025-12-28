namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result from a UI Automation tool operation.
/// </summary>
public sealed record UIAutomationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Action that was performed.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Element results (single or multiple). Always use this property to access found elements.
    /// For single-element results, use Elements[0]. For multiple matches, iterate the array.
    /// </summary>
    public UIElementInfo[]? Elements { get; init; }

    /// <summary>
    /// Number of elements found.
    /// </summary>
    public int? ElementCount { get; init; }

    /// <summary>
    /// Usage hint for LLM agents when elements are found.
    /// Provides guidance on how to interact with the found element(s).
    /// </summary>
    public string? UsageHint { get; init; }

    /// <summary>
    /// Text content (for get_text action).
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Error type if failed.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Suggested recovery action for LLM agents when the operation fails.
    /// Provides actionable guidance on what to try next.
    /// </summary>
    public string? RecoverySuggestion { get; init; }

    /// <summary>
    /// Diagnostic info for debugging.
    /// </summary>
    public UIAutomationDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Information about the window that was the target of the action.
    /// Helps LLM agents verify the action was performed on the correct window.
    /// </summary>
    public TargetWindowInfo? TargetWindow { get; init; }

    /// <summary>
    /// Base64-encoded annotated screenshot image data (for capture_annotated action).
    /// </summary>
    public string? AnnotatedImageData { get; init; }

    /// <summary>
    /// Format of the annotated image (jpeg or png).
    /// </summary>
    public string? AnnotatedImageFormat { get; init; }

    /// <summary>
    /// Width of the annotated image in pixels.
    /// </summary>
    public int? AnnotatedImageWidth { get; init; }

    /// <summary>
    /// Height of the annotated image in pixels.
    /// </summary>
    public int? AnnotatedImageHeight { get; init; }

    /// <summary>
    /// Array of annotated elements with their numbered indices matching the labels on the screenshot.
    /// Use these to reference elements by number in subsequent operations.
    /// </summary>
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
            "Element not found after auto-retry with partial matching. Use get_tree to explore available elements, or verify the window is visible.",

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
            "Wrong window has focus. Use app='...' to target the correct window.",

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
