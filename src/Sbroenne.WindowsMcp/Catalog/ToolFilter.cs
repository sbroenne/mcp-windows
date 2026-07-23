using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Catalog;

/// <summary>
/// Applies an allow/deny filter to the MCP tools registered in a service collection.
/// </summary>
/// <remarks>
/// The MCP SDK's <c>WithToolsFromAssembly()</c> registers every discovered tool as an
/// <see cref="McpServerTool"/> singleton. This helper removes the registrations that a deployment
/// does not want to expose, so an operator can run a least-privilege server (e.g. read-only tools
/// only, or everything except <c>process</c>) without recompiling. Filtering happens before the
/// host is built, so excluded tools never appear in <c>tools/list</c> and cannot be invoked.
/// </remarks>
public static class ToolFilter
{
    /// <summary>
    /// Removes tool registrations that fall outside the include list or inside the exclude list.
    /// </summary>
    /// <param name="services">The service collection already populated with tools.</param>
    /// <param name="include">
    /// If non-empty, only tools whose name is in this set are kept (an allowlist). Empty/null keeps all.
    /// </param>
    /// <param name="exclude">Tools whose name is in this set are always removed (a denylist).</param>
    /// <returns>A summary of which tools were kept and removed, and which requested names were unknown.</returns>
    public static ToolFilterResult Apply(
        IServiceCollection services,
        IEnumerable<string>? include,
        IEnumerable<string>? exclude)
    {
        ArgumentNullException.ThrowIfNull(services);

        var includeSet = Normalize(include);
        var excludeSet = Normalize(exclude);

        var toolDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(McpServerTool))
            .ToList();

        // The SDK registers tools via factories, so a descriptor does not expose its name directly.
        // Resolve the concrete tool instances from a throwaway provider (a snapshot - it does not
        // affect the live collection), decide which to keep by name, then re-register the survivors.
        // Our tools are static-method tools, so the instances carry no provider dependency and are
        // safe to reuse after the throwaway provider is disposed.
        List<McpServerTool> tools;
        using (var provider = services.BuildServiceProvider())
        {
            tools = provider.GetServices<McpServerTool>().ToList();
        }

        var allNames = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<string>();
        var removed = new List<string>();
        var survivors = new List<McpServerTool>();

        foreach (var tool in tools)
        {
            var name = tool.ProtocolTool.Name;
            var normalized = NormalizeName(name);
            allNames.Add(normalized);

            var allowed =
                (includeSet.Count == 0 || includeSet.Contains(normalized)) &&
                !excludeSet.Contains(normalized);

            if (allowed)
            {
                kept.Add(name);
                survivors.Add(tool);
            }
            else
            {
                removed.Add(name);
            }
        }

        // Only rewrite the registrations when the filter actually changed something.
        if (removed.Count > 0)
        {
            foreach (var descriptor in toolDescriptors)
            {
                services.Remove(descriptor);
            }

            foreach (var tool in survivors)
            {
                services.AddSingleton(tool);
            }
        }

        var unknown = includeSet.Concat(excludeSet)
            .Where(requested => !allNames.Contains(requested))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        kept.Sort(StringComparer.Ordinal);
        removed.Sort(StringComparer.Ordinal);

        return new ToolFilterResult(kept, removed, unknown);
    }

    private static HashSet<string> Normalize(IEnumerable<string>? names)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (names is null)
        {
            return set;
        }

        foreach (var raw in names)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            set.Add(NormalizeName(raw));
        }

        return set;
    }

    // Tool names are lower_snake_case; accept hyphens and casing differences from the CLI/env.
    private static string NormalizeName(string name) =>
        name.Trim().Replace('-', '_').ToLowerInvariant();
}

/// <summary>The outcome of a <see cref="ToolFilter.Apply"/> call.</summary>
/// <param name="Kept">Names of the tools that remain registered, ordered.</param>
/// <param name="Removed">Names of the tools that were removed, ordered.</param>
/// <param name="Unknown">Requested include/exclude names that matched no tool, ordered.</param>
public sealed record ToolFilterResult(
    IReadOnlyList<string> Kept,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Unknown);
