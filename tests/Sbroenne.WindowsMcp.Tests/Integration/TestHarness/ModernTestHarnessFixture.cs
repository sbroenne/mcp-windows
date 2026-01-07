using System.Diagnostics;
using System.Runtime.InteropServices;

using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// xUnit fixture that manages the WinUI 3 Modern Test Harness.
/// Unlike the WinForms harness which runs in-process, this launches a separate .exe
/// and communicates via UI Automation to verify the harness is ready.
/// </summary>
public sealed class ModernTestHarnessFixture : IDisposable
{
    private const int ASFW_ANY = -1;
    private const string HarnessWindowTitle = "MCP Windows Modern Test Harness";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? lpClassName, string lpWindowName);

    private Process? _harnessProcess;
    private nint _windowHandle;
    private bool _disposed;

    /// <summary>
    /// Gets whether the harness is ready for use.
    /// </summary>
    public bool IsReady => _windowHandle != nint.Zero && _harnessProcess != null && !_harnessProcess.HasExited;

    /// <summary>
    /// Gets the window handle of the modern test harness.
    /// </summary>
    public nint TestWindowHandle => _windowHandle;

    /// <summary>
    /// Gets the window handle as a decimal string.
    /// </summary>
    public string TestWindowHandleString => WindowHandleParser.Format(_windowHandle);

    /// <summary>
    /// Gets the path to the harness executable.
    /// </summary>
    public string HarnessPath { get; }

    public ModernTestHarnessFixture()
    {
        // Find the harness executable relative to the test assembly
        HarnessPath = FindHarnessExecutable();

        if (!File.Exists(HarnessPath))
        {
            throw new FileNotFoundException(
                $"Modern test harness not found at: {HarnessPath}. " +
                "Build the Sbroenne.WindowsMcp.ModernHarness project first.",
                HarnessPath);
        }

        LaunchHarness();
    }

    private static string FindHarnessExecutable()
    {
        // The harness should be built alongside the test project
        // Look for it in common locations relative to the test assembly
        var testDir = Path.GetDirectoryName(typeof(ModernTestHarnessFixture).Assembly.Location)!;

        // Expected: tests/Sbroenne.WindowsMcp.Tests/bin/Debug/net10.0-windows.../
        // Harness:  tests/Sbroenne.WindowsMcp.ModernHarness/bin/Debug/net10.0-windows.../

        // Try sibling project path
        var parentDir = Path.GetDirectoryName(testDir);
        while (parentDir != null)
        {
            // Check if we're in a bin folder
            if (Path.GetFileName(parentDir)?.Equals("bin", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Go up to project folder, then to sibling project
                var projectDir = Path.GetDirectoryName(parentDir);
                var testsDir = Path.GetDirectoryName(projectDir);
                if (testsDir != null)
                {
                    var harnessProjectDir = Path.Combine(testsDir, "Sbroenne.WindowsMcp.ModernHarness");
                    var configDir = Path.GetFileName(Path.GetDirectoryName(testDir)); // e.g., net10.0-windows...
                    var buildConfig = Path.GetFileName(parentDir) == "bin"
                        ? Path.GetFileName(testDir.Replace(parentDir + Path.DirectorySeparatorChar, "").Split(Path.DirectorySeparatorChar)[0])
                        : "Debug";

                    // Find matching output directory
                    var harnessOutputBase = Path.Combine(harnessProjectDir, "bin");
                    if (Directory.Exists(harnessOutputBase))
                    {
                        // Look for the harness exe in any config/tfm combination
                        var harnessExes = Directory.GetFiles(harnessOutputBase, "Sbroenne.WindowsMcp.ModernHarness.exe", SearchOption.AllDirectories);
                        if (harnessExes.Length > 0)
                        {
                            // Prefer matching configuration
                            var preferred = harnessExes.FirstOrDefault(p => p.Contains(buildConfig, StringComparison.OrdinalIgnoreCase))
                                          ?? harnessExes[0];
                            return preferred;
                        }
                    }
                }
                break;
            }
            parentDir = Path.GetDirectoryName(parentDir);
        }

        // Fallback: assume it's in the same directory as the test assembly
        return Path.Combine(testDir, "Sbroenne.WindowsMcp.ModernHarness.exe");
    }

    private void LaunchHarness()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = HarnessPath,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        _harnessProcess = Process.Start(startInfo);

        if (_harnessProcess == null)
        {
            throw new InvalidOperationException($"Failed to start modern test harness: {HarnessPath}");
        }

        // Wait for the window to appear
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(30); // WinUI apps can take longer to start

        while (stopwatch.Elapsed < timeout)
        {
            _windowHandle = FindWindow(null, HarnessWindowTitle);
            if (_windowHandle != nint.Zero)
            {
                // Give the window a moment to fully initialize
                Thread.Sleep(500);
                return;
            }

            if (_harnessProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Modern test harness exited with code {_harnessProcess.ExitCode}");
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException(
            $"Modern test harness window did not appear within {timeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Brings the harness window to the foreground.
    /// </summary>
    public void BringToFront()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        const int maxRetries = 3;
        const int delayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            AllowSetForegroundWindow(ASFW_ANY);
            SetForegroundWindow(_windowHandle);
            Thread.Sleep(delayMs);

            if (GetForegroundWindow() == _windowHandle)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Closes and restarts the harness to reset state.
    /// </summary>
    public void Reset()
    {
        // For the modern harness, we need to communicate via UI Automation
        // or simply restart the process. For now, restart is simpler.
        CloseHarness();
        LaunchHarness();
    }

    private void CloseHarness()
    {
        if (_harnessProcess != null && !_harnessProcess.HasExited)
        {
            try
            {
                _harnessProcess.CloseMainWindow();
                if (!_harnessProcess.WaitForExit(5000))
                {
                    _harnessProcess.Kill();
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        _harnessProcess?.Dispose();
        _harnessProcess = null;
        _windowHandle = nint.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseHarness();
    }
}

/// <summary>
/// Collection definition for tests that use the modern test harness.
/// Parallelization is disabled to avoid competing for foreground window.
/// </summary>
[CollectionDefinition("ModernTestHarness", DisableParallelization = true)]
public class ModernTestHarnessTestDefinition : ICollectionFixture<ModernTestHarnessFixture>
{
}
