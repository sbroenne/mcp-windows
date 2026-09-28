using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
public sealed class ChromiumFindCandidateTests
{
    [SkippableTheory]
    [InlineData(ChromiumBrowserKind.Chrome, false)]
    [InlineData(ChromiumBrowserKind.Chrome, true)]
    [InlineData(ChromiumBrowserKind.Edge, false)]
    [InlineData(ChromiumBrowserKind.Edge, true)]
    public async Task ExactName_DoesNotReturnUnnamedRawDescendantsOfMatchingText(
        ChromiumBrowserKind browser, bool contentViewOnly)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);
        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();
        var click = await harness.AutomationService.ObserveAndClickAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "Sign in",
            ControlType = "Button",
            TimeoutMs = 5000
        });
        Assert.True(click.Success, click.ErrorMessage);

        const string ExpectedName = "Sign in button focused";
        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = ExpectedName,
            ControlType = "Text",
            ContentViewOnly = contentViewOnly,
            TimeoutMs = 20000
        });
        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotEmpty(result.Items!);
        Assert.All(result.Items!, item => Assert.Equal(ExpectedName, item.Name));
        foreach (var item in result.Items!)
        {
            var read = await harness.AutomationService.GetTextAsync(
                item.Id, session.WindowHandleString, includeChildren: false);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.Equal(ExpectedName, read.Text);
        }
    }
}
