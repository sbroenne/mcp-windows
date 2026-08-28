using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

internal sealed class ChromiumBrowserSession : IDisposable
{
    private static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReadyPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProfileCleanupTimeout = TimeSpan.FromSeconds(5);

    private static readonly PopupSignal[] KnownPopupSignals =
    [
        new("We are now syncing", ["Got it", "Close"]),
        new("Turn on sync", ["No thanks", "Not now", "Close"]),
        new("Welcome to Microsoft Edge", ["Get started", "Close", "Got it"]),
        new("Set up your new tab page", ["Skip", "Close"]),
        new("Sign in to sync your data", ["No thanks", "Not now", "Close"]),
    ];

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(nint hSnapshot, ref ProcessEntry32 lppe);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(nint hSnapshot, ref ProcessEntry32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    private const uint WmClose = 0x0010;
    private const uint Th32csSnapProcess = 0x00000002;
    private static readonly nint InvalidHandleValue = new(-1);

    private readonly Process _browserProcess;
    private readonly Process? _windowProcess;
    private readonly string _browserProcessName;
    private readonly string? _userDataDirectory;
    private bool _disposed;

    private ChromiumBrowserSession(
        Process browserProcess,
        nint windowHandle,
        string browserProcessName,
        string? userDataDirectory,
        IReadOnlySet<int> existingProcessIds)
    {
        _browserProcess = browserProcess;
        _ = NativeMethods.GetWindowThreadProcessId(windowHandle, out var windowProcessId);
        _windowProcess = windowProcessId > 0 &&
            windowProcessId != browserProcess.Id &&
            IsTestOwnedProcess(unchecked((int)windowProcessId), browserProcess.Id, existingProcessIds)
            ? Process.GetProcessById(unchecked((int)windowProcessId))
            : null;
        _browserProcessName = browserProcessName;
        _userDataDirectory = userDataDirectory;
        WindowHandle = windowHandle;
        WindowHandleString = WindowHandleParser.Format(windowHandle);
    }

    public nint WindowHandle { get; }

    public string WindowHandleString { get; }

    public static void SkipUnlessSupported(ChromiumBrowserKind browser = ChromiumBrowserKind.Edge)
    {
        Skip.If(FindBrowserExecutable(browser) is null, $"Chromium browser smoke tests require {GetBrowserDisplayName(browser)} to be installed.");

        // R9: Chrome is opt-in for the default local run to halve launch cost. Edge is the always-on
        // baseline; enable Chrome coverage by setting MCP_TEST_CHROME=1 (e.g., in CI browser matrices).
        if (browser == ChromiumBrowserKind.Chrome)
        {
            var optIn = Environment.GetEnvironmentVariable("MCP_TEST_CHROME");
            var enabled = string.Equals(optIn, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(optIn, "true", StringComparison.OrdinalIgnoreCase);
            Skip.IfNot(enabled, "Chrome coverage is opt-in. Set MCP_TEST_CHROME=1 to run Chrome smoke tests.");
        }
    }

    public static ChromiumBrowserSession LaunchLocalPage(ChromiumBrowserKind browser = ChromiumBrowserKind.Edge)
    {
        var pagePath = FindLocalPagePath();
        return Launch(browser, new BrowserTarget(
            "local page",
            new Uri(pagePath).AbsoluteUri,
            "MCP Chromium Browser Test Page",
            TimeSpan.FromSeconds(15),
            [new ReadyElement("Primary navigation"), new ReadyElement("Docs Search"), new ReadyElement("Sign in", "Button")]));
    }

    internal static ChromiumBrowserSession LaunchLocalPageForReadinessFailureTest(
        ChromiumBrowserKind browser)
    {
        var pagePath = FindLocalPagePath();
        return Launch(browser, new BrowserTarget(
            "intentional readiness failure",
            new Uri(pagePath).AbsoluteUri,
            "MCP Chromium Browser Test Page",
            TimeSpan.FromMilliseconds(500),
            [new ReadyElement("Control that does not exist")]));
    }

    internal static ChromiumBrowserSession LaunchLocalPageForWindowFailureTest(
        ChromiumBrowserKind browser)
    {
        var pagePath = FindLocalPagePath();
        return Launch(browser, new BrowserTarget(
            "intentional window discovery failure",
            new Uri(pagePath).AbsoluteUri,
            "Window title that does not exist",
            TimeSpan.FromMilliseconds(500),
            [],
            WindowTimeout: TimeSpan.FromMilliseconds(500)));
    }

    public static ChromiumBrowserSession LaunchPublicSite(ChromiumBrowserKind browser, ChromiumPublicSite site)
    {
        return Launch(browser, site switch
        {
            ChromiumPublicSite.PlaywrightTodoMvc => new BrowserTarget(
                "Playwright TodoMVC",
                "https://demo.playwright.dev/todomvc/",
                "TodoMVC",
                TimeSpan.FromSeconds(20),
                [new ReadyElement("What needs to be done?", "Edit")]),
            ChromiumPublicSite.GitHubVisualStudioCode => new BrowserTarget(
                "GitHub microsoft/vscode",
                "https://github.com/microsoft/vscode",
                "microsoft/vscode",
                TimeSpan.FromSeconds(30),
                [new ReadyElement("Code")],
                AppMode: false),
            _ => throw new ArgumentOutOfRangeException(nameof(site), site, "Unsupported Chromium public site."),
        });
    }

    public static ChromiumBrowserSession LaunchPublicSite(ChromiumPublicSite site)
    {
        return LaunchPublicSite(ChromiumBrowserKind.Edge, site);
    }

    public static ChromiumBrowserSession Launch()
    {
        return LaunchLocalPage();
    }

    private static ChromiumBrowserSession Launch(ChromiumBrowserKind browser, BrowserTarget target)
    {
        var browserExecutable = FindBrowserExecutable(browser)
            ?? throw new InvalidOperationException($"{GetBrowserDisplayName(browser)} executable was not found.");
        var browserDescriptor = GetBrowserDescriptor(browser);
        var userDataDirectory = PrepareUserDataDirectory(browserExecutable, browserDescriptor);

        var existingWindows = SnapshotBrowserWindows(browserDescriptor.ProcessName);
        var existingProcessIds = SnapshotBrowserProcessIds(browserDescriptor.ProcessName);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = browserExecutable,
                Arguments = BuildLaunchArguments(target, userDataDirectory),
                UseShellExecute = false,
                CreateNoWindow = false,
            }
        };

        ChromiumBrowserSession? session = null;
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start {browserDescriptor.DisplayName} for Chromium browser smoke tests.");
            }

