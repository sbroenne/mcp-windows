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
