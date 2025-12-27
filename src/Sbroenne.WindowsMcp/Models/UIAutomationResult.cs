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
            "Try: 1) Use get_tree action to explore the UI hierarchy. 2) Verify the window is visible and not minimized. 3) For Electron apps (VS Code, Teams), use window_management to activate the window first. 4) Check if element name/type is correct using screenshot_control.",

        UIAutomationErrorType.MultipleMatches =>
            "Try: 1) Add automationId if available (more precise). 2) Use parentElementId to scope the search to a specific container. 3) Add more filters like controlType. 4) Use get_tree to examine the element hierarchy.",

        UIAutomationErrorType.PatternNotSupported =>
            "Try: 1) Use 'find' action to get clickablePoint, then use mouse_control with those coordinates. 2) Check element's supportedPatterns array for available patterns.",

        UIAutomationErrorType.ElementStale =>
            "The element reference expired. Try: 1) Use 'find' action to get a fresh elementId. 2) Element may have been removed from UI - verify with get_tree.",

        UIAutomationErrorType.ElevatedTarget =>
            "Target window runs as Administrator. Try: 1) Run MCP server with elevated privileges. 2) Target a different non-elevated window.",

        UIAutomationErrorType.WindowNotFound =>
            "Try: 1) Use window_management action='list' to find available windows. 2) Window may have closed - check if application is still running.",

        UIAutomationErrorType.Timeout =>
            "Try: 1) Increase timeoutMs parameter. 2) Verify the expected UI change is actually happening. 3) Use screenshot_control to check current UI state.",

        UIAutomationErrorType.WrongTargetWindow =>
            "A different window has focus. Try: 1) Use ui_automation with 'targetWindowHandle' and 'activateFirst=true' to auto-activate the correct window. 2) Or use window_management(action='activate', handle=<handle>) first. 3) Verify expectedWindowTitle/expectedProcessName values are correct.",

        UIAutomationErrorType.InvalidParameter =>
            "Check parameter values. Try: 1) Review parameter requirements for this action. 2) Ensure required parameters like 'text' for type action are provided.",

        _ => "Check the error message and try the operation again with corrected parameters."
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
