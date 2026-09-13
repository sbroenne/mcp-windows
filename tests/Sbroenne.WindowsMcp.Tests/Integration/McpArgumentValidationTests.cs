using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Catalog;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
public sealed class McpArgumentValidationTests(UITestHarnessFixture fixture)
{
    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\" \\t \"")]
    [Trait("Category", "RequiresDesktop")]
    public async Task RegisteredRead_RejectsPresentEmptyIdWithoutWidening(string valueJson)
    {
        fixture.Reset();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["includeChildren"] = JsonSerializer.SerializeToElement(true),
        };
        var valid = await InvokeRegisteredToolAsync("ui_read", arguments);
        Assert.False(valid.IsError);
        Assert.Contains("Submit", Assert.IsType<TextContentBlock>(Assert.Single(valid.Content)).Text, StringComparison.Ordinal);

        arguments["elementId"] = JsonSerializer.Deserialize<JsonElement>(valueJson);
        var rejected = await InvokeRegisteredToolAsync("ui_read", arguments);
        Assert.True(rejected.IsError);
        var error = Assert.IsType<TextContentBlock>(Assert.Single(rejected.Content)).Text;
        Assert.Contains("elementId", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Submit", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "elementId")]
    [InlineData(null, "desiredState")]
    [InlineData("appear", "elementId")]
    [InlineData("appear", "desiredState")]
    [InlineData("disappear", "elementId")]
    [InlineData("disappear", "desiredState")]
    [InlineData(" APPEAR ", "elementId")]
    [Trait("Category", "RequiresDesktop")]
    public async Task RegisteredDiscoveryWait_RejectsExplicitNullStateArguments(string? mode, string property)
    {
        fixture.Reset();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["name"] = JsonSerializer.SerializeToElement(
                mode == "disappear" ? $"Absent-{Guid.NewGuid():N}" : "Submit"),
            ["timeoutMs"] = JsonSerializer.SerializeToElement(500),
        };
        if (mode is not null)
        {
            arguments["mode"] = JsonSerializer.SerializeToElement(mode);
        }
        var valid = await InvokeRegisteredWaitAsync(arguments);
        Assert.False(valid.IsError);

        arguments[property] = JsonSerializer.SerializeToElement<string?>(null);
        var rejected = await InvokeRegisteredWaitAsync(arguments);
        Assert.True(rejected.IsError);
        Assert.Contains(property, Assert.IsType<TextContentBlock>(Assert.Single(rejected.Content)).Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("name", "null")]
    [InlineData("nameContains", "null")]
    [InlineData("namePattern", "null")]
    [InlineData("controlType", "null")]
    [InlineData("automationId", "null")]
    [InlineData("className", "null")]
    [InlineData("parentElementId", "null")]
    [InlineData("scope", "null")]
    [InlineData("scope", "\"window\"")]
    [InlineData("requireUnique", "false")]
    [InlineData("enabledOnly", "null")]
    [Trait("Category", "RequiresDesktop")]
    public async Task RegisteredStateWaitRejectsExplicitSelectorsIncludingNull(string selector, string valueJson)
    {
        fixture.Reset();
        var found = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.TestWindowHandleString,
            AutomationId = "SubmitButton",
            ControlType = "Button",
        });
        Assert.True(found.Success);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["mode"] = JsonSerializer.SerializeToElement("state"),
            ["elementId"] = JsonSerializer.SerializeToElement(Assert.Single(found.Items!).Id),
            ["desiredState"] = JsonSerializer.SerializeToElement("enabled"),
            ["timeoutMs"] = JsonSerializer.SerializeToElement(500),
        };

        var valid = await InvokeRegisteredWaitAsync(arguments);
        Assert.False(valid.IsError);

        arguments["mode"] = JsonSerializer.SerializeToElement(" STATE ");
        arguments[selector] = JsonSerializer.Deserialize<JsonElement>(valueJson);
        var rejected = await InvokeRegisteredWaitAsync(arguments);
        Assert.True(rejected.IsError);
        Assert.Contains(selector, Assert.IsType<TextContentBlock>(Assert.Single(rejected.Content)).Text,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("appear")]
    [InlineData("disappear")]
    [Trait("Category", "RequiresDesktop")]
    public async Task RegisteredDiscoveryWaitPreservesSelectorSupport(string? mode)
    {
        fixture.Reset();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(fixture.TestWindowHandleString),
            ["name"] = JsonSerializer.SerializeToElement(
                mode == "disappear" ? $"Absent-{Guid.NewGuid():N}" : "Submit"),
            ["timeoutMs"] = JsonSerializer.SerializeToElement(500),
        };
        if (mode is not null)
        {
            arguments["mode"] = JsonSerializer.SerializeToElement(mode);
        }

        var result = await InvokeRegisteredWaitAsync(arguments);

        Assert.False(result.IsError);
    }

    private static Task<CallToolResult> InvokeRegisteredWaitAsync(Dictionary<string, JsonElement> arguments) =>
        InvokeRegisteredToolAsync("ui_wait", arguments);

    internal static async Task<CallToolResult> InvokeRegisteredToolAsync(string name, Dictionary<string, JsonElement> arguments)
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(ToolCatalog).Assembly);
        ToolFilter.Apply(services, include: null, exclude: null);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(), t => t.ProtocolTool.Name == name);
        await using var server = McpServer.Create(
            new StreamServerTransport(Stream.Null, Stream.Null),
            new McpServerOptions { ServerInfo = new() { Name = "wait-validation-test", Version = "1.0.0" } },
            serviceProvider: provider);
        var parameters = new CallToolRequestParams { Name = name, Arguments = arguments };
        var request = new RequestContext<CallToolRequestParams>(
            server, new JsonRpcRequest { Id = new RequestId(1), Method = "tools/call" }, parameters);

        return await tool.InvokeAsync(request, CancellationToken.None);
    }

    [Theory]
    [InlineData("name", "Submit")]
    [InlineData("name", null)]
    [InlineData("automationId", "SubmitButton")]
    [InlineData("unknownOption", "ignored")]
    public async Task RegisteredClickRejectsUnknownArgumentsWithoutActing(string argument, string? value)
    {
        fixture.Reset();
        var form = Assert.IsType<UITestHarnessForm>(fixture.Form);
        var window = fixture.TestWindowHandleString;
        var found = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = window,
            AutomationId = "SubmitButton",
            ControlType = "Button",
        });
        Assert.True(found.Success);
        var id = Assert.Single(found.Items!).Id;

        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(ToolCatalog).Assembly);
        ToolFilter.Apply(services, include: null, exclude: null);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(), t => t.ProtocolTool.Name == "ui_click");
        await using var server = McpServer.Create(
            new StreamServerTransport(Stream.Null, Stream.Null),
            new McpServerOptions { ServerInfo = new() { Name = "argument-validation-test", Version = "1.0.0" } },
            serviceProvider: provider);
        var parameters = new CallToolRequestParams
        {
            Name = "ui_click",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["windowHandle"] = JsonSerializer.SerializeToElement(window),
                ["elementId"] = JsonSerializer.SerializeToElement(id),
            },
        };
        var request = new RequestContext<CallToolRequestParams>(
            server, new JsonRpcRequest { Id = new RequestId(1), Method = "tools/call" }, parameters);

        var before = form.SubmitClickCount;
        var valid = await tool.InvokeAsync(request, CancellationToken.None);
        Assert.False(valid.IsError);
        Assert.Equal(before + 1, form.SubmitClickCount);

        parameters.Arguments[argument] = JsonSerializer.SerializeToElement(value);
        var rejected = await tool.InvokeAsync(request, CancellationToken.None);
        Assert.True(rejected.IsError);
        Assert.Contains(argument, Assert.IsType<TextContentBlock>(Assert.Single(rejected.Content)).Text);
        Assert.Equal(before + 1, form.SubmitClickCount);
    }
}
