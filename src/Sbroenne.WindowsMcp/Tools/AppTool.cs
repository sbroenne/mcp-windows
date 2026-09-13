using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// MCP tool for launching applications on Windows.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public static partial class AppTool
{
    /// <summary>
    /// Launch Windows applications by semantic app name or executable path. Prefer this tool over powershell, shell,
    /// terminal, or command-line process launchers whenever the user asks to open, start, or launch an app.
    /// Use this to start programs like notepad.exe, calc.exe, msedge.exe, chrome.exe, winword.exe, excel.exe, etc.
    /// Returns structured launch status and, when uniquely identified, a window handle for subsequent tools.
    /// Keywords: launch, open, start, run, app, application, program, executable, exe, open app,
    /// start program, launch application, run program, notepad, calculator, browser, edge, chrome.
    /// </summary>
    /// <remarks>
    /// This tool observes the launched process and its visible windows and returns normalized
    /// process/window metadata. Do not use powershell or shell commands to launch apps unless this tool fails or
    /// the task explicitly requires shell execution.
    ///
    /// Examples: app(programPath='notepad.exe'), app(programPath='calc.exe'), app(programPath='msedge.exe', arguments='https://example.com').
    /// A returned window is observed, not guaranteed focused or ready for input. Verify the intended
    /// page/document with ui_read or ui_wait before acting. Activate the window explicitly if needed.
    /// Launch a browser with a URL, then use ui_find/ui_click/ui_type with the returned handle to automate page content.
    /// Edge (msedge.exe) and Chrome (chrome.exe) page content is fully automatable: links, buttons, and form fields
    /// surface as ARIA/visible-text UIA names. Browser chrome (address bar, tabs) is best-effort — use keyboard shortcuts.
    ///
    /// NOTE: A clean exit with a surviving pre-launch instance of the same executable returns possibleHandoff.
    /// Delivery and requested content are not verified. Multiple matching windows return no selected handle.
    /// Store launchers that redirect to a different executable cannot be associated by title; use
    /// window_management to inspect the intended application when no matching window can be verified.
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
                if (!Directory.Exists(workingDirectory))
                {
                    return WindowManagementResult.CreateFailure(
                        WindowManagementErrorCode.InvalidParameter,
                        $"workingDirectory does not exist: '{workingDirectory}'");
                }

                startInfo.WorkingDirectory = workingDirectory;
            }

            var baseline = new Dictionary<int, LaunchProcessIdentity>();
            if (waitForWindow)
            {
                var before = await WindowsToolsBase.WindowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
                if (!before.Success)
                {
                    return before;
                }
                foreach (var pid in (before.Windows ?? []).Select(w => w.ProcessId).Distinct())
                {
                    var identity = LaunchProcessIdentity.TryRead(pid);
                    if (identity is not null)
                    {
                        baseline.Add(pid, identity);
                    }
                }
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return WindowManagementResult.CreateFailure(
                    WindowManagementErrorCode.SystemError,
                    $"Failed to start process: '{programPath}'");
            }

            if (waitForWindow)
            {
                var windowService = WindowsToolsBase.WindowService;
                var executablePath = LaunchProcessIdentity.TryRead(process)?.ExecutablePath;
                var timeout = timeoutMs ?? WindowsToolsBase.TimeoutMs;
                var elapsed = Stopwatch.StartNew();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    process.Refresh();
                    if (process.HasExited)
                    {
                        return await ObserveExitAsync(process, programPath, executablePath, baseline, cancellationToken);
                    }

                    var listResult = await windowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
                    // Enumeration is asynchronous: an exit during it must win over stale window evidence.
                    process.Refresh();
                    if (process.HasExited)
                    {
                        return await ObserveExitAsync(process, programPath, executablePath, baseline, cancellationToken);
                    }
                    if (!listResult.Success)
                    {
                        return listResult;
                    }
                    var windows = (listResult.Windows ?? []).Where(w => w.ProcessId == process.Id).ToArray();
                    if (windows.Length > 0)
                    {
                        return ObservedWindows(windows, "windowObserved",
                            $"Launched '{programPath}' (PID: {process.Id}); visible window observed. Focus, input readiness, and requested content are not verified.");
                    }
                    var remaining = timeout - elapsed.ElapsedMilliseconds;
                    if (remaining <= 0)
                    {
                        return WindowManagementResult.CreateSuccess(
                            $"Launched '{programPath}' (PID: {process.Id}), but no window was observed within timeout. Request delivery and content are not verified. Use window_management to inspect the application.")
                            with
                        { LaunchStatus = "started" };
                    }
                    await Task.Delay((int)Math.Min(100, remaining), cancellationToken);
                }
            }

            return WindowManagementResult.CreateSuccess(
                $"Started '{programPath}' (PID: {process.Id}); window and exit status were not observed.")
                with
            { LaunchStatus = "started" };
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

    private static async Task<WindowManagementResult> ObserveExitAsync(
        Process process,
        string programPath,
        string? executablePath,
        Dictionary<int, LaunchProcessIdentity> baseline,
        CancellationToken cancellationToken)
    {
        if (process.ExitCode != 0)
        {
            return WindowManagementResult.CreateFailure(
                WindowManagementErrorCode.SystemError,
                $"Process '{programPath}' exited with code {process.ExitCode}");
        }

        var current = await WindowsToolsBase.WindowService.ListWindowsAsync(includeAllDesktops: true, cancellationToken: cancellationToken);
        if (!current.Success)
        {
            return current;
        }
        var matchingProcesses = new HashSet<int>();
        foreach (var pid in (current.Windows ?? []).Select(w => w.ProcessId).Distinct())
        {
            if (executablePath is not null &&
                baseline.TryGetValue(pid, out var before) &&
                string.Equals(before.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase) &&
                LaunchProcessIdentity.TryRead(pid) == before)
            {
                matchingProcesses.Add(pid);
            }
        }
        var candidates = (current.Windows ?? []).Where(w => matchingProcesses.Contains(w.ProcessId)).ToArray();
        if (candidates.Length > 0)
        {
            return ObservedWindows(candidates, "possibleHandoff",
                $"Process '{programPath}' exited with code 0; a pre-existing instance of the same executable remains. Possible request handoff; delivery, requested content, focus, and input readiness are not verified.");
        }

        return WindowManagementResult.CreateFailure(
            WindowManagementErrorCode.SystemError,
            $"Process '{programPath}' exited with code 0 without an observed window or a verified matching pre-launch instance. Request handoff is not verified; inspect the intended application with window_management before retrying. Redirected Store launchers may use a different executable.")
            with
        { LaunchStatus = "exitedWithoutWindow" };
    }

    private static WindowManagementResult ObservedWindows(WindowInfoCompact[] windows, string status, string message)
    {
        return new WindowManagementResult
        {
            Success = true,
            LaunchStatus = status,
            Window = windows.Length == 1 ? windows[0] : null,
            Windows = windows.Length > 1 ? windows : null,
            Count = windows.Length,
            Message = windows.Length > 1 ? message + " Multiple windows match; no target window was selected." : message,
        };
    }
}
