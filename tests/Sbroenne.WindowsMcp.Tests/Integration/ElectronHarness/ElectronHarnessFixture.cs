using System.Diagnostics;
using System.Runtime.InteropServices;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Native;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;

/// <summary>
/// xUnit fixture that manages an Electron test harness window.
/// Launches the Electron app and provides its window handle for UI Automation testing.
/// </summary>
public sealed class ElectronHarnessFixture : IDisposable
{
    private const string ELECTRON_HARNESS_TITLE = "MCP Electron Test Harness";
    private const int MAX_WAIT_SECONDS = 30;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    private const uint WM_CLOSE = 0x0010;

    private Process? _electronProcess;
    private nint _windowHandle;
    private bool _disposed;
    private readonly string _electronHarnessPath;

    /// <summary>
    /// Gets the window handle of the Electron test harness.
    /// </summary>
    public nint WindowHandle => _windowHandle;

    /// <summary>
    /// Gets the window handle of the Electron test harness as a decimal string.
    /// </summary>
    public string WindowHandleString => WindowHandleParser.Format(WindowHandle);

    /// <summary>
    /// Gets whether the Electron harness is running and ready.
    /// </summary>
    public bool IsReady => _windowHandle != nint.Zero && _electronProcess is { HasExited: false };

    /// <summary>
    /// Gets the process ID of the Electron app.
    /// </summary>
    public int? ProcessId => _electronProcess?.Id;

    public ElectronHarnessFixture()
    {
        // Find the Electron harness directory relative to the test assembly
        var testAssemblyDir = Path.GetDirectoryName(typeof(ElectronHarnessFixture).Assembly.Location)
            ?? throw new InvalidOperationException("Could not determine test assembly location");

        // Navigate up to find the ElectronHarness folder
        // The path could be:
        //   tests/Sbroenne.WindowsMcp.Tests/bin/Debug/net10.0-windows.../  (AnyCPU)
        //   tests/Sbroenne.WindowsMcp.Tests/bin/ARM64/Debug/net10.0-windows.../  (ARM64)
        //   tests/Sbroenne.WindowsMcp.Tests/bin/x64/Debug/net10.0-windows.../  (x64)
        // We need: tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness
        // Navigate up until we find the project directory containing Integration folder
        var currentDir = testAssemblyDir;
        string? projectDir = null;

        for (int i = 0; i < 6; i++)
        {
            currentDir = Path.GetDirectoryName(currentDir);
            if (currentDir == null)
            {
                break;
            }

            var integrationPath = Path.Combine(currentDir, "Integration", "ElectronHarness");
            if (Directory.Exists(integrationPath))
            {
                projectDir = currentDir;
                break;
            }
        }

        if (projectDir == null)
        {
            throw new InvalidOperationException($"Could not find Integration/ElectronHarness folder starting from: {testAssemblyDir}");
        }

        _electronHarnessPath = Path.Combine(projectDir, "Integration", "ElectronHarness");

        if (!Directory.Exists(_electronHarnessPath))
        {
            throw new InvalidOperationException($"Electron harness not found at: {_electronHarnessPath}");
        }

        // Ensure npm packages are installed
        EnsureNodeModulesInstalled();

        try
        {
            StartElectronApp();
            WaitForWindow();
        }
        catch
        {
            CloseElectronApp();
            throw;
        }
    }

    private void EnsureNodeModulesInstalled()
    {
        var nodeModulesPath = Path.Combine(_electronHarnessPath, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            // Run npm install (use cmd.exe /c to find npm on Windows)
            var npmProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm install",
                    WorkingDirectory = _electronHarnessPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            npmProcess.Start();
            if (!npmProcess.WaitForExit(TimeSpan.FromMinutes(2)))
            {
                npmProcess.Kill();
                throw new InvalidOperationException("npm install timed out");
            }

            if (npmProcess.ExitCode != 0)
            {
                var error = npmProcess.StandardError.ReadToEnd();
                throw new InvalidOperationException($"npm install failed: {error}");
            }
        }

        // Run npm run build for TypeScript compilation
        var distPath = Path.Combine(_electronHarnessPath, "dist");
        var mainJsPath = Path.Combine(distPath, "main.js");

        // Check if we need to build (either no dist or source is newer)
        var srcPath = Path.Combine(_electronHarnessPath, "src");
        var needsBuild = !File.Exists(mainJsPath);

        if (!needsBuild && Directory.Exists(srcPath))
        {
            var srcLastWrite = Directory.GetFiles(srcPath, "*.ts").Max(f => File.GetLastWriteTimeUtc(f));
            var distLastWrite = File.GetLastWriteTimeUtc(mainJsPath);
            needsBuild = srcLastWrite > distLastWrite;
        }

        if (needsBuild)
        {
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm run build",
                    WorkingDirectory = _electronHarnessPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            buildProcess.Start();
            if (!buildProcess.WaitForExit(TimeSpan.FromMinutes(1)))
            {
                buildProcess.Kill();
                throw new InvalidOperationException("npm run build timed out");
            }

            if (buildProcess.ExitCode != 0)
            {
                var error = buildProcess.StandardError.ReadToEnd();
                var output = buildProcess.StandardOutput.ReadToEnd();
                throw new InvalidOperationException($"npm run build failed: {error}\n{output}");
            }
        }
    }

