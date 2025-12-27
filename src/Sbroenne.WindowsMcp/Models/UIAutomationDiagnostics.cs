namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Diagnostic information for UI Automation operations.
/// </summary>
public sealed record UIAutomationDiagnostics
{
    /// <summary>
    /// Operation duration in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Window that was searched.
    /// </summary>
    public string? WindowTitle { get; init; }

    /// <summary>
    /// Window handle.
    /// </summary>
    public string? WindowHandle { get; init; }

    /// <summary>
    /// Query that was used.
    /// </summary>
    public ElementQuery? Query { get; init; }

    /// <summary>
    /// Number of elements scanned.
    /// </summary>
    public int? ElementsScanned { get; init; }

    /// <summary>
    /// Elapsed time before timeout (for wait_for).
    /// </summary>
    public long? ElapsedBeforeTimeout { get; init; }

    /// <summary>
    /// Multiple matches when exactly one expected.
    /// </summary>
    public UIElementInfo[]? MultipleMatches { get; init; }

    /// <summary>
    /// Warnings about potential issues (e.g., Chromium app without accessibility flag).
    /// </summary>
    public string[]? Warnings { get; init; }

    /// <summary>
    /// Detected UI framework of the target window (e.g., "Win32", "WPF", "WinForms", "Chromium/Electron", "Qt").
    /// </summary>
    public string? DetectedFramework { get; init; }
}

/// <summary>
/// Represents search criteria for finding UI elements.
/// </summary>
public sealed record ElementQuery
{
    /// <summary>
    /// Element name to search for (exact match, case-insensitive).
    /// For partial matching, use <see cref="NameContains"/> instead.
    /// For regex matching, use <see cref="NamePattern"/> instead.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Substring to search for in element names (case-insensitive).
    /// Returns elements whose Name contains this string.
    /// Cannot be combined with <see cref="Name"/> or <see cref="NamePattern"/>.
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// Regex pattern to match element names.
    /// Returns elements whose Name matches this regex pattern.
    /// Cannot be combined with <see cref="Name"/> or <see cref="NameContains"/>.
    /// </summary>
    public string? NamePattern { get; init; }

    /// <summary>
    /// Control type filter (Button, Edit, Text, List, MenuItem, etc.).
    /// </summary>
    public string? ControlType { get; init; }

    /// <summary>
    /// Automation ID for precise matching.
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Class name filter for the element (e.g., 'Chrome_WidgetWin_1' for Chromium apps).
    /// </summary>
    public string? ClassName { get; init; }

    /// <summary>
    /// Parent element ID to search within.
    /// </summary>
    public string? ParentElementId { get; init; }

    /// <summary>
    /// Window handle to search within.
    /// </summary>
    public string? WindowHandle { get; init; }

    /// <summary>
    /// Maximum depth to search (0 = immediate children only, null = unlimited).
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// Exact depth to search at (only search at this specific depth from the root).
    /// When set, elements at other depths are skipped.
    /// </summary>
    public int? ExactDepth { get; init; }

    /// <summary>
    /// Which matching element to return when multiple match (1-based index, default 1 = first match).
    /// For example, FoundIndex=2 returns the 2nd matching element.
    /// Inspired by Python-UIAutomation's foundIndex parameter.
    /// </summary>
    public int FoundIndex { get; init; } = 1;

    /// <summary>
    /// Whether to include children in results.
    /// </summary>
    public bool IncludeChildren { get; init; }

    /// <summary>
    /// Timeout in milliseconds for implicit wait (0 = no wait).
    /// </summary>
    public int TimeoutMs { get; init; }

    /// <summary>
    /// Sort results by element prominence (bounding box area, largest first).
    /// Useful for disambiguation when multiple elements match - larger elements are typically more prominent/important.
    /// </summary>
    public bool SortByProminence { get; init; }
}
