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
public sealed class ChromiumSnapshotBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public ChromiumSnapshotBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task ChromeStartup_ExposesPageControls()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        using var session = ChromiumBrowserSession.LaunchPublicSite(
            ChromiumBrowserKind.Chrome, ChromiumPublicSite.GitHubVisualStudioCode);
        using var harness = new ChromiumAutomationHarness();
        await ChromiumPageWaiter.WaitForControlAsync(
            harness, session.WindowHandleString, "Code", TimeSpan.FromSeconds(10), "Button");
    }

    [SkippableFact]
    public async Task ChromeRestart_ExposesPageControls()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        for (var launch = 1; launch <= 3; launch++)
        {
            _output.WriteLine($"Chrome launch {launch}");
            using var session = ChromiumBrowserSession.LaunchPublicSite(
                ChromiumBrowserKind.Chrome, ChromiumPublicSite.GitHubVisualStudioCode);
            using var harness = new ChromiumAutomationHarness();
            await ChromiumPageWaiter.WaitForControlAsync(
                harness, session.WindowHandleString, "Code", TimeSpan.FromSeconds(10), "Button");
        }
    }

    [SkippableFact]
    public async Task ChromeNormalWindow_ExposesLocalPageControlsWithoutNavigation()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        using var session = ChromiumBrowserSession.LaunchAddressBar(ChromiumBrowserKind.Chrome);
        using var harness = new ChromiumAutomationHarness();
        await ChromiumPageWaiter.WaitForControlAsync(
            harness, session.WindowHandleString, "Docs Search", TimeSpan.FromSeconds(10), "Edit");
        var snapshot = await harness.AutomationService.GetTreeAsync(
            session.WindowHandleString, null, 20, null);
        Assert.True(snapshot.Success, snapshot.ErrorMessage);
        Assert.Contains(ChromiumAutomationHarness.Flatten(snapshot.Tree),
            element => element.Name == "Docs Search" && element.Type == "Edit");
    }

    [SkippableFact]
    public async Task Type_ChromeAddressBar_ReportsMismatchesWithoutCorrectingThem()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        using var session = ChromiumBrowserSession.LaunchAddressBar(ChromiumBrowserKind.Chrome);
        using var harness = new ChromiumAutomationHarness();
        using var keyboard = new KeyboardInputService();
        var addressId = await harness.ObserveAddressBarAsync(session.WindowHandleString);
        foreach (var (url, title, mayAutocomplete) in new[]
        {
            ("https://github.com/microsoft/vscode/issues", "Issues", false),
            ("https://github.com/microsoft/vscode/pulls", "Pull requests", false),
            ("https://github.com/microsoft/vscode/actions", "Workflow runs", false),
            ("https://github.com/microsoft/vscode", "microsoft/vscode", true)
        })
        {
            var typed = await harness.AutomationService.TypeIntoElementAsync(
                addressId, url, clearFirst: true, session.WindowHandleString, inputMode: "auto");
            var diagnostic = await harness.DescribeObservedElementAsync(addressId);
            _output.WriteLine(diagnostic);
            var read = await harness.AutomationService.GetTextAsync(
                addressId, session.WindowHandleString, includeChildren: false);
            Assert.True(read.Success, read.ErrorMessage);
            if (!typed.Success)
            {
                Assert.True(mayAutocomplete, $"{typed.ErrorMessage} {diagnostic}");
                Assert.Equal(UIAutomationErrorType.VerificationFailed, typed.ErrorType);
                Assert.StartsWith(url, read.Text, StringComparison.Ordinal);
                Assert.NotEqual(url, read.Text);
                Assert.Contains("no automatic correction", typed.ErrorMessage, StringComparison.Ordinal);
                continue;
            }
            Assert.Equal(url, read.Text);
            var enter = await keyboard.PressKeyAsync("enter", ModifierKey.None, 1, session.WindowHandle);
            Assert.True(enter.Success, enter.Error);
            Assert.True(await WaitForWindowTitleAsync(
                session.WindowHandle, title, TimeSpan.FromSeconds(30), CancellationToken.None));
        }
    }

    [SkippableFact]
    [Trait("Category", "SnapshotBenchmark")]
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

        var focusPage = await harness.AutomationService.ObserveAndClickAsync(
            new ElementQuery
            {
                WindowHandle = session.WindowHandleString,
                Name = "Code",
                ControlType = "Button",
                RequireUnique = true,
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

        var type = await harness.AutomationService.ObserveAndTypeAsync(
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

    private static async Task<SnapshotBenchmarkScenario> CreateScenarioAsync(
        ChromiumBrowserKind browser,
        SnapshotBenchmarkArm arm,
        int sample)
    {
        var session = ChromiumBrowserSession.LaunchPublicSite(
            browser,
            ChromiumPublicSite.GitHubVisualStudioCode);
        var harness = new ChromiumAutomationHarness();
        var keyboard = new KeyboardInputService();

        try
        {
            var addressId = await harness.ObserveAddressBarAsync(session.WindowHandleString);
            IReadOnlyList<Func<CancellationToken, Task>> actions =
            [
                token => NavigateAsync(harness, keyboard, session, addressId, "https://github.com/microsoft/vscode/issues", "Issues", token),
                token => NavigateAsync(harness, keyboard, session, addressId, "https://github.com/microsoft/vscode/pulls", "Pull requests", token),
                token => NavigateAsync(harness, keyboard, session, addressId, "https://github.com/microsoft/vscode/actions", "Workflow runs", token),
                token => NavigateAsync(harness, keyboard, session, addressId, "https://github.com/microsoft/vscode", "microsoft/vscode", token)
            ];

            var environment =
                $"{GetBrowserVersion(session.WindowHandle, browser)}; Windows {Environment.OSVersion.Version}";

            return new SnapshotBenchmarkScenario(
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
                MaxDepth: 20);
        }
        catch
        {
            keyboard.Dispose();
            harness.Dispose();
            session.Dispose();
            throw;
        }
    }

    private static async Task NavigateAsync(
        ChromiumAutomationHarness harness,
        KeyboardInputService keyboard,
        ChromiumBrowserSession session,
        string addressId,
        string url,
        string expectedTitle,
        CancellationToken cancellationToken)
    {
        var typeResult = await harness.AutomationService.TypeIntoElementAsync(
            addressId,
            url,
            clearFirst: true,
            session.WindowHandleString,
            inputMode: "auto",
            cancellationToken);
        if (!typeResult.Success)
        {
            var diagnostic = await harness.DescribeObservedElementAsync(addressId);
            Assert.True(typeResult.ErrorType == UIAutomationErrorType.VerificationFailed,
                $"Typing browser URL '{url}' failed: {typeResult.ErrorMessage} {diagnostic}");
            var read = await harness.AutomationService.GetTextAsync(
                addressId, session.WindowHandleString, includeChildren: false, cancellationToken);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.NotNull(read.Text);
            Assert.StartsWith(url, read.Text, StringComparison.Ordinal);
            Assert.NotEqual(url, read.Text);
            Assert.Equal(read.Text[url.Length..], await harness.ReadSelectedTextAsync(addressId));

            // The benchmark caller explicitly rejects a verified selected suggestion.
            // General-purpose typing reports the mismatch without correcting it.
            var delete = await keyboard.PressKeyAsync(
                "Delete",
                ModifierKey.None,
                repeat: 1,
                session.WindowHandle,
                cancellationToken);
            Assert.True(delete.Success, delete.Error);
            var corrected = await harness.AutomationService.GetTextAsync(
                addressId, session.WindowHandleString, includeChildren: false, cancellationToken);
            Assert.True(corrected.Success, corrected.ErrorMessage);
            Assert.Equal(url, corrected.Text);
        }

        var enterResult = await keyboard.PressKeyAsync(
            "enter",
            ModifierKey.None,
            repeat: 1,
            session.WindowHandle,
            cancellationToken);
        Assert.True(enterResult.Success, $"Navigating browser failed: {enterResult.Error}");

        var ready = await WaitForWindowTitleAsync(
            session.WindowHandle,
            expectedTitle,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        Assert.True(
            ready,
            $"GitHub page title did not contain '{expectedTitle}' after navigating to {url}. " +
            $"Current title: '{GetWindowTitle(session.WindowHandle)}'.");

        await ChromiumPageWaiter.WaitForControlAsync(
            harness,
            session.WindowHandleString,
            "Code",
            TimeSpan.FromSeconds(30),
            cancellationToken);
    }

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
