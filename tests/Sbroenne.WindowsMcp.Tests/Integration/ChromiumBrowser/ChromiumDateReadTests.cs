using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Input;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

[Collection("ChromiumBrowser")]
[Trait("Category", "RequiresDesktop")]
[Trait("Category", "ChromiumBrowser")]
public sealed class ChromiumDateReadTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [InlineData("Empty workshop date", "")]
    [InlineData("Populated workshop date", "2026-10-21")]
    public async Task ReadDate_ReturnsValueNotLabel(string name, string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        using var session = ChromiumBrowserSession.LaunchLocalPage(ChromiumBrowserKind.Chrome);
        using var harness = new ChromiumAutomationHarness();
        var found = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = name,
            ControlType = "Edit",
            VisibleOnly = false,
            IncludeChildren = true,
        });
        Assert.True(found.Success, found.ErrorMessage);
        var field = Assert.Single(found.Items!);
        var tree = await harness.AutomationService.GetTreeAsync(session.WindowHandleString, field.Id, 4, null);
        Assert.True(tree.Success, tree.ErrorMessage);
        foreach (var child in ChromiumAutomationHarness.Flatten(tree.Tree))
        {
            output.WriteLine(await harness.DescribeObservedElementAsync(child.Id));
        }
        string[] expectedSegments = expected.Length == 0 ? ["0", "0", "0"] : ["10", "21", "2026"];
        Assert.Equal(expectedSegments, ChromiumAutomationHarness.Flatten(tree.Tree)
            .Where(child => child.Type == "Spinner").Select(child => child.Value));
        var read = await harness.AutomationService.GetTextAsync(field.Id, session.WindowHandleString, false);
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(expected, read.Text);
    }

    [SkippableFact]
    public async Task EnterDate_ByObservedSegments_VerifiesPageValue()
    {
        ChromiumBrowserSession.SkipUnlessSupported(ChromiumBrowserKind.Chrome);
        DesktopInputTests.SkipUnlessEnabled();
        using var session = ChromiumBrowserSession.LaunchLocalPage(ChromiumBrowserKind.Chrome);
        using var harness = new ChromiumAutomationHarness();
        using var keyboard = new KeyboardInputService();
        foreach (var (segment, value) in new[] { ("Month", "10"), ("Day", "21"), ("Year", "2026") })
        {
            var found = await harness.AutomationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = session.WindowHandleString,
                Name = $"{segment} Empty workshop date",
                ControlType = "Spinner",
                VisibleOnly = false,
            });
            Assert.True(found.Success, found.ErrorMessage);
            var field = Assert.Single(found.Items!);
            var scrolled = await harness.AutomationService.ScrollIntoViewAsync(field.Id, null, 2000);
            Assert.True(scrolled.Success, scrolled.ErrorMessage);
            var typed = await harness.AutomationService.TypeIntoElementAsync(
                field.Id, value, false, session.WindowHandleString, "keyboard");
            Assert.True(typed.Success, $"{segment}: {typed.ErrorMessage}");
            var segmentRead = await harness.AutomationService.GetTextAsync(field.Id, session.WindowHandleString, false);
            Assert.True(segmentRead.Success, segmentRead.ErrorMessage);
            Assert.Equal(value, segmentRead.Text);
        }

        var committed = await keyboard.PressKeyAsync("Tab", ModifierKey.None, 1, session.WindowHandle);
        Assert.True(committed.Success, committed.Error);
        var parent = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "Empty workshop date",
            ControlType = "Edit",
            VisibleOnly = false,
        });
        Assert.True(parent.Success, parent.ErrorMessage);
        var finalTree = await harness.AutomationService.GetTreeAsync(
            session.WindowHandleString, Assert.Single(parent.Items!).Id, 4, null);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(finalTree));
        var read = await harness.AutomationService.GetTextAsync(
            Assert.Single(parent.Items!).Id, session.WindowHandleString, false);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine($"Whole-field provider value: '{read.Text}'");
        var check = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "Check workshop date",
            ControlType = "Button",
        });
        Assert.True(check.Success, check.ErrorMessage);
        var clicked = await harness.AutomationService.ClickElementAsync(
            Assert.Single(check.Items!).Id, session.WindowHandleString);
        Assert.True(clicked.Success, clicked.ErrorMessage);
        var pageValue = await harness.AutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = session.WindowHandleString,
            Name = "2026-10-21",
            ControlType = "Text",
            TimeoutMs = 2000,
            RequireUnique = true,
        });
        Assert.True(pageValue.Success, pageValue.ErrorMessage);
        Assert.Single(pageValue.Items!);
    }
}
