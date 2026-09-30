using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class ChromiumReadinessTests
{
    [Fact]
    public void MissingPage_DoesNotRequestUnobservedPopupButtons()
    {
        Assert.Empty(ChromiumBrowserSession.GetKnownPopupDismissButtons(
            [Element("Address and search bar", "Edit")]));
    }

    [Theory]
    [InlineData("Got it", "Button", true, true)]
    [InlineData("Got it", "Text", true, false)]
    [InlineData("Got it", "Button", false, false)]
    [InlineData("Close", "Button", true, false)]
    public void OnlyObservedEnabledDismissButtonsAreSelected(
        string name, string type, bool enabled, bool expected)
    {
        var element = Element(name, type) with { Enabled = enabled };
        var buttons = ChromiumBrowserSession.GetKnownPopupDismissButtons([element]).ToArray();
        Assert.Equal(expected ? 1 : 0, buttons.Length);
        if (expected)
        {
            Assert.Same(element, buttons[0]);
        }
    }

    [Fact]
    public void KnownPopup_SelectsItsObservedButtonFromNestedTree()
    {
        var button = Element("No thanks", "Button");
        var popup = Element("Turn on sync", "Pane") with { Children = [button] };
        Assert.Same(button, Assert.Single(
            ChromiumBrowserSession.GetKnownPopupDismissButtons([popup])));
    }

    [Fact]
    public void KnownPopup_DoesNotInventMissingDismissButton()
    {
        Assert.Empty(ChromiumBrowserSession.GetKnownPopupDismissButtons(
            [Element("Turn on sync", "Pane")]));
    }

    [Fact]
    public void KnownPopup_PreservesDismissButtonPriority()
    {
        var close = Element("Close", "Button");
        var noThanks = Element("No thanks", "Button");
        var popup = Element("Turn on sync", "Pane") with { Children = [close, noThanks] };
        Assert.Equal(
            [noThanks, close],
            ChromiumBrowserSession.GetKnownPopupDismissButtons([popup]).ToArray());
    }

    [Fact]
    public void AmbiguousClose_DoesNotSelectBrowserWindowCloseButton()
    {
        var popup = Element("Welcome to Microsoft Edge", "Pane") with
        {
            Children = [Element("Close", "Button")]
        };
        Assert.Empty(ChromiumBrowserSession.GetKnownPopupDismissButtons(
            [Element("Close", "Button"), popup]));
    }

    private static UIElementCompactTree Element(string name, string type) =>
        new() { Id = name, Name = name, Type = type, Enabled = true };
}
