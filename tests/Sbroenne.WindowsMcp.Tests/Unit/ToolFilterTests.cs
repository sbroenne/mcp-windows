using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Catalog;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ToolFilter"/>. These register the real tool surface via the MCP SDK's
/// assembly scan (the same call the server uses), apply a filter, then resolve the surviving
/// <see cref="McpServerTool"/> singletons - so they prove filtering works against the actual SDK
/// registration shape, not a mock. They run without a desktop.
/// </summary>
public sealed class ToolFilterTests
{
    private static ServiceCollection BuildToolServices()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(ToolCatalog).Assembly);
        return services;
    }

    private static HashSet<string> ResolveToolNames(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetServices<McpServerTool>()
            .Select(tool => tool.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void Apply_WithNoLists_KeepsEveryTool()
    {
        var services = BuildToolServices();
        var before = ResolveToolNames(services).Count;

        var result = ToolFilter.Apply(services, include: null, exclude: null);

        Assert.Empty(result.Removed);
        Assert.Equal(before, ResolveToolNames(services).Count);
    }

    [Fact]
    public void Apply_Allowlist_KeepsOnlyRequestedTools()
    {
        var services = BuildToolServices();

        var result = ToolFilter.Apply(services, include: ["ui_click", "process"], exclude: null);

        var remaining = ResolveToolNames(services);
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { "ui_click", "process" }, remaining);
        Assert.Contains("clipboard", result.Removed);
        Assert.DoesNotContain("ui_click", result.Removed);
    }

    [Fact]
    public void Apply_Denylist_RemovesOnlyExcludedTools()
    {
        var services = BuildToolServices();
        var before = ResolveToolNames(services);

        ToolFilter.Apply(services, include: null, exclude: ["process"]);

        var remaining = ResolveToolNames(services);
        Assert.DoesNotContain("process", remaining);
        Assert.Equal(before.Count - 1, remaining.Count);
    }

    [Fact]
    public void Apply_ExcludeWinsOverInclude()
    {
        var services = BuildToolServices();

        ToolFilter.Apply(services, include: ["ui_click", "process"], exclude: ["process"]);

        var remaining = ResolveToolNames(services);
        Assert.Contains("ui_click", remaining);
        Assert.DoesNotContain("process", remaining);
    }

    [Fact]
    public void Apply_NormalizesHyphensAndCasing()
    {
        var services = BuildToolServices();

        ToolFilter.Apply(services, include: ["UI-Click"], exclude: null);

        var remaining = ResolveToolNames(services);
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { "ui_click" }, remaining);
    }

    [Fact]
    public void Apply_ReportsUnknownRequestedNames()
    {
        var services = BuildToolServices();

        var result = ToolFilter.Apply(services, include: ["ui_click", "does_not_exist"], exclude: null);

        Assert.Contains("does_not_exist", result.Unknown);
        Assert.DoesNotContain("ui_click", result.Unknown);
    }
}
