using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
[Trait("Category", "Integration")]
[Trait("Category", "RequiresDesktop")]
public sealed class ToolPermissionIntegrationTests(UITestHarnessFixture fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ForbiddenLaterStep_DoesNotClickEarlierTarget(bool stopOnError)
    {
        fixture.Reset();
        var id = await DiscoverAsync("SubmitButton");
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["steps"] = JsonSerializer.SerializeToElement(
                $$"""[{"action":"click","elementId":"{{id}}"},{"action":"snapshot"}]"""),
            ["stopOnError"] = JsonSerializer.SerializeToElement(stopOnError),
        };

        var denied = await McpArgumentValidationTests.InvokeRegisteredToolAsync("ui_batch", arguments, ["ui_snapshot"]);

        Assert.True(denied.IsError);
        Assert.Contains("step 2", Text(denied), StringComparison.Ordinal);
        Assert.Equal(0, fixture.Form!.SubmitClickCount);

        var allowed = await McpArgumentValidationTests.InvokeRegisteredToolAsync("ui_batch", arguments);
        Assert.False(allowed.IsError, Text(allowed));
        Assert.Equal(1, fixture.Form.SubmitClickCount);
    }

    [Theory]
    [InlineData("ui_click", "SubmitButton")]
    [InlineData("ui_type", "UsernameInput")]
    [InlineData("ui_select", "CategoryCombo")]
    public async Task ForbiddenSnapshot_DoesNotChangePrimaryTarget(string toolName, string automationId)
    {
        fixture.Reset();
        var form = fixture.Form!;
        if (toolName == "ui_select")
        {
            form.Invoke(() => Assert.IsType<ComboBox>(
                Assert.Single(form.Controls.Find("CategoryCombo", true))).DroppedDown = true);
        }
        var id = await DiscoverAsync(automationId);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["elementId"] = JsonSerializer.SerializeToElement(id),
            ["withSnapshot"] = JsonSerializer.SerializeToElement(true),
        };
        if (toolName == "ui_type")
        {
            arguments["text"] = JsonSerializer.SerializeToElement("permission-test");
            arguments["inputMode"] = JsonSerializer.SerializeToElement("value");
        }
        if (toolName == "ui_select")
        {
            arguments["value"] = JsonSerializer.SerializeToElement("Science");
        }
        var before = (string)form.Invoke(() => $"{form.SubmitClickCount}|{form.UsernameText}|{form.SelectedComboItem}");

        var denied = await McpArgumentValidationTests.InvokeRegisteredToolAsync(toolName, arguments, ["ui_snapshot"]);

        Assert.True(denied.IsError);
        Assert.Contains("ui_snapshot", Text(denied), StringComparison.Ordinal);
        Assert.Equal(before, (string)form.Invoke(() => $"{form.SubmitClickCount}|{form.UsernameText}|{form.SelectedComboItem}"));

        arguments["withSnapshot"] = JsonSerializer.SerializeToElement(false);
        var allowed = await McpArgumentValidationTests.InvokeRegisteredToolAsync(toolName, arguments, ["ui_snapshot"]);
        Assert.False(allowed.IsError, Text(allowed));
        Assert.Equal(toolName == "ui_click" ? 1 : 0, (int)form.Invoke(() => form.SubmitClickCount));
        Assert.Equal(toolName == "ui_type" ? "permission-test" : "", (string)form.Invoke(() => form.UsernameText));
        Assert.True(await TestWait.UntilAsync(() =>
            (string?)form.Invoke(() => form.SelectedComboItem) == (toolName == "ui_select" ? "Science" : "Technology")),
            $"Unexpected selection after {toolName}: {form.Invoke(() => form.SelectedComboItem)}. {Text(allowed)}");
    }

    private async Task<string> DiscoverAsync(string automationId)
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.TestWindowHandleString,
            AutomationId = automationId,
        });
        Assert.True(result.Success, result.ErrorMessage);
        return Assert.Single(result.Items!).Id;
    }

    private static string Text(CallToolResult result) =>
        Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
}
