using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for launching applications on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public static partial class AppTool
{
    /// <summary>
    /// How long to wait to detect if a process is a stub (exits quickly).
    /// </summary>
    private const int StubDetectionDelayMs = 300;

    /// <summary>
    /// Launch Windows applications by semantic app name or executable path. Prefer this tool over powershell, shell,
    /// terminal, or command-line process launchers whenever the user asks to open, start, or launch an app.
    /// Use this to start programs like notepad.exe, calc.exe, msedge.exe, chrome.exe, winword.exe, excel.exe, etc.
    /// Returns structured launch status plus a window handle for use with window_management, keyboard_control, and other tools.
    /// </summary>
    /// <remarks>
    /// This tool is safer and more reliable than shell commands for app launch because it focuses the window,
    /// waits for the first usable window, handles UWP/Store app stubs such as Calculator, and returns normalized
    /// process/window metadata. Do not use powershell or shell commands to launch apps unless this tool fails or
    /// the task explicitly requires shell execution.
    ///
    /// Examples: app(programPath='notepad.exe'), app(programPath='calc.exe'), app(programPath='msedge.exe', arguments='https://example.com').
    /// After launch, the window is focused and ready for input. Use the returned handle for subsequent operations.
    /// Launch a browser with a URL, then use ui_find/ui_click/ui_type with the returned handle to automate page content.
    /// Edge (msedge.exe) and Chrome (chrome.exe) page content is fully automatable: links, buttons, and form fields
    /// surface as ARIA/visible-text UIA names. Browser chrome (address bar, tabs) is best-effort — use keyboard shortcuts.
    ///
    /// NOTE: Some apps (e.g., calc.exe, UWP/Store apps) launch as a stub that exits immediately and spawns a separate
    /// process. If the launch appears to fail, use window_management(action='find', title='Calculator') to locate
    /// the window by title instead.
    /// NOTE: Chromium browsers (Edge, Chrome) also use a stub/session model — if you need an authenticated page,
    /// check window_management(action='find') for an existing signed-in window before calling app().
    /// </remarks>
    /// <param name="programPath">Program to launch. Can be executable name (e.g., 'notepad.exe', 'calc.exe', 'chrome.exe') or full path (e.g., 'C:\\Program Files\\App\\app.exe').</param>
    /// <param name="arguments">Command-line arguments for the program (optional). Example: '--new-window' for browsers.</param>
    /// <param name="workingDirectory">Working directory for the launched program (optional).</param>
    /// <param name="waitForWindow">Wait for the application window to appear before returning (default: true). Set to false for background processes.</param>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for the window to appear (default: 5000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload of the launch operation, including the window handle for subsequent operations. <c>IsError</c> reflects operation success.</returns>
    [McpServerTool(Name = "app", Title = "Launch Application", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string programPath,
        [DefaultValue(null)] string? arguments,
        [DefaultValue(null)] string? workingDirectory,
        [DefaultValue(true)] bool waitForWindow,
        [DefaultValue(null)] int? timeoutMs,
        CancellationToken cancellationToken)
    {
        const string actionName = "launch";

        try
        {
            var result = await HandleLaunchAsync(programPath, arguments, workingDirectory, waitForWindow, timeoutMs, cancellationToken);
            return ToCallToolResult(result);
        }
        catch (OperationCanceledException)
        {
            var errorResult = WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.Timeout,
                "Operation was cancelled");
            return ToCallToolResult(errorResult);
        }
        catch (Exception ex)
        {
            return ErrorResult(WindowsToolsBase.SerializeToolError(actionName, ex));
        }
    }

    /// <summary>
    /// Converts a window management result into an MCP call result. <see cref="CallToolResult.IsError"/>
    /// mirrors <see cref="WindowManagementResult.Success"/>.
    /// </summary>
    private static CallToolResult ToCallToolResult(WindowManagementResult result) =>
        new()
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions) }],
            IsError = !result.Success
        };

    /// <summary>
    /// Wraps a pre-serialized JSON error payload in a failed call result.
    /// </summary>
    private static CallToolResult ErrorResult(string json) =>
        new()
        {
            Content = [new TextContentBlock { Text = json }],
            IsError = true
        };

    private static async Task<WindowManagementResult> HandleLaunchAsync(
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
            // Chromium browsers only expose a complete accessibility tree when an assistive-technology
            // client requests it. Force renderer accessibility on launch so ui_find/ui_read/ui_click see
            // full page content (links, buttons, form fields) instead of a reduced/empty tree.
            arguments = AugmentChromiumArguments(programPath, arguments);

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

            var launchTime = DateTime.UtcNow;
            var process = Process.Start(startInfo);
            if (process is null)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.SystemError,
                    $"Failed to start process: '{programPath}'");
            }

            // Extract the program name for window title matching (used if process is a stub)
            var programName = Path.GetFileNameWithoutExtension(programPath) ?? programPath;

            // If we should wait for the window to appear
            if (waitForWindow)
            {
                var windowService = WindowsToolsBase.WindowService;
                var timeout = timeoutMs ?? WindowsToolsBase.TimeoutMs;
                var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

                // First, give the process a brief moment to see if it's a stub
                // UWP stubs like calc.exe exit almost immediately (< 100ms)
                await Task.Delay(StubDetectionDelayMs, cancellationToken);
                process.Refresh();

                // Check early if this is a stub pattern (process exited quickly with success)
                if (process.HasExited)
                {
                    var exitedQuickly = (DateTime.UtcNow - launchTime).TotalMilliseconds < (StubDetectionDelayMs * 2);
                    var exitedSuccessfully = process.ExitCode == 0;

                    if (exitedQuickly && exitedSuccessfully)
                    {
                        // This is likely a stub that launched another process (e.g., UWP app)
                        // Try to find a window by program name in the title
                        var stubWindow = await FindWindowByTitleAsync(windowService, programName, deadline, cancellationToken);
                        if (stubWindow != null)
                        {
                            return WindowManagementResult.CreateWindowSuccess(
                                stubWindow,
                                $"Launched '{programPath}'. Window is focused and ready. Use this handle for all subsequent operations.");
                        }
                    }

                    return WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.SystemError,
                        $"Process '{programPath}' exited unexpectedly with code {process.ExitCode}");
                }

                // Wait for the process to have a main window
                while (!process.HasExited && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(100, cancellationToken);
                    process.Refresh();

                    // First try MainWindowHandle (works for most apps)
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        var windowInfo = await windowService.GetWindowInfoAsync(process.MainWindowHandle, cancellationToken);
                        if (windowInfo != null)
                        {
                            return WindowManagementResult.CreateWindowSuccess(
                                windowInfo,
                                $"Launched '{programPath}'. Window is focused and ready. Use this handle for all subsequent operations.");
                        }

                        await Task.Delay(50, cancellationToken);
                    }

                    // Fallback: Search for any visible window owned by this process
                    var listResult = await windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
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
                var finalListResult = await windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
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

    /// <summary>
    /// Known Chromium-based browser executables (without extension) that expose their page
    /// accessibility tree lazily and benefit from --force-renderer-accessibility on launch.
    /// </summary>
    private static readonly string[] ChromiumExecutables =
        ["msedge", "chrome", "brave", "vivaldi", "opera", "chromium"];

    /// <summary>
    /// Appends --force-renderer-accessibility when launching a Chromium browser so its page
    /// accessibility tree is fully populated for UIA-based automation. No-op for other programs
    /// or when the flag is already present.
    /// </summary>
    internal static string? AugmentChromiumArguments(string programPath, string? arguments)
    {
        var name = Path.GetFileNameWithoutExtension(programPath);
        if (string.IsNullOrEmpty(name) ||
            !ChromiumExecutables.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return arguments;
        }

        if (arguments != null &&
            arguments.Contains("force-renderer-accessibility", StringComparison.OrdinalIgnoreCase))
        {
            return arguments;
        }

        const string a11yFlag = "--force-renderer-accessibility";
        return string.IsNullOrWhiteSpace(arguments) ? a11yFlag : $"{a11yFlag} {arguments}";
    }

    /// <summary>
    /// Finds a window by searching for the program name in window titles.
    /// This handles cases where the launched process is a stub that redirects to another app
    /// (e.g., UWP apps like Calculator where calc.exe is a stub for the Store app).
    /// </summary>
    private static async Task<WindowInfoCompact?> FindWindowByTitleAsync(
        Window.WindowService windowService,
        string programName,
        DateTime deadline,
        CancellationToken cancellationToken)
    {
        while (DateTime.UtcNow < deadline)
        {
            var listResult = await windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
            if (listResult.Success && listResult.Windows != null)
            {
                // Search for window where title contains the program name
                // e.g., "calc" matches "Calculator", "notepad" matches "Notepad"
                var matchingWindow = listResult.Windows.FirstOrDefault(w =>
                    w.Title?.Contains(programName, StringComparison.OrdinalIgnoreCase) == true);

                if (matchingWindow != null)
                {
                    // Focus the window before returning (Handle is stored as decimal string)
                    if (nint.TryParse(matchingWindow.Handle, out var hwnd))
                    {
                        await windowService.ActivateWindowAsync(hwnd, cancellationToken);
                    }
                    return matchingWindow;
                }
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }
}
