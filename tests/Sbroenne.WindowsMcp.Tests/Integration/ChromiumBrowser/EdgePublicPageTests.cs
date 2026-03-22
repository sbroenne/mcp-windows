using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
[Trait("Category", "RequiresInternet")]
[Trait("Category", "PublicSite")]
public sealed class ChromiumPublicPageTests
{
    private const int QueryTimeoutMs = 20000;

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_PlaywrightTodoMvc_NewTodoInputByPlaceholder_ReturnsEdit(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchPublicSite(browser, ChromiumPublicSite.PlaywrightTodoMvc);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "What needs to be done?",
            ControlType = "Edit",
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        Assert.Equal("Edit", result.Items![0].Type);
    }
}
