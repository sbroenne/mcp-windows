using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Catalog;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class ToolPermissionTests
{
    public static TheoryData<string, string> Dependencies => new()
    {
        { """{"action":"find","name":"Submit"}""", "ui_find" },
        { """{"action":"click","elementId":"observed"}""", "ui_click" },
        { """{"action":"type","elementId":"observed","text":"test"}""", "ui_type" },
        { """{"action":"select","elementId":"observed","value":"Choice"}""", "ui_select" },
        { """{"action":"wait","name":"Submit"}""", "ui_wait" },
        { """{"action":"read"}""", "ui_read" },
        { """{"action":"snapshot"}""", "ui_snapshot" },
        { """{"action":"key","key":"enter"}""", "keyboard_control" },
        { """{"action":"mouse","mouseAction":"get_position"}""", "mouse_control" },
        { """{"action":"polyline","points":[[1,2],[3,4]]}""", "mouse_control" },
    };

    [Theory]
    [MemberData(nameof(Dependencies))]
    public async Task DisabledDependency_RejectsWholeBatchBeforeDispatch(string step, string dependency)
    {
        foreach (var stopOnError in new[] { true, false })
        {
            var (services, probes) = CreateServices();
            ToolFilter.Apply(services, null, [dependency]);
            var arguments = BatchArguments($$"""[{"action":"key","key":"tab"},{{step}}]""", stopOnError);

            var result = await InvokeAsync(services, "ui_batch", arguments);

            Assert.True(result.IsError);
            Assert.Contains(dependency, Text(result), StringComparison.Ordinal);
            Assert.Equal(0, probes["ui_batch"].Calls);
        }
    }

    [Theory]
    [MemberData(nameof(Dependencies))]
    public async Task Allowlist_RequiresExplicitDependency(string step, string dependency)
    {
        var (denied, deniedProbes) = CreateServices();
        ToolFilter.Apply(denied, ["ui_batch"], null);
        var result = await InvokeAsync(denied, "ui_batch", BatchArguments($"[{step}]"));
        Assert.True(result.IsError);
        Assert.Contains(dependency, Text(result), StringComparison.Ordinal);
        Assert.Equal(0, deniedProbes["ui_batch"].Calls);

        var (allowed, allowedProbes) = CreateServices();
        ToolFilter.Apply(allowed, ["ui_batch", dependency], null);
        Assert.False((await InvokeAsync(allowed, "ui_batch", BatchArguments($"[{step}]"))).IsError);
        Assert.Equal(1, allowedProbes["ui_batch"].Calls);
    }

    [Theory]
    [InlineData("ui_click")]
    [InlineData("ui_type")]
    [InlineData("ui_select")]
    [InlineData("ui_batch")]
    public async Task DisabledSnapshot_RejectsBeforePrimaryAction(string name)
    {
        var (services, probes) = CreateServices();
        ToolFilter.Apply(services, null, ["ui_snapshot"]);
        var arguments = name == "ui_batch" ? BatchArguments("""[{"action":"read"}]""") : new Dictionary<string, JsonElement>();
        arguments["withSnapshot"] = JsonSerializer.SerializeToElement(true);

        Assert.True((await InvokeAsync(services, name, arguments)).IsError);
        Assert.Equal(0, probes[name].Calls);

        arguments["withSnapshot"] = JsonSerializer.SerializeToElement(false);
        Assert.False((await InvokeAsync(services, name, arguments)).IsError);
        arguments.Remove("withSnapshot");
        Assert.False((await InvokeAsync(services, name, arguments)).IsError);
        Assert.Equal(2, probes[name].Calls);
    }

    [Theory]
    [InlineData("""[{"Action":" KEY ","key":"enter"}]""")]
    [InlineData("""[{"action":"read","ACTION":"key","key":"enter"}]""")]
    public async Task BindingVariants_CannotHideDeniedAction(string steps)
    {
        var (services, probes) = CreateServices();
        ToolFilter.Apply(services, ["ui_batch", "keyboard_control"], [" KEYBOARD-Control "]);

        var result = await InvokeAsync(services, "ui_batch", BatchArguments(steps));

        Assert.True(result.IsError);
        Assert.Contains("keyboard_control", Text(result), StringComparison.Ordinal);
        Assert.Equal(0, probes["ui_batch"].Calls);
    }

    [Theory]
    [InlineData("\"true\"")]
    [InlineData("1")]
    [InlineData("null")]
    public async Task SnapshotFlag_RejectsNonBooleanBeforeDispatch(string json)
    {
        var (services, probes) = CreateServices();
        ToolFilter.Apply(services, null, ["ui_snapshot"]);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["withSnapshot"] = JsonSerializer.Deserialize<JsonElement>(json),
        };

        var result = await InvokeAsync(services, "ui_click", arguments);

        Assert.True(result.IsError);
        Assert.Contains("withSnapshot", Text(result), StringComparison.Ordinal);
        Assert.Equal(0, probes["ui_click"].Calls);
    }

    [Fact]
    public async Task LastActionWins_AndIndependentRegistrationsDoNotSharePolicy()
    {
        var (restricted, restrictedProbes) = CreateServices();
        ToolFilter.Apply(restricted, null, ["keyboard_control"]);
        var (unrestricted, unrestrictedProbes) = CreateServices();
        ToolFilter.Apply(unrestricted, null, null);
        var key = BatchArguments("""[{"action":"key","key":"enter"}]""");

        Assert.False((await InvokeAsync(unrestricted, "ui_batch", key)).IsError);
        Assert.True((await InvokeAsync(restricted, "ui_batch", key)).IsError);
        Assert.False((await InvokeAsync(restricted, "ui_batch",
            BatchArguments("""[{"action":"key","ACTION":"read"}]"""))).IsError);
        Assert.Equal(1, restrictedProbes["ui_batch"].Calls);
        Assert.Equal(1, unrestrictedProbes["ui_batch"].Calls);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("""[{"action":"future_action"}]""")]
    [InlineData("""[{"action":"read","unknown":true}]""")]
    [InlineData("""[{"action":"read","includeChildren":"not a boolean"}]""")]
    public async Task InvalidBatch_NeverReachesImplementation(string steps)
    {
        var (services, probes) = CreateServices();
        ToolFilter.Apply(services, null, null);

        Assert.True((await InvokeAsync(services, "ui_batch", BatchArguments(steps))).IsError);
        Assert.Equal(0, probes["ui_batch"].Calls);
    }

    [Fact]
    public async Task AllowedType_DoesNotRequireInternalKeyboardTool()
    {
        var (services, probes) = CreateServices();
        ToolFilter.Apply(services, ["ui_batch", "ui_type"], null);

        var result = await InvokeAsync(services, "ui_batch",
            BatchArguments("""[{"action":"type","elementId":"observed","text":"hello","inputMode":"keyboard"}]"""));

        Assert.False(result.IsError);
        Assert.Equal(1, probes["ui_batch"].Calls);
    }

    private static Dictionary<string, JsonElement> BatchArguments(string steps, bool stopOnError = true) => new()
    {
        ["windowHandle"] = JsonSerializer.SerializeToElement("0"),
        ["steps"] = JsonSerializer.SerializeToElement(steps),
        ["stopOnError"] = JsonSerializer.SerializeToElement(stopOnError),
    };

    private static string Text(CallToolResult result) =>
        Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;

    private static (ServiceCollection Services, Dictionary<string, ProbeTool> Probes) CreateServices()
    {
        var services = new ServiceCollection();
        services.AddMcpServer();
        var probes = ToolCatalog.GetTools().ToDictionary(entry => entry.Name, entry => new ProbeTool(entry), StringComparer.Ordinal);
        foreach (var probe in probes.Values)
        {
            services.AddSingleton<McpServerTool>(probe);
        }
        return (services, probes);
    }

    private static async Task<CallToolResult> InvokeAsync(
        IServiceCollection services, string name, Dictionary<string, JsonElement> arguments)
    {
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(), tool => tool.ProtocolTool.Name == name);
        await using var server = McpServer.Create(
            new StreamServerTransport(Stream.Null, Stream.Null),
            new McpServerOptions { ServerInfo = new() { Name = "permission-test", Version = "1.0.0" } },
            serviceProvider: provider);
        var parameters = new CallToolRequestParams { Name = name, Arguments = arguments };
        return await tool.InvokeAsync(new RequestContext<CallToolRequestParams>(
            server, new JsonRpcRequest { Id = new RequestId(1), Method = "tools/call" }, parameters), CancellationToken.None);
    }

    private sealed class ProbeTool(ToolCatalogEntry entry) : McpServerTool
    {
        public int Calls { get; private set; }
        public override Tool ProtocolTool { get; } = new() { Name = entry.Name, InputSchema = entry.InputSchema };
        public override IReadOnlyList<object> Metadata => [];

        public override ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(new CallToolResult { IsError = false, Content = [] });
        }
    }
}
