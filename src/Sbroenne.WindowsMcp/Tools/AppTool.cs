using System.Diagnostics;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for launching applications on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class AppTool
{
    private readonly WindowService _windowService;
    private readonly WindowOperationLogger? _logger;
    private readonly WindowConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppTool"/> class.
    /// </summary>
    /// <param name="windowService">The window service.</param>
    /// <param name="configuration">The window configuration.</param>
    /// <param name="logger">Optional operation logger.</param>
    public AppTool(
        WindowService windowService,
        WindowConfiguration configuration,
        WindowOperationLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(configuration);

        _windowService = windowService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Launch applications. Use this to start programs like notepad.exe, calc.exe, chrome.exe, winword.exe, excel.exe, etc.
    /// Returns a window handle for use with window_management, keyboard_control, and other tools.
    /// </summary>
    /// <remarks>
    /// Examples: app(programPath='notepad.exe'), app(programPath='chrome.exe', arguments='https://example.com').
    /// After launch, the window is focused and ready for input. Use the returned handle for subsequent operations.
    /// </remarks>
    /// <param name="context">The MCP request context for logging and server access.</param>
    /// <param name="programPath">Program to launch. Can be executable name (e.g., 'notepad.exe', 'calc.exe', 'chrome.exe') or full path (e.g., 'C:\\Program Files\\App\\app.exe').</param>
    /// <param name="arguments">Command-line arguments for the program (optional). Example: '--new-window' for browsers.</param>
    /// <param name="workingDirectory">Working directory for the launched program (optional).</param>
    /// <param name="waitForWindow">Wait for the application window to appear before returning (default: true). Set to false for background processes.</param>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for the window to appear (default: 5000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the launch operation including the window handle for subsequent operations.</returns>
    [McpServerTool(Name = "app", Title = "Launch Application", Destructive = true, OpenWorld = false, UseStructuredContent = true)]
    public async Task<WindowManagementResult> ExecuteAsync(
        RequestContext<CallToolRequestParams> context,
        string programPath,
        string? arguments = null,
        string? workingDirectory = null,
        bool waitForWindow = true,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        // Create MCP client logger for observability
        var clientLogger = context.Server?.AsClientLoggerProvider().CreateLogger("App");
        clientLogger?.LogWindowOperationStarted("launch");

        try
        {
            var result = await HandleLaunchAsync(programPath, arguments, workingDirectory, waitForWindow, timeoutMs, cancellationToken);

            stopwatch.Stop();

            _logger?.LogWindowOperation(
                "launch",
                success: result.Success,
                windowTitle: result.Window?.Title,
                errorMessage: result.Error);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var errorResult = WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.Timeout,
                "Operation was cancelled");
            _logger?.LogWindowOperation("launch", success: false, errorMessage: errorResult.Error);
            return errorResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger?.LogError("launch", ex);
            var errorResult = WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SystemError,
                $"An unexpected error occurred: {ex.Message}");
            return errorResult;
        }
    }

    private async Task<WindowManagementResult> HandleLaunchAsync(
        string? programPath,
        string? arguments,
        string? workingDirectory,
        bool waitForWindow,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(programPath))
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.MissingRequiredParameter,
                "programPath is required. Specify the executable name (e.g., 'notepad.exe') or full path.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = programPath,
                UseShellExecute = true // Allows launching by name without full path
            };

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startInfo.Arguments = arguments;
            }

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            var process = Process.Start(startInfo);
            if (process is null)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.SystemError,
                    $"Failed to start process: '{programPath}'");
            }

            // If we should wait for the window to appear
            if (waitForWindow)
            {
                var timeout = timeoutMs ?? _configuration.WaitForTimeoutMs;
                var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

                // Wait for the process to have a main window
                while (!process.HasExited && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(100, cancellationToken);
                    process.Refresh();

                    // First try MainWindowHandle (works for most apps)
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        var windowInfo = await _windowService.GetWindowInfoAsync(process.MainWindowHandle, cancellationToken);
                        if (windowInfo != null)
                        {
                            return WindowManagementResult.CreateWindowSuccess(
                                windowInfo,
                                $"Launched '{programPath}'. Window is focused and ready. Use this handle for all subsequent operations.");
                        }

                        await Task.Delay(50, cancellationToken);
                    }

                    // Fallback: Search for any visible window owned by this process
                    var listResult = await _windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
                    if (listResult.Success && listResult.Windows != null)
                    {
                        var processWindow = listResult.Windows.FirstOrDefault(w =>
                            w.ProcessId == process.Id ||
                            string.Equals(w.ProcessName, process.ProcessName, StringComparison.OrdinalIgnoreCase));

                        if (processWindow != null)
                        {
                            return WindowManagementResult.CreateWindowSuccess(
                                processWindow,
                                $"Launched '{programPath}'. Window is focused and ready. Use this handle for all subsequent operations.");
                        }
                    }
                }

                if (process.HasExited)
                {
                    return WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.SystemError,
                        $"Process '{programPath}' exited unexpectedly with code {process.ExitCode}");
                }

                // Timeout waiting for window - make one final attempt
                var finalListResult = await _windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
                if (finalListResult.Success && finalListResult.Windows != null)
                {
                    var processWindow = finalListResult.Windows.FirstOrDefault(w =>
                        w.ProcessId == process.Id ||
                        string.Equals(w.ProcessName, process.ProcessName, StringComparison.OrdinalIgnoreCase));

                    if (processWindow != null)
                    {
                        return WindowManagementResult.CreateWindowSuccess(
                            processWindow,
                            $"Launched '{programPath}'. Window is focused and ready. Use this handle for all subsequent operations.");
                    }
                }

                return WindowManagementResult.CreateSuccess(
                    $"Launched '{programPath}' (PID: {process.Id}), but window did not appear within timeout. Use window_management(action='find') to locate the window.");
            }

            // Not waiting for window - just return success
            return WindowManagementResult.CreateSuccess(
                $"Launched '{programPath}' successfully (PID: {process.Id})");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2) // ERROR_FILE_NOT_FOUND
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.WindowNotFound,
                $"Program not found: '{programPath}'. Check the path or ensure the program is in the system PATH.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // ERROR_ACCESS_DENIED
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.AccessDenied,
                $"Access denied when trying to launch '{programPath}'. Check permissions.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SystemError,
                $"Failed to launch '{programPath}': {ex.Message}");
        }
    }
}
