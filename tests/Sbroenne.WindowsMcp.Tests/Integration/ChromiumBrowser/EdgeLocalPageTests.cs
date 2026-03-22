using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
public sealed class ChromiumLocalPageTests
{
    private const int QueryTimeoutMs = 5000;
    private const string SearchInputName = "Docs Search";
    private const string SignInButtonName = "Sign in";
    private const string SignedOutStatus = "Signed out";
    private const string FocusedButtonMessage = "Sign in button focused";

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_LocalChromiumPage_PrimaryNavigation_IsDiscoverable(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "Primary navigation",
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_LocalChromiumPage_SearchInputByAriaLabel_ReturnsEdit(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = SearchInputName,
            ControlType = "Edit",
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        Assert.Equal("Edit", result.Items![0].Type);
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Find_LocalChromiumPage_SignInButtonByAriaLabel_ReturnsButton(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        var result = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = SignInButtonName,
            ControlType = "Button",
            TimeoutMs = QueryTimeoutMs,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        Assert.Equal("Button", result.Items![0].Type);
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Type_LocalChromiumPage_SearchInput_ReadsBackTypedValue(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        const string expectedText = "Lambert local query";

        var typeResult = await harness.AutomationService.FindAndTypeAsync(
            CreateLocalPageQuery(session.WindowHandleString, SearchInputName, "Edit"),
            expectedText,
            clearFirst: true);

        Assert.True(typeResult.Success, $"Type failed: {typeResult.ErrorMessage}");

        var searchInput = await FindSingleElementAsync(harness, session.WindowHandleString, SearchInputName, "Edit");
        var readResult = await harness.AutomationService.GetTextAsync(searchInput.Id, session.WindowHandleString, includeChildren: false);

        Assert.True(readResult.Success, $"Read failed: {readResult.ErrorMessage}");
        Assert.Equal(expectedText, readResult.Text);
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Click_LocalChromiumPage_SignInButton_RevealsFocusedContent(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        var clickResult = await harness.AutomationService.FindAndClickAsync(
            CreateLocalPageQuery(session.WindowHandleString, SignInButtonName, "Button"));

        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");

        var focusMessage = await FindSingleElementAsync(harness, session.WindowHandleString, FocusedButtonMessage, "Text");
        var readResult = await harness.AutomationService.GetTextAsync(focusMessage.Id, session.WindowHandleString, includeChildren: false);

        Assert.True(readResult.Success, $"Read failed: {readResult.ErrorMessage}");
        Assert.Equal(FocusedButtonMessage, readResult.Text);
    }

    [Theory]
    [InlineData(ChromiumBrowserKind.Edge)]
    [InlineData(ChromiumBrowserKind.Chrome)]
    public async Task Read_LocalChromiumPage_InitialStatus_ReturnsSignedOut(ChromiumBrowserKind browser)
    {
        ChromiumBrowserSession.SkipUnlessSupported(browser);

        using var session = ChromiumBrowserSession.LaunchLocalPage(browser);
        using var harness = new ChromiumAutomationHarness();

        var statusElement = await FindSingleElementAsync(harness, session.WindowHandleString, SignedOutStatus, "Text");
        var readResult = await harness.AutomationService.GetTextAsync(statusElement.Id, session.WindowHandleString, includeChildren: false);

        Assert.True(readResult.Success, $"Read failed: {readResult.ErrorMessage}");
        Assert.Equal(SignedOutStatus, readResult.Text);
    }

    private static ElementQuery CreateLocalPageQuery(string windowHandle, string name, string controlType)
    {
        return new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = name,
            ControlType = controlType,
            TimeoutMs = QueryTimeoutMs,
        };
    }

    private static async Task<UIElementCompact> FindSingleElementAsync(
        ChromiumAutomationHarness harness,
        string windowHandle,
        string name,
        string controlType)
    {
        var result = await harness.AutomationService.FindElementsAsync(CreateLocalPageQuery(windowHandle, name, controlType));

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        return result.Items![0];
    }
}
