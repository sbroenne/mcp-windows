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
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    private const uint WmClose = 0x0010;

    private readonly Process _browserProcess;
    private readonly string _browserProcessName;
    private readonly string? _userDataDirectory;
    private bool _disposed;

    private ChromiumBrowserSession(Process browserProcess, nint windowHandle, string browserProcessName, string? userDataDirectory)
    {
        _browserProcess = browserProcess;
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
    }

    public static ChromiumBrowserSession LaunchLocalPage(ChromiumBrowserKind browser = ChromiumBrowserKind.Edge)
    {
        var pagePath = FindLocalPagePath();
        return Launch(browser, new BrowserTarget(
            "local page",
            new Uri(pagePath).AbsoluteUri,
            "MCP Chromium Browser Test Page",
            TimeSpan.FromSeconds(15),
            [new ReadyElement("Primary navigation"), new ReadyElement("Docs Search", "Edit"), new ReadyElement("Sign in", "Button")]));
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
        var userDataDirectory = CreateUserDataDirectory();
        var browserDescriptor = GetBrowserDescriptor(browser);

        var existingWindows = SnapshotBrowserWindows(browserDescriptor.ProcessName);

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

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start {browserDescriptor.DisplayName} for Chromium browser smoke tests.");
        }

        var windowHandle = WaitForWindow(process.Id, target.TitleFragment, existingWindows, browserDescriptor.ProcessName);
        var session = new ChromiumBrowserSession(process, windowHandle, browserDescriptor.ProcessName, userDataDirectory);
        session.BringToFront();
        WaitForPageReady(target, session.WindowHandleString);
        return session;
    }

    public void BringToFront()
    {
        AllowSetForegroundWindow(-1);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (SetForegroundWindow(WindowHandle))
            {
                Thread.Sleep(50);
                return;
            }

            Thread.Sleep(50);
        }
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
            EnsureBrowserExited();
            DeleteUserDataDirectory();
            _browserProcess.Dispose();
        }

    }

    private static string? FindBrowserExecutable(ChromiumBrowserKind browser)
    {
        var descriptor = GetBrowserDescriptor(browser);

        return descriptor.CandidatePaths.FirstOrDefault(File.Exists);
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

        var deadline = DateTime.UtcNow.AddSeconds(5);
        var enumerator = new WindowEnumerator(new ElevationDetector());
        while (DateTime.UtcNow < deadline)
        {
            var windows = enumerator.EnumerateWindowsAsync(cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!windows.Any(window => string.Equals(window.Handle, WindowHandleString, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    private void EnsureBrowserExited()
    {
        try
        {
            if (_browserProcess.HasExited)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        if (_browserProcess.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
        {
            return;
        }

        try
        {
            _browserProcess.Kill(entireProcessTree: true);
            _browserProcess.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds);
        }
        catch
        {
            // Best-effort cleanup in test code.
        }
    }

    private void DeleteUserDataDirectory()
    {
        if (string.IsNullOrWhiteSpace(_userDataDirectory) || !Directory.Exists(_userDataDirectory))
        {
            return;
        }

        var deadline = DateTime.UtcNow.Add(ProfileCleanupTimeout);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                Directory.Delete(_userDataDirectory, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
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
        string[] arguments =
        [
            "--new-window",
            $"--app=\"{target.Url}\"",
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

        var deadline = DateTime.UtcNow.Add(target.ReadyTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (IsReady(target, automationService, windowHandle))
            {
                Thread.Sleep(250);
                return;
            }

            if (TryDismissKnownPopup(automationService, windowHandle))
            {
                Thread.Sleep(500);
                continue;
            }

            Thread.Sleep(ReadyPollInterval);
        }

        throw new InvalidOperationException($"Timed out waiting for Chromium target '{target.Name}' to become ready without Edge first-run UI interference.");
    }

    private static bool IsReady(BrowserTarget target, UIAutomationService automationService, string windowHandle)
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

            if (!result.Success || result.Items is not { Length: > 0 })
            {
                return false;
            }
        }

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

    private static nint WaitForWindow(int processId, string titleFragment, HashSet<string> existingWindows, string browserProcessName)
    {
        var enumerator = new WindowEnumerator(new ElevationDetector());
        var deadline = DateTime.UtcNow.Add(LaunchTimeout);

        while (DateTime.UtcNow < deadline)
        {
            var windows = enumerator.EnumerateWindowsAsync(cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var directMatch = windows.FirstOrDefault(window =>
                window.ProcessId == processId &&
                string.Equals(window.ProcessName, browserProcessName, StringComparison.OrdinalIgnoreCase) &&
                window.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase));

            if (directMatch is not null && WindowHandleParser.TryParse(directMatch.Handle, out var handle) && handle != nint.Zero)
            {
                return handle;
            }

            var reusedProfileMatch = windows.FirstOrDefault(window =>
                string.Equals(window.ProcessName, browserProcessName, StringComparison.OrdinalIgnoreCase) &&
                window.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase) &&
                !existingWindows.Contains(window.Handle));

            if (reusedProfileMatch is not null && WindowHandleParser.TryParse(reusedProfileMatch.Handle, out handle) && handle != nint.Zero)
            {
                return handle;
            }

            Thread.Sleep(200);
        }

        throw new InvalidOperationException($"Timed out waiting for Chromium browser test window '{titleFragment}'.");
    }

    private sealed record BrowserTarget(
        string Name,
        string Url,
        string TitleFragment,
        TimeSpan ReadyTimeout,
        IReadOnlyList<ReadyElement> ReadyElements);
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
