using System.Text.Json;
using Sbroenne.WindowsMcp.Catalog;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ToolCatalog"/> - the single source of truth that both the MCP server
/// and the <c>wincli</c> CLI derive their tool surface from. These run without a desktop.
/// </summary>
public sealed class ToolCatalogTests
{
    [Fact]
    public void GetTools_ReturnsAllToolsWithMetadata()
    {
        var tools = ToolCatalog.GetTools();

        // Every registered MCP tool must surface with a name, a non-trivial description, and a
        // JSON Schema exposing its parameters - this is what an MCP client sees via tools/list.
        Assert.NotEmpty(tools);
        Assert.All(tools, tool =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Name), "tool name must be present");
            Assert.False(string.IsNullOrWhiteSpace(tool.Description), $"{tool.Name} must have a description");
            Assert.Equal(JsonValueKind.Object, tool.InputSchema.ValueKind);
            Assert.True(
                tool.InputSchema.TryGetProperty("properties", out var properties),
                $"{tool.Name} schema must expose properties");
            Assert.Equal(JsonValueKind.Object, properties.ValueKind);
        });
    }

    [Fact]
    public void GetTools_IncludesKnownToolsWithExpectedParameters()
    {
        var tools = ToolCatalog.GetTools().ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        // Spot-check a representative slice of the surface, including the tools shipped in this PR.
        Assert.Contains("clipboard", tools.Keys);
        Assert.Contains("file_open", tools.Keys);
        Assert.Contains("ui_macro", tools.Keys);
        Assert.Contains("ui_batch", tools.Keys);
        Assert.Contains("window_management", tools.Keys);

        Assert.Contains("filePath", ParameterNames(tools["file_open"]));
        Assert.Contains("windowHandle", ParameterNames(tools["file_open"]));
        Assert.Contains("action", ParameterNames(tools["clipboard"]));
        Assert.Contains("steps", ParameterNames(tools["ui_macro"]));
    }

    [Fact]
    public void ToJson_ProducesToolsListShapedManifest()
    {
        var json = ToolCatalog.ToJson();

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("tools", out var toolsElement));
        Assert.Equal(JsonValueKind.Array, toolsElement.ValueKind);

        var catalogCount = ToolCatalog.GetTools().Count;
        Assert.Equal(catalogCount, toolsElement.GetArrayLength());

        foreach (var tool in toolsElement.EnumerateArray())
        {
            Assert.True(tool.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String);
            Assert.True(tool.TryGetProperty("description", out _));
            Assert.True(tool.TryGetProperty("inputSchema", out var schema) && schema.ValueKind == JsonValueKind.Object);
        }
    }

    private static IEnumerable<string> ParameterNames(ToolCatalogEntry entry) =>
        entry.InputSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name);
}
