using Microsoft.Extensions.Logging;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Logging methods for UI Automation service.
/// </summary>
public sealed partial class UIAutomationService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Search {Action}: scanned {ElementsScanned} elements in {DurationMs}ms, found {ResultCount} matches")]
    private static partial void LogSearchPerformance(ILogger logger, string action, int elementsScanned, long durationMs, int resultCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Tree traversal truncated at {ElementsScanned} elements (max: {MaxElements})")]
    private static partial void LogTreeTruncated(ILogger logger, int elementsScanned, int maxElements);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Target at ({X}, {Y}) is elevated, current process is not")]
    private static partial void LogElevatedTargetWarning(ILogger logger, int x, int y);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in FindElementsAsync")]
    private static partial void LogFindElementsError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in GetTreeAsync for window handle {WindowHandle}")]
    private static partial void LogGetTreeError(ILogger logger, string? windowHandle, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in InvokePatternAsync for pattern {Pattern} on element {ElementId}")]
    private static partial void LogInvokePatternError(ILogger logger, string pattern, string elementId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in ScrollIntoViewAsync for element {ElementId}")]
    private static partial void LogScrollIntoViewError(ILogger logger, string? elementId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in FocusElementAsync for element {ElementId}")]
    private static partial void LogFocusElementError(ILogger logger, string elementId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in GetTextAsync for element {ElementId}")]
    private static partial void LogGetTextError(ILogger logger, string? elementId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error clicking element {ElementName}")]
    private static partial void LogFindAndClickError(ILogger logger, string elementName, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error typing into element {ElementName}")]
    private static partial void LogFindAndTypeError(ILogger logger, string elementName, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error selecting value {Value} in element {ElementName}")]
    private static partial void LogFindAndSelectError(ILogger logger, string elementName, string value, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error getting focused element")]
    private static partial void LogGetFocusedElementError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error getting element at cursor")]
    private static partial void LogGetElementAtCursorError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error getting ancestors for element {ElementId}")]
    private static partial void LogGetAncestorsError(ILogger logger, string elementId, Exception ex);
}
