using System.Diagnostics;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
public sealed class ChromiumBrowserSessionCleanupTests
{
    [SkippableFact]
    public void FailedReadinessCheck_ClosesLaunchedBrowserWindow()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Edge);
        var before = BrowserProcessIds();
        var launchedProcessId = 0;
        var observed = new Dictionary<int, DateTime>();

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForReadinessFailureTest(
                ChromiumBrowserKind.Edge,
                processId => launchedProcessId = processId,
                () => observed = CaptureOwnedProcesses(before, launchedProcessId)));

        AssertKnownProcessesExited(observed);
        AssertLaunchedBrowserProcessesExited(
            before,
            launchedProcessId,
            "A failed Chromium readiness check left a test-owned browser process running.");
    }

    [SkippableFact]
    public void FailedWindowDiscovery_ClosesLaunchedBrowserProcess()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Edge);
        var before = BrowserProcessIds();
        var launchedProcessId = 0;
        var observed = new Dictionary<int, DateTime>();

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForWindowFailureTest(
                ChromiumBrowserKind.Edge,
                processId => launchedProcessId = processId,
                () => observed = CaptureOwnedProcesses(before, launchedProcessId)));

        AssertKnownProcessesExited(observed);
        AssertLaunchedBrowserProcessesExited(
            before,
            launchedProcessId,
            "A failed Chromium window search left a test-owned browser process running.");
    }

    [Fact]
    public void IsTestOwnedProcess_RejectsProcessThatExistedBeforeLaunch()
    {
        HashSet<int> existingProcessIds = [42, 99];

        Assert.False(ChromiumBrowserSession.IsTestOwnedProcess(42, 43, existingProcessIds));
        Assert.True(ChromiumBrowserSession.IsTestOwnedProcess(43, 43, existingProcessIds));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cleanup_ParentExitsFirst_StillStopsPreviouslyOwnedChild(bool closeThrows)
    {
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(
            "$child = Start-Process powershell.exe -ArgumentList '-NoProfile -NonInteractive -Command Start-Sleep -Seconds 60' -PassThru; " +
            "Write-Output $child.Id; Start-Sleep -Seconds 60");
        using var parent = Process.Start(start)!;
        using var unrelated = Process.Start(new ProcessStartInfo("powershell.exe")
        {
            Arguments = "-NoProfile -NonInteractive -Command Start-Sleep -Seconds 60",
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        Process? child = null;
        try
        {
            var line = await parent.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            child = Process.GetProcessById(int.Parse(line!, System.Globalization.CultureInfo.InvariantCulture));
            _ = child.SafeHandle;
            void Cleanup() => ChromiumBrowserSession.CloseOwnedProcesses(() =>
            {
                parent.Kill();
                Assert.True(parent.WaitForExit(5000));
                if (closeThrows)
                {
                    throw new InvalidOperationException("Injected close failure");
                }
            }, parent);
            if (closeThrows)
            {
                var failure = Assert.Throws<AggregateException>(Cleanup);
                Assert.Equal("Injected close failure", Assert.Single(failure.InnerExceptions).Message);
            }
            else
            {
                Cleanup();
            }
            Assert.True(child.WaitForExit(1000), "Cleanup lost the child when its parent exited first.");
            Assert.False(unrelated.HasExited, "Cleanup terminated a process outside its owned tree.");
        }
        finally
        {
            if (!unrelated.HasExited)
            {
                unrelated.Kill();
                await unrelated.WaitForExitAsync();
            }
            if (!parent.HasExited)
            {
                parent.Kill(entireProcessTree: true);
                await parent.WaitForExitAsync();
            }
            if (child is not null)
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync();
                }
                child.Dispose();
            }
        }
    }

    private static void AssertLaunchedBrowserProcessesExited(
        HashSet<int> before,
        int launchedProcessId,
        string failureMessage)
    {
        Assert.NotEqual(0, launchedProcessId);
        var restored = TestWait.Until(
            condition: () => BrowserProcessIds().All(processId =>
                !ChromiumBrowserSession.IsTestOwnedProcess(
                    processId,
                    launchedProcessId,
                    before)),
            timeout: TimeSpan.FromSeconds(10),
            pollInterval: TimeSpan.FromMilliseconds(200));

        Assert.True(restored, failureMessage);
    }

    private static HashSet<int> BrowserProcessIds()
    {
        var processes = Process.GetProcessesByName("msedge");
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

    private static Dictionary<int, DateTime> CaptureOwnedProcesses(HashSet<int> before, int launchedProcessId)
    {
        var observed = new Dictionary<int, DateTime>();
        foreach (var id in BrowserProcessIds().Where(id =>
            ChromiumBrowserSession.IsTestOwnedProcess(id, launchedProcessId, before)))
        {
            try
            {
                using var process = Process.GetProcessById(id);
                observed.Add(id, process.StartTime);
            }
            catch (ArgumentException)
            {
                // The process exited between enumeration and opening its handle.
            }
        }
        return observed;
    }

    private static void AssertKnownProcessesExited(Dictionary<int, DateTime> observed)
    {
        Assert.NotEmpty(observed);
        var remaining = new List<int>();
        var exited = TestWait.Until(() =>
        {
            remaining.Clear();
            foreach (var (id, started) in observed)
            {
                try
                {
                    using var process = Process.GetProcessById(id);
                    if (!process.HasExited && process.StartTime == started)
                    {
                        remaining.Add(id);
                    }
                }
                catch (ArgumentException)
                {
                    // A disappeared PID is the expected result of cleanup.
                }
            }
            return remaining.Count == 0;
        }, TimeSpan.FromSeconds(10));
        Assert.True(exited, $"Previously observed browser processes survived cleanup: {string.Join(", ", remaining)}");
    }
}
