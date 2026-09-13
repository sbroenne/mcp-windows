using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class ElementReferenceLifetimeTests(UITestHarnessFixture fixture)
{
    [Fact]
    public async Task RenamePreservesReferenceButReplacementDoesNot()
    {
        var form = Assert.IsType<UITestHarnessForm>(fixture.Form);
        const string name = "Reference lifetime sentinel";
        Button? button = null;
        form.Invoke(() =>
        {
            button = new Button { Name = "LifetimeSentinel", Text = name, Width = 240 };
            form.Controls.Add(button);
            button.BringToFront();
        });
        try
        {
            var original = await FindAsync(name);
            var firstRead = await WindowsToolsBase.UIAutomationService.GetTextAsync(
                original, fixture.TestWindowHandleString, includeChildren: false, CancellationToken.None);
            Assert.True(firstRead.Success, firstRead.ErrorMessage);

            form.Invoke(() => button!.Text = name + " renamed");
            var renamed = await FindAsync(name + " renamed");
            Assert.Equal(original, renamed);
            var beforeReplacement = await WindowsToolsBase.CaptureSnapshotAsync(
                fixture.TestWindowHandleString, null, 5, null, SnapshotMode.Reset, CancellationToken.None);
            Assert.True(beforeReplacement.Success, beforeReplacement.ErrorMessage);

            form.Invoke(() =>
            {
                button!.Dispose();
                button = new Button { Name = "LifetimeSentinel", Text = name + " renamed", Width = 240 };
                form.Controls.Add(button);
                button.BringToFront();
            });
            var replacement = await FindAsync(name + " renamed");
            Assert.NotEqual(original, replacement);
            var afterReplacement = await WindowsToolsBase.CaptureSnapshotAsync(
                fixture.TestWindowHandleString, null, 5, null, SnapshotMode.Auto, CancellationToken.None);
            Assert.True(afterReplacement.Success, afterReplacement.ErrorMessage);

            var oldRead = await WindowsToolsBase.UIAutomationService.GetTextAsync(
                original, fixture.TestWindowHandleString, includeChildren: false, CancellationToken.None);
            Assert.False(oldRead.Success);
            var currentRead = await WindowsToolsBase.UIAutomationService.GetTextAsync(
                replacement, fixture.TestWindowHandleString, includeChildren: false, CancellationToken.None);
            Assert.True(currentRead.Success, currentRead.ErrorMessage);
        }
        finally
        {
            form.Invoke(() => button?.Dispose());
        }
    }

    [Fact]
    public async Task SubtreeReferenceCannotOverrideExplicitWindow()
    {
        var id = await FindAsync("Submit");
        var snapshot = await WindowsToolsBase.UIAutomationService.GetTreeAsync(
            "2147483647", id, 5, null, CancellationToken.None);
        var find = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = "2147483647",
            ParentElementId = id,
            Name = "Submit"
        });
        Assert.False(snapshot.Success);
        Assert.False(find.Success);
    }

    [Fact]
    public async Task RawInternalIdentityCannotBeUsedInsteadOfIssuedReference()
    {
        var id = await FindAsync("Submit");
        var internalIdentity = Assert.IsType<string>(ElementIdGenerator.ResolveFullId(id));
        var result = await WindowsToolsBase.UIAutomationService.GetTextAsync(
            internalIdentity, fixture.TestWindowHandleString, includeChildren: false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(ElementIdGenerator.TryResolveWindowHandle(internalIdentity, out _));
    }

    private async Task<string> FindAsync(string name)
    {
        var found = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.TestWindowHandleString,
            Name = name,
            ControlType = "Button"
        });
        Assert.True(found.Success, found.ErrorMessage);
        return Assert.Single(found.Items!).Id;
    }
}
