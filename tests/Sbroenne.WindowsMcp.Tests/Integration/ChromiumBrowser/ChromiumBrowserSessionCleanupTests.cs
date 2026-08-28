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

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForReadinessFailureTest(
                ChromiumBrowserKind.Edge));

        AssertBrowserProcessesRestored(
            before,
            "A failed Chromium readiness check changed the pre-existing browser processes.");
    }

    [SkippableFact]
    public void FailedWindowDiscovery_ClosesLaunchedBrowserProcess()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Edge);
        var before = BrowserProcessIds();

        Assert.Throws<InvalidOperationException>(
            () => ChromiumBrowserSession.LaunchLocalPageForWindowFailureTest(
                ChromiumBrowserKind.Edge));

        AssertBrowserProcessesRestored(
            before,
            "A failed Chromium window search changed the pre-existing browser processes.");
    }

    [Fact]
    public void IsTestOwnedProcess_RejectsProcessThatExistedBeforeLaunch()
    {
        HashSet<int> existingProcessIds = [42, 99];

        Assert.False(ChromiumBrowserSession.IsTestOwnedProcess(42, 43, existingProcessIds));
        Assert.True(ChromiumBrowserSession.IsTestOwnedProcess(43, 43, existingProcessIds));
    }

    private static void AssertBrowserProcessesRestored(
        HashSet<int> before,
        string failureMessage)
    {
        var restored = TestWait.Until(
            condition: () => BrowserProcessIds().SetEquals(before),
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
