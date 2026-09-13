using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
public sealed class UIReadScopeIntegrationTests(UITestHarnessFixture fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid-scope-test-id")]
    public async Task Read_InvalidElementId_DoesNotFallBackToWindowOcr(string id)
    {
        var result = await UIReadTool.ExecuteAsync(
            fixture.TestWindowHandleString, id, true, null, null, false, CancellationToken.None);
        using var json = Payload(result);
        Assert.True(result.IsError);
        Assert.False(json.RootElement.TryGetProperty("text", out _));
    }

    [Fact]
    public async Task Read_InvalidWindow_DoesNotFallBackToDesktop()
    {
        var result = await UIReadTool.ExecuteAsync(
            "2147483647", null, true, null, null, false, CancellationToken.None);
        using var json = Payload(result);
        Assert.True(result.IsError);
        Assert.False(json.RootElement.TryGetProperty("text", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_ExplicitWindowOrObservedElement_RetainsText(bool elementRead)
    {
        fixture.Reset();
        fixture.BringToFront();
        string? id = null;
        if (elementRead)
        {
            var found = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = fixture.TestWindowHandleString,
                Name = "Submit",
                ControlType = "Button",
                RequireUnique = true
            });
            id = Assert.Single(found.Items!).Id;
        }
        var result = await UIReadTool.ExecuteAsync(
            fixture.TestWindowHandleString, id, true, null, null, false, CancellationToken.None);
        using var json = Payload(result);
        Assert.False(result.IsError);
        Assert.Contains("Submit", json.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Cli_RemovedReadSelector_ReturnsUsageErrorWithoutWindowText()
    {
        var (code, stdout, stderr) = await CliIntegrationTests.RunSeparateProcessAsync(
            "ui", "read", "--window", fixture.TestWindowHandleString,
            "--name", "Definitely absent scope sentinel", "--include-children");
        Assert.Equal(2, code);
        Assert.Empty(stdout);
        Assert.Contains("selector", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_EmptyObservedInput_DoesNotReturnWholeWindowOcr()
    {
        fixture.Reset();
        fixture.BringToFront();
        var found = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.TestWindowHandleString,
            AutomationId = "UsernameInput",
            RequireUnique = true
        });
        Assert.True(found.Success, found.ErrorMessage);
        var result = await UIReadTool.ExecuteAsync(fixture.TestWindowHandleString,
            Assert.Single(found.Items!).Id, false, null, null, false, CancellationToken.None);
        using var json = Payload(result);
        Assert.False(result.IsError);
        Assert.False(json.RootElement.TryGetProperty("usageHint", out _));
        if (json.RootElement.TryGetProperty("text", out var text))
        {
            Assert.DoesNotContain("Submit", text.GetString());
        }
    }

    private static JsonDocument Payload(CallToolResult result) =>
        JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
}
