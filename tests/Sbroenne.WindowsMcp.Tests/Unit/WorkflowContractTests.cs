using System.Text.RegularExpressions;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed partial class WorkflowContractTests
{
    [Fact]
    public void LlmTestsWorkflow_IsManualDispatchOnly()
    {
        var workflow = ReadWorkflow("llm-tests.yml");
        var triggerSection = TriggerSectionRegex().Match(workflow).Groups["section"].Value;
        var triggers = TopLevelTriggerRegex()
            .Matches(triggerSection)
            .Select(match => match.Groups["trigger"].Value)
            .ToArray();

        Assert.Equal(["workflow_dispatch"], triggers);
    }

    [Fact]
    public void ReleaseWorkflow_DoesNotReferenceLlmTests()
    {
        var workflow = ReadWorkflow("release-unified.yml");
        var normalizedWorkflow = workflow.ReplaceLineEndings("\n");

        Assert.DoesNotContain("llm-tests", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LLM Integration Tests", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COPILOT_GITHUB_TOKEN", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "  build-standalone:\n    name: Build Standalone MCP Server\n    needs: [version]\n",
            normalizedWorkflow,
            StringComparison.Ordinal);
        Assert.Contains(
            "  build-vscode-extension:\n    name: Build VS Code Extension\n    needs: [version]\n",
            normalizedWorkflow,
            StringComparison.Ordinal);
    }

    private static string ReadWorkflow(string fileName)
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", fileName));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sbroenne.WindowsMcp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    [GeneratedRegex(@"(?m)^  (?<trigger>[a-z_]+):(?:\s|$)")]
    private static partial Regex TopLevelTriggerRegex();

    [GeneratedRegex(@"(?ms)^on:\s*\r?\n(?<section>.*?)(?=^[a-z_]+:)")]
    private static partial Regex TriggerSectionRegex();
}
