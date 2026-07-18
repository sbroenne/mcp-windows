using Sbroenne.WindowsMcp.Catalog;
using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Contract tests that keep the <c>wincli</c> CLI in exact sync with the MCP tool surface. The MCP
/// tools are the single source of truth (<see cref="ToolCatalog"/>); <see cref="CliCommandCatalog"/>
/// records which CLI command drives each one. If the two drift - a new MCP tool with no CLI command,
/// or a stale CLI mapping - the build fails here rather than silently shipping an incomplete CLI.
/// </summary>
public sealed class CliToolCoverageTests
{
    [Fact]
    public void EveryMcpTool_HasAWinCliCommand()
    {
        var toolNames = ToolCatalog.GetTools().Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        var missing = toolNames
            .Where(name => !CliCommandCatalog.ToolToCommand.ContainsKey(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"MCP tools without a wincli command mapping: {string.Join(", ", missing)}. " +
            "Add a command in CommandDispatcher and register it in CliCommandCatalog.");
    }

    [Fact]
    public void EveryWinCliMapping_PointsAtARealMcpTool()
    {
        var toolNames = ToolCatalog.GetTools().Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        var stale = CliCommandCatalog.ToolToCommand.Keys
            .Where(name => !toolNames.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            stale.Length == 0,
            $"CliCommandCatalog references tools that no longer exist: {string.Join(", ", stale)}.");
    }
}
