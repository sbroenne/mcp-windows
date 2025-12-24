namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Error types for UI Automation operations.
/// </summary>
public static class UIAutomationErrorType
{
    /// <summary>No element matching the query was found.</summary>
    public const string ElementNotFound = "element_not_found";

    /// <summary>The wait_for operation exceeded its timeout.</summary>
    public const string Timeout = "timeout";

    /// <summary>Multiple elements matched when exactly one was expected.</summary>
    public const string MultipleMatches = "multiple_matches";

    /// <summary>The requested pattern is not supported by the element.</summary>
    public const string PatternNotSupported = "pattern_not_supported";

    /// <summary>The cached element reference is no longer valid.</summary>
    public const string ElementStale = "element_stale";

    /// <summary>The target window is running with elevated privileges.</summary>
    public const string ElevatedTarget = "elevated_target";

    /// <summary>An invalid parameter was provided.</summary>
    public const string InvalidParameter = "invalid_parameter";

    /// <summary>Scrolled through entire list without finding element.</summary>
    public const string ScrollExhausted = "scroll_exhausted";

    /// <summary>The specified window handle is not valid.</summary>
    public const string WindowNotFound = "window_not_found";

    /// <summary>An internal error occurred.</summary>
    public const string InternalError = "internal_error";

    /// <summary>OCR found no text in the specified region.</summary>
    public const string NoTextFound = "no_text_found";

    /// <summary>The OCR region is outside screen bounds.</summary>
    public const string InvalidRegion = "invalid_region";

    /// <summary>The requested OCR language is not available.</summary>
    public const string LanguageNotSupported = "language_not_supported";

    /// <summary>The foreground window does not match the expected target window.</summary>
    public const string WrongTargetWindow = "wrong_target_window";
}
