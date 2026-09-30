using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
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
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
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
            _loggerFactory.CreateLogger<UIAutomationService>());
    }

    [SkippableTheory]
    [InlineData(OfficeApplication.Word)]
    [InlineData(OfficeApplication.Excel)]
    public async Task Snapshot_AfterOfficeEdit_KeepsDocumentAvailable(OfficeApplication application)
    {
        var executable = FindOfficeExecutable(application);
        Skip.If(executable is null, $"{application} desktop is not installed.");
        await using var scenario = await CreateScenarioAsync(
            application, executable!, SnapshotBenchmarkArm.Full, 1);
        foreach (var action in scenario.Actions)
        {
            await action(CancellationToken.None);
            var snapshot = await _automationService.GetTreeAsync(
                scenario.CurrentWindowHandle!(), null, scenario.MaxDepth, null);
            Assert.True(snapshot.Success, snapshot.ErrorMessage);
            Assert.NotEmpty(snapshot.Tree!);
        }
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
        _loggerFactory.Dispose();
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

            windowHandle = WaitForMainWindow(
                process, Path.GetFileNameWithoutExtension(tempPath), TimeSpan.FromSeconds(30));
            var handle = WindowHandleParser.Format(windowHandle);
            var version = FileVersionInfo.GetVersionInfo(executable).FileVersion ?? "unknown";
            var activated = await new WindowActivator().ActivateWindowAsync(windowHandle);
            Assert.True(activated, $"Could not activate Microsoft {application}.");

            IReadOnlyList<Func<CancellationToken, Task>> actions = application switch
            {
                OfficeApplication.Word =>
                [
                    token => TypeInWordAsync(windowHandle, "Incremental snapshot benchmark", token),
                    token => TypeInWordAsync(windowHandle, "\nMeasured against a real Word document.", token),
                    token => UndoAsync(windowHandle, token),
                    token => TypeInWordAsync(windowHandle, "\nFinal benchmark paragraph.", token)
                ],
                OfficeApplication.Excel =>
                [
                    token => TypeInExcelAsync(windowHandle, "Revenue", token),
                    token => TypeInExcelAsync(windowHandle, "125000", token),
                    token => TypeInExcelAsync(windowHandle, "Expenses", token),
                    token => TypeInExcelAsync(windowHandle, "75000", token)
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
                    _output.WriteLine(
                        $"{application} PID {process.Id}, HWND {process.MainWindowHandle}, title '{process.MainWindowTitle}'");
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
        nint windowHandle,
        string text,
        CancellationToken cancellationToken)
    {
        var result = await _keyboard.TypeTextAsync(text, windowHandle, cancellationToken);
        Assert.True(result.Success, $"Typing in Word failed: {result.Error}");
    }

    private async Task TypeInExcelAsync(nint windowHandle, string text, CancellationToken cancellationToken)
    {
        var typeResult = await _keyboard.TypeTextAsync(text, windowHandle, cancellationToken);
        Assert.True(typeResult.Success, $"Typing in Excel failed: {typeResult.Error}");
        var enterResult = await _keyboard.PressKeyAsync(
            "enter",
            ModifierKey.None,
            repeat: 1,
            windowHandle,
            cancellationToken);
        Assert.True(enterResult.Success, $"Committing the Excel cell failed: {enterResult.Error}");
    }

    private async Task UndoAsync(
        nint windowHandle,
        CancellationToken cancellationToken)
    {
        var result = await _keyboard.PressKeyAsync(
            "z",
            ModifierKey.Ctrl,
            repeat: 1,
            windowHandle,
            cancellationToken);
        Assert.True(result.Success, $"Undo in Word failed: {result.Error}");
    }

    private static nint WaitForMainWindow(Process process, string documentName, TimeSpan timeout)
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
                NativeMethods.IsWindowVisible(process.MainWindowHandle) &&
                process.MainWindowTitle.Contains(documentName, StringComparison.OrdinalIgnoreCase))
            {
                return process.MainWindowHandle;
            }

            Thread.Sleep(200);
        }

        throw new TimeoutException(
            $"Office process {process.Id} did not open '{documentName}' within {timeout.TotalSeconds:F0}s. " +
            $"Current title: '{process.MainWindowTitle}'.");
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
