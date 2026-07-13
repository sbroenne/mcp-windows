using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

/// <summary>
/// Integration coverage for the R5 content-view optimization (#136 follow-up #139).
/// Verifies that, under the Chromium strategy, <c>ui_find</c> scans the leaner UI Automation
/// content view by default, that this never scans more elements than the full control view, and
/// that meaningful elements remain discoverable (with a control-view fallback escape hatch).
/// </summary>
[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
public sealed class ChromiumContentViewTests : IClassFixture<ChromiumReadOnlySessionFixture>
{
    private const int QueryTimeoutMs = 5000;
    private const string PrimaryNavigationName = "Primary navigation";

    private readonly ChromiumReadOnlySessionFixture _readOnlySessions;

    public ChromiumContentViewTests(ChromiumReadOnlySessionFixture readOnlySessions)
    {
        _readOnlySessions = readOnlySessions;
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_DefaultOnChromium_UsesContentView(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        var session = _readOnlySessions.GetSession(browser);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = PrimaryNavigationName,
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.Items!);
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.UsedContentView, "Chromium find should default to the content view.");
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_ContentView_ScansNoMoreThanControlView(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        var session = _readOnlySessions.GetSession(browser);
        using var harness = new ChromiumAutomationHarness();

        // Broad, unfiltered scan so the full candidate set is exercised on both views.
        var controlView = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            ContentViewOnly = false,
            TimeoutMs = QueryTimeoutMs,
        });

        var contentView = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            ContentViewOnly = true,
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(controlView.Success, $"Control-view find failed: {controlView.ErrorMessage}");
        Assert.True(contentView.Success, $"Content-view find failed: {contentView.ErrorMessage}");

        var controlScanned = controlView.Diagnostics!.ElementsScanned!.Value;
        var contentScanned = contentView.Diagnostics!.ElementsScanned!.Value;

        Assert.True(contentScanned > 0, "Content view should still surface elements.");
        Assert.False(controlView.Diagnostics.UsedContentView);
        Assert.True(contentView.Diagnostics.UsedContentView);

        // Content view is a strict subset of the control view, so it can never scan more nodes.
        Assert.True(
            contentScanned <= controlScanned,
            $"Content view scanned {contentScanned} elements but control view scanned {controlScanned}.");
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_ContentViewOff_StillFindsElement(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        var session = _readOnlySessions.GetSession(browser);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = PrimaryNavigationName,
            ContentViewOnly = false,
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.Items!);
        Assert.False(result.Diagnostics!.UsedContentView, "Explicit contentViewOnly=false must scan the control view.");
    }
}
