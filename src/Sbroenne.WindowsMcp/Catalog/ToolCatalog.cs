using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Catalog;

/// <summary>
/// A single, authoritative view of the tools this package exposes.
/// </summary>
/// <remarks>
/// The catalog is built from the exact same SDK registrations the MCP server uses
/// (<c>AddMcpServer().WithToolsFromAssembly()</c>), so every tool name, description, and JSON input
/// schema is identical to what an MCP client sees via <c>tools/list</c>. Both the MCP server and the
/// <c>wincli</c> CLI derive their surface from this one definition - there is no second, hand-written
/// tool list to keep in sync.
/// </remarks>
public static class ToolCatalog
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true,
        // Keep emoji/`<>` in descriptions readable instead of \uXXXX-escaped.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions CompactJson = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Enumerates every registered MCP tool with its canonical protocol metadata, ordered by name.
    /// </summary>
    /// <returns>The tool entries, each carrying the same name/title/description/input schema the server advertises.</returns>
    public static IReadOnlyList<ToolCatalogEntry> GetTools()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(ToolCatalog).Assembly);
        using var provider = services.BuildServiceProvider();

        return provider.GetServices<McpServerTool>()
            .Select(tool => tool.ProtocolTool)
            // Clone() detaches the schema from the provider we are about to dispose.
            .Select(tool => new ToolCatalogEntry(tool.Name, tool.Title, tool.Description, tool.InputSchema.Clone()))
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Serializes the catalog as a machine-readable manifest shaped like an MCP <c>tools/list</c>
    /// result (<c>{ "tools": [ { "name", "title", "description", "inputSchema" } ] }</c>).
    /// </summary>
    /// <param name="indented">Whether to pretty-print the JSON. Defaults to <see langword="true"/>.</param>
    /// <returns>The JSON manifest text.</returns>
    public static string ToJson(bool indented = true)
    {
        var manifest = new
        {
            tools = GetTools().Select(entry => new
            {
                name = entry.Name,
                title = entry.Title,
                description = entry.Description,
                inputSchema = entry.InputSchema,
            }),
        };

        return JsonSerializer.Serialize(manifest, indented ? IndentedJson : CompactJson);
    }
}

/// <summary>A single tool's canonical metadata, sourced from the SDK tool registration.</summary>
/// <param name="Name">The MCP tool name (e.g. <c>ui_click</c>).</param>
/// <param name="Title">The human-friendly tool title, if any.</param>
/// <param name="Description">The full tool description advertised to clients.</param>
/// <param name="InputSchema">The JSON Schema describing the tool's parameters.</param>
public sealed record ToolCatalogEntry(string Name, string? Title, string? Description, JsonElement InputSchema);
