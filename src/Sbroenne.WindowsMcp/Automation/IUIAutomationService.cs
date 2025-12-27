using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Service interface for Windows UI Automation operations.
/// </summary>
public interface IUIAutomationService
{
    /// <summary>
    /// Finds UI elements matching the specified query.
    /// </summary>
    /// <param name="query">The search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with found elements.</returns>
    Task<UIAutomationResult> FindElementsAsync(ElementQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the UI element tree for a window or element subtree.
    /// </summary>
    /// <param name="windowHandle">The window handle (null for foreground window).</param>
    /// <param name="parentElementId">Element ID to start tree from (takes precedence over windowHandle).</param>
    /// <param name="maxDepth">Maximum tree depth to traverse.</param>
    /// <param name="controlTypeFilter">Optional comma-separated list of control types to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with element tree.</returns>
    Task<UIAutomationResult> GetTreeAsync(nint? windowHandle, string? parentElementId, int maxDepth, string? controlTypeFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for an element matching the query to appear.
    /// </summary>
    /// <param name="query">The search criteria.</param>
    /// <param name="timeoutMs">Maximum wait time in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with the found element or timeout error.</returns>
    Task<UIAutomationResult> WaitForElementAsync(ElementQuery query, int timeoutMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrolls the parent container to make an element visible.
    /// </summary>
    /// <param name="elementId">The element ID to scroll into view.</param>
    /// <param name="query">Optional query if element ID not provided.</param>
    /// <param name="timeoutMs">Maximum scroll time in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with the element now visible.</returns>
    Task<UIAutomationResult> ScrollIntoViewAsync(string? elementId, ElementQuery? query, int timeoutMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets text content from an element.
    /// </summary>
    /// <param name="elementId">The element ID (null for window text).</param>
    /// <param name="windowHandle">Window handle for window-level text extraction.</param>
    /// <param name="includeChildren">Whether to include text from child elements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with text content.</returns>
    Task<UIAutomationResult> GetTextAsync(string? elementId, nint? windowHandle, bool includeChildren, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a UI Automation pattern on an element.
    /// </summary>
    /// <param name="elementId">The element ID.</param>
    /// <param name="pattern">The pattern to invoke (Invoke, Toggle, etc.).</param>
    /// <param name="value">Optional value for Value/RangeValue patterns.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with updated element state.</returns>
    Task<UIAutomationResult> InvokePatternAsync(string elementId, string pattern, string? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets keyboard focus to an element.
    /// </summary>
    /// <param name="elementId">The element ID to focus.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result.</returns>
    Task<UIAutomationResult> FocusElementAsync(string elementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an element and clicks it.
    /// </summary>
    /// <param name="query">The search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with clicked element.</returns>
    Task<UIAutomationResult> FindAndClickAsync(ElementQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an element and types text into it.
    /// </summary>
    /// <param name="query">The search criteria.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="clearFirst">Whether to clear existing text first.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with typed element.</returns>
    Task<UIAutomationResult> FindAndTypeAsync(ElementQuery query, string text, bool clearFirst, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a dropdown/combobox and selects an option.
    /// </summary>
    /// <param name="query">The search criteria.</param>
    /// <param name="value">The option value to select.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with selected element.</returns>
    Task<UIAutomationResult> FindAndSelectAsync(ElementQuery query, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an element ID to its current UI Automation element info.
    /// </summary>
    /// <param name="elementId">The element ID to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The element info if found, or null if stale.</returns>
    Task<UIElementInfo?> ResolveElementAsync(string elementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the UI element at the current cursor position.
    /// Equivalent to Python-UIAutomation's ControlFromCursor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with the element under the cursor.</returns>
    Task<UIAutomationResult> GetElementAtCursorAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently focused UI element.
    /// Equivalent to Python-UIAutomation's GetFocusedControl.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with the focused element.</returns>
    Task<UIAutomationResult> GetFocusedElementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ancestor elements of the specified element up to the desktop root.
    /// Equivalent to Python-UIAutomation's GetAncestorControl.
    /// </summary>
    /// <param name="elementId">The element ID to get ancestors for.</param>
    /// <param name="maxDepth">Maximum number of ancestor levels to return (null for all).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with ancestor elements (ordered from immediate parent to root).</returns>
    Task<UIAutomationResult> GetAncestorsAsync(string elementId, int? maxDepth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clicks an element directly using its element ID.
    /// </summary>
    /// <param name="elementId">The element ID to click.</param>
    /// <param name="windowHandle">Optional window handle for activation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result with clicked element.</returns>
    Task<UIAutomationResult> ClickElementAsync(string elementId, nint? windowHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Highlights an element by drawing a visible rectangle around it.
    /// The highlight persists until HideHighlightAsync is called.
    /// </summary>
    /// <param name="elementId">The element ID to highlight.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result.</returns>
    Task<UIAutomationResult> HighlightElementAsync(string elementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides the currently visible highlight rectangle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI Automation result.</returns>
    Task<UIAutomationResult> HideHighlightAsync(CancellationToken cancellationToken = default);
}
