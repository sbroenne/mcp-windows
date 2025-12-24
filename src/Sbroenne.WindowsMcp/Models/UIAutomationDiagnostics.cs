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
    public nint? WindowHandle { get; init; }

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
}

/// <summary>
/// Represents search criteria for finding UI elements.
/// </summary>
public sealed record ElementQuery
{
    /// <summary>
    /// Element name to search for (partial match supported).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Control type filter (Button, Edit, Text, List, MenuItem, etc.).
    /// </summary>
    public string? ControlType { get; init; }

    /// <summary>
    /// Automation ID for precise matching.
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Parent element ID to search within.
    /// </summary>
    public string? ParentElementId { get; init; }

    /// <summary>
    /// Window handle to search within.
    /// </summary>
    public nint? WindowHandle { get; init; }

    /// <summary>
    /// Maximum depth to search (0 = immediate children only, null = unlimited).
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// Whether to include children in results.
    /// </summary>
    public bool IncludeChildren { get; init; }

    /// <summary>
    /// Timeout in milliseconds for implicit wait (0 = no wait).
    /// </summary>
    public int TimeoutMs { get; init; }
}
