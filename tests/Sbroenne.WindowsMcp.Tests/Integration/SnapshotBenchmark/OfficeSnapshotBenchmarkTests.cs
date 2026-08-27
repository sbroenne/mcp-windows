using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

[Collection("UIAutomation")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "RequiresOffice")]
[Trait("Category", "SnapshotBenchmark")]
public sealed class OfficeSnapshotBenchmarkTests : IDisposable
{
    private readonly UIAutomationThread _staThread = new();
    private readonly KeyboardInputService _keyboard = new();
    private readonly UIAutomationService _automationService;
    private readonly ITestOutputHelper _output;

    public OfficeSnapshotBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
        _automationService = new UIAutomationService(
            _staThread,
            new MonitorService(),
            new MouseInputService(),
            _keyboard,
            new WindowActivator(),
            new ElevationDetector(),
            NullLogger<UIAutomationService>.Instance);
    }

    [SkippableTheory]
    [InlineData(OfficeApplication.Word)]
    [InlineData(OfficeApplication.Excel)]
    public async Task Benchmark_RealOfficeWorkflow(OfficeApplication application)
    {
        var executable = FindOfficeExecutable(application);
        Skip.If(executable is null, $"{application} desktop is not installed.");

        var result = await SnapshotBenchmarkRunner.RunAsync(
            $"Microsoft {application}",
            (arm, sample) => CreateScenarioAsync(application, executable!, arm, sample));

        _output.WriteLine(SnapshotBenchmarkRunner.FormatReport(result));
    }

    public void Dispose()
    {
        _automationService.Dispose();
        _keyboard.Dispose();
        _staThread.Dispose();
    }

    private async Task<SnapshotBenchmarkScenario> CreateScenarioAsync(
        OfficeApplication application,
        string executable,
        SnapshotBenchmarkArm arm,
        int sample)
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-windows-snapshot-{application.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}" +
            (application == OfficeApplication.Word ? ".rtf" : ".csv"));
        File.WriteAllText(
            tempPath,
            application == OfficeApplication.Word
                ? @"{\rtf1\ansi Snapshot benchmark document\par}"
                : "Metric,Value\r\nBaseline,100\r\n");

        Process? process = null;
        var windowHandle = nint.Zero;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = (application == OfficeApplication.Word ? "/w " : "/x ") + $"\"{tempPath}\"",
                UseShellExecute = false
            }) ?? throw new InvalidOperationException($"Could not launch Microsoft {application}.");

            windowHandle = WaitForMainWindow(process, TimeSpan.FromSeconds(30));
            var handle = WindowHandleParser.Format(windowHandle);
            var version = FileVersionInfo.GetVersionInfo(executable).FileVersion ?? "unknown";
            var activated = await new WindowActivator().ActivateWindowAsync(windowHandle);
            Assert.True(activated, $"Could not activate Microsoft {application}.");
            await Task.Delay(3000);

            IReadOnlyList<Func<CancellationToken, Task>> actions = application switch
            {
                OfficeApplication.Word =>
                [
                    token => TypeInWordAsync(handle, "Incremental snapshot benchmark", token),
                    token => TypeInWordAsync(handle, "\nMeasured against a real Word document.", token),
                    token => UndoAsync(handle, token),
                    token => TypeInWordAsync(handle, "\nFinal benchmark paragraph.", token)
                ],
                OfficeApplication.Excel =>
                [
                    token => TypeInExcelAsync("Revenue", token),
                    token => TypeInExcelAsync("125000", token),
                    token => TypeInExcelAsync("Expenses", token),
                    token => TypeInExcelAsync("75000", token)
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(application), application, null)
            };

            return new SnapshotBenchmarkScenario(
                $"Microsoft {application}",
                handle,
                _automationService,
                actions,
                $"{application} {version}; Windows {Environment.OSVersion.Version}",
                () =>
                {
                    CloseDedicatedOfficeProcess(process, windowHandle);
                    File.Delete(tempPath);
                    return ValueTask.CompletedTask;
                },
                MaxDepth: 3,
                CurrentWindowHandle: () =>
                {
                    process.Refresh();
                    return WindowHandleParser.Format(process.MainWindowHandle);
                });
        }
        catch
        {
            if (process is not null)
            {
                CloseDedicatedOfficeProcess(process, windowHandle);
            }

            File.Delete(tempPath);
            throw;
        }
    }

    private async Task TypeInWordAsync(
        string windowHandle,
        string text,
        CancellationToken cancellationToken)
    {
        var result = await _keyboard.TypeTextAsync(text, cancellationToken);
        Assert.True(result.Success, $"Typing in Word failed: {result.Error}");
    }

    private async Task TypeInExcelAsync(string text, CancellationToken cancellationToken)
    {
        var typeResult = await _keyboard.TypeTextAsync(text, cancellationToken);
        Assert.True(typeResult.Success, $"Typing in Excel failed: {typeResult.Error}");
        var enterResult = await _keyboard.PressKeyAsync(
            "enter",
            ModifierKey.None,
            repeat: 1,
            cancellationToken);
        Assert.True(enterResult.Success, $"Committing the Excel cell failed: {enterResult.Error}");
    }

    private async Task UndoAsync(
        string windowHandle,
        CancellationToken cancellationToken)
    {
        var result = await _keyboard.PressKeyAsync(
            "z",
            ModifierKey.Ctrl,
            repeat: 1,
            cancellationToken);
        Assert.True(result.Success, $"Undo in Word failed: {result.Error}");
    }

    private static nint WaitForMainWindow(Process process, TimeSpan timeout)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < timeout)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Office process {process.Id} exited before creating a window.");
            }

            process.Refresh();
            if (process.MainWindowHandle != nint.Zero &&
                NativeMethods.IsWindowVisible(process.MainWindowHandle))
            {
                return process.MainWindowHandle;
            }

            Thread.Sleep(200);
        }

        throw new TimeoutException(
            $"Office process {process.Id} did not create a visible window within {timeout.TotalSeconds:F0}s.");
    }

    private static string? FindOfficeExecutable(OfficeApplication application)
    {
        var executable = application == OfficeApplication.Word ? "WINWORD.EXE" : "EXCEL.EXE";
        var registryPaths = new[]
        {
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executable}",
            $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\{executable}"
        };

        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var registryPath in registryPaths)
            {
                using var key = hive.OpenSubKey(registryPath);
                if (key?.GetValue(null) is string path && File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private static void CloseDedicatedOfficeProcess(Process process, nint windowHandle)
    {
        try
        {
            if (!process.HasExited)
            {
                _ = PostMessage(windowHandle, WmClose, nint.Zero, nint.Zero);
                if (!process.WaitForExit(TimeSpan.FromSeconds(2)))
                {
                    process.Kill(entireProcessTree: true);
                    _ = process.WaitForExit(TimeSpan.FromSeconds(5));
                }
            }
        }
        finally
        {
            process.Dispose();
        }
    }

    private const uint WmClose = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}

public enum OfficeApplication
{
    Word,
    Excel
}