    private void StartElectronApp()
    {
        // Allow any process to set foreground window (needed for tests)
        AllowSetForegroundWindow(-1);

        // Run electron.exe directly rather than via npm start
        var electronExePath = Path.Combine(_electronHarnessPath, "node_modules", "electron", "dist", "electron.exe");

        if (!File.Exists(electronExePath))
        {
            throw new InvalidOperationException($"Electron executable not found at: {electronExePath}");
        }

        // Remove orphans from earlier runs before starting, so they cannot compete for the
        // foreground or outlive this fixture.
        KillStaleHarnessProcesses(electronExePath);

        // Use "." as argument and set working directory - same as `electron .` from command line
        _electronProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = electronExePath,
                Arguments = ".",
                WorkingDirectory = _electronHarnessPath,
                UseShellExecute = true, // Use shell execute for proper accessibility tree initialization
                CreateNoWindow = false, // Allow window to be created
            }
        };

        // Note: With UseShellExecute = true, we cannot redirect output or set environment variables
        // This is intentional - Chromium needs proper shell environment for accessibility support

        _electronProcess.Start();
    }

    private void WaitForWindow()
    {
        var electronProcessId = _electronProcess?.Id
            ?? throw new InvalidOperationException("Electron process was not started.");

        var appeared = TestWait.Until(
            condition: () =>
            {
                if (_electronProcess?.HasExited == true)
                {
                    throw new InvalidOperationException(
                        $"Electron process exited unexpectedly (exit code {_electronProcess.ExitCode})");
                }

                _windowHandle = FindHarnessWindow(electronProcessId);

                return _windowHandle != nint.Zero;
            },
            timeout: TimeSpan.FromSeconds(MAX_WAIT_SECONDS),
            pollInterval: TimeSpan.FromMilliseconds(100));

        if (!appeared)
        {
            throw new InvalidOperationException(
                $"Electron harness window did not appear within {MAX_WAIT_SECONDS} seconds");
        }

        // Note: readiness is bimodal in practice - the tree is exposed within a second or two, or
        // not at all - so a longer budget only slows failures down.
        var automationReady = TestWait.Until(
            condition: IsAutomationTreeReady,
            timeout: TimeSpan.FromSeconds(10),
            pollInterval: TimeSpan.FromMilliseconds(100));
        if (!automationReady)
        {
            var windows = string.Join(
                ", ",
                GetProcessWindows(electronProcessId).Select(h => $"{h}:'{GetWindowTitle(h)}'"));

            throw new TimeoutException(
                "Electron harness UI Automation tree did not become ready. " +
                $"Process {electronProcessId} owns window {_windowHandle}; visible windows: [{windows}].");
        }
    }

    /// <summary>
    /// Finds the harness window belonging to <paramref name="processId"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>FindWindow(null, title)</c>. That searches every top-level window on the
    /// desktop, so an orphaned harness left behind by an earlier run wins over the process this
    /// fixture just started. UI Automation then runs against a window the fixture does not own and
    /// never becomes ready, the fixture constructor throws, and xUnit fails every test in the
    /// collection instantly (issue #195).
    /// </remarks>
    private static nint FindHarnessWindow(int processId) =>
        GetProcessWindows(processId, ELECTRON_HARNESS_TITLE).FirstOrDefault();

    /// <summary>
    /// Gets the visible top-level windows owned by <paramref name="processId"/>, optionally
    /// filtered to an exact window title.
    /// </summary>
    private static List<nint> GetProcessWindows(int processId, string? title = null)
    {
        var windows = new List<nint>();

        NativeMethods.EnumWindows(
            (hWnd, _) =>
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out var owningProcessId);
                if (owningProcessId != (uint)processId || !NativeMethods.IsWindowVisible(hWnd))
                {
                    return true;
                }

                if (title == null || GetWindowTitle(hWnd) == title)
                {
                    windows.Add(hWnd);
                }

                return true;
            },
            nint.Zero);

        return windows;
    }

    private static string GetWindowTitle(nint hWnd)
    {
        var buffer = new char[512];
        var length = NativeMethods.GetWindowText(hWnd, buffer, buffer.Length);

        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    /// <summary>
    /// Kills harness processes left behind by an earlier run before starting a new one.
    /// </summary>
    /// <remarks>
    /// Scoped to processes started from this repository's own Electron executable, so a developer's
    /// unrelated Electron applications are never touched. Orphans accumulate because a failed run
    /// can close the window while its process tree survives, and they then compete for the
    /// foreground and for the harness window title.
    /// </remarks>
    private static void KillStaleHarnessProcesses(string electronExePath)
    {
        foreach (var process in Process.GetProcessesByName("electron"))
        {
            try
            {
                if (string.Equals(process.MainModule?.FileName, electronExePath, StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // MainModule throws for processes we cannot open, and the process may exit while
                // we look at it. Either way there is nothing useful to do.
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    /// <summary>
    /// Brings the Electron harness window to the foreground.
    /// </summary>
    public void BringToFront()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        TestWait.RetryUntil(
            attempt: () =>
            {
                AllowSetForegroundWindow(-1);
                SetForegroundWindow(_windowHandle);
            },
            condition: () => GetForegroundWindow() == _windowHandle,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    /// Dismisses any open modal dialogs (Save As, etc.) by sending WM_CLOSE.
    /// </summary>
    /// <remarks>
    /// Scoped to windows owned by the harness process. A global <c>FindWindow(null, "Save")</c>
    /// would match any window on the desktop with that title, including one belonging to an
    /// unrelated application.
    /// </remarks>
    public void DismissDialogs()
    {
        if (_electronProcess is not { HasExited: false })
        {
            return;
        }

        var processId = _electronProcess.Id;
        string[] dialogTitles = ["Save As", "Save as", "Save"];

        foreach (var title in dialogTitles)
        {
            foreach (var dialogHwnd in GetProcessWindows(processId, title))
            {
                PostMessage(dialogHwnd, WM_CLOSE, nint.Zero, nint.Zero);
                TestWait.Until(
                    () => !NativeMethods.IsWindow(dialogHwnd) || !NativeMethods.IsWindowVisible(dialogHwnd),
                    timeout: TimeSpan.FromSeconds(2));
            }
        }
    }

    private bool IsAutomationTreeReady()
    {
        try
        {
            return FindNavigationButton() != null;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the fixture state between tests.
    /// Dismisses any leftover dialogs and brings the main window to front.
    /// </summary>
    public void Reset()
    {
        DismissDialogs();
        BringToFront();

        var restored = TestWait.RetryUntil(
            attempt: RestoreInitialViewport,
            condition: IsInitialViewportVisible,
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(50));
        if (!restored)
        {
            throw new TimeoutException("Electron harness did not restore its initial viewport.");
        }
    }

    private void RestoreInitialViewport()
    {
        FindNavigationButton()?.TryScrollIntoView();
    }

    private bool IsInitialViewportVisible()
    {
        var navigationButton = FindNavigationButton();
        return navigationButton != null && !navigationButton.IsOffscreen();
    }

    private UIA.IUIAutomationElement? FindNavigationButton()
    {
        var automation = UIA3Automation.Instance;
        var root = automation.ElementFromHandle(_windowHandle);
        var condition = automation.CreatePropertyCondition(
            UIA3PropertyIds.Name,
            "Navigate Home");
        return root?.FindFirst(UIA.TreeScope.TreeScope_Descendants, condition);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseElectronApp();
    }

    private void CloseElectronApp()
    {
        try
        {
            // Dismiss any leftover dialogs before closing
            DismissDialogs();

            if (_electronProcess is { HasExited: false })
            {
                // Use PostMessage(WM_CLOSE) on the known window handle — more reliable than
                // CloseMainWindow() which may be a no-op when UseShellExecute=true.
                if (_windowHandle != nint.Zero)
                {
                    PostMessage(_windowHandle, WM_CLOSE, nint.Zero, nint.Zero);
                }

                if (!_electronProcess.WaitForExit(TimeSpan.FromSeconds(5)))
                {
                    _electronProcess.Kill(entireProcessTree: true);
                }
            }

            _electronProcess?.Dispose();
            _electronProcess = null;
            _windowHandle = nint.Zero;
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}

/// <summary>
/// Collection definition for tests that use the Electron harness.
/// Parallelization is disabled to avoid competing for foreground window and input focus.
/// </summary>
[CollectionDefinition("ElectronHarness", DisableParallelization = true)]
public class ElectronHarnessTestDefinition : ICollectionFixture<ElectronHarnessFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
