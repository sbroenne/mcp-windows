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

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForReadinessFailureTest(
                ChromiumBrowserKind.Edge,
                processId => launchedProcessId = processId));

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

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForWindowFailureTest(
                ChromiumBrowserKind.Edge,
                processId => launchedProcessId = processId));

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
}
