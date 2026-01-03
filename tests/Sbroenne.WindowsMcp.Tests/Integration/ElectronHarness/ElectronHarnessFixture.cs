using System.Diagnostics;
using System.Runtime.InteropServices;

using Sbroenne.WindowsMcp.Native;

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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? lpClassName, string lpWindowName);

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
        // The path is: tests/Sbroenne.WindowsMcp.Tests/bin/Debug/net10.0-windows.../Sbroenne.WindowsMcp.Tests.dll
        // We need: tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness
        // Navigate: assembly dir -> net10.0 (..) -> Debug (..) -> bin (..) -> Sbroenne.WindowsMcp.Tests (project root)
        var projectDir = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", ".."));
        _electronHarnessPath = Path.Combine(projectDir, "Integration", "ElectronHarness");

        if (!Directory.Exists(_electronHarnessPath))
        {
            throw new InvalidOperationException($"Electron harness not found at: {_electronHarnessPath}");
        }

        // Ensure npm packages are installed
        EnsureNodeModulesInstalled();

        // Start the Electron app
        StartElectronApp();

        // Wait for the window to appear
        WaitForWindow();
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

        // Use "." as argument and set working directory - same as `electron .` from command line
        _electronProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = electronExePath,
                Arguments = ".",
                WorkingDirectory = _electronHarnessPath,
                UseShellExecute = false, // Need false to redirect output
                CreateNoWindow = false, // Allow window to be created
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        // Set environment variable for Electron logging
        _electronProcess.StartInfo.EnvironmentVariables["ELECTRON_ENABLE_LOGGING"] = "1";

        // CRITICAL: Unset ELECTRON_RUN_AS_NODE which causes Electron to run as plain Node.js
        // VS Code and other Electron-based IDEs may set this in the environment
        _electronProcess.StartInfo.EnvironmentVariables.Remove("ELECTRON_RUN_AS_NODE");

        _electronProcess.Start();
    }

    private void WaitForWindow()
    {
        var deadline = DateTime.UtcNow.AddSeconds(MAX_WAIT_SECONDS);

        while (DateTime.UtcNow < deadline)
        {
            _windowHandle = FindWindow(null, ELECTRON_HARNESS_TITLE);
            if (_windowHandle != nint.Zero)
            {
                // Give the window time to fully initialize (Chromium UIA tree needs extra time)
                Thread.Sleep(1000);
                return;
            }

            // Check if process exited unexpectedly
            if (_electronProcess?.HasExited == true)
            {
                var error = _electronProcess.StandardError.ReadToEnd();
                var output = _electronProcess.StandardOutput.ReadToEnd();
                throw new InvalidOperationException($"Electron process exited unexpectedly (exit code {_electronProcess.ExitCode}):\nStderr: {error}\nStdout: {output}");
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException($"Electron harness window did not appear within {MAX_WAIT_SECONDS} seconds");
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

        AllowSetForegroundWindow(-1);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            SetForegroundWindow(_windowHandle);
            Thread.Sleep(50);

            if (GetForegroundWindow() == _windowHandle)
            {
                return;
            }
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
            if (_electronProcess is { HasExited: false })
            {
                // Try graceful shutdown first
                _electronProcess.CloseMainWindow();

                if (!_electronProcess.WaitForExit(TimeSpan.FromSeconds(5)))
                {
                    _electronProcess.Kill(entireProcessTree: true);
                }
            }

            _electronProcess?.Dispose();
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
