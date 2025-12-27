using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Window;

/// <summary>
/// Interface for the main window management service.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Lists all visible top-level windows.
    /// </summary>
    /// <param name="filter">Optional filter for title or process name.</param>
    /// <param name="useRegex">Whether to use regex matching.</param>
    /// <param name="includeAllDesktops">Whether to include windows on other virtual desktops.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing list of windows or error.</returns>
    Task<WindowManagementResult> ListWindowsAsync(
        string? filter = null,
        bool useRegex = false,
        bool includeAllDesktops = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds windows matching the specified title.
    /// </summary>
    /// <param name="title">Title to search for.</param>
    /// <param name="useRegex">Whether to use regex matching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing matching windows or error.</returns>
    Task<WindowManagementResult> FindWindowAsync(
        string title,
        bool useRegex = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a window and brings it to the foreground.
    /// </summary>
    /// <param name="handle">Window handle to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with activated window info or error.</returns>
    Task<WindowManagementResult> ActivateWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the current foreground window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with foreground window info or error.</returns>
    Task<WindowManagementResult> GetForegroundWindowAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Minimizes a window to the taskbar.
    /// </summary>
    /// <param name="handle">Window handle to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after minimization or error.</returns>
    Task<WindowManagementResult> MinimizeWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Maximizes a window to fill the screen.
    /// </summary>
    /// <param name="handle">Window handle to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after maximization or error.</returns>
    Task<WindowManagementResult> MaximizeWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a window to its normal size.
    /// </summary>
    /// <param name="handle">Window handle to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after restoration or error.</returns>
    Task<WindowManagementResult> RestoreWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a window to close (sends WM_CLOSE).
    /// </summary>
    /// <param name="handle">Window handle to close.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<WindowManagementResult> CloseWindowAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a window to the specified position.
    /// </summary>
    /// <param name="handle">Window handle to move.</param>
    /// <param name="x">New x-coordinate.</param>
    /// <param name="y">New y-coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after move or error.</returns>
    Task<WindowManagementResult> MoveWindowAsync(
        nint handle,
        int x,
        int y,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes a window to the specified dimensions.
    /// </summary>
    /// <param name="handle">Window handle to resize.</param>
    /// <param name="width">New width.</param>
    /// <param name="height">New height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after resize or error.</returns>
    Task<WindowManagementResult> ResizeWindowAsync(
        nint handle,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the window bounds (position and size) atomically.
    /// </summary>
    /// <param name="handle">Window handle to modify.</param>
    /// <param name="bounds">New bounds for the window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after update or error.</returns>
    Task<WindowManagementResult> SetBoundsAsync(
        nint handle,
        WindowBounds bounds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a window with the specified title to appear.
    /// </summary>
    /// <param name="title">Title to search for.</param>
    /// <param name="useRegex">Whether to use regex matching.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info when found or timeout error.</returns>
    Task<WindowManagementResult> WaitForWindowAsync(
        string title,
        bool useRegex = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a window to a specific monitor by index.
    /// </summary>
    /// <param name="handle">Window handle to move.</param>
    /// <param name="monitorIndex">Target monitor index (0-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info after move or error.</returns>
    Task<WindowManagementResult> MoveToMonitorAsync(
        nint handle,
        int monitorIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a window by its handle.
    /// </summary>
    /// <param name="handle">Window handle to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info including state or error.</returns>
    Task<WindowManagementResult> GetWindowStateAsync(
        nint handle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a window to reach a specific state.
    /// </summary>
    /// <param name="handle">Window handle to monitor.</param>
    /// <param name="targetState">The state to wait for.</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with window info when state is reached or timeout error.</returns>
    Task<WindowManagementResult> WaitForStateAsync(
        nint handle,
        WindowState targetState,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default);
}