            var windowHandle = WaitForWindow(
                process.Id,
                target.TitleFragment,
                existingWindows,
                existingProcessIds,
                browserDescriptor.ProcessName,
                target.WindowTimeout ?? LaunchTimeout);
            session = new ChromiumBrowserSession(
                process,
                windowHandle,
                browserDescriptor.ProcessName,
                userDataDirectory,
                existingProcessIds);
            session.BringToFront();
            WaitForPageReady(target, session.WindowHandleString);
            return session;
        }
        catch
        {
            if (session is not null)
            {
                session.Dispose();
            }
            else
            {
                EnsureProcessExited(process);
                DeleteUserDataDirectory(userDataDirectory);
                process.Dispose();
            }

            throw;
        }
    }

    public void BringToFront()
    {
        TestWait.RetryUntil(
            attempt: () =>
            {
                AllowSetForegroundWindow(-1);
                SetForegroundWindow(WindowHandle);
            },
            condition: () => GetForegroundWindow() == WindowHandle,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(50));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            CloseWindow();
        }
        catch
        {
            // Best-effort cleanup in test code.
        }
        finally
        {
            EnsureProcessExited(_windowProcess);
            EnsureProcessExited(_browserProcess);
            DeleteUserDataDirectory(_userDataDirectory);
            _windowProcess?.Dispose();
            _browserProcess.Dispose();
        }

    }

    private static string? FindBrowserExecutable(ChromiumBrowserKind browser)
    {
        var descriptor = GetBrowserDescriptor(browser);
        var overridePath = Environment.GetEnvironmentVariable(
            browser == ChromiumBrowserKind.Chrome
                ? "MCP_TEST_CHROME_PATH"
                : "MCP_TEST_EDGE_PATH");

        return !string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath)
            ? overridePath
            : descriptor.CandidatePaths.FirstOrDefault(File.Exists);
    }

    private static string CreateUserDataDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "mcp-windows-chromium-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    // R7: a per-run warmed profile template. Chromium writes its first-run/baseline state (Local State,
    // default profile) on the first launch; copying that state into each fresh, isolated session profile
    // lets subsequent launches skip most first-run UI without sharing a live profile directory (which
    // would collide with the still-open read-only fixture session). Warm-up is a pure optimization: any
    // failure falls back to a cold profile, matching the previous behavior exactly.
    private static readonly object s_templateGate = new();
    private static readonly Dictionary<string, string?> s_warmedTemplates = new(StringComparer.OrdinalIgnoreCase);
    private static int s_templateCleanupHooked;

    private static string PrepareUserDataDirectory(string browserExecutable, BrowserDescriptor descriptor)
    {
        var sessionDirectory = CreateUserDataDirectory();

        try
        {
            var template = GetOrCreateWarmedTemplate(browserExecutable, descriptor);
            if (template is not null)
            {
                CopyProfileTemplate(template, sessionDirectory);
            }
        }
        catch
        {
            // Fall back to a cold profile on any warm-up/copy failure.
        }

        return sessionDirectory;
    }

    private static string? GetOrCreateWarmedTemplate(string browserExecutable, BrowserDescriptor descriptor)
    {
        lock (s_templateGate)
        {
            if (s_warmedTemplates.TryGetValue(descriptor.ProcessName, out var existing))
            {
                return existing;
            }

            string? template;
            try
            {
                template = BuildWarmedTemplate(browserExecutable);
            }
            catch
            {
                template = null;
            }

            s_warmedTemplates[descriptor.ProcessName] = template;
            HookTemplateCleanup();
            return template;
        }
    }

    private static string? BuildWarmedTemplate(string browserExecutable)
    {
        var templateDirectory = Path.Combine(
            Path.GetTempPath(),
            "mcp-windows-chromium-tests",
            $"warm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(templateDirectory);

        string[] arguments =
        [
            "--headless=new",
            $"--user-data-dir=\"{templateDirectory}\"",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-sync",
            "--disable-extensions",
            "about:blank",
        ];

        using var warmProcess = Process.Start(new ProcessStartInfo
        {
            FileName = browserExecutable,
            Arguments = string.Join(" ", arguments),
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        if (warmProcess is null)
        {
            return null;
        }

        // Wait until Chromium has written its baseline profile state, then shut the warm-up down.
        var localState = Path.Combine(templateDirectory, "Local State");
        TestWait.Until(
            () => File.Exists(localState),
            timeout: TimeSpan.FromSeconds(10),
            pollInterval: TimeSpan.FromMilliseconds(100));

        try
        {
            if (!warmProcess.HasExited)
            {
                warmProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort shutdown of the warm-up process.
        }

        try
        {
            warmProcess.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds);
        }
        catch
        {
            // Ignore — locks are released on exit regardless.
        }

        return File.Exists(localState) ? templateDirectory : null;
    }

    private static void CopyProfileTemplate(string sourceDirectory, string destinationDirectory)
    {
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);

            // Skip single-instance lock artifacts — they must not be shared between profiles.
            if (fileName.StartsWith("Singleton", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("lockfile", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var target = file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal);

            try
            {
                File.Copy(file, target, overwrite: true);
            }
            catch (IOException)
            {
                // Skip files still locked by the browser; they are non-essential for warm start.
            }
        }
    }

    private static void HookTemplateCleanup()
    {
        if (Interlocked.Exchange(ref s_templateCleanupHooked, 1) != 0)
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
        {
            foreach (var directory in s_warmedTemplates.Values)
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup at process exit.
                }
            }
        };
    }

    private static string FindLocalPagePath()
    {
        var currentDir = AppContext.BaseDirectory;

        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(currentDir, "Integration", "ChromiumBrowser", "chromium-local-page.html");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            currentDir = Path.GetDirectoryName(currentDir)
                ?? throw new InvalidOperationException("Could not determine Chromium browser test page path.");
        }

        throw new InvalidOperationException("Could not locate chromium-local-page.html for Chromium browser smoke tests.");
    }

    private void CloseWindow()
    {
        if (WindowHandle == nint.Zero)
        {
            return;
        }

        PostMessage(WindowHandle, WmClose, nint.Zero, nint.Zero);

        var enumerator = new WindowEnumerator(new ElevationDetector());
        TestWait.Until(
            condition: () =>
            {
                var windows = enumerator.EnumerateWindowsAsync(cancellationToken: CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                return !windows.Any(window =>
                    string.Equals(window.Handle, WindowHandleString, StringComparison.OrdinalIgnoreCase));
            },
            timeout: TimeSpan.FromSeconds(5),
            pollInterval: TimeSpan.FromMilliseconds(100));
    }

    private static void EnsureProcessExited(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (process.HasExited)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        if (process.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds);
        }
        catch
        {
            // Best-effort cleanup in test code.
        }
    }

    private static void DeleteUserDataDirectory(string? userDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(userDataDirectory) || !Directory.Exists(userDataDirectory))
        {
            return;
        }

        TestWait.Until(
            condition: () =>
            {
                try
                {
                    Directory.Delete(userDataDirectory, recursive: true);
                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            },
            timeout: ProfileCleanupTimeout,
            pollInterval: TimeSpan.FromMilliseconds(100));
    }

    private static HashSet<string> SnapshotBrowserWindows(string browserProcessName)
    {
        var enumerator = new WindowEnumerator(new ElevationDetector());
        var windows = enumerator.EnumerateWindowsAsync(cancellationToken: CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return windows
            .Where(window => string.Equals(window.ProcessName, browserProcessName, StringComparison.OrdinalIgnoreCase))
            .Select(window => window.Handle)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildLaunchArguments(BrowserTarget target, string userDataDirectory)
    {
        var targetArgument = target.AppMode
            ? $"--app=\"{target.Url}\""
            : $"\"{target.Url}\"";
        string[] arguments =
        [
            "--new-window",
            targetArgument,
            $"--user-data-dir=\"{userDataDirectory}\"",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-session-crashed-bubble",
            "--disable-sync",
            "--disable-extensions",
            "--disable-component-extensions-with-background-pages",
            "--force-renderer-accessibility",
            "--window-size=1280,900",
        ];

        return string.Join(" ", arguments);
    }

    private static void WaitForPageReady(BrowserTarget target, string windowHandle)
    {
        using var staThread = new UIAutomationThread();
        using var automationService = new UIAutomationService(
            staThread,
            new MonitorService(),
            new MouseInputService(),
            new KeyboardInputService(),
            new WindowActivator(),
            new ElevationDetector(),
            NullLogger<UIAutomationService>.Instance);
        string? missingReadyElement = null;

        var ready = TestWait.Until(
            condition: () =>
            {
                if (IsReady(target, automationService, windowHandle, out missingReadyElement))
                {
                    return true;
                }

                TryDismissKnownPopup(automationService, windowHandle);
                return false;
            },
            timeout: target.ReadyTimeout,
            pollInterval: ReadyPollInterval);

        if (!ready)
        {
            throw new InvalidOperationException(
                $"Timed out waiting for Chromium target '{target.Name}' to become ready without Edge first-run UI interference. " +
                $"Missing control: {missingReadyElement ?? "unknown"}.");
        }
    }

    private static bool IsReady(
        BrowserTarget target,
        UIAutomationService automationService,
        string windowHandle,
        out string? missingReadyElement)
    {
        foreach (var readyElement in target.ReadyElements)
        {
            var result = automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = windowHandle,
                Name = readyElement.Name,
                ControlType = readyElement.ControlType,
                TimeoutMs = 1000,
            }).GetAwaiter().GetResult();

            if (!result.Success ||
                result.Items is not { Length: > 0 } ||
                !result.Items.Any(item =>
                    string.Equals(item.Name, readyElement.Name, StringComparison.Ordinal) &&
                    (readyElement.ControlType is null ||
                     string.Equals(item.Type, readyElement.ControlType, StringComparison.Ordinal))))
            {
                missingReadyElement = readyElement.ControlType is null
                    ? readyElement.Name
                    : $"{readyElement.ControlType} '{readyElement.Name}'";
                return false;
            }
        }

        missingReadyElement = null;
        return true;
    }

    private static bool TryDismissKnownPopup(UIAutomationService automationService, string windowHandle)
    {
        var gotItResult = automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = "Got it",
            ControlType = "Button",
            TimeoutMs = 500,
        }).GetAwaiter().GetResult();

        if (gotItResult.Success)
        {
            return true;
        }

        foreach (var popupSignal in KnownPopupSignals)
        {
            var popupResult = automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = windowHandle,
                NameContains = popupSignal.SignalText,
                TimeoutMs = 500,
            }).GetAwaiter().GetResult();

            if (!popupResult.Success || popupResult.Items is not { Length: > 0 })
            {
                continue;
            }

            foreach (var buttonName in popupSignal.DismissButtons)
            {
                var clickResult = automationService.FindAndClickAsync(new ElementQuery
                {
                    WindowHandle = windowHandle,
                    Name = buttonName,
                    ControlType = "Button",
                    TimeoutMs = 1000,
                }).GetAwaiter().GetResult();

                if (clickResult.Success)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static nint WaitForWindow(
        int processId,
        string titleFragment,
        HashSet<string> existingWindows,
        IReadOnlySet<int> existingProcessIds,
        string browserProcessName,
        TimeSpan timeout)
    {
        var enumerator = new WindowEnumerator(new ElevationDetector());

        nint foundHandle = nint.Zero;
        var found = TestWait.Until(
            condition: () =>
            {
                var windows = enumerator.EnumerateWindowsAsync(cancellationToken: CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                var match = windows.FirstOrDefault(window =>
                    window.ProcessId == processId &&
                    string.Equals(window.ProcessName, browserProcessName, StringComparison.OrdinalIgnoreCase) &&
                    window.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase))
                    ?? windows.FirstOrDefault(window =>
                        string.Equals(window.ProcessName, browserProcessName, StringComparison.OrdinalIgnoreCase) &&
                        window.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase) &&
                        !existingWindows.Contains(window.Handle) &&
                        IsTestOwnedProcess(window.ProcessId, processId, existingProcessIds));

                if (match is null ||
                    !WindowHandleParser.TryParse(match.Handle, out foundHandle) ||
                    foundHandle == nint.Zero)
                {
                    foundHandle = nint.Zero;
                    return false;
                }

                return true;
            },
            timeout: timeout,
            pollInterval: TimeSpan.FromMilliseconds(200));

        if (found)
        {
            return foundHandle;
        }

        throw new InvalidOperationException($"Timed out waiting for Chromium browser test window '{titleFragment}'.");
    }

    internal static bool IsTestOwnedProcess(
        int candidateProcessId,
        int launchedProcessId,
        IReadOnlySet<int> existingProcessIds)
    {
        if (existingProcessIds.Contains(candidateProcessId))
        {
            return false;
        }

        return candidateProcessId == launchedProcessId ||
            IsDescendantProcess(candidateProcessId, launchedProcessId);
    }

    private static bool IsDescendantProcess(int candidateProcessId, int ancestorProcessId)
    {
        var parentsByProcessId = SnapshotProcessParents();
        var visited = new HashSet<int>();
        var currentProcessId = candidateProcessId;

        while (visited.Add(currentProcessId) &&
               parentsByProcessId.TryGetValue(currentProcessId, out var parentProcessId) &&
               parentProcessId > 0)
        {
            if (parentProcessId == ancestorProcessId)
            {
                return true;
            }

            currentProcessId = parentProcessId;
        }

        return false;
    }

    private static Dictionary<int, int> SnapshotProcessParents()
    {
        var snapshot = CreateToolhelp32Snapshot(Th32csSnapProcess, 0);
        if (snapshot == InvalidHandleValue)
        {
            return [];
        }

        try
        {
            var result = new Dictionary<int, int>();
            var entry = new ProcessEntry32
            {
                Size = checked((uint)Marshal.SizeOf<ProcessEntry32>())
            };

            if (!Process32First(snapshot, ref entry))
            {
                return result;
            }

            do
            {
                result[unchecked((int)entry.ProcessId)] =
                    unchecked((int)entry.ParentProcessId);
            }
            while (Process32Next(snapshot, ref entry));

            return result;
        }
        finally
        {
            _ = CloseHandle(snapshot);
        }
    }

    private static HashSet<int> SnapshotBrowserProcessIds(string browserProcessName)
    {
        var processes = Process.GetProcessesByName(browserProcessName);
        try
        {
            return processes.Select(process => process.Id).ToHashSet();
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public nint DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExecutableFile;
    }

    private sealed record BrowserTarget(
        string Name,
        string Url,
        string TitleFragment,
        TimeSpan ReadyTimeout,
        IReadOnlyList<ReadyElement> ReadyElements,
        bool AppMode = true,
        TimeSpan? WindowTimeout = null);
    private sealed record BrowserDescriptor(string DisplayName, string ProcessName, IReadOnlyList<string> CandidatePaths);
    private sealed record ReadyElement(string Name, string? ControlType = null);
    private sealed record PopupSignal(string SignalText, IReadOnlyList<string> DismissButtons);

    private static BrowserDescriptor GetBrowserDescriptor(ChromiumBrowserKind browser)
    {
        return browser switch
        {
            ChromiumBrowserKind.Edge => new BrowserDescriptor(
                "Microsoft Edge",
                "msedge",
                [
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                ]),
            ChromiumBrowserKind.Chrome => new BrowserDescriptor(
                "Google Chrome",
                "chrome",
                [
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(browser), browser, "Unsupported Chromium browser."),
        };
    }

    private static string GetBrowserDisplayName(ChromiumBrowserKind browser)
    {
        return GetBrowserDescriptor(browser).DisplayName;
    }
}
