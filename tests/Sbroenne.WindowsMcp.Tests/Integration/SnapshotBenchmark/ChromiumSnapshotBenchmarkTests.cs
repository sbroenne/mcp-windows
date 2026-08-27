using System.Diagnostics;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.SnapshotBenchmark;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "RequiresInternet")]
[Trait("Category", "SnapshotBenchmark")]
public sealed class ChromiumSnapshotBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public ChromiumSnapshotBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task Benchmark_PublicGitHubRepositoryWorkflow_Chrome()
    {
        const ChromiumBrowserKind browser = ChromiumBrowserKind.Chrome;
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        var result = await SnapshotBenchmarkRunner.RunAsync(
            $"GitHub microsoft/vscode in {browser}",
            (arm, sample) => CreateScenarioAsync(browser, arm, sample));

        _output.WriteLine(SnapshotBenchmarkRunner.FormatReport(result));
    }

    [SkippableTheory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task AutoSnapshot_PublicGitHubSearchDialog_ReturnsDiff(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchPublicSite(
            browser,
            ChromiumPublicSite.GitHubVisualStudioCode);
        using var harness = new ChromiumAutomationHarness();
        using var keyboard = new KeyboardInputService();
        using var state = new SnapshotStateService();
        var key = SnapshotRequestKey.Create(session.WindowHandleString, null, 5, "Edit");

        var focusPage = await harness.AutomationService.FindAndClickAsync(
            new ElementQuery
            {
                WindowHandle = session.WindowHandleString,
                Name = "Code",
                TimeoutMs = 10000
            },
            CancellationToken.None);
        Assert.True(focusPage.Success, focusPage.ErrorMessage);
        var closeMenu = await keyboard.PressKeyAsync("escape", cancellationToken: CancellationToken.None);
        Assert.True(closeMenu.Success, closeMenu.Error);
        var openSearch = await keyboard.TypeTextAsync("/", CancellationToken.None);
        Assert.True(openSearch.Success, openSearch.Error);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        var baseline = await state.CaptureAsync(
            key,
            SnapshotMode.Reset,
            token => harness.AutomationService.GetTreeAsync(
                session.WindowHandleString, null, 5, "Edit", token),
            CancellationToken.None);
        Assert.Equal("full", baseline.Kind);

        var type = await harness.AutomationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = session.WindowHandleString,
                ControlType = "Edit",
                TimeoutMs = 10000
            },
            "incremental snapshot",
            clearFirst: true,
            CancellationToken.None);
        Assert.True(type.Success, type.ErrorMessage);
        await Task.Delay(TimeSpan.FromSeconds(1));

        var result = await state.CaptureAsync(
            key,
            SnapshotMode.Auto,
            token => harness.AutomationService.GetTreeAsync(
                session.WindowHandleString, null, 5, "Edit", token),
            CancellationToken.None);

        Assert.Equal("diff", result.Kind);
        Assert.NotEmpty(result.Changes ?? []);
        Assert.Contains(
            result.Changes!,
            change => change.Set?.TryGetValue("value", out var value) == true &&
                      string.Equals(value as string, "incremental snapshot", StringComparison.Ordinal));
    }

    private static Task<SnapshotBenchmarkScenario> CreateScenarioAsync(
        ChromiumBrowserKind browser,
        SnapshotBenchmarkArm arm,
        int sample)
    {
        var session = ChromiumBrowserSession.LaunchPublicSite(
            browser,
            ChromiumPublicSite.GitHubVisualStudioCode);
        var harness = new ChromiumAutomationHarness();
        var keyboard = new KeyboardInputService();

        IReadOnlyList<Func<CancellationToken, Task>> actions =
        [
            token => NavigateAsync(harness, keyboard, session, "https://github.com/microsoft/vscode/issues", "Issues", token),
            token => NavigateAsync(harness, keyboard, session, "https://github.com/microsoft/vscode/pulls", "Pull requests", token),
            token => NavigateAsync(harness, keyboard, session, "https://github.com/microsoft/vscode/actions", "Workflow runs", token),
            token => NavigateAsync(harness, keyboard, session, "https://github.com/microsoft/vscode", "microsoft/vscode", token)
        ];

        var environment =
            $"{GetBrowserVersion(session.WindowHandle, browser)}; Windows {Environment.OSVersion.Version}";

        return Task.FromResult(new SnapshotBenchmarkScenario(
            $"GitHub microsoft/vscode in {browser}",
            session.WindowHandleString,
            harness.AutomationService,
            actions,
            environment,
            () =>
            {
                keyboard.Dispose();
                harness.Dispose();
                session.Dispose();
                return ValueTask.CompletedTask;
            },
            MaxDepth: 20));
    }

    private static async Task NavigateAsync(
        ChromiumAutomationHarness harness,
        KeyboardInputService keyboard,
        ChromiumBrowserSession session,
        string url,
        string expectedTitle,
        CancellationToken cancellationToken)
    {
        var ready = false;
        for (var attempt = 1; attempt <= 2 && !ready; attempt++)
        {
            var typeResult = await harness.AutomationService.FindAndTypeAsync(
                new ElementQuery
                {
                    WindowHandle = session.WindowHandleString,
                    Name = "Address and search bar",
                    ControlType = "Edit",
                    ContentViewOnly = false,
                    TimeoutMs = 10000
                },
                url,
                clearFirst: true,
                cancellationToken);
            Assert.True(
                typeResult.Success || IsChromeValueReadBackFailure(typeResult.ErrorMessage),
                $"Typing browser URL failed: {typeResult.ErrorMessage}");

            var enterResult = await keyboard.PressKeyAsync(
                "enter",
                ModifierKey.None,
                repeat: 1,
                cancellationToken);
            Assert.True(enterResult.Success, $"Navigating browser failed: {enterResult.Error}");

            ready = await WaitForWindowTitleAsync(
                session.WindowHandle,
                expectedTitle,
                TimeSpan.FromSeconds(30),
                cancellationToken);
        }

        Assert.True(
            ready,
            $"GitHub page title did not contain '{expectedTitle}' after navigating to {url}. " +
            $"Current title: '{GetWindowTitle(session.WindowHandle)}'.");

        await ChromiumPageWaiter.WaitForControlAsync(
            harness,
            session.WindowHandleString,
            expectedTitle,
            TimeSpan.FromSeconds(30),
            cancellationToken);
    }

    private static bool IsChromeValueReadBackFailure(string? errorMessage) =>
        errorMessage?.Contains(
            "ValuePattern accepted the text, but the requested value was not observable",
            StringComparison.Ordinal) == true;

    private static async Task<bool> WaitForWindowTitleAsync(
        nint windowHandle,
        string expectedTitle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        var buffer = new char[512];
        while (clock.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var length = NativeMethods.GetWindowText(windowHandle, buffer, buffer.Length);
            if (length > 0 &&
                new string(buffer, 0, length).Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            await Task.Delay(200, cancellationToken);
        }

        return false;
    }

    private static string GetWindowTitle(nint windowHandle)
    {
        var buffer = new char[512];
        var length = NativeMethods.GetWindowText(windowHandle, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    private static string GetBrowserVersion(nint windowHandle, ChromiumBrowserKind browser)
    {
        _ = NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
        try
        {
            using var process = Process.GetProcessById(unchecked((int)processId));
            var version = process.MainModule?.FileVersionInfo.FileVersion;
            return string.IsNullOrWhiteSpace(version) ? browser.ToString() : $"{browser} {version}";
        }
        catch (Exception ex) when (
            ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return browser.ToString();
        }
    }
}
